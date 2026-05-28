using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Votify.Domain.VoteFolder;
using Votify.Domain.VoteFolder.Strategies;
using Votify.Domain.Factory;

namespace Votify.Tests
{
    public class PointDistributionVotingStrategyTests
    {
        private readonly Mock<IVoteRepository> _voteRepoMock;
        private readonly VoteCreatorFactory _voteCreatorFactory;
        private readonly PointDistributionVotingStrategy _strategy;

        public PointDistributionVotingStrategyTests()
        {
            _voteRepoMock = new Mock<IVoteRepository>();
            _voteCreatorFactory = new VoteCreatorFactory(
                new VoteCreator[] { new PublicVoteCreator(), new ExpertVoteCreator() });
            _strategy = new PointDistributionVotingStrategy(_voteRepoMock.Object, _voteCreatorFactory);
        }

        private static VotingSession CreateSession(int pointsPerVoter = 100, int? maxPointsPerProject = null, bool requireComments = false)
        {
            return new VotingSession(
                "cat-1", "Sesión Puntos", VoterType.Public, EvaluationType.PointDistribution,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            {
                Id = "session-1",
                PointsPerVoter = pointsPerVoter,
                MaxPointsPerProject = maxPointsPerProject,
                RequireComments = requireComments
            };
        }

        #region ValidateAsync

        [Fact]
        public async Task ValidateAsync_SinAsignaciones_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                PointAllocations = new List<PointAllocationInput>()
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input));
            Assert.Contains("al menos un proyecto", ex.Message);
        }

        [Fact]
        public async Task ValidateAsync_TodasCeroPuntos_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                PointAllocations = new List<PointAllocationInput>
                {
                    new PointAllocationInput("p1", 0, null),
                    new PointAllocationInput("p2", 0, null)
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input));
            Assert.Contains("al menos un proyecto", ex.Message);
        }

        [Fact]
        public async Task ValidateAsync_SuperaPresupuesto_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession(pointsPerVoter: 50);
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                PointAllocations = new List<PointAllocationInput>
                {
                    new PointAllocationInput("p1", 30, null),
                    new PointAllocationInput("p2", 30, null)
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input));
            Assert.Contains("50 puntos", ex.Message);
        }

        [Fact]
        public async Task ValidateAsync_ProyectoSuperaMaxPorProyecto_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession(pointsPerVoter: 100, maxPointsPerProject: 30);
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                PointAllocations = new List<PointAllocationInput>
                {
                    new PointAllocationInput("p1", 50, null)
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input));
            Assert.Contains("30 puntos", ex.Message);
        }

        [Fact]
        public async Task ValidateAsync_ComentarioObligatorioFalta_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession(requireComments: true);
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                PointAllocations = new List<PointAllocationInput>
                {
                    new PointAllocationInput("p1", 10, "Bueno"),
                    new PointAllocationInput("p2", 10, "  ")
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input));
            Assert.Contains("comentario es obligatorio", ex.Message);
        }

        [Fact]
        public async Task ValidateAsync_DatosValidos_NoLanzaExcepcion()
        {
            // Arrange
            var session = CreateSession(pointsPerVoter: 100, maxPointsPerProject: 50);
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                PointAllocations = new List<PointAllocationInput>
                {
                    new PointAllocationInput("p1", 30, null),
                    new PointAllocationInput("p2", 20, null)
                }
            };

            // Act
            var ex = await Record.ExceptionAsync(() => _strategy.ValidateAsync(session, input));

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task ValidateAsync_ExactamenteEnLimite_NoLanzaExcepcion()
        {
            // Arrange
            var session = CreateSession(pointsPerVoter: 50, maxPointsPerProject: 50);
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                PointAllocations = new List<PointAllocationInput>
                {
                    new PointAllocationInput("p1", 50, null)
                }
            };

            // Act
            var ex = await Record.ExceptionAsync(() => _strategy.ValidateAsync(session, input));

            // Assert
            Assert.Null(ex);
        }

        #endregion

        #region ExecuteAsync

        [Fact]
        public async Task ExecuteAsync_CreaVotosConPuntos()
        {
            // Arrange
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                PointAllocations = new List<PointAllocationInput>
                {
                    new PointAllocationInput("proj-1", 30, "Comentario1"),
                    new PointAllocationInput("proj-2", 20, null)
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _voteRepoMock.Verify(r => r.RemoveByUserInCategoryAsync("user-1", "cat-1", "session-1"), Times.Once);
            _voteRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<Vote>>(votos =>
                votos.Count == 2 &&
                votos.Any(v => v.VotedProjectId == "proj-1" && v.Points == 30 && v.TopPosition == 0) &&
                votos.Any(v => v.VotedProjectId == "proj-2" && v.Points == 20))), Times.Once);
            _voteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_FiltraPuntosCero()
        {
            // Arrange
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                PointAllocations = new List<PointAllocationInput>
                {
                    new PointAllocationInput("proj-1", 30, null),
                    new PointAllocationInput("proj-2", 0, null)
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _voteRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<Vote>>(votos =>
                votos.Count == 1 && votos[0].VotedProjectId == "proj-1")), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ComentarioNormal_Trimmed()
        {
            // Arrange
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "u1",
                CategoryId = "c1",
                PointAllocations = new List<PointAllocationInput>
                {
                    new PointAllocationInput("p1", 10, "  Bueno  ")
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _voteRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<Vote>>(votos =>
                votos[0].Comment == "Bueno")), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ComentarioVacio_ComentarioNull()
        {
            // Arrange
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "u1",
                CategoryId = "c1",
                PointAllocations = new List<PointAllocationInput>
                {
                    new PointAllocationInput("p1", 10, "  ")
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _voteRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<Vote>>(votos =>
                votos[0].Comment == null)), Times.Once);
        }

        #endregion
    }
}
