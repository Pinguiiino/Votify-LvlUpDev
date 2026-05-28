using Xunit;
using System;
using Votify.Domain.VoteFolder;
using Votify.Domain.VoteFolder.States;

namespace Votify.Tests
{
    public class VotingSessionStateTests
    {
        private static VotingSession CreateScheduledSession(
            DateTime? openAt = null,
            DateTime? closeAt = null)
        {
            return new VotingSession(
                categoryId: "cat-1",
                name: "Sesión Test",
                voterType: VoterType.Public,
                evaluationType: EvaluationType.TopN,
                openAt: openAt ?? DateTime.UtcNow.AddDays(1),
                closeAt: closeAt ?? DateTime.UtcNow.AddDays(5))
            {
                TopN = 3
            };
        }

        #region ScheduledState

        [Fact]
        public void ScheduledState_Abrir_TransicionaAOpen()
        {
            // Arrange
            var session = CreateScheduledSession(
                openAt: DateTime.UtcNow.AddDays(-1));

            // Act
            session.Abrir();

            // Assert
            Assert.True(session.IsOpen);
            Assert.Equal("open", session.ManualStatus);
            Assert.True(session.IsManuallyAdjusted);
        }

        [Fact]
        public void ScheduledState_Abrir_OpenAtEnFuturo_AjustaOpenAt()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddHours(2);
            var session = CreateScheduledSession(openAt: futureDate);

            // Act
            session.Abrir();

            // Assert
            Assert.True(session.IsOpen);
            Assert.True(session.OpenAt <= DateTime.UtcNow);
        }

