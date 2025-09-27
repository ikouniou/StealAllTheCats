namespace StealAllTheCatsWebApi {
	public class CatTag {

		public int CatId { get; set; }
		public Cat? Cat { get; set; }
		public int TagId { get; set; }
		public Tag? Tag { get; set; }
	}
}
