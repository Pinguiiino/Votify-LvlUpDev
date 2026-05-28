using Xunit;
using Moq;
using Votify.Domain.Facade;
using Votify.Domain.VoteFolder;
using Votify.Domain.VoteFolder.Strategies;
using Votify.Domain.CategoryFolder;
using Votify.Domain.ProjectFolder;
using Votify.Domain.Factory;

namespace Votify.Tests
{
    public class VotingFacadeTests
    {
        private readonly Mock<IVotingSessionRepository> _sessionRepoMock;
        private readonly Mock<IVoteRepository> _voteRepoMock;
        private readonly Mock<ICategoryRepository> _categoryRepoMock;
        private readonly Mock<IProjectRepository> _projectRepoMock;
        private readonly VotingStrategyResolver _strategyResolver;
        private readonly VoteCreatorFactory _voteCreatorFactory;
        private readonly VotingFacade _facade;

        public VotingFacadeTests()
        {
            _sessionRepoMock = new Mock<IVotingSessionRepository>();
            _voteRepoMock = new Mock<IVoteRepository>();
            _categoryRepoMock = new Mock<ICategoryRepository>();
            _projectRepoMock = new Mock<IProjectRepository>();

            _voteCreatorFactory = new VoteCreatorFactory(
                new VoteCreator[] { new PublicVoteCreator(), new ExpertVoteCreator() });

            var topN = new TopNVotingStrategy(_voteRepoMock.Object, _voteCreatorFactory);
            var pointDist = new PointDistributionVotingStrategy(_voteRepoMock.Object, _voteCreatorFactory);
            var weighted = new WeightedVotingStrategy(Mock.Of<IWeightedVoteRepository>());
            _strategyResolver = new VotingStrategyResolver(
                new IVotingStrategy[] { topN, pointDist, weighted });

            _facade = new VotingFacade(
                _sessionRepoMock.Object,
                _voteRepoMock.Object,
                _categoryRepoMock.Object,
                _projectRepoMock.Object,
                _strategyResolver,
                _voteCreatorFactory);
        }

        private static VotingSession CreateOpenSession(
            string id = "session-1",
            string categoryId = "cat-1",
            EvaluationType evalType = EvaluationType.TopN)
        {
            var session = new VotingSession(
                categoryId, "Sesión", VoterType.Public, evalType,
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(5))
            {
                Id = id,
                TopN = 3
            };
            session.Abrir();
            return session;
        }

        #region SubmitVoteAsync - Sesion

        [Fact]
        public async Task SubmitVoteAsync_SesionNoExiste_LanzaExcepcion()
        {
            // Arrange
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-999"))
                .ReturnsAsync((VotingSession?)null);

            var request = new VoteRequestDto("user-1", "cat-1", "session-999",
                RankedProjects: new List<RankedProjectDto>
                {
                    new RankedProjectDto("proj-1", 1, null)
                });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _facade.SubmitVoteAsync(request));
            Assert.Contains("no existe", ex.Message);
        }

