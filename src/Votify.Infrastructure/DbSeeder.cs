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

        // HackUPC — Proyectos de IA
        var catIA = new Category(hackUPC.Id, "Proyectos de IA", "Proyectos que aplican IA o machine learning.");
        var crIA1 = new Criterion(catIA.Id, "Innovación técnica", CriterionType.Numeric, 0.65, "Nivel de novedad y complejidad técnica.");
        var crIA2 = new Criterion(catIA.Id, "Impacto potencial", CriterionType.Numeric, 0.35, "Alcance e impacto esperado del proyecto.");
        var prIA1 = new Prize(catIA.Id, 1, "Premio IA — 1er lugar", "1.000€ + mentoría de 6 meses en startup de IA.");

        // Asignamos a la categoría
        catIA.Criteria.Add(crIA1);
        catIA.Criteria.Add(crIA2);
        catIA.Prizes.Add(prIA1);

        // HackUPC — Proyectos Sociales
        var catSocial = new Category(hackUPC.Id, "Proyectos Sociales", "Proyectos con impacto social directo.");
        var crSoc1 = new Criterion(catSocial.Id, "Impacto social", CriterionType.Numeric, 0.50, "Mejora real en la vida de personas.");
        var crSoc2 = new Criterion(catSocial.Id, "Viabilidad", CriterionType.Numeric, 0.50, "Posibilidad de implantación real.");
        var prSoc1 = new Prize(catSocial.Id, 1, "Premio Social — 1er lugar", "500€ + acceso a incubadora universitaria.");

        catSocial.Criteria.Add(crSoc1);
        catSocial.Criteria.Add(crSoc2);
        catSocial.Prizes.Add(prSoc1);

        // HackUPC — Sostenibilidad
        var catSost = new Category(hackUPC.Id, "Sostenibilidad", "Proyectos enfocados en medioambiente y economía circular.");
        var crSost1 = new Criterion(catSost.Id, "Reducción de huella", CriterionType.Numeric, 0.55, "Impacto medioambiental medible.");
        var crSost2 = new Criterion(catSost.Id, "Escalabilidad", CriterionType.Numeric, 0.45, "Capacidad de crecer y replicarse.");
        var prSost1 = new Prize(catSost.Id, 1, "Premio Sostenibilidad — 1er lugar", "750€ + presentación en COP31.");

        catSost.Criteria.Add(crSost1);
        catSost.Criteria.Add(crSost2);
        catSost.Prizes.Add(prSost1);

        // FIB Fair — Startups Tecnológicas
        var catStartup = new Category(fibFair.Id, "Startups Tecnológicas", "Proyectos con modelo de negocio escalable y base tecnológica.");
        var crStu1 = new Criterion(catStartup.Id, "Modelo de negocio", CriterionType.Numeric, 0.60, "Claridad y solidez del plan de negocio.");
        var crStu2 = new Criterion(catStartup.Id, "Tecnología base", CriterionType.Numeric, 0.40, "Madurez y diferenciación tecnológica.");
        var prStu1 = new Prize(catStartup.Id, 1, "Premio Startup — 1er lugar", "2.000€ + ronda de presentaciones a inversores.");

        catStartup.Criteria.Add(crStu1);
        catStartup.Criteria.Add(crStu2);
        catStartup.Prizes.Add(prStu1);

        // FIB Fair — Impacto Social
        var catImpacto = new Category(fibFair.Id, "Impacto Social", "Proyectos que priorizan el bienestar social.");
        var crImp1 = new Criterion(catImpacto.Id, "Alcance social", CriterionType.Numeric, 0.45, "Número de personas beneficiadas.");
        var crImp2 = new Criterion(catImpacto.Id, "Sostenibilidad", CriterionType.Numeric, 0.55, "Continuidad del proyecto en el tiempo.");
        var prImp1 = new Prize(catImpacto.Id, 1, "Premio Impacto — 1er lugar", "1.000€ + colaboración con ONG partner.");

        catImpacto.Criteria.Add(crImp1);
        catImpacto.Criteria.Add(crImp2);
        catImpacto.Prizes.Add(prImp1);

        context.Categories.AddRange(catIA, catSocial, catSost, catStartup, catImpacto);
        await context.SaveChangesAsync();

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
        static ProjectCategory MakePC(Project project, Category category, params (Criterion criterion, double score)[] scores)
        {
            var pc = new ProjectCategory(project.Id, category.Id);
            foreach (var (cr, sc) in scores)
                pc.CriterionScores.Add(new CriterionScore(pc.Id, cr.Id, sc));
            return pc;
        }

        var relacionesProyectos = new List<ProjectCategory>
        {
            new ProjectCategory(p1.Id, catIA.Id),
            new ProjectCategory(p1.Id, catSocial.Id),
            new ProjectCategory(p2.Id, catIA.Id),
            new ProjectCategory(p3.Id, catIA.Id),
            new ProjectCategory(p4.Id, catSocial.Id),
            new ProjectCategory(p5.Id, catSocial.Id),
            new ProjectCategory(p6.Id, catSost.Id),
            new ProjectCategory(p7.Id, catSost.Id),
            new ProjectCategory(p8.Id, catStartup.Id),
            new ProjectCategory(p9.Id, catStartup.Id),
            new ProjectCategory(p10.Id, catImpacto.Id)
        };

        context.Set<ProjectCategory>().AddRange(relacionesProyectos);
        await context.SaveChangesAsync();

        // ── 7. VOTOS (ACTUALIZADO A TOP N POSITIONS) ──────────────────────
        VoteCreator expertCreator = new ExpertVoteCreator();
        VoteCreator publicCreator = new PublicVoteCreator();

        var votes = new List<Vote>();

        // JURY 1 vota su Top 3 en Categoría IA
        votes.Add(expertCreator.Create(sessionHack.Id, p1.Id, jury1.Id, catIA.Id, 1));
        votes.Add(expertCreator.Create(sessionHack.Id, p2.Id, jury1.Id, catIA.Id, 2));
        votes.Add(expertCreator.Create(sessionHack.Id, p3.Id, jury1.Id, catIA.Id, 3));

        // JURY 2 vota su Top 3 en Categoría IA (en distinto orden)
        votes.Add(expertCreator.Create(sessionHack.Id, p2.Id, jury2.Id, catIA.Id, 1));
        votes.Add(expertCreator.Create(sessionHack.Id, p3.Id, jury2.Id, catIA.Id, 2));
        votes.Add(expertCreator.Create(sessionHack.Id, p1.Id, jury2.Id, catIA.Id, 3));

        // JURY 3 vota en Sociales y Sostenibilidad
        votes.Add(expertCreator.Create(sessionHack.Id, p4.Id, jury3.Id, catSocial.Id, 1));
        votes.Add(expertCreator.Create(sessionHack.Id, p5.Id, jury3.Id, catSocial.Id, 2));
        votes.Add(expertCreator.Create(sessionHack.Id, p6.Id, jury3.Id, catSost.Id, 1));
        votes.Add(expertCreator.Create(sessionHack.Id, p7.Id, jury3.Id, catSost.Id, 2));

        // PÚBLICO 1
        votes.Add(publicCreator.Create(sessionHack.Id, p1.Id, pub1.Id, catIA.Id, 1));
        votes.Add(publicCreator.Create(sessionHack.Id, p2.Id, pub1.Id, catIA.Id, 2));
        votes.Add(publicCreator.Create(sessionHack.Id, p4.Id, pub1.Id, catSocial.Id, 1));

        // PÚBLICO 2
        votes.Add(publicCreator.Create(sessionHack.Id, p1.Id, pub2.Id, catIA.Id, 1));
        votes.Add(publicCreator.Create(sessionHack.Id, p6.Id, pub2.Id, catSost.Id, 1));
        votes.Add(publicCreator.Create(sessionFib.Id, p10.Id, pub2.Id, catImpacto.Id, 1));

        // PÚBLICO 3
        votes.Add(publicCreator.Create(sessionHack.Id, p3.Id, pub3.Id, catIA.Id, 1));
        votes.Add(publicCreator.Create(sessionHack.Id, p7.Id, pub3.Id, catSost.Id, 1));
        votes.Add(publicCreator.Create(sessionFib.Id, p8.Id, pub3.Id, catStartup.Id, 1));

        context.Votes.AddRange(votes);
        await context.SaveChangesAsync();
    }
}