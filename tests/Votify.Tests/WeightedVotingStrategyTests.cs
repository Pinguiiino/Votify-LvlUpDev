using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Votify.Domain.VoteFolder;
using Votify.Domain.VoteFolder.Strategies;

namespace Votify.Tests
{
    public class WeightedVotingStrategyTests
    {
        private readonly Mock<IWeightedVoteRepository> _weightedRepoMock;
        private readonly WeightedVotingStrategy _strategy;

        public WeightedVotingStrategyTests()
        {
            _weightedRepoMock = new Mock<IWeightedVoteRepository>();
            _strategy = new WeightedVotingStrategy(_weightedRepoMock.Object);
        }

        private static VotingSession CreateSession(bool requireComments = false, bool allowCommentsPerCriterion = false)
        {
            return new VotingSession(
                "cat-1", "Sesión Baremo", VoterType.Jury, EvaluationType.WeightedScale,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            {
                Id = "session-1",
                RequireComments = requireComments,
                AllowCommentsPerCriterion = allowCommentsPerCriterion
            };
        }

        #region ValidateAsync

        [Fact]
        public async Task ValidateAsync_SinProyectos_LanzaExcepcion()
        {
            // Arrange
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                WeightedProjects = new List<WeightedProjectInput>()
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input));
            Assert.Contains("No se han proporcionado proyectos para evaluar.", ex.Message);
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
                WeightedProjects = new List<WeightedProjectInput>
                {
                    new WeightedProjectInput("p1", "Bueno", new List<CriterionScoreInput>()),
                    new WeightedProjectInput("p2", "  ", new List<CriterionScoreInput>())
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
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                WeightedProjects = new List<WeightedProjectInput>
                {
                    new WeightedProjectInput("p1", null, new List<CriterionScoreInput>())
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
        public async Task ExecuteAsync_CreaVotosPonderados()
        {
            // Arrange
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                WeightedProjects = new List<WeightedProjectInput>
                {
                    new WeightedProjectInput("proj-1", "Comentario1", new List<CriterionScoreInput>
                    {
                        new CriterionScoreInput("crit-1", 8.5, null),
                        new CriterionScoreInput("crit-2", 7.0, null)
                    })
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _weightedRepoMock.Verify(r => r.RemoveByUserAndSessionAsync("user-1", "session-1"), Times.Once);
            _weightedRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<WeightedVote>>(votos =>
                votos.Count == 1 &&
                votos[0].ProjectId == "proj-1" &&
                votos[0].UserId == "user-1" &&
                votos[0].Comment == "Comentario1" &&
                votos[0].CriterionScores.Count == 2)), Times.Once);
            _weightedRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ScoreMayor10_Clamped()
        {
            // Arrange
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                WeightedProjects = new List<WeightedProjectInput>
                {
                    new WeightedProjectInput("proj-1", null, new List<CriterionScoreInput>
                    {
                        new CriterionScoreInput("crit-1", 15.0, null)
                    })
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _weightedRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<WeightedVote>>(votos =>
                votos[0].CriterionScores[0].Score == 10.0)), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ScoreNegativo_ClampedACero()
        {
            // Arrange
            var session = CreateSession();
            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                WeightedProjects = new List<WeightedProjectInput>
                {
                    new WeightedProjectInput("proj-1", null, new List<CriterionScoreInput>
                    {
                        new CriterionScoreInput("crit-1", -5.0, null)
                    })
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _weightedRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<WeightedVote>>(votos =>
                votos[0].CriterionScores[0].Score == 0.0)), Times.Once);
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
                WeightedProjects = new List<WeightedProjectInput>
                {
                    new WeightedProjectInput("p1", "  Bueno  ", new List<CriterionScoreInput>())
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _weightedRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<WeightedVote>>(votos =>
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
                WeightedProjects = new List<WeightedProjectInput>
                {
                    new WeightedProjectInput("p1", "  ", new List<CriterionScoreInput>())
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _weightedRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<WeightedVote>>(votos =>
                votos[0].Comment == null)), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ComentarioPorCriterio_Activado_GuardaComentario()
        {
            // Arrange
            var session = CreateSession(allowCommentsPerCriterion: true);
            var input = new VoteStrategyInput
            {
                UserId = "u1",
                CategoryId = "c1",
                WeightedProjects = new List<WeightedProjectInput>
                {
                    new WeightedProjectInput("p1", null, new List<CriterionScoreInput>
                    {
                        new CriterionScoreInput("crit-1", 8.0, "  Muy bueno  ")
                    })
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _weightedRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<WeightedVote>>(votos =>
                votos[0].CriterionScores[0].Comment == "Muy bueno")), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ComentarioPorCriterio_Desactivado_ComentarioNull()
        {
            // Arrange
            var session = CreateSession(allowCommentsPerCriterion: false);
            var input = new VoteStrategyInput
            {
                UserId = "u1",
                CategoryId = "c1",
                WeightedProjects = new List<WeightedProjectInput>
                {
                    new WeightedProjectInput("p1", null, new List<CriterionScoreInput>
                    {
                        new CriterionScoreInput("crit-1", 8.0, "Comentario")
                    })
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _weightedRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<WeightedVote>>(votos =>
                votos[0].CriterionScores[0].Comment == null)), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ComentarioPorCriterioVacio_ComentarioNull()
        {
            // Arrange
            var session = CreateSession(allowCommentsPerCriterion: true);
            var input = new VoteStrategyInput
            {
                UserId = "u1",
                CategoryId = "c1",
                WeightedProjects = new List<WeightedProjectInput>
                {
                    new WeightedProjectInput("p1", null, new List<CriterionScoreInput>
                    {
                        new CriterionScoreInput("crit-1", 8.0, "  ")
                    })
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _weightedRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<WeightedVote>>(votos =>
                votos[0].CriterionScores[0].Comment == null)), Times.Once);
        }

        #endregion
    }
}
