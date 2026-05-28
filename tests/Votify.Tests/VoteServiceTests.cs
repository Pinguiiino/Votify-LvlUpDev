using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Votify.Domain.VoteFolder;
using Votify.Domain.VoteFolder.Strategies;
using Votify.Domain.CategoryFolder;
using Votify.Domain.ProjectFolder;
using Votify.Domain.Factory;

namespace Votify.Tests
{
    public class VoteServiceTests
    {
        private readonly Mock<IVoteRepository> _voteRepoMock;
        private readonly Mock<IVotingSessionRepository> _sessionRepoMock;
        private readonly Mock<ICategoryRepository> _categoryRepoMock;
        private readonly Mock<IProjectRepository> _projectRepoMock;
        private readonly Mock<IWeightedVoteRepository> _weightedRepoMock;
        private readonly VotingStrategyResolver _strategyResolver;
        private readonly VoteCreatorFactory _voteCreatorFactory;
        private readonly VoteService _service;

        public VoteServiceTests()
        {
            _voteRepoMock = new Mock<IVoteRepository>();
            _sessionRepoMock = new Mock<IVotingSessionRepository>();
            _categoryRepoMock = new Mock<ICategoryRepository>();
            _projectRepoMock = new Mock<IProjectRepository>();
            _weightedRepoMock = new Mock<IWeightedVoteRepository>();

            _voteCreatorFactory = new VoteCreatorFactory(
                new VoteCreator[] { new PublicVoteCreator(), new ExpertVoteCreator() });

            var topN = new TopNVotingStrategy(_voteRepoMock.Object, _voteCreatorFactory);
            var pointDist = new PointDistributionVotingStrategy(_voteRepoMock.Object, _voteCreatorFactory);
            var weighted = new WeightedVotingStrategy(_weightedRepoMock.Object);
            _strategyResolver = new VotingStrategyResolver(
                new IVotingStrategy[] { topN, pointDist, weighted });

            _service = new VoteService(
                _voteRepoMock.Object,
                _sessionRepoMock.Object,
                _categoryRepoMock.Object,
                _projectRepoMock.Object,
                _weightedRepoMock.Object,
                _strategyResolver,
                _voteCreatorFactory);
        }

        private static VotingSession CreateSession(
            string id = "session-1",
            string categoryId = "cat-1",
            EvaluationType evalType = EvaluationType.TopN,
            VoterType voterType = VoterType.Public,
            int? topN = 3,
            bool isManuallyAdjusted = false,
            string? manualStatus = null)
        {
            var session = new VotingSession(
                categoryId, "Sesión", voterType, evalType,
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(5))
            {
                Id = id,
                TopN = topN,
                IsManuallyAdjusted = isManuallyAdjusted,
                ManualStatus = manualStatus
            };
            if (manualStatus != null)
                session.RestaurarEstado();
            return session;
        }

        #region CastVotesByStrategyAsync

        [Fact]
        public async Task CastVotesByStrategyAsync_SesionNoExiste_LanzaExcepcion()
        {
            // Arrange
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-999"))
                .ReturnsAsync((VotingSession?)null);

            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                RankedProjects = new List<RankedProjectInput>
                {
                    new RankedProjectInput("p1", 1, null)
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CastVotesByStrategyAsync("session-999", input));
            Assert.Contains("no existe", ex.Message);
        }

        [Fact]
        public async Task CastVotesByStrategyAsync_SesionNoAbierta_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession(manualStatus: "closed");
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                RankedProjects = new List<RankedProjectInput>
                {
                    new RankedProjectInput("p1", 1, null)
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CastVotesByStrategyAsync("session-1", input));
            Assert.Contains("no está abierta", ex.Message);
        }

