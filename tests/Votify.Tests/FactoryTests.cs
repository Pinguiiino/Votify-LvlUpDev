using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Votify.Domain.Factory;
using Votify.Domain.VoteFolder;
using Votify.Domain.ProjectFolder;
using Votify.Domain.EventFolder;

namespace Votify.Tests
{
    public class FactoryTests
    {
        #region VoteCreatorFactory

        [Fact]
        public void GetCreator_VotoPublico_RetornaPublicVoteCreator()
        {
            // Arrange
            var publicCreator = new PublicVoteCreator();
            var expertCreator = new ExpertVoteCreator();
            var factory = new VoteCreatorFactory(new VoteCreator[] { publicCreator, expertCreator });

            // Act
            var result = factory.GetCreator(VoterType.Public);

            // Assert
            Assert.IsType<PublicVoteCreator>(result);
        }

        [Fact]
        public void GetCreator_VotoJurado_RetornaExpertVoteCreator()
        {
            // Arrange
            var publicCreator = new PublicVoteCreator();
            var expertCreator = new ExpertVoteCreator();
            var factory = new VoteCreatorFactory(new VoteCreator[] { publicCreator, expertCreator });

            // Act
            var result = factory.GetCreator(VoterType.Jury);

            // Assert
            Assert.IsType<ExpertVoteCreator>(result);
        }

        [Fact]
        public void GetCreator_TipoNoRegistrado_LanzaNotSupportedException()
        {
            // Arrange
            var factory = new VoteCreatorFactory(new VoteCreator[] { new PublicVoteCreator() });

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(
                () => factory.GetCreator(VoterType.Jury));
            Assert.Contains("No hay ningún VoteCreator registrado", ex.Message);
        }

        [Fact]
        public void Constructor_ListaVoca_DiccionarioVacio()
        {
            // Arrange & Act
            var factory = new VoteCreatorFactory(Array.Empty<VoteCreator>());

            // Assert
            var ex = Assert.Throws<NotSupportedException>(
                () => factory.GetCreator(VoterType.Public));
            Assert.Contains("No hay ningún VoteCreator registrado", ex.Message);
        }

        #endregion

        #region PublicVoteCreator

        [Fact]
        public void PublicVoteCreator_SupportedType_EsPublic()
        {
            // Arrange
            var creator = new PublicVoteCreator();

            // Act & Assert
            Assert.Equal(VoterType.Public, creator.SupportedType);
        }

        [Fact]
        public void PublicVoteCreator_Create_RetornaPublicVote()
        {
            // Arrange
            var creator = new PublicVoteCreator();

            // Act
            var result = creator.Create("session-1", "proj-1", "user-1", "cat-1", 1, "Comentario", 10);

            // Assert
            Assert.IsType<PublicVote>(result);
            Assert.Equal("session-1", result.VotingSessionId);
            Assert.Equal("proj-1", result.VotedProjectId);
            Assert.Equal("user-1", result.UserId);
            Assert.Equal("cat-1", result.CategoryId);
            Assert.Equal(1, result.TopPosition);
            Assert.Equal("Comentario", result.Comment);
            Assert.Equal(10, result.Points);
        }

        [Fact]
        public void PublicVoteCreator_Create_SinComentarioNiPuntos_RetornaValoresNulos()
        {
            // Arrange
            var creator = new PublicVoteCreator();

            // Act
            var result = creator.Create("session-1", "proj-1", "user-1", "cat-1", 1);

            // Assert
            Assert.Null(result.Comment);
            Assert.Null(result.Points);
        }

        #endregion

        #region ExpertVoteCreator

        [Fact]
        public void ExpertVoteCreator_SupportedType_EsJury()
        {
            // Arrange
            var creator = new ExpertVoteCreator();

            // Act & Assert
            Assert.Equal(VoterType.Jury, creator.SupportedType);
        }

        [Fact]
        public void ExpertVoteCreator_Create_RetornaExpertVote()
        {
            // Arrange
            var creator = new ExpertVoteCreator();

            // Act
            var result = creator.Create("session-1", "proj-1", "user-1", "cat-1", 2, "Bueno", 50);

            // Assert
            Assert.IsType<ExpertVote>(result);
            Assert.Equal("session-1", result.VotingSessionId);
            Assert.Equal("proj-1", result.VotedProjectId);
            Assert.Equal("user-1", result.UserId);
            Assert.Equal("cat-1", result.CategoryId);
            Assert.Equal(2, result.TopPosition);
            Assert.Equal("Bueno", result.Comment);
            Assert.Equal(50, result.Points);
        }

        [Fact]
        public void ExpertVoteCreator_Create_SinOpcionales_RetornaValoresNulos()
        {
            // Arrange
            var creator = new ExpertVoteCreator();

            // Act
            var result = creator.Create("s", "p", "u", "c", 1);

            // Assert
            Assert.Null(result.Comment);
            Assert.Null(result.Points);
        }

        #endregion

        #region AiProjectCreator

        [Fact]
        public void AiProjectCreator_Create_RetornaAiProject()
        {
            // Arrange
            var creator = new AiProjectCreator();

            // Act
            var result = creator.Create("Title", "event-1", "owner-1", "Desc", "img.png");

            // Assert
            Assert.IsType<AiProject>(result);
            Assert.Equal("Title", result.Title);
            Assert.Equal("event-1", result.EventId);
            Assert.Equal("owner-1", result.OwnerId);
            Assert.Equal("Desc", result.Description);
            Assert.Equal("img.png", result.ImageUrl);
        }

