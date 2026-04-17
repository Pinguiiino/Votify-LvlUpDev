using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;


namespace Votify.Infrastructure;

// Solo lo usa `dotnet ef` en tiempo de diseño (migraciones).
// En runtime se sigue usando la configuración de Program.cs de la API.
public class VotifyDbContextFactory : IDesignTimeDbContextFactory<VotifyDbContext>
{
    public VotifyDbContext CreateDbContext(string[] args)
    {
        // Ruta al proyecto Votify.Api para leer su appsettings.json
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