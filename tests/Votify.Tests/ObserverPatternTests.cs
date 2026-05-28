using Xunit;
using Moq;
using Votify.Domain.VoteFolder;

namespace Votify.Tests
{
    public class ObserverPatternTests
    {
        #region IVoteObserver - Interfaz

        [Fact]
        public async Task IVoteObserver_Update_SePuedeImplementar()
        {
            // Arrange
            var updateCalled = false;
            var observer = new Mock<IVoteObserver>();
            observer
                .Setup(o => o.Update(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e => updateCalled = true)
                .Returns(Task.CompletedTask);

            var voteEvent = new VoteChangedEvent("event-1", "cat-1", "session-1");

            // Act
            await observer.Object.Update(voteEvent);

            // Assert
            Assert.True(updateCalled);
        }

        [Fact]
        public async Task IVoteObserver_Update_RecibeDatosCorrectos()
        {
            // Arrange
            VoteChangedEvent? receivedEvent = null;
            var observer = new Mock<IVoteObserver>();
            observer
                .Setup(o => o.Update(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e => receivedEvent = e)
                .Returns(Task.CompletedTask);

            var voteEvent = new VoteChangedEvent("event-1", "cat-1", "session-1");

            // Act
            await observer.Object.Update(voteEvent);

            // Assert
            Assert.NotNull(receivedEvent);
            Assert.Equal("event-1", receivedEvent!.EventId);
            Assert.Equal("cat-1", receivedEvent.CategoryId);
            Assert.Equal("session-1", receivedEvent.SessionId);
        }

        [Fact]
        public async Task IVoteObserver_MultiplesObservers_SeEjecutanTodos()
        {
            // Arrange
            var callCount = 0;
            var observer1 = new Mock<IVoteObserver>();
            observer1
                .Setup(o => o.Update(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e => callCount++)
                .Returns(Task.CompletedTask);

            var observer2 = new Mock<IVoteObserver>();
            observer2
                .Setup(o => o.Update(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e => callCount++)
                .Returns(Task.CompletedTask);

            var voteEvent = new VoteChangedEvent("event-1", "cat-1", "session-1");

            // Act
            await observer1.Object.Update(voteEvent);
            await observer2.Object.Update(voteEvent);

            // Assert
            Assert.Equal(2, callCount);
        }

        #endregion

        #region IVoteSubject - Interfaz

        [Fact]
        public void IVoteSubject_SePuedeImplementar()
        {
            // Arrange
            var subject = new Mock<IVoteSubject>();

            // Act & Assert
            subject.Verify(s => s.RegisterObserver(It.IsAny<IVoteObserver>()), Times.Never);
            subject.Verify(s => s.RemoveObserver(It.IsAny<IVoteObserver>()), Times.Never);
            subject.Verify(s => s.NotifyObservers(It.IsAny<VoteChangedEvent>()), Times.Never);
        }

        [Fact]
        public void IVoteSubject_RegisterObserver_SeLlamaCorrectamente()
        {
            // Arrange
            var subject = new Mock<IVoteSubject>();
            var observer = new Mock<IVoteObserver>();

            // Act
            subject.Object.RegisterObserver(observer.Object);

            // Assert
            subject.Verify(s => s.RegisterObserver(observer.Object), Times.Once);
        }

        [Fact]
        public void IVoteSubject_RemoveObserver_SeLlamaCorrectamente()
        {
            // Arrange
            var subject = new Mock<IVoteSubject>();
            var observer = new Mock<IVoteObserver>();

            // Act
            subject.Object.RemoveObserver(observer.Object);

            // Assert
            subject.Verify(s => s.RemoveObserver(observer.Object), Times.Once);
        }

        [Fact]
        public void IVoteSubject_NotifyObservers_SeLlamaCorrectamente()
        {
            // Arrange
            var subject = new Mock<IVoteSubject>();
            var voteEvent = new VoteChangedEvent("event-1", "cat-1", "session-1");

            // Act
            subject.Object.NotifyObservers(voteEvent);

            // Assert
            subject.Verify(s => s.NotifyObservers(voteEvent), Times.Once);
        }

        #endregion

        #region VoteChangedEvent - Record

        [Fact]
        public void VoteChangedEvent_Propiedades_SonCorrectas()
        {
            // Arrange & Act
            var voteEvent = new VoteChangedEvent("event-1", "cat-1", "session-1");

            // Assert
            Assert.Equal("event-1", voteEvent.EventId);
            Assert.Equal("cat-1", voteEvent.CategoryId);
            Assert.Equal("session-1", voteEvent.SessionId);
        }

        [Fact]
        public void VoteChangedEvent_Igualdad_SonIguales()
        {
            // Arrange & Act
            var event1 = new VoteChangedEvent("e1", "c1", "s1");
            var event2 = new VoteChangedEvent("e1", "c1", "s1");

            // Assert
            Assert.Equal(event1, event2);
        }

        [Fact]
        public void VoteChangedEvent_Diferentes_NoSonIguales()
        {
            // Arrange & Act
            var event1 = new VoteChangedEvent("e1", "c1", "s1");
            var event2 = new VoteChangedEvent("e2", "c2", "s2");

            // Assert
            Assert.NotEqual(event1, event2);
        }

        [Fact]
        public void VoteChangedEvent_Deconstruccion_ObtieneValores()
        {
            // Arrange
            var voteEvent = new VoteChangedEvent("e1", "c1", "s1");

            // Act
            var (eventId, categoryId, sessionId) = voteEvent;

            // Assert
            Assert.Equal("e1", eventId);
            Assert.Equal("c1", categoryId);
            Assert.Equal("s1", sessionId);
        }

        [Fact]
        public void VoteChangedEvent_ToString_RetornaRepresentation()
        {
            // Arrange
            var voteEvent = new VoteChangedEvent("e1", "c1", "s1");

            // Act
            var str = voteEvent.ToString();

            // Assert
            Assert.NotNull(str);
            Assert.Contains("e1", str);
            Assert.Contains("c1", str);
            Assert.Contains("s1", str);
        }

        #endregion

        #region Patrón Observer - Flujo Completo

        [Fact]
        public async Task Observer_FlujoCompleto_RegistrarNotificar()
        {
            // Arrange
            var receivedEvents = new List<VoteChangedEvent>();
            var observer = new Mock<IVoteObserver>();
            observer
                .Setup(o => o.Update(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e => receivedEvents.Add(e))
                .Returns(Task.CompletedTask);

            var subject = new Mock<IVoteSubject>();
            subject
                .Setup(s => s.NotifyObservers(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e => observer.Object.Update(e).Wait());

            var voteEvent = new VoteChangedEvent("event-1", "cat-1", "session-1");

            // Act
            subject.Object.RegisterObserver(observer.Object);
            subject.Object.NotifyObservers(voteEvent);

            // Assert
            Assert.Single(receivedEvents);
            Assert.Equal("event-1", receivedEvents[0].EventId);
        }

        [Fact]
        public async Task Observer_FlujoCompleto_MultiplesObservadores()
        {
            // Arrange
            var receivedByObserver1 = new List<VoteChangedEvent>();
            var receivedByObserver2 = new List<VoteChangedEvent>();

            var observer1 = new Mock<IVoteObserver>();
            observer1
                .Setup(o => o.Update(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e => receivedByObserver1.Add(e))
                .Returns(Task.CompletedTask);

            var observer2 = new Mock<IVoteObserver>();
            observer2
                .Setup(o => o.Update(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e => receivedByObserver2.Add(e))
                .Returns(Task.CompletedTask);

            var subject = new Mock<IVoteSubject>();
            subject
                .Setup(s => s.NotifyObservers(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e =>
                {
                    observer1.Object.Update(e).Wait();
                    observer2.Object.Update(e).Wait();
                });

            var voteEvent = new VoteChangedEvent("event-1", "cat-1", "session-1");

            // Act
            subject.Object.RegisterObserver(observer1.Object);
            subject.Object.RegisterObserver(observer2.Object);
            subject.Object.NotifyObservers(voteEvent);

            // Assert
            Assert.Single(receivedByObserver1);
            Assert.Single(receivedByObserver2);
        }

        [Fact]
        public async Task Observer_FlujoCompleto_RegistrarYLuegoRemover()
        {
            // Arrange
            var receivedEvents = new List<VoteChangedEvent>();
            var observer = new Mock<IVoteObserver>();
            observer
                .Setup(o => o.Update(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e => receivedEvents.Add(e))
                .Returns(Task.CompletedTask);

            var subject = new Mock<IVoteSubject>();
            subject
                .Setup(s => s.NotifyObservers(It.IsAny<VoteChangedEvent>()))
                .Callback<VoteChangedEvent>(e => observer.Object.Update(e).Wait());

            var voteEvent = new VoteChangedEvent("event-1", "cat-1", "session-1");

            // Act
            subject.Object.RegisterObserver(observer.Object);
            subject.Object.NotifyObservers(voteEvent);
            subject.Object.RemoveObserver(observer.Object);
            subject.Object.NotifyObservers(voteEvent);

            // Assert
            subject.Verify(s => s.RegisterObserver(observer.Object), Times.Once);
            subject.Verify(s => s.RemoveObserver(observer.Object), Times.Once);
        }

        #endregion
    }
}
