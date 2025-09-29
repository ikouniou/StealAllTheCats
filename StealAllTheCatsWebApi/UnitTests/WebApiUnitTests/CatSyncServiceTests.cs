using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ORM;
using StealAllTheCatsWebApi.Services;
using System.Net;

namespace WebApiUnitTests {
	public class CatSyncServiceTests {

		[Fact]
		public async Task SyncCatsAsync_WhenApiReturnsOneImage_SavesCatAndTags() {
			// fake API returns 1 image with 2 temperaments
			var json = """
			[
			  { "id":"cat1","width":200,"height":100,"url":"http://catimg/cat1.jpg",
				"breeds":[{"temperament":"Playful, Affectionate"}] }
			]
			""";

			var fakeHttp = new HttpClient(new FakeHttpHandler(json)) {
				BaseAddress = new Uri("https://fake-Catapi/")
			};

			var options = new DbContextOptionsBuilder<StealTheCatsContext>()
				.UseInMemoryDatabase($"CatsTestDb_{Guid.NewGuid()}")
				.Options;

			using var db = new StealTheCatsContext(options);
			var service = new CatSyncService(
				new SimpleHttpFactory(fakeHttp),
				db,
				NullLogger<CatSyncService>.Instance
			);

			var result = (await service.SyncCatsAsync(CancellationToken.None)).ToList();

			Assert.Single(result);
			Assert.Equal("cat1", result[0].CatId);
			Assert.Equal(2, result[0].Tags.Length); // Playful + Affectionate

			Assert.Equal(1, await db.Cats.CountAsync());
			Assert.Equal(2, await db.Tags.CountAsync());

			var saved = await db.Cats
				.Include(cat => cat.CatTags).ThenInclude(ct => ct.Tag)
				.SingleAsync();

			var savedTagNames = saved.CatTags.Select(ct => ct.Tag!.Name).OrderBy(name => name).ToArray();
			Assert.Equal(new[] { "Affectionate", "Playful" }, savedTagNames);
		}

		[Fact]
		public async Task SyncCatsAsync_WhenApiReturnsEmpty_ShouldReturnEmptyList() {
			var fakeHttp = new HttpClient(new FakeHttpHandler("[]")) {
				BaseAddress = new Uri("https://fake-Catapi/")
			};

			var options = new DbContextOptionsBuilder<StealTheCatsContext>()
				.UseInMemoryDatabase($"CatsTestDb_{Guid.NewGuid()}")
				.Options;
			using var db = new StealTheCatsContext(options);

			var service = new CatSyncService(
				new SimpleHttpFactory(fakeHttp),
				db,
				NullLogger<CatSyncService>.Instance);

			var result = await service.SyncCatsAsync(CancellationToken.None);

			Assert.Empty(result);
			Assert.Equal(0, await db.Cats.CountAsync());
			Assert.Equal(0, await db.Tags.CountAsync());
		}
	}

	public class SimpleHttpFactory : IHttpClientFactory {
		private readonly HttpClient _client;
		public SimpleHttpFactory(HttpClient client) {
			_client = client;
		}

		public HttpClient CreateClient(string name) {
			return _client;
		}
	}

	public class FakeHttpHandler : HttpMessageHandler {
		private readonly string _response;
		public FakeHttpHandler(string response) { 
			_response = response;
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, CancellationToken cancellationToken) {
			var msg = new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StringContent(_response)
			};
			return Task.FromResult(msg);
		}
	}
}