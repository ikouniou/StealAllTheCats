namespace StealAllTheCatsWebApi {
	public class Tag {
		public int Id { get; set; }
		public string? Name { get; set; }
		public DateTime Created {  get; set; } = DateTime.Now;

		public List<CatTag> CatTags { get; set; } = new List<CatTag>();
	}
}
