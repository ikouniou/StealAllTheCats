namespace LibCat {
	public class Cat {
		public int Id { get; set; }
		public string? CatId { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public string? Image { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;
		public List<CatTag> CatTags { get; set; } = new List<CatTag>();
	}
}
