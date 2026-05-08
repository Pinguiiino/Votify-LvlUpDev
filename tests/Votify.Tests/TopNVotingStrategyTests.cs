using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Votify.Domain.VoteFolder;
using Votify.Domain.VoteFolder.Strategies;

namespace Votify.Tests
{
    public class TopNVotingStrategyTests
    {
        private readonly Mock<IVoteRepository> _voteRepoMock;
        private readonly TopNVotingStrategy _strategy;

        public TopNVotingStrategyTests()
        {
            
            _voteRepoMock = new Mock<IVoteRepository>();

            
            _strategy = new TopNVotingStrategy(_voteRepoMock.Object);
        }

        [Fact]
        public async Task ValidateAsync_SinProyectos_LanzaExcepcion()
        {
            
            var session = new VotingSession { Id = "session1", TopN = 3 };
            var input = new VoteStrategyInput
            {
                UserId = "user1",
                CategoryId = "cat1",
                RankedProjects = new List<RankedProjectInput>() 
            };

            
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input)
            );

            
            Assert.Equal("No se han proporcionado proyectos para votar.", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_SuperaLimiteProyectos_LanzaExcepcion()
        {
            
            var session = new VotingSession { Id = "session1", TopN = 2 };

           
            var rankedProjects = new List<RankedProjectInput>
            {
                new RankedProjectInput("Proyecto1", 1, null),
                new RankedProjectInput("Proyecto2", 2, null),
                new RankedProjectInput("Proyecto3", 3, null) 
            };

            var input = new VoteStrategyInput { RankedProjects = rankedProjects };

            
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input)
            );

            Assert.Contains("No se pueden votar más de", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_FaltaComentarioObligatorio_LanzaExcepcion()
        {
            
            var session = new VotingSession { Id = "session1", TopN = 3, RequireComments = true };

            var rankedProjects = new List<RankedProjectInput>
            {
                new RankedProjectInput("Proyecto1", 1, "Buen proyecto"),
                new RankedProjectInput("Proyecto2", 2, " ") 
            };

            var input = new VoteStrategyInput { RankedProjects = rankedProjects };

            
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _strategy.ValidateAsync(session, input)
            );

            Assert.Equal("Esta votación exige un comentario en cada voto.", exception.Message);
        }
    }
}