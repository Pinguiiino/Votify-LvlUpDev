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

app.Run();
