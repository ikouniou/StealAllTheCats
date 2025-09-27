using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StealAllTheCatsWebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Configurations {
	public class CatTagConfiguration : IEntityTypeConfiguration<CatTag> {

		public void Configure(EntityTypeBuilder<CatTag> builder) {

			builder.ToTable("CatTags");
			builder.HasKey(catTag => new { catTag.CatId, catTag.TagId });

			//RELATIONS
			builder.HasOne(catTag => catTag.Cat)
				.WithMany(cat => cat.CatTags)
				.HasForeignKey(catTag => catTag.CatId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasOne(catTag => catTag.Tag)
				.WithMany(tag => tag.CatTags)
				.HasForeignKey(catTag => catTag.TagId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
