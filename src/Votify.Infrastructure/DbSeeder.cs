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

        if (await context.Users.AnyAsync() || await context.Events.AnyAsync())
            return;

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

        context.Users.AddRange(
            org1, org2,
            jury1, jury2, jury3, jury4,
            pub1, pub2, pub3,
            part1, part2, part3, part4, part5, part6);
        await context.SaveChangesAsync();

        EventCreator modalityCreator = new ModalityEventCreator("Presencial");

        var hackUPC = modalityCreator.Create(
            name: "HackUPC 2026",
            maxProjects: 30,
            startDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            description: "El hackathon más grande de la UPC.");

        EventCreator hybridCreator = new ModalityEventCreator("Híbrido");

        var fibFair = hybridCreator.Create(
            name: "FIB Innovation Fair 2026",
            maxProjects: 20,
            startDate: new DateTime(2026, 5, 22, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 5, 23, 0, 0, 0, DateTimeKind.Utc),
            description: "Feria de innovación anual de la FIB.");

        context.Events.AddRange(hackUPC, fibFair);
        await context.SaveChangesAsync();

        var hackOpen = new DateTime(2026, 4, 11, 18, 0, 0, DateTimeKind.Utc);
        var hackClose = new DateTime(2026, 4, 12, 20, 0, 0, DateTimeKind.Utc);
        var fibOpen = new DateTime(2026, 5, 22, 16, 0, 0, DateTimeKind.Utc);
        var fibClose = new DateTime(2026, 5, 23, 18, 0, 0, DateTimeKind.Utc);

        var catIA = new Category(hackUPC.Id, "Proyectos de IA", "Proyectos que aplican IA o machine learning.",
                                 combineResults: true, juryWeight: 0.7, publicWeight: 0.3);
        var sesIAJury = new VotingSession(catIA.Id, "Jurado - IA", VoterType.Jury, EvaluationType.WeightedScale,
                                          hackOpen, hackClose, "Evaluación experta con baremo ponderado.",
                                          criterionType: CriterionType.Numeric,
                                          allowComments: true, requireComments: true,
                                          reminderMinutesBeforeClose: 30);
        sesIAJury.Criteria.Add(new Criterion(sesIAJury.Id, "Innovación técnica", 0.65, "Nivel de novedad y complejidad técnica."));
        sesIAJury.Criteria.Add(new Criterion(sesIAJury.Id, "Impacto potencial", 0.35, "Alcance e impacto esperado del proyecto."));
        // ── AÑADIDO PREMIO AL JURADO ──
        sesIAJury.Prizes.Add(new Prize(sesIAJury.Id, 1, "Premio IA — 1er lugar", "1.000€ + mentoría de 6 meses."));

        var sesIAPublic = new VotingSession(catIA.Id, "Público - IA", VoterType.Public, EvaluationType.TopN,
                                            hackOpen, hackClose, "Votación popular por ranking.",
                                            topN: 3, allowComments: true,
                                            reminderMinutesBeforeClose: 30);
        // ── AÑADIDO PREMIO AL PÚBLICO ──
        sesIAPublic.Prizes.Add(new Prize(sesIAPublic.Id, 2, "Premio IA — 2º lugar", "500€."));

        catIA.VotingSessions.Add(sesIAJury);
        catIA.VotingSessions.Add(sesIAPublic);

        var catSocial = new Category(hackUPC.Id, "Proyectos Sociales", "Proyectos con impacto social directo.");
        var sesSocJury = new VotingSession(catSocial.Id, "Jurado - Social", VoterType.Jury, EvaluationType.WeightedScale,
                                           hackOpen, hackClose, "Evaluación experta con baremo ponderado.",
                                           criterionType: CriterionType.Numeric,
                                           allowComments: true,
                                           reminderMinutesBeforeClose: 30);
        sesSocJury.Criteria.Add(new Criterion(sesSocJury.Id, "Impacto social", 0.50, "Mejora real en la vida de personas."));
        sesSocJury.Criteria.Add(new Criterion(sesSocJury.Id, "Viabilidad", 0.50, "Posibilidad de implantación real."));
        // ── AÑADIDO PREMIO AL JURADO ──
        sesSocJury.Prizes.Add(new Prize(sesSocJury.Id, 1, "Premio Social — 1er lugar", "500€ + acceso a incubadora universitaria."));
        catSocial.VotingSessions.Add(sesSocJury);

        var catSost = new Category(hackUPC.Id, "Sostenibilidad", "Proyectos enfocados en medioambiente y economía circular.");
        var sesSostJury = new VotingSession(catSost.Id, "Jurado - Sostenibilidad", VoterType.Jury, EvaluationType.PointDistribution,
                                            hackOpen, hackClose, "Reparto de puntos entre proyectos.",
                                            criterionType: CriterionType.Numeric,
                                            pointsPerVoter: 100, maxPointsPerProject: 40,
                                            allowComments: true, allowCommentsPerCriterion: true,
                                            reminderMinutesBeforeClose: 30);
        sesSostJury.Criteria.Add(new Criterion(sesSostJury.Id, "Reducción de huella", 0.55, "Impacto medioambiental medible."));
        sesSostJury.Criteria.Add(new Criterion(sesSostJury.Id, "Escalabilidad", 0.45, "Capacidad de crecer y replicarse."));
        // ── AÑADIDO PREMIO AL JURADO ──
        sesSostJury.Prizes.Add(new Prize(sesSostJury.Id, 1, "Premio Sostenibilidad — 1er lugar", "750€ + presentación en COP31."));
        catSost.VotingSessions.Add(sesSostJury);

        var catStartup = new Category(fibFair.Id, "Startups Tecnológicas", "Proyectos con modelo de negocio escalable y base tecnológica.");
        var sesStuJury = new VotingSession(catStartup.Id, "Jurado - Startups", VoterType.Jury, EvaluationType.WeightedScale,
                                           fibOpen, fibClose, "Evaluación experta con baremo ponderado.",
                                           criterionType: CriterionType.Rubric,
                                           allowComments: true, requireComments: true,
                                           reminderMinutesBeforeClose: 60);
        sesStuJury.Criteria.Add(new Criterion(sesStuJury.Id, "Modelo de negocio", 0.60, "Claridad y solidez del plan de negocio."));
        sesStuJury.Criteria.Add(new Criterion(sesStuJury.Id, "Tecnología base", 0.40, "Madurez y diferenciación tecnológica."));
        // ── AÑADIDO PREMIO AL JURADO ──
        sesStuJury.Prizes.Add(new Prize(sesStuJury.Id, 1, "Premio Startup — 1er lugar", "2.000€ + ronda de inversores."));
        catStartup.VotingSessions.Add(sesStuJury);

        var catImpacto = new Category(fibFair.Id, "Impacto Social", "Proyectos que priorizan el bienestar social.");
        var sesImpPublic = new VotingSession(catImpacto.Id, "Público - Impacto", VoterType.Public, EvaluationType.PointDistribution,
                                             fibOpen, fibClose, "Reparto de puntos popular.",
                                             pointsPerVoter: 50, maxPointsPerProject: 20,
                                             allowComments: true,
                                             reminderMinutesBeforeClose: 60);
        // ── AÑADIDO PREMIO AL PÚBLICO ──
        sesImpPublic.Prizes.Add(new Prize(sesImpPublic.Id, 1, "Premio Impacto — 1er lugar", "1.000€ + colaboración con ONG partner."));
        catImpacto.VotingSessions.Add(sesImpPublic);

        context.Categories.AddRange(catIA, catSocial, catSost, catStartup, catImpacto);
        await context.SaveChangesAsync();

        ProjectCreator aiCreator = new AiProjectCreator();
        ProjectCreator sustCreator = new SustainabilityProjectCreator();

        var p1 = aiCreator.Create("MediScan AI", hackUPC.Id, ownerId: part1.Id, description: "Detección temprana de enfermedades.");
        var p2 = aiCreator.Create("EduAdapt", hackUPC.Id, ownerId: part2.Id, description: "Plataforma de aprendizaje adaptativo.");
        var p3 = aiCreator.Create("TrafficFlow", hackUPC.Id, ownerId: part3.Id, description: "Optimización de semáforos con IA.");
        var p4 = aiCreator.Create("RefugeeConnect", hackUPC.Id, ownerId: part4.Id, description: "App de integración laboral para refugiados.");
        var p5 = aiCreator.Create("CuidaMayor", hackUPC.Id, ownerId: part5.Id, description: "Plataforma de teleasistencia para mayores.");
        var p6 = sustCreator.Create("PlastiTrack", hackUPC.Id, ownerId: part6.Id, description: "Trazabilidad de plásticos en la cadena de reciclaje.");
        var p7 = sustCreator.Create("SolarGrid", hackUPC.Id, ownerId: part1.Id, description: "Red P2P de energía solar entre vecinos.");
        var p8 = aiCreator.Create("QuantumSec", fibFair.Id, ownerId: part2.Id, description: "Criptografía post-cuántica para IoT.");
        var p9 = aiCreator.Create("BioPrint3D", fibFair.Id, ownerId: part3.Id, description: "Impresión 3D de tejidos biológicos.");
        var p10 = sustCreator.Create("WaterSense", fibFair.Id, ownerId: part4.Id, description: "Sensores IoT para gestión eficiente del agua.");

        context.Projects.AddRange(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
        await context.SaveChangesAsync();

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

        VoteCreator expertCreator = new ExpertVoteCreator();
        VoteCreator publicCreator = new PublicVoteCreator();

        var votes = new List<Vote>();

        votes.Add(expertCreator.Create(sesIAJury.Id, p1.Id, jury1.Id, catIA.Id, 1));
        votes.Add(expertCreator.Create(sesIAJury.Id, p2.Id, jury1.Id, catIA.Id, 2));
        votes.Add(expertCreator.Create(sesIAJury.Id, p3.Id, jury1.Id, catIA.Id, 3));

        votes.Add(expertCreator.Create(sesIAJury.Id, p2.Id, jury2.Id, catIA.Id, 1));
        votes.Add(expertCreator.Create(sesIAJury.Id, p3.Id, jury2.Id, catIA.Id, 2));
        votes.Add(expertCreator.Create(sesIAJury.Id, p1.Id, jury2.Id, catIA.Id, 3));

        votes.Add(expertCreator.Create(sesSocJury.Id, p4.Id, jury3.Id, catSocial.Id, 1));
        votes.Add(expertCreator.Create(sesSocJury.Id, p5.Id, jury3.Id, catSocial.Id, 2));
        votes.Add(expertCreator.Create(sesSostJury.Id, p6.Id, jury3.Id, catSost.Id, 1));
        votes.Add(expertCreator.Create(sesSostJury.Id, p7.Id, jury3.Id, catSost.Id, 2));

        votes.Add(publicCreator.Create(sesIAPublic.Id, p1.Id, pub1.Id, catIA.Id, 1));
        votes.Add(publicCreator.Create(sesIAPublic.Id, p2.Id, pub1.Id, catIA.Id, 2));
        votes.Add(publicCreator.Create(sesIAPublic.Id, p1.Id, pub2.Id, catIA.Id, 1));
        votes.Add(publicCreator.Create(sesImpPublic.Id, p10.Id, pub2.Id, catImpacto.Id, 1));
        votes.Add(publicCreator.Create(sesIAPublic.Id, p3.Id, pub3.Id, catIA.Id, 1));

        foreach (var v in votes)
            v.GenerateIntegrityHash();

        context.Votes.AddRange(votes);
        await context.SaveChangesAsync();
    }
}