        [Fact]
        public async Task CastVotesByStrategyAsync_SesionCategoriaDistinta_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession(categoryId: "cat-1");
            session.Abrir();
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-otra",
                RankedProjects = new List<RankedProjectInput>
                {
                    new RankedProjectInput("p1", 1, null)
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CastVotesByStrategyAsync("session-1", input));
            Assert.Contains("no pertenece", ex.Message);
        }

        [Fact]
        public async Task CastVotesByStrategyAsync_SelfVotingNoPermitido_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession();
            session.Abrir();
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = false };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            var project = new GeneralProject("P1", "event-1", "user-1") { Id = "p1" };
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("p1"))
                .ReturnsAsync(project);

            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                RankedProjects = new List<RankedProjectInput>
                {
                    new RankedProjectInput("p1", 1, null)
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CastVotesByStrategyAsync("session-1", input));
            Assert.Contains("No puedes votar tu propio proyecto", ex.Message);
        }

        [Fact]
        public async Task CastVotesByStrategyAsync_DatosValidos_ExecutaEstrategia()
        {
            // Arrange
            var session = CreateSession();
            session.Abrir();
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = true };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                RankedProjects = new List<RankedProjectInput>
                {
                    new RankedProjectInput("proj-1", 1, null)
                }
            };

            // Act
            await _service.CastVotesByStrategyAsync("session-1", input);

            // Assert
            _voteRepoMock.Verify(r => r.RemoveByUserInCategoryAsync("user-1", "cat-1", "session-1"), Times.Once);
            _voteRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<List<Vote>>()), Times.Once);
            _voteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region CastVoteAsync

        [Fact]
        public async Task CastVoteAsync_SinSesionAbierta_LanzaExcepcion()
        {
            // Arrange
            _sessionRepoMock
                .Setup(r => r.GetActiveByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession>());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CastVoteAsync("proj-1", "cat-1", "event-1", "user-1", 1));
            Assert.Contains("No hay una sesión", ex.Message);
        }

        [Fact]
        public async Task CastVoteAsync_CategoriaNoExiste_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession();
            session.Abrir();
            _sessionRepoMock
                .Setup(r => r.GetActiveByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession> { session });
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync((Category?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CastVoteAsync("proj-1", "cat-1", "event-1", "user-1", 1));
            Assert.Contains("Categoría no encontrada", ex.Message);
        }

        [Fact]
        public async Task CastVoteAsync_SelfVotingNoPermitido_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession();
            session.Abrir();
            _sessionRepoMock
                .Setup(r => r.GetActiveByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession> { session });

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = false };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            var project = new GeneralProject("P1", "event-1", "user-1") { Id = "proj-1" };
            _projectRepoMock
                .Setup(r => r.GetByIdAsync("proj-1"))
                .ReturnsAsync(project);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CastVoteAsync("proj-1", "cat-1", "event-1", "user-1", 1));
            Assert.Contains("No puedes votar tu propio proyecto", ex.Message);
        }

        [Fact]
        public async Task CastVoteAsync_TopNLimiteAlcanzado_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession(topN: 2);
            session.Abrir();
            _sessionRepoMock
                .Setup(r => r.GetActiveByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession> { session });

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = true };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            _voteRepoMock
                .Setup(r => r.CountVotesByUserInCategoryAsync("user-1", "cat-1"))
                .ReturnsAsync(2);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CastVoteAsync("proj-1", "cat-1", "event-1", "user-1", 1));
            Assert.Contains("Límite de votos", ex.Message);
        }

        [Fact]
        public async Task CastVoteAsync_YaVotoPorProyecto_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession(topN: 5);
            session.Abrir();
            _sessionRepoMock
                .Setup(r => r.GetActiveByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession> { session });

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = true };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            _voteRepoMock
                .Setup(r => r.CountVotesByUserInCategoryAsync("user-1", "cat-1"))
                .ReturnsAsync(0);
            _voteRepoMock
                .Setup(r => r.HasUserVotedForProjectAsync("user-1", "proj-1"))
                .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CastVoteAsync("proj-1", "cat-1", "event-1", "user-1", 1));
            Assert.Contains("Ya has votado", ex.Message);
        }

        [Fact]
        public async Task CastVoteAsync_DatosValidos_AgregaVoto()
        {
            // Arrange
            var session = CreateSession(topN: 5);
            session.Abrir();
            _sessionRepoMock
                .Setup(r => r.GetActiveByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession> { session });

            var category = new Category("event-1", "Cat") { Id = "cat-1", AllowSelfVoting = true };
            _categoryRepoMock
                .Setup(r => r.GetByIdAsync("cat-1"))
                .ReturnsAsync(category);

            _voteRepoMock
                .Setup(r => r.CountVotesByUserInCategoryAsync("user-1", "cat-1"))
                .ReturnsAsync(0);
            _voteRepoMock
                .Setup(r => r.HasUserVotedForProjectAsync("user-1", "proj-1"))
                .ReturnsAsync(false);
            _voteRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Vote>()))
                .ReturnsAsync((Vote v) => v);

            // Act
            var result = await _service.CastVoteAsync("proj-1", "cat-1", "event-1", "user-1", 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("proj-1", result.VotedProjectId);
            Assert.Equal("user-1", result.UserId);
            _voteRepoMock.Verify(r => r.AddAsync(It.IsAny<Vote>()), Times.Once);
        }

        #endregion

        #region GetWeightedVotesByUserAndSessionAsync

        [Fact]
        public async Task GetWeightedVotesByUserAndSessionAsync_RetornaDtos()
        {
            // Arrange
            var weightedVotes = new List<WeightedVote>
            {
                new WeightedVote("session-1", "proj-1", "user-1", "cat-1", "Comentario")
                {
                    CriterionScores = new List<WeightedCriterionScore>
                    {
                        new WeightedCriterionScore("wv-1", "crit-1", 8.5, "Bueno")
                    }
                }
            };
            _weightedRepoMock
                .Setup(r => r.GetByUserAndSessionAsync("user-1", "session-1"))
                .ReturnsAsync(weightedVotes);

            // Act
            var result = await _service.GetWeightedVotesByUserAndSessionAsync("user-1", "session-1");

            // Assert
            var dto = Assert.Single(result);
            Assert.Equal("proj-1", dto.ProjectId);
            Assert.Equal("crit-1", dto.CriterionId);
            Assert.Equal(8.5, dto.Score);
            Assert.Equal("Comentario", dto.Comment);
            Assert.Equal("Bueno", dto.CriterionComment);
        }

        #endregion

        #region GetUserVotesAsync

        [Fact]
        public async Task GetUserVotesAsync_DelegaAlRepositorio()
        {
            // Arrange
            var votes = new List<Vote>
            {
                new PublicVote("s1", "p1", "u1", "c1", 1)
            };
            _voteRepoMock
                .Setup(r => r.GetByUserIdAndCategoryAsync("user-1", "cat-1"))
                .ReturnsAsync(votes);

            // Act
            var result = await _service.GetUserVotesAsync("user-1", "cat-1");

            // Assert
            Assert.Single(result);
        }

        #endregion

        #region GetPointDistributionVotesByUserAsync

        [Fact]
        public async Task GetPointDistributionVotesByUserAsync_FiltraPorSesion()
        {
            // Arrange
            var votes = new List<Vote>
            {
                new PublicVote("session-1", "proj-1", "user-1", "cat-1", 0) { Points = 30 },
                new PublicVote("session-2", "proj-2", "user-1", "cat-1", 0) { Points = 20 }
            };
            _voteRepoMock
                .Setup(r => r.GetByUserIdAndCategoryAsync("user-1", "cat-1"))
                .ReturnsAsync(votes);

            // Act
            var result = await _service.GetPointDistributionVotesByUserAsync(
                "user-1", "cat-1", "session-1");

            // Assert
            var dto = Assert.Single(result);
            Assert.Equal("proj-1", dto.ProjectId);
            Assert.Equal(30, dto.Points);
        }

        #endregion

        #region GetCommentsByProjectAsync

        [Fact]
        public async Task GetCommentsByProjectAsync_SinVotos_RetornaVacio()
        {
            // Arrange
            _voteRepoMock
                .Setup(r => r.GetByProjectAsync("proj-1"))
                .ReturnsAsync(new List<Vote>());
            _weightedRepoMock
                .Setup(r => r.GetByProjectAsync("proj-1"))
                .ReturnsAsync(new List<WeightedVote>());

            // Act
            var result = await _service.GetCommentsByProjectAsync("proj-1");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCommentsByProjectAsync_ConVotosNormales_RetornaComentarios()
        {
            // Arrange
            var session = CreateSession(evalType: EvaluationType.TopN);
            var votes = new List<Vote>
            {
                new PublicVote("session-1", "proj-1", "user-1", "cat-1", 1, "Buen proyecto")
            };
            _voteRepoMock
                .Setup(r => r.GetByProjectAsync("proj-1"))
                .ReturnsAsync(votes);
            _weightedRepoMock
                .Setup(r => r.GetByProjectAsync("proj-1"))
                .ReturnsAsync(new List<WeightedVote>());
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            // Act
            var result = await _service.GetCommentsByProjectAsync("proj-1");

            // Assert
            var comment = Assert.Single(result);
            Assert.Equal("Buen proyecto", comment.Comment);
            Assert.Equal(1, comment.TopPosition);
            Assert.Equal("Public", comment.VoteType);
        }

        [Fact]
        public async Task GetCommentsByProjectAsync_ConVotosPonderados_RetornaComentarios()
        {
            // Arrange
            var session = CreateSession(evalType: EvaluationType.WeightedScale, voterType: VoterType.Jury);
            var weightedVotes = new List<WeightedVote>
            {
                new WeightedVote("session-1", "proj-1", "user-1", "cat-1", "Excelente")
            };
            _voteRepoMock
                .Setup(r => r.GetByProjectAsync("proj-1"))
                .ReturnsAsync(new List<Vote>());
            _weightedRepoMock
                .Setup(r => r.GetByProjectAsync("proj-1"))
                .ReturnsAsync(weightedVotes);
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("session-1"))
                .ReturnsAsync(session);

            // Act
            var result = await _service.GetCommentsByProjectAsync("proj-1");

            // Assert
            var comment = Assert.Single(result);
            Assert.Equal("Excelente", comment.Comment);
            Assert.Equal("Expert", comment.VoteType);
        }

        [Fact]
        public async Task GetCommentsByProjectAsync_OrdenaExpertPrimero()
        {
            // Arrange
            var sessionJury = CreateSession(id: "s-jury", evalType: EvaluationType.WeightedScale, voterType: VoterType.Jury);
            var sessionPublic = CreateSession(id: "s-pub", evalType: EvaluationType.TopN, voterType: VoterType.Public);

            var votes = new List<Vote>
            {
                new PublicVote("s-pub", "proj-1", "user-1", "cat-1", 1, "Comentario Público")
            };
            var weightedVotes = new List<WeightedVote>
            {
                new WeightedVote("s-jury", "proj-1", "user-2", "cat-1", "Comentario Jurado")
            };

            _voteRepoMock
                .Setup(r => r.GetByProjectAsync("proj-1"))
                .ReturnsAsync(votes);
            _weightedRepoMock
                .Setup(r => r.GetByProjectAsync("proj-1"))
                .ReturnsAsync(weightedVotes);
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("s-jury"))
                .ReturnsAsync(sessionJury);
            _sessionRepoMock
                .Setup(r => r.GetByIdAsync("s-pub"))
                .ReturnsAsync(sessionPublic);

            // Act
            var result = await _service.GetCommentsByProjectAsync("proj-1");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Expert", result[0].VoteType);
            Assert.Equal("Public", result[1].VoteType);
        }

        #endregion

        #region GetVotesByUserInCategoryAsync

        [Fact]
        public async Task GetVotesByUserInCategoryAsync_RetornaProjectIds()
        {
            // Arrange
            var votes = new List<Vote>
            {
                new PublicVote("s1", "proj-1", "user-1", "cat-1", 1),
                new PublicVote("s1", "proj-2", "user-1", "cat-1", 2)
            };
            _voteRepoMock
                .Setup(r => r.GetByUserIdAndCategoryAsync("user-1", "cat-1"))
                .ReturnsAsync(votes);

            // Act
            var result = await _service.GetVotesByUserInCategoryAsync("user-1", "cat-1");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("proj-1", result);
            Assert.Contains("proj-2", result);
        }

        #endregion
    }
}
