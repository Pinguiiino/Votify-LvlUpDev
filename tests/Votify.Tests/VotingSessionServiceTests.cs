using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Votify.Domain.VoteFolder;

namespace Votify.Tests
{
    public class VotingSessionServiceTests
    {
        private readonly Mock<IVotingSessionRepository> _repoMock;
        private readonly VotingSessionService _service;

        public VotingSessionServiceTests()
        {
            _repoMock = new Mock<IVotingSessionRepository>();
            _service = new VotingSessionService(_repoMock.Object);
        }

        #region GetByEventAsync

        [Fact]
        public async Task GetByEventAsync_DelegaAlRepositorio()
        {
            // Arrange
            var sessions = new List<VotingSession>
            {
                new VotingSession("cat-1", "S1", VoterType.Public, EvaluationType.TopN,
                    DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            };
            _repoMock
                .Setup(r => r.GetByEventAsync("event-1"))
                .ReturnsAsync(sessions);

            // Act
            var result = await _service.GetByEventAsync("event-1");

            // Assert
            Assert.Single(result);
            _repoMock.Verify(r => r.GetByEventAsync("event-1"), Times.Once);
        }

        #endregion

        #region GetActiveByEventAsync

        [Fact]
        public async Task GetActiveByEventAsync_DelegaAlRepositorio()
        {
            // Arrange
            _repoMock
                .Setup(r => r.GetActiveByEventAsync("event-1"))
                .ReturnsAsync(new List<VotingSession>());

            // Act
            var result = await _service.GetActiveByEventAsync("event-1");

            // Assert
            Assert.Empty(result);
            _repoMock.Verify(r => r.GetActiveByEventAsync("event-1"), Times.Once);
        }

        #endregion

        #region GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_Existe_RetornaSesion()
        {
            // Arrange
            var session = new VotingSession("cat-1", "S1", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5)) { Id = "s-1" };
            _repoMock
                .Setup(r => r.GetByIdAsync("s-1"))
                .ReturnsAsync(session);

            // Act
            var result = await _service.GetByIdAsync("s-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("s-1", result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NoExiste_RetornaNull()
        {
            // Arrange
            _repoMock
                .Setup(r => r.GetByIdAsync("s-999"))
                .ReturnsAsync((VotingSession?)null);

            // Act
            var result = await _service.GetByIdAsync("s-999");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByCategoryAsync

        [Fact]
        public async Task GetByCategoryAsync_DelegaAlRepositorio()
        {
            // Arrange
            var sessions = new List<VotingSession>
            {
                new VotingSession("cat-1", "S1", VoterType.Public, EvaluationType.TopN,
                    DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            };
            _repoMock
                .Setup(r => r.GetByCategoryAsync("cat-1"))
                .ReturnsAsync(sessions);

            // Act
            var result = await _service.GetByCategoryAsync("cat-1");

            // Assert
            Assert.Single(result);
            _repoMock.Verify(r => r.GetByCategoryAsync("cat-1"), Times.Once);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_DelegaAlRepositorio()
        {
            // Arrange
            var session = new VotingSession("cat-1", "S1", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5)) { Id = "s-1" };
            _repoMock
                .Setup(r => r.UpdateAsync(session))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(session);

            // Assert
            _repoMock.Verify(r => r.UpdateAsync(session), Times.Once);
        }

        #endregion
    }
}
