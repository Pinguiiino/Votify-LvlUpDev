using Microsoft.EntityFrameworkCore;
using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.Factory;
using Votify.Domain.ProjectFolder;
using Votify.Domain.UserFolder;
using Votify.Domain.VoteFolder;

namespace Votify.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(VotifyDbContext context)
    {
        await context.Database.MigrateAsync();

        // Guard: no sembrar si ya hay datos
        if (await context.Usuarios.AnyAsync() || await context.Events.AnyAsync())
            return;

        // ════════════════════════════════════════════════════════════════════
        // 1. USUARIOS
        // ════════════════════════════════════════════════════════════════════

        var org1 = new Organizer("Alan Brito", "alan.brito@votify.com", "Hash$1234");
        var org2 = new Organizer("Laura Pérez", "laura.perez@votify.com", "Hash$5678");

        var jury1 = new Jury("Ada Lovelace", "ada@test.com", "Hash$Ada1");
        var jury2 = new Jury("Grace Hopper", "grace@test.com", "Hash$Gra2");
        var jury3 = new Jury("Linus Torvalds", "linus@test.com", "Hash$Lin3");
        var jury4 = new Jury("Margaret Hamilton", "margaret@test.com", "Hash$Mag4");

        var pub1 = new Public("Alan Turing", "alan.turing@test.com", "Hash$Tur1");
        var pub2 = new Public("John von Neumann", "john@test.com", "Hash$Joh2");
        var pub3 = new Public("Claude Shannon", "claude@test.com", "Hash$Sha3");

        var part1 = new Participant("Aitor Tilla", "aitor@test.com", "Hash$Ait1");
        var part2 = new Participant("Elsa Capunta", "elsa@test.com", "Hash$Els2");
        var part3 = new Participant("Pedro Alves", "pedro@test.com", "Hash$Ped3");
        var part4 = new Participant("Sofía Martín", "sofia@test.com", "Hash$Sof4");
        var part5 = new Participant("Carlos Ruiz", "carlos@test.com", "Hash$Car5");
        var part6 = new Participant("Marta López", "marta@test.com", "Hash$Mar6");

        context.Usuarios.AddRange(
            org1, org2, jury1, jury2, jury3, jury4, pub1, pub2, pub3, part1, part2, part3, part4, part5, part6
        );

        // ════════════════════════════════════════════════════════════════════
        // 2. EVENTOS (Actualizado a ModalityEventCreator)
        // ════════════════════════════════════════════════════════════════════

        // Usamos la nueva fábrica que creaste
        EventCreator modalityCreator = new ModalityEventCreator();

        var hackUPC = modalityCreator.Create(
            name: "HackUPC 2026",
            maxProjects: 30,
            startDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            modality: "Presencial", // <-- NUEVO PARÁMETRO
            description: "El hackathon más grande de la UPC..."
        );

        var fibFair = modalityCreator.Create(
            name: "FIB Innovation Fair 2026",
            maxProjects: 20,
            startDate: new DateTime(2026, 5, 22, 0, 0, 0, DateTimeKind.Utc),
            modality: "Híbrido", // <-- NUEVO PARÁMETRO
            description: "Feria de innovación anual..."
        );

        context.Events.AddRange(hackUPC, fibFair);

        await context.SaveChangesAsync();

        // ════════════════════════════════════════════════════════════════════
        // 3. CATEGORÍAS
        // ════════════════════════════════════════════════════════════════════

        var catIA = new Category(
            eventId: hackUPC.Id,
            name: "Proyectos de IA",
            weightA: 0.65, weightB: 0.35,
            description: "Proyectos que aplican inteligencia artificial o machine learning.",
            prizeDescription: "1.000€ + mentoría de 6 meses en startup de IA"
        );
        var catSocial = new Category(
            eventId: hackUPC.Id,
            name: "Proyectos Sociales",
            weightA: 0.50, weightB: 0.50,
            description: "Proyectos con impacto social directo en la comunidad.",
            prizeDescription: "500€ + acceso a incubadora universitaria"
        );
        var catSostenibilidad = new Category(
            eventId: hackUPC.Id,
            name: "Sostenibilidad",
            weightA: 0.55, weightB: 0.45,
            description: "Proyectos enfocados en medioambiente y economía circular.",
            prizeDescription: "750€ + presentación en COP31"
        );

        var catStartup = new Category(
            eventId: fibFair.Id,
            name: "Startups Tecnológicas",
            weightA: 0.60, weightB: 0.40,
            description: "Proyectos con modelo de negocio escalable y base tecnológica.",
            prizeDescription: "2.000€ + ronda de presentaciones a inversores"
        );
        var catImpacto = new Category(
            eventId: fibFair.Id,
            name: "Impacto Social",
            weightA: 0.45, weightB: 0.55,
            description: "Proyectos que priorizan el bienestar social sobre el beneficio económico.",
            prizeDescription: "1.000€ + colaboración con ONG partner"
        );

        context.Categories.AddRange(catIA, catSocial, catSostenibilidad, catStartup, catImpacto);
        await context.SaveChangesAsync();

        // ════════════════════════════════════════════════════════════════════
        // 4. PROYECTOS
        // ════════════════════════════════════════════════════════════════════

        ProjectCreator aiCreator = new AiProjectCreator();
        ProjectCreator sustCreator = new SustainabilityProjectCreator();

        var p1 = aiCreator.Create("MediScan AI", hackUPC.Id, catIA.Id, 9.0, 7.5, "Detección temprana de enfermedades.");
        var p2 = aiCreator.Create("EduAdapt", hackUPC.Id, catIA.Id, 8.0, 8.5, "Plataforma de aprendizaje adaptativo.");
        var p3 = aiCreator.Create("TrafficFlow", hackUPC.Id, catIA.Id, 7.5, 6.0, "Optimización de semáforos.");
        var p4 = aiCreator.Create("RefugeeConnect", hackUPC.Id, catSocial.Id, 8.5, 9.0, "App de integración laboral.");
        var p5 = aiCreator.Create("CuidaMayor", hackUPC.Id, catSocial.Id, 7.0, 8.0, "Plataforma de teleasistencia.");

        var p6 = sustCreator.Create("PlastiTrack", hackUPC.Id, catSostenibilidad.Id, 9.5, 8.0, "Trazabilidad de plásticos.");
        var p7 = sustCreator.Create("SolarGrid", hackUPC.Id, catSostenibilidad.Id, 8.0, 9.0, "Red P2P de energía solar.");

        var p8 = aiCreator.Create("QuantumSec", fibFair.Id, catStartup.Id, 9.0, 8.0, "Criptografía post-cuántica.");
        var p9 = aiCreator.Create("BioPrint3D", fibFair.Id, catStartup.Id, 8.5, 7.5, "Impresión 3D de tejidos.");
        var p10 = sustCreator.Create("WaterSense", fibFair.Id, catImpacto.Id, 7.5, 9.5, "Sensores IoT de agua.");

        context.Projects.AddRange(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
        await context.SaveChangesAsync();

        // ════════════════════════════════════════════════════════════════════
        // 5. VOTOS
        // ════════════════════════════════════════════════════════════════════

        VoteCreator expertCreator = new ExpertVoteCreator();
        VoteCreator publicCreator = new PublicVoteCreator();

        var votes = new List<Vote>();

        foreach (var proyecto in new[] { p1, p2, p3 })
        {
            votes.Add(expertCreator.Create(proyecto.Id, jury1.Id, RawScore(8.0, 9.5)));
            votes.Add(expertCreator.Create(proyecto.Id, jury2.Id, RawScore(7.0, 9.0)));
        }

        foreach (var proyecto in new[] { p4, p5, p6, p7 })
        {
            votes.Add(expertCreator.Create(proyecto.Id, jury3.Id, RawScore(7.5, 9.5)));
        }

        foreach (var proyecto in new[] { p8, p9, p10 })
        {
            votes.Add(expertCreator.Create(proyecto.Id, jury4.Id, RawScore(7.0, 10.0)));
        }

        votes.Add(publicCreator.Create(p1.Id, pub1.Id, RawScore(6.0, 9.0)));
        votes.Add(publicCreator.Create(p2.Id, pub1.Id, RawScore(7.0, 8.5)));
        votes.Add(publicCreator.Create(p4.Id, pub1.Id, RawScore(8.0, 9.0)));
        votes.Add(publicCreator.Create(p1.Id, pub2.Id, RawScore(7.5, 9.0)));
        votes.Add(publicCreator.Create(p6.Id, pub2.Id, RawScore(9.0, 8.0)));
        votes.Add(publicCreator.Create(p10.Id, pub2.Id, RawScore(8.5, 9.5)));
        votes.Add(publicCreator.Create(p3.Id, pub3.Id, RawScore(6.5, 8.0)));
        votes.Add(publicCreator.Create(p7.Id, pub3.Id, RawScore(8.0, 7.5)));
        votes.Add(publicCreator.Create(p8.Id, pub3.Id, RawScore(9.0, 8.5)));

        context.Votes.AddRange(votes);
        await context.SaveChangesAsync();
    }

    private static readonly Random _rnd = new(42);

    private static double RawScore(double min, double max)
        => Math.Round(min + _rnd.NextDouble() * (max - min), 1);
}