        [Fact]
        public void ScheduledState_Pausar_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Pausar());
            Assert.Contains("No se puede pausar", ex.Message);
        }

        [Fact]
        public void ScheduledState_Reanudar_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Reanudar());
            Assert.Contains("No se puede reanudar", ex.Message);
        }

        [Fact]
        public void ScheduledState_Cerrar_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Cerrar());
            Assert.Contains("No se puede cerrar", ex.Message);
        }

        [Fact]
        public void ScheduledState_PuedeVotar_FueraDeVentana_RetornaFalse()
        {
            // Arrange - OpenAt en el futuro
            var session = CreateScheduledSession(
                openAt: DateTime.UtcNow.AddDays(1),
                closeAt: DateTime.UtcNow.AddDays(5));

            // Act & Assert
            Assert.False(session.IsOpen);
        }

        [Fact]
        public void ScheduledState_PuedeVotar_DentroDeVentana_RetornaTrue()
        {
            // Arrange
            var session = CreateScheduledSession(
                openAt: DateTime.UtcNow.AddDays(-1),
                closeAt: DateTime.UtcNow.AddDays(5));

            // Act & Assert
            Assert.True(session.IsOpen);
        }

        #endregion

        #region OpenState

        [Fact]
        public void OpenState_Abrir_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Abrir());
            Assert.Contains("ya está abierta", ex.Message);
        }

        [Fact]
        public void OpenState_Pausar_TransicionaAPaused()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();

            // Act
            session.Pausar();

            // Assert
            Assert.False(session.IsOpen);
            Assert.Equal("paused", session.ManualStatus);
        }

        [Fact]
        public void OpenState_Reanudar_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Reanudar());
            Assert.Contains("no está pausada", ex.Message);
        }

        [Fact]
        public void OpenState_Cerrar_TransicionaAClosed()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();

            // Act
            session.Cerrar();

            // Assert
            Assert.False(session.IsOpen);
            Assert.Equal("closed", session.ManualStatus);
            Assert.NotNull(session.AdjustedCloseAt);
        }

        [Fact]
        public void OpenState_Cerrar_AjustaCloseAt_Ahora()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            var antes = DateTime.UtcNow.AddSeconds(-1);

            // Act
            session.Cerrar();

            // Assert
            Assert.True(session.AdjustedCloseAt >= antes);
        }

        [Fact]
        public void OpenState_PuedeVotar_DentroDeVentana_RetornaTrue()
        {
            // Arrange
            var session = CreateScheduledSession(
                openAt: DateTime.UtcNow.AddDays(-1),
                closeAt: DateTime.UtcNow.AddDays(5));
            session.Abrir();

            // Act & Assert
            Assert.True(session.IsOpen);
        }

        #endregion

        #region PausedState

        [Fact]
        public void PausedState_Abrir_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.Pausar();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Abrir());
            Assert.Contains("pausada", ex.Message);
        }

        [Fact]
        public void PausedState_Pausar_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.Pausar();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Pausar());
            Assert.Contains("ya está pausada", ex.Message);
        }

        [Fact]
        public void PausedState_Reanudar_TransicionaAOpen()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.Pausar();

            // Act
            session.Reanudar();

            // Assert
            Assert.True(session.IsOpen);
            Assert.Equal("open", session.ManualStatus);
        }

        [Fact]
        public void PausedState_Cerrar_TransicionaAClosed()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.Pausar();

            // Act
            session.Cerrar();

            // Assert
            Assert.False(session.IsOpen);
            Assert.Equal("closed", session.ManualStatus);
        }

        [Fact]
        public void PausedState_PuedeVotar_RetornaFalse()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.Pausar();

            // Act & Assert
            Assert.False(session.IsOpen);
        }

        #endregion

        #region ClosedState

        [Fact]
        public void ClosedState_Abrir_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.Cerrar();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Abrir());
            Assert.Contains("cerrada no puede reabrirse", ex.Message);
        }

        [Fact]
        public void ClosedState_Pausar_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.Cerrar();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Pausar());
            Assert.Contains("cerrada no puede pausarse", ex.Message);
        }

        [Fact]
        public void ClosedState_Reanudar_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.Cerrar();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Reanudar());
            Assert.Contains("cerrada no puede reanudarse", ex.Message);
        }

        [Fact]
        public void ClosedState_Cerrar_LanzaExcepcion()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.Cerrar();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => session.Cerrar());
            Assert.Contains("ya está cerrada", ex.Message);
        }

        [Fact]
        public void ClosedState_PuedeVotar_RetornaFalse()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.Cerrar();

            // Act & Assert
            Assert.False(session.IsOpen);
        }

        #endregion

        #region Transiciones Complejas

        [Fact]
        public void TransicionCompleta_OpenPausarReanudarCierra_EstadoFinal()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));

            // Act & Assert - Abrir
            session.Abrir();
            Assert.True(session.IsOpen);
            Assert.Equal("open", session.ManualStatus);

            // Pausar
            session.Pausar();
            Assert.False(session.IsOpen);
            Assert.Equal("paused", session.ManualStatus);

            // Reanudar
            session.Reanudar();
            Assert.True(session.IsOpen);
            Assert.Equal("open", session.ManualStatus);

            // Cerrar
            session.Cerrar();
            Assert.False(session.IsOpen);
            Assert.Equal("closed", session.ManualStatus);
        }

        [Fact]
        public void ScheduledToClosed_SinPasarPorOpen_CloseAtAjustado()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));

            // Abrir y cerrar inmediatamente
            session.Abrir();
            session.Cerrar();

            // Assert
            Assert.NotNull(session.AdjustedCloseAt);
            Assert.Equal("closed", session.ManualStatus);
        }

        #endregion

        #region RestaurarEstado

        [Fact]
        public void RestaurarEstado_ManualStatusOpen_RestauraOpenState()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.Abrir();
            session.ManualStatus = "open";

            // Act
            session.RestaurarEstado();

            // Assert
            Assert.True(session.IsOpen);
        }

        [Fact]
        public void RestaurarEstado_ManualStatusPaused_RestauraPausedState()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.ManualStatus = "paused";

            // Act
            session.RestaurarEstado();

            // Assert
            Assert.False(session.IsOpen);
        }

        [Fact]
        public void RestaurarEstado_ManualStatusClosed_RestauraClosedState()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.ManualStatus = "closed";

            // Act
            session.RestaurarEstado();

            // Assert
            Assert.False(session.IsOpen);
        }

        [Fact]
        public void RestaurarEstado_ManualStatusNull_RestauraScheduledState()
        {
            // Arrange
            var session = CreateScheduledSession(openAt: DateTime.UtcNow.AddDays(-1));
            session.ManualStatus = null;

            // Act
            session.RestaurarEstado();

            // Assert
            Assert.False(session.IsOpen);
        }

        #endregion
    }
}