        [Fact]
        public void AiProjectCreator_Create_SinOpcionales_ValoresNulos()
        {
            // Arrange
            var creator = new AiProjectCreator();

            // Act
            var result = creator.Create("Title", "event-1");

            // Assert
            Assert.IsType<AiProject>(result);
            Assert.Null(result.OwnerId);
            Assert.Null(result.Description);
            Assert.Null(result.ImageUrl);
        }

        [Fact]
        public void AiProjectCreator_Create_ProjectType_EsAI()
        {
            // Arrange
            var creator = new AiProjectCreator();

            // Act
            var result = creator.Create("Title", "event-1");

            // Assert
            Assert.Equal("AI", result.ProjectType());
        }

        #endregion

        #region GeneralProjectCreator

        [Fact]
        public void GeneralProjectCreator_Create_RetornaGeneralProject()
        {
            // Arrange
            var creator = new GeneralProjectCreator();

            // Act
            var result = creator.Create("Title", "event-1", "owner-1", "Desc", "img.png");

            // Assert
            Assert.IsType<GeneralProject>(result);
            Assert.Equal("Title", result.Title);
            Assert.Equal("event-1", result.EventId);
            Assert.Equal("owner-1", result.OwnerId);
            Assert.Equal("Desc", result.Description);
            Assert.Equal("img.png", result.ImageUrl);
        }

        [Fact]
        public void GeneralProjectCreator_Create_SinOpcionales_ValoresNulos()
        {
            // Arrange
            var creator = new GeneralProjectCreator();

            // Act
            var result = creator.Create("Title", "event-1");

            // Assert
            Assert.IsType<GeneralProject>(result);
            Assert.Null(result.OwnerId);
            Assert.Null(result.Description);
            Assert.Null(result.ImageUrl);
        }

        [Fact]
        public void GeneralProjectCreator_Create_ProjectType_EsGeneral()
        {
            // Arrange
            var creator = new GeneralProjectCreator();

            // Act
            var result = creator.Create("Title", "event-1");

            // Assert
            Assert.Equal("General", result.ProjectType());
        }

        #endregion

        #region SustainabilityProjectCreator

        [Fact]
        public void SustainabilityProjectCreator_Create_RetornaSustainabilityProject()
        {
            // Arrange
            var creator = new SustainabilityProjectCreator();

            // Act
            var result = creator.Create("Title", "event-1", "owner-1", "Desc", "img.png");

            // Assert
            Assert.IsType<SustainabilityProject>(result);
            Assert.Equal("Title", result.Title);
            Assert.Equal("event-1", result.EventId);
            Assert.Equal("owner-1", result.OwnerId);
            Assert.Equal("Desc", result.Description);
            Assert.Equal("img.png", result.ImageUrl);
        }

        [Fact]
        public void SustainabilityProjectCreator_Create_SinOpcionales_ValoresNulos()
        {
            // Arrange
            var creator = new SustainabilityProjectCreator();

            // Act
            var result = creator.Create("Title", "event-1");

            // Assert
            Assert.IsType<SustainabilityProject>(result);
            Assert.Null(result.OwnerId);
            Assert.Null(result.Description);
            Assert.Null(result.ImageUrl);
        }

        [Fact]
        public void SustainabilityProjectCreator_Create_ProjectType_EsSustainability()
        {
            // Arrange
            var creator = new SustainabilityProjectCreator();

            // Act
            var result = creator.Create("Title", "event-1");

            // Assert
            Assert.Equal("Sustainability", result.ProjectType());
        }

        #endregion

        #region ModalityEventCreator

        [Fact]
        public void ModalityEventCreator_Create_RetornaModalityEvent()
        {
            // Arrange
            var creator = new ModalityEventCreator("MiModality");
            var start = new DateTime(2026, 6, 1);
            var end = new DateTime(2026, 6, 30);

            // Act
            var result = creator.Create("Evento", 10, start, end, "Desc", "img.png");

            // Assert
            Assert.IsType<ModalityEvent>(result);
            Assert.Equal("Evento", result.Name);
            Assert.Equal(10, result.MaxProjects);
            Assert.Equal(start, result.StartDate);
            Assert.Equal(end, result.EndDate);
            Assert.Equal("Desc", result.Description);
            Assert.Equal("img.png", result.ImageUrl);
            Assert.Equal("MiModality", result.Modality());
        }

        [Fact]
        public void ModalityEventCreator_Create_SinOpcionales_DescripcionNula()
        {
            // Arrange
            var creator = new ModalityEventCreator("Mod");
            var start = new DateTime(2026, 6, 1);
            var end = new DateTime(2026, 6, 30);

            // Act
            var result = creator.Create("Evento", 5, start, end);

            // Assert
            Assert.IsType<ModalityEvent>(result);
            Assert.Null(result.Description);
            Assert.Null(result.ImageUrl);
        }

        [Fact]
        public void ModalityEventCreator_SetModality_CambiaModalidad()
        {
            // Arrange
            var creator = new ModalityEventCreator("Original");
            var evento = creator.Create("E", 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

            // Act
            evento.SetModality("Nueva");

            // Assert
            Assert.Equal("Nueva", evento.Modality());
        }

        #endregion

        #region EventCreator.BuildSummary

        [Fact]
        public void EventCreator_BuildSummary_RetornaResumenEsperado()
        {
            // Arrange
            var creator = new ModalityEventCreator("TestMod");
            var start = new DateTime(2026, 6, 1);
            var end = new DateTime(2026, 6, 30);

            // Act
            var summary = creator.BuildSummary("MiEvento", 15, start, end);

            // Assert
            Assert.Contains("MiEvento", summary);
            Assert.Contains("TestMod", summary);
            Assert.Contains("15", summary);
        }

        #endregion
    }
}
