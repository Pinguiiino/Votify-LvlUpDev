using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Votify.Domain.EventFolder;
using Votify.Domain.CategoryFolder;
using Votify.Domain.ProjectFolder;
using Votify.Domain.UserFolder;
using Votify.Domain.VoteFolder;

namespace Votify.Tests
{
    public class EventServiceTests
    {
        private readonly Mock<IEventRepository> _eventRepoMock;
        private readonly Mock<IProjectRepository> _projectRepoMock;
        private readonly Mock<ICategoryRepository> _categoryRepoMock;
        private readonly Mock<IVotingSessionRepository> _votingSessionRepoMock;
        private readonly Mock<IVoteRepository> _voteRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IWeightedVoteRepository> _weightedVoteRepoMock;
        private readonly EventService _service;

        public EventServiceTests()
        {
            _eventRepoMock = new Mock<IEventRepository>();
            _projectRepoMock = new Mock<IProjectRepository>();
            _categoryRepoMock = new Mock<ICategoryRepository>();
            _votingSessionRepoMock = new Mock<IVotingSessionRepository>();
            _voteRepoMock = new Mock<IVoteRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _weightedVoteRepoMock = new Mock<IWeightedVoteRepository>();
            _service = new EventService(
                _eventRepoMock.Object,
                _projectRepoMock.Object,
                _categoryRepoMock.Object,
                _votingSessionRepoMock.Object,
                _voteRepoMock.Object,
                _userRepoMock.Object,
                _weightedVoteRepoMock.Object);
        }

        private static ModalityEvent CreateValidEvent(
            string id = "event-1",
            string organizer = "org-1",
            string auditor = "aud-1",
            int maxProjects = 10,
            DateTime? startDate = null)
        {
            return new ModalityEvent(
                name: "Evento Test",
                maxProjects: maxProjects,
                startDate: startDate ?? DateTime.UtcNow.AddDays(10),
                endDate: DateTime.UtcNow.AddDays(20),
                modality: "TestModality")
            {
                Id = id,
                Organizer = organizer,
                Auditor = auditor,
                Participants = new List<GeneralUser>(),
                Public = new List<GeneralUser>()
            };
        }

        private static EventData CreateValidEventData(
            string name = "Evento Test",
            string modality = "TestModality",
            string auditorEmail = "auditor@test.com")
        {
            return new EventData
            {
                Name = name,
                Modality = modality,
                MaxProjects = 10,
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(20),
                AuditorEmail = auditorEmail
            };
        }

        #region GetAllAsync

