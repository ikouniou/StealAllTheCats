using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCat {
	public class CatTag {

		public int CatId { get; set; }
		public Cat? Cat { get; set; }
		public int TagId { get; set; }
		public Tag? Tag { get; set; }
	}
}
