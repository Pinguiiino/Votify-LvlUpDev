using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Votify.Domain.AuditFolder;
using Votify.Domain.VoteFolder;

namespace Votify.Tests
{
    public class AuditServiceTests
    {
        private readonly Mock<IAuditRequestRepository> _auditRepoMock;
        private readonly Mock<IVoteRepository> _voteRepoMock;
        private readonly AuditService _service;

        public AuditServiceTests()
        {
            _auditRepoMock = new Mock<IAuditRequestRepository>();
            _voteRepoMock = new Mock<IVoteRepository>();
            _service = new AuditService(_auditRepoMock.Object, _voteRepoMock.Object);
        }

        #region GetProjectAuditAsync

        [Fact]
        public async Task GetProjectAuditAsync_SinVotos_RetornaListaVacia()
        {
            // Arrange
            var projectId = "proj-1";
            _voteRepoMock
                .Setup(r => r.GetByProjectAsync(projectId))
                .ReturnsAsync(new List<Vote>());

            // Act
            var result = await _service.GetProjectAuditAsync(projectId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetProjectAuditAsync_ConVotos_RetornaEntradasMapeadas()
        {
            // Arrange
            var projectId = "proj-1";
            var fecha = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var votes = new List<Vote>
            {
                new PublicVote("session1", projectId, "user1", "cat1", 1, "Buen proyecto")
                {
                    CreatedAt = fecha,
                    IntegrityHash = "abc123hash",
                    Comment = "Buen proyecto"
                }
            };

            _voteRepoMock
                .Setup(r => r.GetByProjectAsync(projectId))
                .ReturnsAsync(votes);

            // Act
            var result = await _service.GetProjectAuditAsync(projectId);

            // Assert
            var entry = Assert.Single(result);
            Assert.Equal("user1", entry.Voter);
            Assert.Equal(fecha, entry.Date);
            Assert.Equal("abc123hash", entry.Hash);
            Assert.Equal(1, entry.TopPosition);
            Assert.Equal("Buen proyecto", entry.Comment);
        }

        [Fact]
        public async Task GetProjectAuditAsync_ConMultiplesVotos_RetornaTodasLasEntradas()
        {
            // Arrange
            var projectId = "proj-1";
            var votes = new List<Vote>
            {
                new PublicVote("session1", projectId, "user1", "cat1", 1),
                new PublicVote("session1", projectId, "user2", "cat1", 2),
                new PublicVote("session1", projectId, "user3", "cat1", 3)
            };

            _voteRepoMock
                .Setup(r => r.GetByProjectAsync(projectId))
                .ReturnsAsync(votes);

            // Act
            var result = await _service.GetProjectAuditAsync(projectId);

            // Assert
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetProjectAuditAsync_VotoSinComentarioNiHash_ComentarioYHashNulos()
        {
            // Arrange
            var projectId = "proj-1";
            var votes = new List<Vote>
            {
                new PublicVote("session1", projectId, "user1", "cat1", 1)
                {
                    IntegrityHash = null!,
                    Comment = null
                }
            };

            _voteRepoMock
                .Setup(r => r.GetByProjectAsync(projectId))
                .ReturnsAsync(votes);

            // Act
            var result = await _service.GetProjectAuditAsync(projectId);

            // Assert
            var entry = Assert.Single(result);
            Assert.Null(entry.Hash);
            Assert.Null(entry.Comment);
        }

        [Fact]
        public async Task GetProjectAuditAsync_ProyectoIdVacio_RetornaListaVacia()
        {
            // Arrange
            _voteRepoMock
                .Setup(r => r.GetByProjectAsync(string.Empty))
                .ReturnsAsync(new List<Vote>());

            // Act
            var result = await _service.GetProjectAuditAsync(string.Empty);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region RequestAuditAsync

        [Fact]
        public async Task RequestAuditAsync_SolicitudNoExiste_AgregaYGuarda()
        {
            // Arrange
            var projectId = "proj-1";
            _auditRepoMock
                .Setup(r => r.ExistsByProjectIdAsync(projectId))
                .ReturnsAsync(false);

            // Act
            await _service.RequestAuditAsync(projectId);

            // Assert
            _auditRepoMock.Verify(r => r.ExistsByProjectIdAsync(projectId), Times.Once);
            _auditRepoMock.Verify(r => r.AddAsync(It.Is<AuditRequest>(
                a => a.ProjectId == projectId)), Times.Once);
            _auditRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RequestAuditAsync_SolicitudExiste_NoAgregaNiGuarda()
        {
            // Arrange
            var projectId = "proj-1";
            _auditRepoMock
                .Setup(r => r.ExistsByProjectIdAsync(projectId))
                .ReturnsAsync(true);

            // Act
            await _service.RequestAuditAsync(projectId);

            // Assert
            _auditRepoMock.Verify(r => r.ExistsByProjectIdAsync(projectId), Times.Once);
            _auditRepoMock.Verify(r => r.AddAsync(It.IsAny<AuditRequest>()), Times.Never);
            _auditRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task RequestAuditAsync_ProyectoIdVacio_NoLanzaExcepcion()
        {
            // Arrange
            _auditRepoMock
                .Setup(r => r.ExistsByProjectIdAsync(string.Empty))
                .ReturnsAsync(false);

            // Act
            var exception = await Record.ExceptionAsync(
                () => _service.RequestAuditAsync(string.Empty));

            // Assert
            Assert.Null(exception);
        }

        #endregion

        #region GetRequestedProjectIdsAsync

        [Fact]
        public async Task GetRequestedProjectIdsAsync_RetornaListaDeProyectos()
        {
            // Arrange
            var projectIds = new List<string> { "proj-1", "proj-2", "proj-3" };
            _auditRepoMock
                .Setup(r => r.GetAllProjectIdsAsync())
                .ReturnsAsync(projectIds);

            // Act
            var result = await _service.GetRequestedProjectIdsAsync();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains("proj-1", result);
            Assert.Contains("proj-2", result);
            Assert.Contains("proj-3", result);
        }

        [Fact]
        public async Task GetRequestedProjectIdsAsync_SinSolicitudes_RetornaListaVacia()
        {
            // Arrange
            _auditRepoMock
                .Setup(r => r.GetAllProjectIdsAsync())
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _service.GetRequestedProjectIdsAsync();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetDashboardByEventAsync

        [Fact]
        public async Task GetDashboardByEventAsync_ConElementos_RetornaDashboard()
        {
            // Arrange
            var eventId = "event-1";
            var items = new List<AuditDashboardItem>
            {
                new AuditDashboardItem("audit-1", "proj-1", "Proyecto Alpha", DateTime.UtcNow),
                new AuditDashboardItem("audit-2", "proj-2", "Proyecto Beta", DateTime.UtcNow)
            };

            _auditRepoMock
                .Setup(r => r.GetDashboardByEventAsync(eventId))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetDashboardByEventAsync(eventId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("audit-1", result[0].AuditId);
            Assert.Equal("Proyecto Alpha", result[0].ProjectTitle);
        }

        [Fact]
        public async Task GetDashboardByEventAsync_SinElementos_RetornaListaVacia()
        {
            // Arrange
            var eventId = "event-1";
            _auditRepoMock
                .Setup(r => r.GetDashboardByEventAsync(eventId))
                .ReturnsAsync(new List<AuditDashboardItem>());

            // Act
            var result = await _service.GetDashboardByEventAsync(eventId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDashboardByEventAsync_EventoIdVacio_RetornaListaVacia()
        {
            // Arrange
            _auditRepoMock
                .Setup(r => r.GetDashboardByEventAsync(string.Empty))
                .ReturnsAsync(new List<AuditDashboardItem>());

            // Act
            var result = await _service.GetDashboardByEventAsync(string.Empty);

            // Assert
            Assert.Empty(result);
        }

        #endregion
    }
}