        [Fact]
        public async Task GetAllAsync_DelegaAlRepositorio()
        {
            // Arrange
            var eventos = new List<Event> { CreateValidEvent() };
            _eventRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(eventos);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.Single(result);
            _eventRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        #endregion

        #region GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_Existe_RetornaEvento()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act
            var result = await _service.GetByIdAsync("event-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("event-1", result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NoExiste_RetornaNull()
        {
            // Arrange
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);

            // Act
            var result = await _service.GetByIdAsync("event-999");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetCategoriesWithDetailsAsync

        [Fact]
        public async Task GetCategoriesWithDetailsAsync_DelegaAlRepositorio()
        {
            // Arrange
            var categorias = new List<Category> { new Category("event-1", "Cat1") };
            _eventRepoMock
                .Setup(r => r.GetCategoriesWithDetailsAsync("event-1"))
                .ReturnsAsync(categorias);

            // Act
            var result = await _service.GetCategoriesWithDetailsAsync("event-1");

            // Assert
            Assert.Single(result);
            _eventRepoMock.Verify(r => r.GetCategoriesWithDetailsAsync("event-1"), Times.Once);
        }

        #endregion

        #region CreateEventAsync

        [Fact]
        public async Task CreateEventAsync_DatosValidos_RetornaEventoCreado()
        {
            // Arrange
            var data = CreateValidEventData();
            var auditor = new GeneralUser("Auditor", "auditor@test.com", "pass");
            _eventRepoMock
                .Setup(r => r.ExistsByNameAsync(data.Name))
                .ReturnsAsync(false);
            _userRepoMock
                .Setup(r => r.GetByEmailAsync(data.AuditorEmail))
                .ReturnsAsync(auditor);

            // Act
            var result = await _service.CreateEventAsync(data, "org-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("org-1", result.Organizer);
            Assert.Equal(auditor.Id, result.Auditor);
            Assert.NotNull(result.Participants);
            Assert.NotNull(result.Public);
            _eventRepoMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Once);
            _eventRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateEventAsync_NombreDuplicado_LanzaExcepcion()
        {
            // Arrange
            var data = CreateValidEventData(name: "Duplicado");
            _eventRepoMock
                .Setup(r => r.ExistsByNameAsync("Duplicado"))
                .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateEventAsync(data, "org-1"));
            Assert.Contains("Ya existe un evento con el nombre", ex.Message);
        }

        [Fact]
        public async Task CreateEventAsync_AuditorNoExiste_LanzaExcepcion()
        {
            // Arrange
            var data = CreateValidEventData(auditorEmail: "noexiste@test.com");
            _eventRepoMock
                .Setup(r => r.ExistsByNameAsync(data.Name))
                .ReturnsAsync(false);
            _userRepoMock
                .Setup(r => r.GetByEmailAsync("noexiste@test.com"))
                .ReturnsAsync((GeneralUser?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateEventAsync(data, "org-1"));
            Assert.Contains("No existe ninguna cuenta", ex.Message);
        }

        [Fact]
        public async Task CreateEventAsync_NombreEvento_Trimmed()
        {
            // Arrange
            var data = CreateValidEventData(name: "  Evento Limpio  ");
            var auditor = new GeneralUser("Auditor", "auditor@test.com", "pass");
            _eventRepoMock
                .Setup(r => r.ExistsByNameAsync("  Evento Limpio  "))
                .ReturnsAsync(false);
            _userRepoMock
                .Setup(r => r.GetByEmailAsync(data.AuditorEmail))
                .ReturnsAsync(auditor);

            // Act
            var result = await _service.CreateEventAsync(data, "org-1");

            // Assert
            Assert.NotNull(result);
            _eventRepoMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Once);
        }

        #endregion

        #region EnrollUserAsync

        [Fact]
        public async Task EnrollUserAsync_RolParticipant_AgregaAParticipants()
        {
            // Arrange
            var evento = CreateValidEvent();
            var user = new GeneralUser("User1", "user1@test.com", "pass") { Id = "user-1" };
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Act
            await _service.EnrollUserAsync("event-1", "user-1", "Participant");

            // Assert
            Assert.Single(evento.Participants!);
            Assert.Equal("user-1", evento.Participants[0].Id);
            _eventRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task EnrollUserAsync_RolPublic_AgregaAPublic()
        {
            // Arrange
            var evento = CreateValidEvent();
            var user = new GeneralUser("User1", "user1@test.com", "pass") { Id = "user-1" };
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Act
            await _service.EnrollUserAsync("event-1", "user-1", "Public");

            // Assert
            Assert.Single(evento.Public!);
            Assert.Equal("user-1", evento.Public[0].Id);
        }

        [Fact]
        public async Task EnrollUserAsync_EventoNoExiste_LanzaExcepcion()
        {
            // Arrange
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.EnrollUserAsync("event-999", "user-1", "Participant"));
            Assert.Equal("Evento no encontrado.", ex.Message);
        }

        [Fact]
        public async Task EnrollUserAsync_UsuarioNoExiste_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-999"))
                .ReturnsAsync((GeneralUser?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.EnrollUserAsync("event-1", "user-999", "Participant"));
            Assert.Contains("Usuario no encontrado", ex.Message);
        }

        [Fact]
        public async Task EnrollUserAsync_RolInvalido_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            var user = new GeneralUser("User1", "user1@test.com", "pass") { Id = "user-1" };
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.EnrollUserAsync("event-1", "user-1", "Admin"));
            Assert.Contains("Rol no válido", ex.Message);
        }

