namespace StealAllTheCatsWebApi.DTOs {
	public class CatDTO {
		public int Id { get; set; }
		public string? CatId { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public string? Image { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;
		public string[] Tags { get; set; } = Array.Empty<string>();
	}
}
