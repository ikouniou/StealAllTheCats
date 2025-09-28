using LibCat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ORM;

namespace StealAllTheCatsWebApi.Controllers {

	[ApiController]
	[Route("[controller]")]
	public class CatController : ControllerBase {

		private readonly StealTheCatsContext _db;
		private readonly HttpClient _http;
		private readonly ILogger<CatController> _logger;

		public CatController(StealTheCatsContext db, IHttpClientFactory httpFactory, ILogger<CatController> logger) {
			_db = db;
			_http = httpFactory.CreateClient("CatApi");
			_logger = logger;
		}

		private record CatApiBreeds(string? Temperament);
		private record CatApiImage(string Id, int Width, int Height, string Url, List<CatApiBreeds> Breeds);

		// POST /api/cats/fetch
		[HttpPost("api/fetch")]
		public async Task<IActionResult> Fetch(CancellationToken ct = default) {
			var images = await _http.GetFromJsonAsync<List<CatApiImage>>("v1/images/search?limit=25&has_breeds=1&order=Rand", cancellationToken: ct)
						 ?? new List<CatApiImage>();


			var allTagNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var imageTags = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

			foreach (var img in images) {
				if (string.IsNullOrWhiteSpace(img.Id)) 
					continue;

				var names = (img.Breeds ?? new())
					.Where(b => !string.IsNullOrWhiteSpace(b.Temperament))
					.SelectMany(b => b!.Temperament!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					.Select(t => t.Trim())
					.Where(t => t.Length > 0)
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();

				imageTags[img.Id] = names;
				foreach (var n in names) 
					allTagNames.Add(n);
			}

			var existingTags = await _db.Tags
				.Where(t => allTagNames.Contains(t.Name!))
				.ToDictionaryAsync(t => t.Name!, StringComparer.OrdinalIgnoreCase, ct);

			var createdTags = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);
			foreach (var name in allTagNames) {
				if (existingTags.ContainsKey(name)) 
					continue;

				var tag = new Tag { Name = name };
				_db.Tags.Add(tag);
				createdTags[name] = tag;
			}

			var addedCats = new List<Cat>();

			foreach (var img in images) {
				if (string.IsNullOrWhiteSpace(img.Id)) 
					continue;

				if (await _db.Cats.AnyAsync(c => c.CatId == img.Id, ct)) 
					continue;

				var cat = new Cat {
					CatId = img.Id,
					Width = img.Width,
					Height = img.Height,
					Image = img.Url
				};

				foreach (var tagName in imageTags[img.Id]) {
					if (!existingTags.TryGetValue(tagName, out var tag))
						tag = createdTags[tagName];

					cat.CatTags.Add(new CatTag { Cat = cat, Tag = tag });
				}

				_db.Cats.Add(cat);
				addedCats.Add(cat);
			}

			await _db.SaveChangesAsync(ct);

			var result = addedCats.Select(c => new {
				c.Id,
				c.CatId,
				c.Width,
				c.Height,
				c.Image,
				Tags = c.CatTags.Select(t => t.Tag).ToList().Select(tag => tag?.Name).ToArray(),
				c.Created
			});

			return Ok(result);
		}

		// GET DB/{id} from DB
		[HttpGet("DB/{id:int}")]
		public async Task<IActionResult> GetById(int id, CancellationToken ct = default) {
			var result = await _db.Cats
				.Where(cat => cat.Id == id)
				.Select(cat => new {
					cat.Id,
					cat.CatId,
					cat.Width,
				    cat.Height,
					cat.Image,
					Tags = cat.CatTags.Select(ct => ct.Tag!.Name).ToArray(),
					cat.Created
				})
				.AsNoTracking()
				.FirstOrDefaultAsync(ct);

			return result is null ? NotFound() : Ok(result);
		}

		// GET /api/cats/{id}
		[HttpGet("api/{id}")]
		public async Task<IActionResult> GetExternalById(string id, CancellationToken ct = default) {
			if (string.IsNullOrWhiteSpace(id)) 
				return BadRequest("CatID is required.");

			var cat = await _http.GetFromJsonAsync<CatApiImage>($"v1/images/{id}", cancellationToken: ct);
			if (cat is null) 
				return NotFound();

			var tags = (cat.Breeds ?? new())
				.Where(b => !string.IsNullOrWhiteSpace(b.Temperament))
				.SelectMany(b => b!.Temperament!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();

			var result = new {
				cat.Id,
				cat.Width,
				cat.Height,
				cat.Url,
				Tags = tags,
			};

			return Ok(result);
		}
	}
}
