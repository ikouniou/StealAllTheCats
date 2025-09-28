using LibCat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Configurations {
	public class CatConfiguration : IEntityTypeConfiguration<Cat> {
		public void Configure(EntityTypeBuilder<Cat> builder) {

			builder.ToTable("Cats");
			builder.HasKey(cat => cat.Id);
			builder.Property(cat => cat.CatId).HasMaxLength(64).IsRequired(true);
			builder.HasIndex(cat => cat.CatId).IsUnique();
			builder.Property(cat => cat.Image).HasMaxLength(2048).IsRequired(true);
		}
	}
}
