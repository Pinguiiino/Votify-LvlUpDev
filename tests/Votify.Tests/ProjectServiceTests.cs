using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Votify.Domain.CategoryFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.ProjectFolder;
using Votify.Domain.UserFolder;
using Votify.Domain.VoteFolder;
using Xunit;

namespace Votify.Tests
{
    public class ProjectServiceTests
    {
        private readonly Mock<IProjectRepository> _projectRepoMock;
        private readonly Mock<IEventRepository> _eventRepoMock;
        private readonly Mock<ICategoryRepository> _categoryRepoMock;
        private readonly ProjectService _service;

        public ProjectServiceTests()
        {
            _projectRepoMock = new Mock<IProjectRepository>();
            _eventRepoMock = new Mock<IEventRepository>();
            _categoryRepoMock = new Mock<ICategoryRepository>();
            _service = new ProjectService(
                _projectRepoMock.Object,
                _eventRepoMock.Object,
                _categoryRepoMock.Object);
        }

        private static ModalityEvent CreateValidEvent(
            string id = "event-1",
            string organizer = "org-1",
            int maxProjects = 10,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            return new ModalityEvent(
                name: "Evento Test",
                maxProjects: maxProjects,
                startDate: startDate ?? DateTime.UtcNow.AddDays(-5),
                endDate: endDate ?? DateTime.UtcNow.AddDays(30),
                modality: "TestModality")
            {
                Id = id,
                Organizer = organizer,
                Participants = new List<GeneralUser>(),
                Public = new List<GeneralUser>()
            };
        }

        private static Category CreateValidCategory(
            string id = "cat-1",
            string eventId = "event-1",
            string name = "Categoría1")
        {
            return new Category(eventId, name) { Id = id };
        }

        private static GeneralProject CreateValidProject(
            string id = "proj-1",
            string eventId = "event-1",
            string title = "Proyecto1",
            string? ownerId = "owner-1")
        {
            return new GeneralProject(title, eventId, ownerId)
            {
                Id = id,
                ValidationStatus = ValidationStatus.Pending
            };
        }

        #region CreateProjectAsync

        [Fact]
        public async Task CreateProjectAsync_DatosValidos_RetornaProyectoCreado()
        {
            // Arrange
            var evento = CreateValidEvent();
            var categoria = CreateValidCategory();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category> { categoria });
            _projectRepoMock
                .Setup(r => r.TitleExistsInEventAsync("Proyecto1", "event-1"))
                .ReturnsAsync(false);

            var materials = new List<(MaterialType, string, string?)>
            {
                (MaterialType.Photo, "https://img.com/photo.jpg", "Foto")
            };

            // Act
            var result = await _service.CreateProjectAsync(
                "Proyecto1", "event-1", "Descripción", "General", "img.png",
                "owner-1", new List<string> { "cat-1" }, materials);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Proyecto1", result.Title);
            Assert.Equal("General", result.ProjectType());
            Assert.Equal(ValidationStatus.Pending, result.ValidationStatus);
            Assert.Single(result.Materials);
            Assert.Single(result.ProjectCategories);
            _projectRepoMock.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Once);
            _projectRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateProjectAsync_TipoAI_RetornaAiProject()
        {
            // Arrange
            var evento = CreateValidEvent();
            var categoria = CreateValidCategory();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category> { categoria });
            _projectRepoMock
                .Setup(r => r.TitleExistsInEventAsync("AI Proj", "event-1"))
                .ReturnsAsync(false);

            // Act
            var result = await _service.CreateProjectAsync(
                "AI Proj", "event-1", null, "AI", null,
                "owner-1", new List<string> { "cat-1" },
                new List<(MaterialType, string, string?)>());

