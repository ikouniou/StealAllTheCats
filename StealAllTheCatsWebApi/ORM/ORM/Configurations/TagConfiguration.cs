using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StealAllTheCatsWebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Configurations {
	public class TagConfiguration : IEntityTypeConfiguration<Tag> {
		public void Configure(EntityTypeBuilder<Tag> builder) {

			builder.ToTable("Tags");
			builder.HasKey(tag => tag.Id);
			builder.Property(tag => tag.Name).HasMaxLength(200).IsRequired(true);
			builder.HasIndex(tag => tag.Name).IsUnique();
		}
	}
}
