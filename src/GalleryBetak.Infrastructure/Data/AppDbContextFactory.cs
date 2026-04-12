using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GalleryBetak.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core CLI commands (migrations/update).
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Used by EF tools at design time only; runtime uses DI configuration.
        var connectionString =
            Environment.GetEnvironmentVariable("GALLERYBETAK_DB_CONNECTION")
            ?? "Server=(localdb)\\mssqllocaldb;Database=GalleryBetakDb;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true";

        optionsBuilder.UseSqlServer(connectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}
