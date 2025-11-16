using Microsoft.EntityFrameworkCore;
using Gfd.Models;

namespace Gfd.Data;

public sealed class GfdDbContext : DbContext
{
	public GfdDbContext(DbContextOptions<GfdDbContext> options) : base(options)
	{
	}

	public DbSet<WebsiteRecord> WebsiteRecords { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasPostgresExtension("uuid-ossp");
		modelBuilder.HasPostgresExtension("vector");

		modelBuilder.Entity<WebsiteRecord>(entity =>
		{
			entity.ToTable("website_record");

			entity.HasKey(e => e.Id);

			entity.Property(e => e.Id)
				.HasColumnName("id")
				.HasColumnType("uuid")
				.IsRequired();

			entity.Property(e => e.Url)
				.HasColumnName("url")
				.HasColumnType("varchar(4096)");

			entity.Property(e => e.Title)
				.HasColumnName("title")
				.HasColumnType("varchar(1024)");

			entity.Property(e => e.Description)
				.HasColumnName("description")
				.HasColumnType("varchar(2048)");

			entity.Property(e => e.TitleMeaning)
				.HasColumnName("title_meaning")
				.HasColumnType("vector(768)");

			entity.Property(e => e.DescriptionMeaning)
				.HasColumnName("description_meaning")
				.HasColumnType("vector(768)");

			entity.Property(e => e.PageMeaning)
				.HasColumnName("page_meaning")
				.HasColumnType("vector(768)");
		});
	}
}