            // Assert
            Assert.IsType<AiProject>(result);
        }

        [Fact]
        public async Task CreateProjectAsync_TipoSustainability_RetornaSustainabilityProject()
        {
            // Arrange
            var evento = CreateValidEvent();
            var categoria = CreateValidCategory();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category> { categoria });
            _projectRepoMock
                .Setup(r => r.TitleExistsInEventAsync("Sust", "event-1"))
                .ReturnsAsync(false);

            // Act
            var result = await _service.CreateProjectAsync(
                "Sust", "event-1", null, "Sustainability", null,
                "owner-1", new List<string> { "cat-1" },
                new List<(MaterialType, string, string?)>());

            // Assert
            Assert.IsType<SustainabilityProject>(result);
        }

        [Fact]
        public async Task CreateProjectAsync_EventoNoExiste_LanzaExcepcion()
        {
            // Arrange
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateProjectAsync(
                    "Title", "event-999", null, "General", null,
                    "owner-1", new List<string>(), new List<(MaterialType, string, string?)>()));
            Assert.Equal("Evento no encontrado.", ex.Message);
        }

        [Fact]
        public async Task CreateProjectAsync_CategoriaConVotacionAbierta_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            var sesionAbierta = new VotingSession(
                "cat-1", "Sesión", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(5))
            { Id = "session-1", TopN = 3, ManualStatus = "open" };

            var categoria = CreateValidCategory();
            categoria.VotingSessions = new List<VotingSession> { sesionAbierta };

            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category> { categoria });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateProjectAsync(
                    "Title", "event-1", null, "General", null,
                    "owner-1", new List<string> { "cat-1" },
                    new List<(MaterialType, string, string?)>()));
            Assert.Contains("La votación ya ha iniciado", ex.Message);
        }

        [Fact]
        public async Task CreateProjectAsync_CategoriaConSesionPausada_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            var sesionPausada = new VotingSession(
                "cat-1", "Sesión", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(5))
            { Id = "session-1", TopN = 3, ManualStatus = "paused" };

            var categoria = CreateValidCategory();
            categoria.VotingSessions = new List<VotingSession> { sesionPausada };

            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category> { categoria });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateProjectAsync(
                    "Title", "event-1", null, "General", null,
                    "owner-1", new List<string> { "cat-1" },
                    new List<(MaterialType, string, string?)>()));
            Assert.Contains("La votación ya ha iniciado", ex.Message);
        }

        [Fact]
        public async Task CreateProjectAsync_CategoriaConSesionCerrada_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            var sesionCerrada = new VotingSession(
                "cat-1", "Sesión", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-5))
            { Id = "session-1", TopN = 3, ManualStatus = "closed" };

            var categoria = CreateValidCategory();
            categoria.VotingSessions = new List<VotingSession> { sesionCerrada };

            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category> { categoria });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateProjectAsync(
                    "Title", "event-1", null, "General", null,
                    "owner-1", new List<string> { "cat-1" },
                    new List<(MaterialType, string, string?)>()));
            Assert.Contains("La votación ya ha iniciado", ex.Message);
        }

        [Fact]
        public async Task CreateProjectAsync_EventoNoComenzado_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent(startDate: DateTime.UtcNow.AddDays(10));
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category>());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateProjectAsync(
                    "Title", "event-1", null, "General", null,
                    "owner-1", new List<string>(), new List<(MaterialType, string, string?)>()));
            Assert.Contains("aún no ha comenzado", ex.Message);
        }

        [Fact]
        public async Task CreateProjectAsync_EventoFinalizado_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent(endDate: DateTime.UtcNow.AddDays(-1));
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category>());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateProjectAsync(
                    "Title", "event-1", null, "General", null,
                    "owner-1", new List<string>(), new List<(MaterialType, string, string?)>()));
            Assert.Contains("ha finalizado", ex.Message);
        }

        [Fact]
        public async Task CreateProjectAsync_TipoDesconocido_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category>());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateProjectAsync(
                    "Title", "event-1", null, "InvalidType", null,
                    "owner-1", new List<string>(), new List<(MaterialType, string, string?)>()));
            Assert.Contains("Tipo desconocido", ex.Message);
        }

        [Fact]
        public async Task CreateProjectAsync_TituloDuplicado_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category>());
            _projectRepoMock
                .Setup(r => r.TitleExistsInEventAsync("Duplicado", "event-1"))
                .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateProjectAsync(
                    "Duplicado", "event-1", null, "General", null,
                    "owner-1", new List<string>(), new List<(MaterialType, string, string?)>()));
            Assert.Contains("Ya existe un proyecto con el título", ex.Message);
        }

        [Fact]
        public async Task CreateProjectAsync_ConMateriales_AgregaMateriales()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category>());
            _projectRepoMock
                .Setup(r => r.TitleExistsInEventAsync("Title", "event-1"))
                .ReturnsAsync(false);

            var materials = new List<(MaterialType, string, string?)>
            {
                (MaterialType.Photo, "https://img.com/1.jpg", "Foto1"),
                (MaterialType.Video, "https://vid.com/1.mp4", "Video1")
            };

            // Act
            var result = await _service.CreateProjectAsync(
                "Title", "event-1", null, "General", null,
                "owner-1", new List<string>(), materials);

            // Assert
            Assert.Equal(2, result.Materials.Count);
            Assert.Equal(MaterialType.Photo, result.Materials[0].Type);
            Assert.Equal(MaterialType.Video, result.Materials[1].Type);
        }

        [Fact]
        public async Task CreateProjectAsync_ConCategorias_AgregaProjectCategories()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category>());
            _projectRepoMock
                .Setup(r => r.TitleExistsInEventAsync("Title", "event-1"))
                .ReturnsAsync(false);

            var categoryIds = new List<string> { "cat-1", "cat-2", "cat-3" };

            // Act
            var result = await _service.CreateProjectAsync(
                "Title", "event-1", null, "General", null,
                "owner-1", categoryIds, new List<(MaterialType, string, string?)>());

            // Assert
            Assert.Equal(3, result.ProjectCategories.Count);
        }

        #endregion

        #region UpdateProjectAsync

        [Fact]
        public async Task UpdateProjectAsync_DatosValidos_ActualizaProyecto()
        {
            // Arrange
            var project = CreateValidProject();
            var evento = CreateValidEvent();
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var materials = new List<(MaterialType, string, string?)>
            {
                (MaterialType.Document, "https://doc.com/file.pdf", null)
            };

            // Act
            var result = await _service.UpdateProjectAsync(
                "proj-1", "owner-1", "Nueva desc", "new.png", materials);

            // Assert
            Assert.Equal("Nueva desc", result.Description);
            Assert.Equal("new.png", result.ImageUrl);
            Assert.Single(result.Materials);
            _projectRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProjectAsync_ProyectoNoExiste_LanzaExcepcion()
        {
            // Arrange
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-999"))
                .ReturnsAsync((GeneralProject?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateProjectAsync(
                    "proj-999", "owner-1", null, null,
                    new List<(MaterialType, string, string?)>()));
            Assert.Equal("Proyecto no encontrado.", ex.Message);
        }

        [Fact]
        public async Task UpdateProjectAsync_OwnerDiferente_LanzaUnauthorized()
        {
            // Arrange
            var project = CreateValidProject(ownerId: "owner-real");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.UpdateProjectAsync(
                    "proj-1", "hacker", null, null,
                    new List<(MaterialType, string, string?)>()));
            Assert.Contains("No tienes permiso", ex.Message);
        }

        [Fact]
        public async Task UpdateProjectAsync_EventoFinalizado_LanzaInvalidOperation()
        {
            // Arrange
            var project = CreateValidProject();
            var evento = CreateValidEvent(endDate: DateTime.UtcNow.AddDays(-1));
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateProjectAsync(
                    "proj-1", "owner-1", null, null,
                    new List<(MaterialType, string, string?)>()));
            Assert.Contains("ha finalizado", ex.Message);
        }

        [Fact]
        public async Task UpdateProjectAsync_SinOwnerId_Permitido()
        {
            // Arrange
            var project = CreateValidProject(ownerId: null);
            var evento = CreateValidEvent();
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act
            var result = await _service.UpdateProjectAsync(
                "proj-1", "cualquiera", "Desc", null,
                new List<(MaterialType, string, string?)>());

            // Assert
            Assert.Equal("Desc", result.Description);
        }

        [Fact]
        public async Task UpdateProjectAsync_ReemplazaMateriales_LimpiaYAddsNuevos()
        {
            // Arrange
            var project = CreateValidProject();
            project.Materials.Add(new ProjectMaterial("proj-1", MaterialType.Photo, "old.jpg"));
            var evento = CreateValidEvent();
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            var materials = new List<(MaterialType, string, string?)>
            {
                (MaterialType.Video, "new.mp4", null)
            };

            // Act
            var result = await _service.UpdateProjectAsync(
                "proj-1", "owner-1", null, null, materials);

            // Assert
            Assert.Single(result.Materials);
            Assert.Equal(MaterialType.Video, result.Materials[0].Type);
        }

        #endregion

        #region ApproveAsync

        [Fact]
        public async Task ApproveAsync_DatosValidos_ApruebaProyecto()
        {
            // Arrange
            var project = CreateValidProject();
            var evento = CreateValidEvent(organizer: "org-1");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act
            await _service.ApproveAsync("proj-1", "org-1");

            // Assert
            Assert.Equal(ValidationStatus.Approved, project.ValidationStatus);
            Assert.Null(project.RejectionReason);
            _projectRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ApproveAsync_ProyectoNoExiste_LanzaExcepcion()
        {
            // Arrange
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-999"))
                .ReturnsAsync((GeneralProject?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ApproveAsync("proj-999", "org-1"));
            Assert.Equal("Proyecto no encontrado.", ex.Message);
        }

        [Fact]
        public async Task ApproveAsync_EventoNoExiste_LanzaExcepcion()
        {
            // Arrange
            var project = CreateValidProject();
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync((ModalityEvent?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ApproveAsync("proj-1", "org-1"));
            Assert.Equal("Evento no encontrado.", ex.Message);
        }

        [Fact]
        public async Task ApproveAsync_RequesterNoEsOrganizador_LanzaUnauthorized()
        {
            // Arrange
            var project = CreateValidProject();
            var evento = CreateValidEvent(organizer: "org-real");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.ApproveAsync("proj-1", "hacker"));
            Assert.Contains("Solo el organizador", ex.Message);
        }

        [Fact]
        public async Task ApproveAsync_YaAprobado_LanzaInvalidOperation()
        {
            // Arrange
            var project = CreateValidProject();
            project.ValidationStatus = ValidationStatus.Approved;
            var evento = CreateValidEvent(organizer: "org-1");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAsync("proj-1", "org-1"));
            Assert.Contains("ya ha sido validado", ex.Message);
        }

        [Fact]
        public async Task ApproveAsync_YaRechazado_LanzaInvalidOperation()
        {
            // Arrange
            var project = CreateValidProject();
            project.ValidationStatus = ValidationStatus.Rejected;
            var evento = CreateValidEvent(organizer: "org-1");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAsync("proj-1", "org-1"));
            Assert.Contains("ya ha sido validado", ex.Message);
        }

        #endregion

        #region RejectAsync

        [Fact]
        public async Task RejectAsync_ConRazon_RechazaProyecto()
        {
            // Arrange
            var project = CreateValidProject();
            var evento = CreateValidEvent(organizer: "org-1");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act
            await _service.RejectAsync("proj-1", "org-1", "No cumple requisitos");

            // Assert
            Assert.Equal(ValidationStatus.Rejected, project.ValidationStatus);
            Assert.Equal("No cumple requisitos", project.RejectionReason);
        }

        [Fact]
        public async Task RejectAsync_SinRazon_RechazaConReasonNull()
        {
            // Arrange
            var project = CreateValidProject();
            var evento = CreateValidEvent(organizer: "org-1");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act
            await _service.RejectAsync("proj-1", "org-1", null);

            // Assert
            Assert.Equal(ValidationStatus.Rejected, project.ValidationStatus);
            Assert.Null(project.RejectionReason);
        }

        [Fact]
        public async Task RejectAsync_RazonVacia_RechazaConReasonNull()
        {
            // Arrange
            var project = CreateValidProject();
            var evento = CreateValidEvent(organizer: "org-1");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act
            await _service.RejectAsync("proj-1", "org-1", "   ");

            // Assert
            Assert.Null(project.RejectionReason);
        }

        [Fact]
        public async Task RejectAsync_YaAprobado_LanzaInvalidOperation()
        {
            // Arrange
            var project = CreateValidProject();
            project.ValidationStatus = ValidationStatus.Approved;
            var evento = CreateValidEvent(organizer: "org-1");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RejectAsync("proj-1", "org-1", "reason"));
            Assert.Contains("ya ha sido validado", ex.Message);
        }

        [Fact]
        public async Task RejectAsync_RequesterNoEsOrganizador_LanzaUnauthorized()
        {
            // Arrange
            var project = CreateValidProject();
            var evento = CreateValidEvent(organizer: "org-real");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.RejectAsync("proj-1", "hacker", "reason"));
            Assert.Contains("Solo el organizador", ex.Message);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_DatosValidos_EliminaProyecto()
        {
            // Arrange
            var project = CreateValidProject(ownerId: "owner-1");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);

            // Act
            await _service.DeleteAsync("proj-1", "owner-1");

            // Assert
            _projectRepoMock.Verify(r => r.DeleteAsync("proj-1"), Times.Once);
            _projectRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ProyectoNoExiste_LanzaExcepcion()
        {
            // Arrange
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-999"))
                .ReturnsAsync((GeneralProject?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.DeleteAsync("proj-999", "owner-1"));
            Assert.Equal("Proyecto no encontrado.", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_OwnerDiferente_LanzaUnauthorized()
        {
            // Arrange
            var project = CreateValidProject(ownerId: "owner-real");
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.DeleteAsync("proj-1", "hacker"));
            Assert.Contains("Solo el creador", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_SinOwnerId_Permitido()
        {
            // Arrange
            var project = CreateValidProject(ownerId: null);
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);

            // Act
            await _service.DeleteAsync("proj-1", "cualquiera");

            // Assert
            _projectRepoMock.Verify(r => r.DeleteAsync("proj-1"), Times.Once);
        }

        #endregion

        #region GetResultsAsync

        [Fact]
        public async Task GetResultsAsync_ProyectoNoExiste_RetornaNull()
        {
            // Arrange
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-999"))
                .ReturnsAsync((GeneralProject?)null);

            // Act
            var result = await _service.GetResultsAsync("proj-999", null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetResultsAsync_EventoNoExiste_RetornaNull()
        {
            // Arrange
            var project = CreateValidProject();
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync((ModalityEvent?)null);

            // Act
            var result = await _service.GetResultsAsync("proj-1", null!);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetProjectTypes

        [Fact]
        public void GetProjectTypes_RetornaLosTresTipos()
        {
            // Act
            var result = _service.GetProjectTypes();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains("AI", result);
            Assert.Contains("Sustainability", result);
            Assert.Contains("General", result);
        }

        #endregion

        #region GetMaterialTypes

        [Fact]
        public void GetMaterialTypes_RetornaTodosLosTipos()
        {
            // Act
            var result = _service.GetMaterialTypes();

            // Assert
            Assert.Contains("Photo", result);
            Assert.Contains("Video", result);
            Assert.Contains("Document", result);
            Assert.Contains("Audio", result);
            Assert.Contains("Other", result);
        }

        #endregion

        #region Delegation methods

        [Fact]
        public async Task GetByIdAsync_DelegaAlRepositorio()
        {
            // Arrange
            var project = CreateValidProject();
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);

            // Act
            var result = await _service.GetByIdAsync("proj-1");

            // Assert
            Assert.NotNull(result);
            _projectRepoMock.Verify(r => r.GetByIdAsync("proj-1"), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_DelegaAlRepositorio()
        {
            // Arrange
            _projectRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Project>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByCategoryAsync_DelegaAlRepositorio()
        {
            // Arrange
            _projectRepoMock
                .Setup(r => r.GetByCategoryAsync("cat-1"))
                .ReturnsAsync(new List<Project>());

            // Act
            var result = await _service.GetByCategoryAsync("cat-1");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByOwnerAsync_DelegaAlRepositorio()
        {
            // Arrange
            _projectRepoMock
                .Setup(r => r.GetByOwnerAsync("owner-1"))
                .ReturnsAsync(new List<Project>());

            // Act
            var result = await _service.GetByOwnerAsync("owner-1");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPendingByEventAsync_DelegaAlRepositorio()
        {
            // Arrange
            _projectRepoMock
                .Setup(r => r.GetPendingByEventAsync("event-1"))
                .ReturnsAsync(new List<Project>());

            // Act
            var result = await _service.GetPendingByEventAsync("event-1");

            // Assert
            Assert.Empty(result);
        }

        #endregion
    }
}
