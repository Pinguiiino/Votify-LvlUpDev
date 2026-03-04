using Microsoft.EntityFrameworkCore;
using Votify.Domain;
using Votify.Domain.UserFolder;
using Votify.Domain.VoteFoler;

namespace Votify.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(VotifyDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Usuarios.AnyAsync() || await context.Votes.AnyAsync())
        {
            return;
        }
        var jurado1 = new Jury("Ada Lovelace", "ada@test.com", "Contraseña");
        var publico1 = new Public("Alan Turing", "alan@test.com", "Contraseña");

        context.Usuarios.AddRange(jurado1, publico1);
        await context.SaveChangesAsync();

        var idProyectoFicticio = Guid.NewGuid().ToString();

        var votoExperto = new ExpertVote(
            idProyectoFicticio,
            jurado1.Id.ToString(),
            5 
        );

        var votoPublico = new PublicVote(
            idProyectoFicticio,
            publico1.Id.ToString()
        );

        context.Votes.AddRange(votoExperto, votoPublico);
        await context.SaveChangesAsync();
    }
}
