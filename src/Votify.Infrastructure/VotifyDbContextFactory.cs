using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;


namespace Votify.Infrastructure;

public class VotifyDbContextFactory : IDesignTimeDbContextFactory<VotifyDbContext>
{
    public VotifyDbContext CreateDbContext(string[] args)
    {
        var apiPath = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Votify.Api"));

        var config = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<VotifyDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new VotifyDbContext(optionsBuilder.Options);
    }
}