        [Fact]
        public async Task SubmitVoteAsync_SesionCerrada_LanzaExcepcion()
        {
            // Arrange
            var session = CreateOpenSession();
            session.Cerrar();
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var request = new VoteRequestDto("user-1", "cat-1", "session-1",
                RankedProjects: new List<RankedProjectDto>
                {
                    new RankedProjectDto("proj-1", 1, null)
                });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _facade.SubmitVoteAsync(request));
            Assert.Contains("no está abierta", ex.Message);
        }

        [Fact]
        public async Task SubmitVoteAsync_SesionCategoriaDistinta_LanzaExcepcion()
        {
            // Arrange
            var session = CreateOpenSession(categoryId: "cat-1");
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var request = new VoteRequestDto("user-1", "cat-otra", "session-1",
                RankedProjects: new List<RankedProjectDto>
                {
                    new RankedProjectDto("proj-1", 1, null)
                });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _facade.SubmitVoteAsync(request));
            Assert.Contains("no pertenece", ex.Message);
        }

        #endregion

        #region SubmitVoteAsync - SelfVoting

        [Fact]
        public async Task SubmitVoteAsync_SelfVotingNoPermitido_LanzaExcepcion()
        {
            // Arrange
            var session = CreateOpenSession();
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = false };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            var project = new GeneralProject("P1", "event-1", "user-1") { Id = "proj-1" };
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);

            var request = new VoteRequestDto("user-1", "cat-1", "session-1",
                RankedProjects: new List<RankedProjectDto>
                {
                    new RankedProjectDto("proj-1", 1, null)
                });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _facade.SubmitVoteAsync(request));
            Assert.Contains("No puedes votar tu propio proyecto", ex.Message);
        }

        #endregion

        #region SubmitVoteAsync - Happy Path

        [Fact]
        public async Task SubmitVoteAsync_DatosValidos_RetornaExito()
        {
            // Arrange
            var session = CreateOpenSession();
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = true };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            var request = new VoteRequestDto("user-1", "cat-1", "session-1",
                RankedProjects: new List<RankedProjectDto>
                {
                    new RankedProjectDto("proj-1", 1, "Bueno")
                });

            // Act
            var result = await _facade.SubmitVoteAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Voto registrado correctamente.", result.Message);
            Assert.Equal("session-1", result.SessionId);
            Assert.Equal("TopN", result.EvaluationType);
        }

        [Fact]
        public async Task SubmitVoteAsync_DatosValidos_PersisteVoto()
        {
            // Arrange
            var session = CreateOpenSession();
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = true };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            var request = new VoteRequestDto("user-1", "cat-1", "session-1",
                RankedProjects: new List<RankedProjectDto>
                {
                    new RankedProjectDto("proj-1", 1, null)
                });

            // Act
            await _facade.SubmitVoteAsync(request);

            // Assert
            _voteRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<List<Vote>>()), Times.Once);
            _voteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitVoteAsync_MultiplesProyectos_PersisteTodos()
        {
            // Arrange
            var session = CreateOpenSession();
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = true };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            var request = new VoteRequestDto("user-1", "cat-1", "session-1",
                RankedProjects: new List<RankedProjectDto>
                {
                    new RankedProjectDto("proj-1", 1, null),
                    new RankedProjectDto("proj-2", 2, null),
                    new RankedProjectDto("proj-3", 3, null)
                });

            // Act
            var result = await _facade.SubmitVoteAsync(request);

            // Assert
            Assert.True(result.Success);
            _voteRepoMock.Verify(
                r => r.AddRangeAsync(It.Is<List<Vote>>(v => v.Count == 3)),
                Times.Once);
        }

        #endregion

        #region SubmitVoteAsync - PointDistribution

        [Fact]
        public async Task SubmitVoteAsync_PointDistribution_UsaEstrategiaCorrecta()
        {
            // Arrange
            var session = CreateOpenSession(evalType: EvaluationType.PointDistribution);
            session.PointsPerVoter = 100;
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = true };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            var request = new VoteRequestDto("user-1", "cat-1", "session-1",
                PointAllocations: new List<PointAllocationDto>
                {
                    new PointAllocationDto("proj-1", 30, null)
                });

            // Act
            var result = await _facade.SubmitVoteAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("PointDistribution", result.EvaluationType);
        }

        #endregion

        #region SubmitVoteAsync - Validación Estrategia

        [Fact]
        public async Task SubmitVoteAsync_EstrategiaRechaza_LanzaExcepcion()
        {
            // Arrange
            var session = CreateOpenSession(evalType: EvaluationType.TopN);
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = true };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            var request = new VoteRequestDto("user-1", "cat-1", "session-1",
                RankedProjects: new List<RankedProjectDto>());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _facade.SubmitVoteAsync(request));
            Assert.Contains("No se han proporcionado proyectos", ex.Message);
        }

        #endregion
    }
}
