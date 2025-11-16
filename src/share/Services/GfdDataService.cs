using System.Data.Common;
using Gfd.Data;
using Gfd.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Gfd.Services;

public interface IGfdDataService
{
	Task<WebsiteRecord?> GetWebsiteRecordAsync(Guid id, CancellationToken cancellationToken = default);
	Task UpsertWebsiteRecordAsync(WebsiteRecord record, CancellationToken cancellationToken = default);
}

public sealed class GfdDataService : IGfdDataService
{
	private readonly PooledDbContextFactory<GfdDbContext> _factory;

	public GfdDataService(string connectionString, int poolSize = 16)
	{
		if (string.IsNullOrWhiteSpace(connectionString))
			throw new ArgumentException("Connection string must be provided", nameof(connectionString));
		if (poolSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(poolSize), "Pool size must be positive");

		var options = new DbContextOptionsBuilder<GfdDbContext>()
			.UseNpgsql(connectionString, npgsql =>
			{
				npgsql.EnableRetryOnFailure();
			})
			.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
			.Options;

		_factory = new PooledDbContextFactory<GfdDbContext>(options, poolSize);
	}

	private Task<GfdDbContext> CreateContextAsync(CancellationToken cancellationToken = default)
		=> _factory.CreateDbContextAsync(cancellationToken);

	public async Task<WebsiteRecord?> GetWebsiteRecordAsync(Guid id, CancellationToken cancellationToken = default)
	{
		await using var db = await CreateContextAsync(cancellationToken);
		return await db.WebsiteRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
	}

	public async Task UpsertWebsiteRecordAsync(WebsiteRecord record, CancellationToken cancellationToken = default)
	{
		await using var db = await CreateContextAsync(cancellationToken);

		var existing = await db.WebsiteRecords
			.AsTracking()
			.FirstOrDefaultAsync(x => x.Id == record.Id, cancellationToken);

		if (existing is null)
		{
			db.Add(record);
		}
		else
		{
			db.Entry(existing).CurrentValues.SetValues(record);
		}

		await db.SaveChangesAsync(cancellationToken);
	}

}


