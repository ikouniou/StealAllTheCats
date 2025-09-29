using LibCat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ORM;
using StealAllTheCatsWebApi.DTOs;

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

			try {
				await _db.SaveChangesAsync(ct);
			} catch (DbUpdateException ex) {
				_logger.LogError(ex, "Error while saving to database.");
				return Conflict("An error occured while updating database.");
			}

			var result = addedCats.Select(cat => new CatDTO{
				Id = cat.Id,
				CatId =	cat.CatId,
				Width = cat.Width,
				Height = cat.Height,
				Image = cat.Image,
				Tags = cat.CatTags.Select(ct => ct.Tag?.Name!).ToArray(),
				Created = cat.Created
			});

			return Ok(result);
		}

		// GET api/{id}
		[HttpGet("api/{id:int}")]
		public async Task<IActionResult> GetById(int id, CancellationToken ct = default) {
			var result = await _db.Cats
				.Where(cat => cat.Id == id)
				.Select(cat => new CatDTO{
					Id = cat.Id,
					CatId = cat.CatId,
					Width = cat.Width,
				    Height = cat.Height,
					Image = cat.Image,
					Tags = cat.CatTags.Select(ct => ct.Tag!.Name!).ToArray(),
					Created = cat.Created
				})
				.AsNoTracking()
				.FirstOrDefaultAsync(ct);

			return result is null ? NotFound() : Ok(result);
		}

		// GET /externalApi/cats/{id}
		[HttpGet("externalApi/{id}")]
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

			var result = new CatDTO{
				CatId = cat.Id,
				Width = cat.Width,
				Height = cat.Height,
				Image = cat.Url,
				Tags = tags,
			};

			return Ok(result);
		}

		// GET /api/cats?page=1&pageSize=10
		[HttpGet("api/paging")]
		public async Task<IActionResult> Get(
			[FromQuery] int page,
			[FromQuery] int pageSize,
			CancellationToken ct = default) {

			if (page < 1 || pageSize is < 1 or > 100)
				return BadRequest("Invalid paging.");

			var query = _db.Cats.AsNoTracking().Include(cat => cat.CatTags).AsQueryable();
			var total = await query.CountAsync(ct);

			var items = await query
				.OrderBy(cat => cat.Id)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(cat => new CatDTO{
					Id = cat.Id,
					CatId = cat.CatId,
					Width = cat.Width,
					Height = cat.Height,
					Image = cat.Image,
					Tags = cat.CatTags.Select(ct => ct.Tag!.Name!).ToArray(),
					Created = cat.Created
				})
				.ToListAsync(ct);

			return Ok(new { 
				page, 
				pageSize,
				total,
				totalPages = (int)Math.Ceiling(total / (double)pageSize), 
				items 
			});
		}

		// GET /api/cats?tag=playful&page=1&pageSize=10”)
		[HttpGet("api/tag")]
		public async Task<IActionResult> Get(
			[FromQuery] int page,
	        [FromQuery] int pageSize,
	        [FromQuery] string? tag = null,
			CancellationToken ct = default) {

			if (page < 1 || pageSize is < 1 or > 100)
				return BadRequest("Invalid paging.");

			var query = _db.Cats.AsNoTracking().Include(cat => cat.CatTags).AsQueryable();

			if (!string.IsNullOrWhiteSpace(tag)) {
				var t = tag.Trim();
				query = query.Where(cat => cat.CatTags.Any(ct => ct.Tag!.Name == t));
			}

			var total = await query.CountAsync(ct);

			var items = await query
				.OrderBy(cat => cat.Id)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(cat => new CatDTO{
					Id = cat.Id,
					CatId = cat.CatId,
					Width = cat.Width,
					Height = cat.Height,
					Image = cat.Image,
					Tags = cat.CatTags.Select(ct => ct.Tag!.Name!).ToArray(),
					Created = cat.Created
				})
				.ToListAsync(ct);

			return Ok(new {
				page,
				pageSize,
				total,
				totalPages = (int)Math.Ceiling(total / (double)pageSize),
				items
			});
		}
	}
}
