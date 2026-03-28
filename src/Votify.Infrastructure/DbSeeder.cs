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
    private static readonly Random _rnd = new(42);
    private static double RawScore(double min, double max)
        => Math.Round(min + _rnd.NextDouble() * (max - min), 1);

    public static async Task SeedAsync(VotifyDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Usuarios.AnyAsync() || await context.Events.AnyAsync())
            return;

        // ── 1. USUARIOS ───────────────────────────────────────────────────
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
            org1, org2,
            jury1, jury2, jury3, jury4,
            pub1, pub2, pub3,
            part1, part2, part3, part4, part5, part6);
        await context.SaveChangesAsync();

        // ── 2. EVENTOS ────────────────────────────────────────────────────
        EventCreator modalityCreator = new ModalityEventCreator("Presencial");

        var hackUPC = modalityCreator.Create(
            name: "HackUPC 2026",
            maxProjects: 30,
            startDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            topNProjectsAllowed: 3,
            description: "El hackathon más grande de la UPC.");

        // ModalityEventCreator guarda una sola modalidad en su estado interno;
        // creamos uno nuevo para el segundo evento con modalidad diferente.
        EventCreator hybridCreator = new ModalityEventCreator("Híbrido");

        var fibFair = hybridCreator.Create(
            name: "FIB Innovation Fair 2026",
            maxProjects: 20,
            startDate: new DateTime(2026, 5, 22, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 5, 23, 0, 0, 0, DateTimeKind.Utc),
            topNProjectsAllowed: 2,
            description: "Feria de innovación anual de la FIB.");

        context.Events.AddRange(hackUPC, fibFair);
        await context.SaveChangesAsync();

        // ── 3. CATEGORÍAS + CRITERIOS + PREMIOS ───────────────────────────

        // HackUPC — Proyectos de IA  (pesos: innovación 65 %, impacto 35 %)
        var catIA = new Category(hackUPC.Id, "Proyectos de IA",
                                 "Proyectos que aplican IA o machine learning.");
        var crIA1 = new Criterion(catIA.Id, "Innovación técnica", CriterionType.Numeric, 0.65,
                                  "Nivel de novedad y complejidad técnica.");
        var crIA2 = new Criterion(catIA.Id, "Impacto potencial", CriterionType.Numeric, 0.35,
                                  "Alcance e impacto esperado del proyecto.");
        var prIA1 = new Prize(catIA.Id, 1, "Premio IA — 1er lugar",
                              "1.000€ + mentoría de 6 meses en startup de IA.");

        // HackUPC — Proyectos Sociales  (pesos: 50 / 50)
        var catSocial = new Category(hackUPC.Id, "Proyectos Sociales",
                                     "Proyectos con impacto social directo.");
        var crSoc1 = new Criterion(catSocial.Id, "Impacto social", CriterionType.Numeric, 0.50,
                                   "Mejora real en la vida de personas.");
        var crSoc2 = new Criterion(catSocial.Id, "Viabilidad", CriterionType.Numeric, 0.50,
                                   "Posibilidad de implantación real.");
        var prSoc1 = new Prize(catSocial.Id, 1, "Premio Social — 1er lugar",
                               "500€ + acceso a incubadora universitaria.");

        // HackUPC — Sostenibilidad  (pesos: 55 / 45)
        var catSost = new Category(hackUPC.Id, "Sostenibilidad",
                                   "Proyectos enfocados en medioambiente y economía circular.");
        var crSost1 = new Criterion(catSost.Id, "Reducción de huella", CriterionType.Numeric, 0.55,
                                    "Impacto medioambiental medible.");
        var crSost2 = new Criterion(catSost.Id, "Escalabilidad", CriterionType.Numeric, 0.45,
                                    "Capacidad de crecer y replicarse.");
        var prSost1 = new Prize(catSost.Id, 1, "Premio Sostenibilidad — 1er lugar",
                                "750€ + presentación en COP31.");

        // FIB Fair — Startups Tecnológicas  (pesos: 60 / 40)
        var catStartup = new Category(fibFair.Id, "Startups Tecnológicas",
                                      "Proyectos con modelo de negocio escalable y base tecnológica.");
        var crStu1 = new Criterion(catStartup.Id, "Modelo de negocio", CriterionType.Numeric, 0.60,
                                   "Claridad y solidez del plan de negocio.");
        var crStu2 = new Criterion(catStartup.Id, "Tecnología base", CriterionType.Numeric, 0.40,
                                   "Madurez y diferenciación tecnológica.");
        var prStu1 = new Prize(catStartup.Id, 1, "Premio Startup — 1er lugar",
                               "2.000€ + ronda de presentaciones a inversores.");

        // FIB Fair — Impacto Social  (pesos: 45 / 55)
        var catImpacto = new Category(fibFair.Id, "Impacto Social",
                                      "Proyectos que priorizan el bienestar social.");
        var crImp1 = new Criterion(catImpacto.Id, "Alcance social", CriterionType.Numeric, 0.45,
                                   "Número de personas beneficiadas.");
        var crImp2 = new Criterion(catImpacto.Id, "Sostenibilidad", CriterionType.Numeric, 0.55,
                                   "Continuidad del proyecto en el tiempo.");
        var prImp1 = new Prize(catImpacto.Id, 1, "Premio Impacto — 1er lugar",
                               "1.000€ + colaboración con ONG partner.");

        // ── 4. SESIONES DE VOTACIÓN ───────────────────────────────────────
        var sessionHack = new VotingSession(
            eventId: hackUPC.Id,
            name: "Votación HackUPC 2026",
            openAt: new DateTime(2026, 4, 11, 18, 0, 0, DateTimeKind.Utc),
            closeAt: new DateTime(2026, 4, 12, 20, 0, 0, DateTimeKind.Utc),
            description: "Sesión principal de votación del hackathon.",
            reminderMinutesBeforeClose: 30);

        var sessionFib = new VotingSession(
            eventId: fibFair.Id,
            name: "Votación FIB Innovation Fair 2026",
            openAt: new DateTime(2026, 5, 22, 16, 0, 0, DateTimeKind.Utc),
            closeAt: new DateTime(2026, 5, 23, 18, 0, 0, DateTimeKind.Utc),
            description: "Sesión de votación de la feria.",
            reminderMinutesBeforeClose: 60);

        context.VotingSessions.AddRange(sessionHack, sessionFib);
        await context.SaveChangesAsync();

        // ── 5. PROYECTOS ─────────────────────────────────────────────────
        ProjectCreator aiCreator = new AiProjectCreator();
        ProjectCreator sustCreator = new SustainabilityProjectCreator();

        var p1 = aiCreator.Create("MediScan AI", hackUPC.Id, "Detección temprana de enfermedades.");
        var p2 = aiCreator.Create("EduAdapt", hackUPC.Id, "Plataforma de aprendizaje adaptativo.");
        var p3 = aiCreator.Create("TrafficFlow", hackUPC.Id, "Optimización de semáforos con IA.");
        var p4 = aiCreator.Create("RefugeeConnect", hackUPC.Id, "App de integración laboral para refugiados.");
        var p5 = aiCreator.Create("CuidaMayor", hackUPC.Id, "Plataforma de teleasistencia para mayores.");
        var p6 = sustCreator.Create("PlastiTrack", hackUPC.Id, "Trazabilidad de plásticos en la cadena de reciclaje.");
        var p7 = sustCreator.Create("SolarGrid", hackUPC.Id, "Red P2P de energía solar entre vecinos.");
        var p8 = aiCreator.Create("QuantumSec", fibFair.Id, "Criptografía post-cuántica para IoT.");
        var p9 = aiCreator.Create("BioPrint3D", fibFair.Id, "Impresión 3D de tejidos biológicos.");
        var p10 = sustCreator.Create("WaterSense", fibFair.Id, "Sensores IoT para gestión eficiente del agua.");

        context.Projects.AddRange(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
        await context.SaveChangesAsync();

        // ── 6. PROJECTCATEGORIES + CRITERIONSCORES ────────────────────────
        // Helper para crear un ProjectCategory con sus CriterionScores de una vez.
        static ProjectCategory MakePC(
            Project project,
            Category category,
            params (Criterion criterion, double score)[] scores)
        {
            var pc = new ProjectCategory(project.Id, category.Id);
            foreach (var (cr, sc) in scores)
                pc.CriterionScores.Add(new CriterionScore(pc.Id, cr.Id, sc));
            return pc;
        }

        var projectCategories = new List<ProjectCategory>
        {
            // p1 — MediScan AI: IA + Social
            MakePC(p1, catIA,     (crIA1,  9.0), (crIA2,  7.5)),
            MakePC(p1, catSocial, (crSoc1, 8.0), (crSoc2, 7.0)),
 
            // p2 — EduAdapt: IA
            MakePC(p2, catIA,     (crIA1,  8.0), (crIA2,  8.5)),
 
            // p3 — TrafficFlow: IA
            MakePC(p3, catIA,     (crIA1,  7.5), (crIA2,  6.0)),
 
            // p4 — RefugeeConnect: Social
            MakePC(p4, catSocial, (crSoc1, 8.5), (crSoc2, 9.0)),
 
            // p5 — CuidaMayor: Social
            MakePC(p5, catSocial, (crSoc1, 7.0), (crSoc2, 8.0)),
 
            // p6 — PlastiTrack: Sostenibilidad
            MakePC(p6, catSost,   (crSost1, 9.5), (crSost2, 8.0)),
 
            // p7 — SolarGrid: Sostenibilidad
            MakePC(p7, catSost,   (crSost1, 8.0), (crSost2, 9.0)),
 
            // p8 — QuantumSec: Startup
            MakePC(p8, catStartup,(crStu1, 9.0), (crStu2, 8.0)),
 
            // p9 — BioPrint3D: Startup
            MakePC(p9, catStartup,(crStu1, 8.5), (crStu2, 7.5)),
 
            // p10 — WaterSense: Impacto Social
            MakePC(p10, catImpacto,(crImp1, 7.5), (crImp2, 9.5)),
        };

        context.ProjectCategories.AddRange(projectCategories);
        await context.SaveChangesAsync();

        // ── 7. VOTOS ──────────────────────────────────────────────────────
        // VoteCreator.Create firma: (votingSessionId, projectId, userId, categoryId, rawScore, comment?)

        VoteCreator expertCreator = new ExpertVoteCreator();
        VoteCreator publicCreator = new PublicVoteCreator();

        var votes = new List<Vote>();

        // jury1 y jury2 votan p1, p2, p3 en catIA
        foreach (var proj in new[] { p1, p2, p3 })
        {
            votes.Add(expertCreator.Create(sessionHack.Id, proj.Id, jury1.Id, catIA.Id, RawScore(8.0, 9.5)));
            votes.Add(expertCreator.Create(sessionHack.Id, proj.Id, jury2.Id, catIA.Id, RawScore(7.0, 9.0)));
        }

        // jury3 vota p4, p5 en catSocial y p6, p7 en catSost
        votes.Add(expertCreator.Create(sessionHack.Id, p4.Id, jury3.Id, catSocial.Id, RawScore(7.5, 9.5)));
        votes.Add(expertCreator.Create(sessionHack.Id, p5.Id, jury3.Id, catSocial.Id, RawScore(7.5, 9.5)));
        votes.Add(expertCreator.Create(sessionHack.Id, p6.Id, jury3.Id, catSost.Id, RawScore(7.5, 9.5)));
        votes.Add(expertCreator.Create(sessionHack.Id, p7.Id, jury3.Id, catSost.Id, RawScore(7.5, 9.5)));

        // jury4 vota p8, p9 en catStartup y p10 en catImpacto
        votes.Add(expertCreator.Create(sessionFib.Id, p8.Id, jury4.Id, catStartup.Id, RawScore(7.0, 10.0)));
        votes.Add(expertCreator.Create(sessionFib.Id, p9.Id, jury4.Id, catStartup.Id, RawScore(7.0, 10.0)));
        votes.Add(expertCreator.Create(sessionFib.Id, p10.Id, jury4.Id, catImpacto.Id, RawScore(7.0, 10.0)));

        // público — votos en sus categorías correspondientes
        votes.Add(publicCreator.Create(sessionHack.Id, p1.Id, pub1.Id, catIA.Id, RawScore(6.0, 9.0)));
        votes.Add(publicCreator.Create(sessionHack.Id, p2.Id, pub1.Id, catIA.Id, RawScore(7.0, 8.5)));
        votes.Add(publicCreator.Create(sessionHack.Id, p4.Id, pub1.Id, catSocial.Id, RawScore(8.0, 9.0)));
        votes.Add(publicCreator.Create(sessionHack.Id, p1.Id, pub2.Id, catIA.Id, RawScore(7.5, 9.0)));
        votes.Add(publicCreator.Create(sessionHack.Id, p6.Id, pub2.Id, catSost.Id, RawScore(9.0, 8.0)));  // corregido: catSost
        votes.Add(publicCreator.Create(sessionFib.Id, p10.Id, pub2.Id, catImpacto.Id, RawScore(8.5, 9.5)));
        votes.Add(publicCreator.Create(sessionHack.Id, p3.Id, pub3.Id, catIA.Id, RawScore(6.5, 8.0)));
        votes.Add(publicCreator.Create(sessionHack.Id, p7.Id, pub3.Id, catSost.Id, RawScore(8.0, 7.5)));  // corregido: catSost
        votes.Add(publicCreator.Create(sessionFib.Id, p8.Id, pub3.Id, catStartup.Id, RawScore(9.0, 8.5)));

        context.Votes.AddRange(votes);
        await context.SaveChangesAsync();
    }
}