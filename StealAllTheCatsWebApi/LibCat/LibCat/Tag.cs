using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCat {
	public class Tag {
		public int Id { get; set; }
		public string? Name { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;

		public List<CatTag> CatTags { get; set; } = new List<CatTag>();
	}
}