        [Fact]
        public async Task EnrollUserAsync_YaInscrito_NoDuplica()
        {
            // Arrange
            var user = new GeneralUser("User1", "user1@test.com", "pass") { Id = "user-1" };
            var evento = CreateValidEvent();
            evento.Participants = new List<GeneralUser> { user };
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Act
            await _service.EnrollUserAsync("event-1", "user-1", "Participant");

            // Assert
            Assert.Single(evento.Participants);
        }

        [Fact]
        public async Task EnrollUserAsync_YaEnPublic_NoDuplica()
        {
            // Arrange
            var user = new GeneralUser("User1", "user1@test.com", "pass") { Id = "user-1" };
            var evento = CreateValidEvent();
            evento.Public = new List<GeneralUser> { user };
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Act
            await _service.EnrollUserAsync("event-1", "user-1", "Public");

            // Assert
            Assert.Single(evento.Public);
        }

        #endregion

        #region GetDashboardStatsAsync

        [Fact]
        public async Task GetDashboardStatsAsync_EventoNoExiste_RetornaNull()
        {
            // Arrange
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);

            // Act
            var result = await _service.GetDashboardStatsAsync("event-999");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_SinVotos_RetornaDashboardVacio()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _projectRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Project>());
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category>());
            _votingSessionRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession>());
            _voteRepoMock
                .Setup(r => r.GetByProjectIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Vote>());
            _userRepoMock
                .Setup(r => r.CountAsync())
                .ReturnsAsync(0);

            // Act
            var result = await _service.GetDashboardStatsAsync("event-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalVotantes);
            Assert.Empty(result.Ranking);
            Assert.Empty(result.SessionProgresses);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ConVotosTopN_CalculaPuntos()
        {
            // Arrange
            var evento = CreateValidEvent();
            var proyecto = Mock.Of<Project>(p => p.Id == "proj-1" && p.Title == "Proyecto1");
            var categoria = new Category("event-1", "Cat1") { Id = "cat-1" };
            var sesion = new VotingSession(
                "cat-1", "Sesión1", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            { Id = "session-1", TopN = 3 };

            var voto = Mock.Of<Vote>(v =>
                v.VotedProjectId == "proj-1" &&
                v.CategoryId == "cat-1" &&
                v.VotingSessionId == "session-1" &&
                v.TopPosition == 1 &&
                v.UserId == "user-1");

            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _projectRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Project> { proyecto });
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category> { categoria });
            _votingSessionRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession> { sesion });
            _voteRepoMock
                .Setup(r => r.GetByProjectIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Vote> { voto });
            _userRepoMock
                .Setup(r => r.CountAsync())
                .ReturnsAsync(10);

            // Act
            var result = await _service.GetDashboardStatsAsync("event-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.TotalVotantes);
            Assert.Equal(1, result.VotosEmitidos);
            Assert.Single(result.Ranking);
            Assert.Equal(30, result.Ranking[0].Puntos);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ConVotosPointDistribution_CalculaPuntos()
        {
            // Arrange
            var evento = CreateValidEvent();
            var proyecto = Mock.Of<Project>(p => p.Id == "proj-1" && p.Title == "Proyecto1");
            var categoria = new Category("event-1", "Cat1") { Id = "cat-1" };
            var sesion = new VotingSession(
                "cat-1", "Sesión1", VoterType.Public, EvaluationType.PointDistribution,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            { Id = "session-1", PointsPerVoter = 100 };

            var voto = Mock.Of<Vote>(v =>
                v.VotedProjectId == "proj-1" &&
                v.CategoryId == "cat-1" &&
                v.VotingSessionId == "session-1" &&
                v.Points == 50 &&
                v.TopPosition == 1 &&
                v.UserId == "user-1");

            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _projectRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Project> { proyecto });
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category> { categoria });
            _votingSessionRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession> { sesion });
            _voteRepoMock
                .Setup(r => r.GetByProjectIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Vote> { voto });
            _userRepoMock
                .Setup(r => r.CountAsync())
                .ReturnsAsync(5);

            // Act
            var result = await _service.GetDashboardStatsAsync("event-1");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Ranking);
            Assert.Equal(50, result.Ranking[0].Puntos);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ConSesionesJurado_CuentaJurados()
        {
            // Arrange
            var evento = CreateValidEvent();
            evento.Participants = new List<GeneralUser>
            {
                new GeneralUser("P1", "p1@test.com", "pass") { Id = "p1" },
                new GeneralUser("P2", "p2@test.com", "pass") { Id = "p2" }
            };
            evento.Public = new List<GeneralUser>
            {
                new GeneralUser("Pub1", "pub1@test.com", "pass") { Id = "pub1" }
            };

            var sesion = new VotingSession(
                "cat-1", "Jurado", VoterType.Jury, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            {
                Id = "session-1",
                TopN = 3,
                JurorEmails = new List<string> { "j1@test.com", "j2@test.com", "j3@test.com" }
            };

            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _projectRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Project>());
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category>());
            _votingSessionRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession> { sesion });
            _voteRepoMock
                .Setup(r => r.GetByProjectIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Vote>());
            _userRepoMock
                .Setup(r => r.CountAsync())
                .ReturnsAsync(0);

            // Act
            var result = await _service.GetDashboardStatsAsync("event-1");

            // Assert
            Assert.NotNull(result);
            var sesionDto = Assert.Single(result.SessionProgresses);
            Assert.Equal(3, sesionDto.TotalVoters);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_SesionSinVotos_UniqueVotersEsCero()
        {
            // Arrange
            var evento = CreateValidEvent();
            var sesion = new VotingSession(
                "cat-1", "Pública", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            { Id = "session-1", TopN = 3 };

            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _projectRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Project>());
            _categoryRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<Category>());
            _votingSessionRepoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession> { sesion });
            _voteRepoMock
                .Setup(r => r.GetByProjectIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Vote>());
            _userRepoMock
                .Setup(r => r.CountAsync())
                .ReturnsAsync(0);

            // Act
            var result = await _service.GetDashboardStatsAsync("event-1");

            // Assert
            Assert.NotNull(result);
            var sesionDto = Assert.Single(result.SessionProgresses);
            Assert.Equal(0, sesionDto.UniqueVoters);
        }

        #endregion

        #region AssignAuditorAsync

        [Fact]
        public async Task AssignAuditorAsync_EventoExiste_ActualizaAuditor()
        {
            // Arrange
            var evento = CreateValidEvent();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act
            await _service.AssignAuditorAsync("event-1", "nuevo-auditor");

            // Assert
            Assert.Equal("nuevo-auditor", evento.Auditor);
            _eventRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AssignAuditorAsync_EventoNoExiste_LanzaExcepcion()
        {
            // Arrange
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AssignAuditorAsync("event-999", "aud-1"));
            Assert.Equal("Evento no encontrado.", ex.Message);
        }

        #endregion

        #region UpdateEventAsync

        [Fact]
        public async Task UpdateEventAsync_DatosValidos_ActualizaEvento()
        {
            // Arrange
            var evento = CreateValidEvent();
            var data = CreateValidEventData(name: "NombreActualizado");
            var auditor = new GeneralUser("NewAud", "newaud@test.com", "pass") { Id = "new-aud" };

            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _eventRepoMock
                .Setup(r => r.ExistsByNameAsync("NombreActualizado"))
                .ReturnsAsync(false);
            _userRepoMock
                .Setup(r => r.GetByEmailAsync(data.AuditorEmail))
                .ReturnsAsync(auditor);

            // Act
            var result = await _service.UpdateEventAsync("event-1", data);

            // Assert
            Assert.Equal("NombreActualizado", result.Name);
            Assert.Equal("new-aud", result.Auditor);
            _eventRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_EventoNoExiste_LanzaExcepcion()
        {
            // Arrange
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);
            var data = CreateValidEventData();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateEventAsync("event-999", data));
            Assert.Equal("Evento no encontrado.", ex.Message);
        }

        [Fact]
        public async Task UpdateEventAsync_EventoYaComenzo_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent(startDate: DateTime.UtcNow.AddDays(-5));
            var data = CreateValidEventData();
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateEventAsync("event-1", data));
            Assert.Contains("ya ha comenzado", ex.Message);
        }

        [Fact]
        public async Task UpdateEventAsync_NombreDuplicado_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            var data = CreateValidEventData(name: "Duplicado");
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _eventRepoMock
                .Setup(r => r.ExistsByNameAsync("Duplicado"))
                .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateEventAsync("event-1", data));
            Assert.Contains("Ya existe un evento con el nombre", ex.Message);
        }

        [Fact]
        public async Task UpdateEventAsync_MismoNombre_NoValidaDuplicado()
        {
            // Arrange
            var evento = CreateValidEvent();
            evento.Name = "MismoNombre";
            var data = CreateValidEventData(name: "MismoNombre");
            var auditor = new GeneralUser("Aud", "aud@test.com", "pass") { Id = "aud-1" };

            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _userRepoMock
                .Setup(r => r.GetByEmailAsync(data.AuditorEmail))
                .ReturnsAsync(auditor);

            // Act
            var result = await _service.UpdateEventAsync("event-1", data);

            // Assert
            _eventRepoMock.Verify(
                r => r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEventAsync_AuditorNoExiste_LanzaExcepcion()
        {
            // Arrange
            var evento = CreateValidEvent();
            var data = CreateValidEventData(auditorEmail: "noexiste@test.com");
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);
            _eventRepoMock
                .Setup(r => r.ExistsByNameAsync(data.Name))
                .ReturnsAsync(false);
            _userRepoMock
                .Setup(r => r.GetByEmailAsync("noexiste@test.com"))
                .ReturnsAsync((GeneralUser?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateEventAsync("event-1", data));
            Assert.Contains("No existe ninguna cuenta", ex.Message);
        }

        #endregion

        #region DeleteEventAsync

        [Fact]
        public async Task DeleteEventAsync_DatosValidos_EliminaEvento()
        {
            // Arrange
            var evento = CreateValidEvent(organizer: "org-1");
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act
            await _service.DeleteEventAsync("event-1", "org-1");

            // Assert
            _eventRepoMock.Verify(r => r.DeleteAsync("event-1"), Times.Once);
            _eventRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteEventAsync_EventoNoExiste_LanzaExcepcion()
        {
            // Arrange
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-999"))
                .ReturnsAsync((ModalityEvent?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.DeleteEventAsync("event-999", "org-1"));
            Assert.Equal("Evento no encontrado.", ex.Message);
        }

        [Fact]
        public async Task DeleteEventAsync_RequesterNoEsOrganizador_LanzaUnauthorized()
        {
            // Arrange
            var evento = CreateValidEvent(organizer: "org-real");
            _eventRepoMock
                .Setup(r => r.GetByIdAsync("event-1"))
                .ReturnsAsync(evento);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.DeleteEventAsync("event-1", "hacker"));
            Assert.Contains("organizador", ex.Message);
        }

        #endregion

        #region GetUserEmailByIdAsync

        [Fact]
        public async Task GetUserEmailByIdAsync_IdValido_RetornaEmail()
        {
            // Arrange
            var user = new GeneralUser("User1", "user1@test.com", "pass") { Id = "user-1" };
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-1"))
                .ReturnsAsync(user);

            // Act
            var result = await _service.GetUserEmailByIdAsync("user-1");

            // Assert
            Assert.Equal("user1@test.com", result);
        }

        [Fact]
        public async Task GetUserEmailByIdAsync_IdNulo_RetornaVacio()
        {
            // Act
            var result = await _service.GetUserEmailByIdAsync(null!);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetUserEmailByIdAsync_IdVacio_RetornaVacio()
        {
            // Act
            var result = await _service.GetUserEmailByIdAsync(string.Empty);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetUserEmailByIdAsync_UsuarioNoExiste_RetornaVacio()
        {
            // Arrange
            _userRepoMock
                .Setup(r => r.GetByIdAsync("user-999"))
                .ReturnsAsync((GeneralUser?)null);

            // Act
            var result = await _service.GetUserEmailByIdAsync("user-999");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        #endregion
    }
}
