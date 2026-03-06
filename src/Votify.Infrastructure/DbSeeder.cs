using Microsoft.EntityFrameworkCore;
using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.ProjectFolder;
using Votify.Domain.UserFolder;
using Votify.Domain.VoteFolder;
using Votify.Factory;

namespace Votify.Infrastructure.Data;

/// <summary>
/// Puebla la base de datos con datos de prueba realistas.
/// Simula dos eventos completos con categorías, proyectos, usuarios y votos.
///
/// Estructura sembrada:
///   Evento 1 — HackUPC 2026 (Hackathon)
///     ├─ Categoría: Proyectos de IA        (weightA=0.65, weightB=0.35)
///     ├─ Categoría: Proyectos Sociales     (weightA=0.50, weightB=0.50)
///     └─ Categoría: Sostenibilidad         (weightA=0.55, weightB=0.45)
///
///   Evento 2 — FIB Innovation Fair 2026 (Feria de Innovación)
///     ├─ Categoría: Startups Tecnológicas  (weightA=0.60, weightB=0.40)
///     └─ Categoría: Impacto Social         (weightA=0.45, weightB=0.55)
///
///   Usuarios: 2 organizadores, 4 jurado, 3 público, 6 participantes
///   Proyectos: 10 proyectos distribuidos entre categorías
///   Votos: votos de experto y público sobre los proyectos
/// </summary>
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

        // Organizadores
        var org1 = new Organizer("Alan Brito", "alan.brito@votify.com", "Hash$1234");
        var org2 = new Organizer("Laura Pérez", "laura.perez@votify.com", "Hash$5678");

        // Jurado (emiten ExpertVote)
        var jury1 = new Jury("Ada Lovelace", "ada@test.com", "Hash$Ada1");
        var jury2 = new Jury("Grace Hopper", "grace@test.com", "Hash$Gra2");
        var jury3 = new Jury("Linus Torvalds", "linus@test.com", "Hash$Lin3");
        var jury4 = new Jury("Margaret Hamilton", "margaret@test.com", "Hash$Mag4");

        // Público general (emiten PublicVote)
        var pub1 = new Public("Alan Turing", "alan.turing@test.com", "Hash$Tur1");
        var pub2 = new Public("John von Neumann", "john@test.com", "Hash$Joh2");
        var pub3 = new Public("Claude Shannon", "claude@test.com", "Hash$Sha3");

        // Participantes (presentan proyectos)
        var part1 = new Participant("Aitor Tilla", "aitor@test.com", "Hash$Ait1");
        var part2 = new Participant("Elsa Capunta", "elsa@test.com", "Hash$Els2");
        var part3 = new Participant("Pedro Alves", "pedro@test.com", "Hash$Ped3");
        var part4 = new Participant("Sofía Martín", "sofia@test.com", "Hash$Sof4");
        var part5 = new Participant("Carlos Ruiz", "carlos@test.com", "Hash$Car5");
        var part6 = new Participant("Marta López", "marta@test.com", "Hash$Mar6");

        context.Usuarios.AddRange(
            org1, org2,
            jury1, jury2, jury3, jury4,
            pub1, pub2, pub3,
            part1, part2, part3, part4, part5, part6
        );
        await context.SaveChangesAsync();

        // ════════════════════════════════════════════════════════════════════
        // 2. EVENTOS (via Factory Method)
        // ════════════════════════════════════════════════════════════════════

        var hackUPC = new Event(
            name: "HackUPC 2026",
            maxProjects: 30,
            startDate: new DateTime(2026, 4, 10),
            modality: "hackatoonEvent",
            description: "El hackathon más grande de la UPC. 24h de innovación continua."
        );

        var fibFair = new Event(
            name: "FIB Innovation Fair 2026",
            maxProjects: 20,
            startDate: new DateTime(2026, 5, 22),
            modality: "innovationFairEvent",
            description: "Feria de innovación anual de la Facultad de Informática de Barcelona."
        );

        context.Events.AddRange(hackUPC, fibFair);
        await context.SaveChangesAsync();

        // ════════════════════════════════════════════════════════════════════
        // 3. CATEGORÍAS (definidas por el organizador para cada evento)
        //    Los pesos de criterios los configura el organizador, no el código.
        // ════════════════════════════════════════════════════════════════════

        // — HackUPC 2026 —
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

        // — FIB Innovation Fair 2026 —
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
        // 4. PROYECTOS (via Factory Method)
        // ════════════════════════════════════════════════════════════════════

        ProjectCreator aiCreator = new AiProjectCreator();
        ProjectCreator sustCreator = new SustainabilityProjectCreator();

        // HackUPC — Proyectos de IA
        var p1 = aiCreator.Create(
            title: "MediScan AI",
            eventId: hackUPC.Id,
            categoryId: catIA.Id,
            criterionA: 9.0, criterionB: 7.5,
            description: "Detección temprana de enfermedades mediante visión por computador."
        );
        var p2 = aiCreator.Create(
            title: "EduAdapt",
            eventId: hackUPC.Id,
            categoryId: catIA.Id,
            criterionA: 8.0, criterionB: 8.5,
            description: "Plataforma de aprendizaje adaptativo con LLMs personalizados."
        );
        var p3 = aiCreator.Create(
            title: "TrafficFlow",
            eventId: hackUPC.Id,
            categoryId: catIA.Id,
            criterionA: 7.5, criterionB: 6.0,
            description: "Optimización de semáforos en tiempo real con reinforcement learning."
        );

        // HackUPC — Proyectos Sociales
        var p4 = aiCreator.Create(
            title: "RefugeeConnect",
            eventId: hackUPC.Id,
            categoryId: catSocial.Id,
            criterionA: 8.5, criterionB: 9.0,
            description: "App de integración laboral para personas refugiadas."
        );
        var p5 = aiCreator.Create(
            title: "CuidaMayor",
            eventId: hackUPC.Id,
            categoryId: catSocial.Id,
            criterionA: 7.0, criterionB: 8.0,
            description: "Plataforma de teleasistencia para personas mayores en zonas rurales."
        );

        // HackUPC — Sostenibilidad
        var p6 = sustCreator.Create(
            title: "PlastiTrack",
            eventId: hackUPC.Id,
            categoryId: catSostenibilidad.Id,
            criterionA: 9.5, criterionB: 8.0,
            description: "Trazabilidad blockchain de residuos plásticos en la cadena de suministro."
        );
        var p7 = sustCreator.Create(
            title: "SolarGrid",
            eventId: hackUPC.Id,
            categoryId: catSostenibilidad.Id,
            criterionA: 8.0, criterionB: 9.0,
            description: "Red P2P de intercambio de energía solar entre vecinos."
        );

        // FIB Innovation Fair — Startups Tecnológicas
        var p8 = aiCreator.Create(
            title: "QuantumSec",
            eventId: fibFair.Id,
            categoryId: catStartup.Id,
            criterionA: 9.0, criterionB: 8.0,
            description: "Criptografía post-cuántica accesible para pymes."
        );
        var p9 = aiCreator.Create(
            title: "BioPrint3D",
            eventId: fibFair.Id,
            categoryId: catStartup.Id,
            criterionA: 8.5, criterionB: 7.5,
            description: "Impresión 3D de tejidos para medicina regenerativa."
        );

        // FIB Innovation Fair — Impacto Social
        var p10 = sustCreator.Create(
            title: "WaterSense",
            eventId: fibFair.Id,
            categoryId: catImpacto.Id,
            criterionA: 7.5, criterionB: 9.5,
            description: "Sensores IoT de bajo coste para monitorizar la calidad del agua en países en desarrollo."
        );

        context.Projects.AddRange(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
        await context.SaveChangesAsync();

        // ════════════════════════════════════════════════════════════════════
        // 5. VOTOS (via Factory Method)
        //    Cada jurado vota todos los proyectos de su categoría asignada.
        //    El público vota libremente.
        //    Se garantiza: una persona no vota más de una vez el mismo proyecto.
        // ════════════════════════════════════════════════════════════════════

        VoteCreator expertCreator = new ExpertVoteCreator();
        VoteCreator publicCreator = new PublicVoteCreator();

        var votes = new List<Vote>();

        // — Jury1 y Jury2 evalúan proyectos de IA del HackUPC —
        foreach (var proyecto in new[] { p1, p2, p3 })
        {
            votes.Add(expertCreator.Create(proyecto.Id, jury1.Id, RawScore(8.0, 9.5)));
            votes.Add(expertCreator.Create(proyecto.Id, jury2.Id, RawScore(7.0, 9.0)));
        }

        // — Jury3 evalúa proyectos sociales y sostenibilidad del HackUPC —
        foreach (var proyecto in new[] { p4, p5, p6, p7 })
        {
            votes.Add(expertCreator.Create(proyecto.Id, jury3.Id, RawScore(7.5, 9.5)));
        }

        // — Jury4 evalúa proyectos de la FIB Fair —
        foreach (var proyecto in new[] { p8, p9, p10 })
        {
            votes.Add(expertCreator.Create(proyecto.Id, jury4.Id, RawScore(7.0, 10.0)));
        }

        // — Público vota una selección de proyectos —
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

    // ────────────────────────────────────────────────────────────────────────
    // Helper: genera una puntuación aleatoria dentro de un rango realista
    // ────────────────────────────────────────────────────────────────────────
    private static readonly Random _rnd = new(42); // seed fija para reproducibilidad

    private static double RawScore(double min, double max)
        => Math.Round(min + _rnd.NextDouble() * (max - min), 1);
}