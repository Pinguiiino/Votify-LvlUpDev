using Microsoft.EntityFrameworkCore;
using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.ProjectFolder;
using Votify.Domain.VoteFolder;
using Votify.Infrastructure;
using Votify.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<VotifyDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ProjectService>();

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<CategoryService>();

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<EventService>();

builder.Services.AddScoped<IVoteRepository, VoteRepository>();
builder.Services.AddScoped<IVotingSessionRepository, VotingSessionRepository>();
builder.Services.AddScoped<VoteService>();

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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

var app = builder.Build();

app.UseCors("AllowBlazor");

// Servir archivos estáticos (wwwroot/uploads/...) para que las imágenes
// subidas sean accesibles desde el navegador.
app.UseStaticFiles();

app.MapGet("/", () => "API funcionando");

app.MapControllers();

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
