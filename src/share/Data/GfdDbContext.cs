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
			entity.ToTable("WebsiteRecord");

			entity.HasKey(e => e.Id);

			entity.Property(e => e.Id)
				.HasColumnName("Id")
				.HasColumnType("uuid")
				.IsRequired();

			entity.Property(e => e.Url)
				.HasColumnName("Url")
				.HasColumnType("varchar(4096)");

			entity.Property(e => e.Title)
				.HasColumnName("Title")
				.HasColumnType("varchar(1024)");

			entity.Property(e => e.Description)
				.HasColumnName("Description")
				.HasColumnType("varchar(2048)");

			entity.Property(e => e.TitleMeaning)
				.HasColumnName("TitleMeaning")
				.HasColumnType("vector(768)");

			entity.Property(e => e.DescriptionMeaning)
				.HasColumnName("DescriptionMeaning")
				.HasColumnType("vector(768)");

			entity.Property(e => e.PageMeaning)
				.HasColumnName("PageMeaning")
				.HasColumnType("vector(768)");
		});
	}
}


