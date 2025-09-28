using LibCat;
using Microsoft.EntityFrameworkCore;
using ORM.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM {
	public class StealTheCatsContext : DbContext {

		public DbSet<Cat> Cats { get; set; }
		public DbSet<Tag> Tags { get; set; }
		public DbSet<CatTag> CatTags { get; set; }

		public StealTheCatsContext(DbContextOptions<StealTheCatsContext> options)
			: base(options) { 

		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			modelBuilder.ApplyConfiguration(new CatConfiguration());
			modelBuilder.ApplyConfiguration(new TagConfiguration());
			modelBuilder.ApplyConfiguration(new CatTagConfiguration());

			base.OnModelCreating(modelBuilder);
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
			optionsBuilder.UseSqlServer("Data Source=localhost\\SQLEXPRESS;Initial Catalog=CatsDb;Integrated Security=True;" +
				"Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;");

			base.OnConfiguring(optionsBuilder);
		}
	}
}
