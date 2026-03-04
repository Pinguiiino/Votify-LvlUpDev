using Microsoft.EntityFrameworkCore;
using Votify.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<VotifyDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor",
        policy =>
        {
            policy.WithOrigins("https://localhost:5276") // puerto de tu Blazor
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

app.UseCors("AllowBlazor");

app.MapGet("/", () => "API funcionando");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<VotifyDbContext>();

        await Votify.Infrastructure.Data.DbSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al poblar la base de datos.");
    }
}

app.Run();
