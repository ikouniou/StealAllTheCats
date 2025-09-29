using LibCat;
using Microsoft.EntityFrameworkCore;
using ORM;
using StealAllTheCatsWebApi.DTOs;
using System;

namespace StealAllTheCatsWebApi.Services {
	public interface ICatSyncService {
		Task<IEnumerable<CatDTO>> SyncCatsAsync(CancellationToken ct);
	}

	public class CatSyncService : ICatSyncService {
		private readonly HttpClient _http;
		private readonly StealTheCatsContext _db;
		private readonly ILogger<CatSyncService> _logger;

		private record CatApiBreeds(string? Temperament);
		private record CatApiImage(string Id, int Width, int Height, string Url, List<CatApiBreeds> Breeds);

		public CatSyncService(IHttpClientFactory httpFactory, StealTheCatsContext db, ILogger<CatSyncService> logger) {
			_http = httpFactory.CreateClient("CatApi");
			_db = db;
			_logger = logger;
		}

		public async Task<IEnumerable<CatDTO>> SyncCatsAsync(CancellationToken ct) {
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
				_logger.LogError(ex, "An error occured while saving to database.");
				throw;
			}

			return addedCats.Select(cat => new CatDTO {
				Id = cat.Id,
				CatId = cat.CatId,
				Width = cat.Width,
				Height = cat.Height,
				Image = cat.Image,
				Tags = cat.CatTags.Select(ct => ct.Tag?.Name!).ToArray(),
				Created = cat.Created
			});
		}
	}
}
