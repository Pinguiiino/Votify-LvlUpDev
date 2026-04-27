using Microsoft.EntityFrameworkCore;
using Votify.Domain.AuditFolder;
using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.ProjectFolder;
using Votify.Domain.VoteFolder;
using Votify.Domain.UserFolder;
using Votify.Infrastructure;
using Votify.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<VotifyDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ProjectService>();

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<EventService>();

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<CategoryService>();

builder.Services.AddScoped<IVoteRepository, VoteRepository>();
builder.Services.AddScoped<IVotingSessionRepository, VotingSessionRepository>();
builder.Services.AddScoped<VoteService>();
builder.Services.AddScoped<VotingSessionService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<IAuditRequestRepository, AuditRequestRepository>();
builder.Services.AddScoped<AuditService>();

builder.Services.AddScoped<IWeightedVoteRepository, WeightedVoteRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor",
        policy =>
        {
            policy.WithOrigins("https://localhost:5276")
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
