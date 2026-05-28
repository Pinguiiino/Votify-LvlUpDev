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
    public class TopNVotingStrategyTests
    {
        private readonly Mock<IVoteRepository> _voteRepoMock;
        private readonly VoteCreatorFactory _voteCreatorFactory;
        private readonly TopNVotingStrategy _strategy;

        public TopNVotingStrategyTests()
        {
            _voteRepoMock = new Mock<IVoteRepository>();
            _voteCreatorFactory = new VoteCreatorFactory(
                new VoteCreator[] { new PublicVoteCreator(), new ExpertVoteCreator() });
            _strategy = new TopNVotingStrategy(_voteRepoMock.Object, _voteCreatorFactory);
        }

        [Fact]
        public async Task ValidateAsync_SinProyectos_LanzaExcepcion()
        {
            // Arrange
            var session = new VotingSession { Id = "session1", TopN = 3 };
            var input = new VoteStrategyInput
            {
                UserId = "user1",
                CategoryId = "cat1",
                RankedProjects = new List<RankedProjectInput>()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input));
            Assert.Equal("No se han proporcionado proyectos para votar.", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_SuperaLimiteProyectos_LanzaExcepcion()
        {
            // Arrange
            var session = new VotingSession { Id = "session1", TopN = 2 };
            var rankedProjects = new List<RankedProjectInput>
            {
                new RankedProjectInput("Proyecto1", 1, null),
                new RankedProjectInput("Proyecto2", 2, null),
                new RankedProjectInput("Proyecto3", 3, null)
            };
            var input = new VoteStrategyInput { RankedProjects = rankedProjects };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input));
            Assert.Contains("No se pueden votar más de", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_FaltaComentarioObligatorio_LanzaExcepcion()
        {
            // Arrange
            var session = new VotingSession { Id = "session1", TopN = 3, RequireComments = true };
            var rankedProjects = new List<RankedProjectInput>
            {
                new RankedProjectInput("Proyecto1", 1, "Buen proyecto"),
                new RankedProjectInput("Proyecto2", 2, " ")
            };
            var input = new VoteStrategyInput { RankedProjects = rankedProjects };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input));
            Assert.Equal("Esta votación exige un comentario en cada voto.", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_DatosValidos_NoLanzaExcepcion()
        {
            // Arrange
            var session = new VotingSession { Id = "session1", TopN = 3 };
            var input = new VoteStrategyInput
            {
                UserId = "user1",
                CategoryId = "cat1",
                RankedProjects = new List<RankedProjectInput>
                {
                    new RankedProjectInput("p1", 1, null),
                    new RankedProjectInput("p2", 2, null)
                }
            };

            // Act
            var ex = await Record.ExceptionAsync(() => _strategy.ValidateAsync(session, input));

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task ExecuteAsync_CreaVotosCorrectamente()
        {
            // Arrange
            var session = new VotingSession(
                "cat-1", "Sesión", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            { Id = "session-1", TopN = 3 };

            var input = new VoteStrategyInput
            {
                UserId = "user-1",
                CategoryId = "cat-1",
                RankedProjects = new List<RankedProjectInput>
                {
                    new RankedProjectInput("proj-1", 1, "Comentario1"),
                    new RankedProjectInput("proj-2", 2, null)
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _voteRepoMock.Verify(r => r.RemoveByUserInCategoryAsync("user-1", "cat-1", "session-1"), Times.Once);
            _voteRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<Vote>>(votos =>
                votos.Count == 2 &&
                votos[0].VotedProjectId == "proj-1" &&
                votos[0].TopPosition == 1 &&
                votos[1].VotedProjectId == "proj-2" &&
                votos[1].TopPosition == 2)), Times.Once);
            _voteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ComentarioNormal_NoLoNormaliza()
        {
            // Arrange
            var session = new VotingSession(
                "cat-1", "S", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            { Id = "s-1", TopN = 1 };

            var input = new VoteStrategyInput
            {
                UserId = "u1",
                CategoryId = "c1",
                RankedProjects = new List<RankedProjectInput>
                {
                    new RankedProjectInput("p1", 1, "  Bueno  ")
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
            var session = new VotingSession(
                "cat-1", "S", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            { Id = "s-1", TopN = 1 };

            var input = new VoteStrategyInput
            {
                UserId = "u1",
                CategoryId = "c1",
                RankedProjects = new List<RankedProjectInput>
                {
                    new RankedProjectInput("p1", 1, "  ")
                }
            };

            // Act
            await _strategy.ExecuteAsync(session, input);

            // Assert
            _voteRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<Vote>>(votos =>
                votos[0].Comment == null)), Times.Once);
        }
    }
}