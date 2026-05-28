using Xunit;
using Votify.Domain.EventFolder;
using Votify.Domain.CategoryFolder;
using Votify.Domain.VoteFolder;
using Votify.Domain.UserFolder;
using Votify.Infrastructure.Repositories;

namespace Votify.IntegrationTests
{
    public class EventRepositoryTests : IClassFixture<TestDatabaseFixture>, IAsyncLifetime
    {
        private readonly TestDatabaseFixture _fixture;

        public EventRepositoryTests(TestDatabaseFixture fixture) => _fixture = fixture;

        public Task InitializeAsync() => _fixture.ClearDatabaseAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private static ModalityEvent CreateEvent(string name = "Evento Test", string organizer = "org-1")
        {
            return new ModalityEvent(
                name, 10,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(30),
                "TestModality")
            {
                Organizer = organizer,
                Auditor = "aud-1",
                Participants = new List<GeneralUser>(),
                Public = new List<GeneralUser>()
            };
        }

        [Fact]
        public async Task AddAsync_YGetByIdAsync_GuardaYRecuperaEvento()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new EventRepository(context);
            var evento = CreateEvent();

            // Act
            await repo.AddAsync(evento);
            await repo.SaveChangesAsync();

            var result = await repo.GetByIdAsync(evento.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Evento Test", result.Name);
            Assert.Equal("org-1", result.Organizer);
            Assert.Equal("TestModality", result.Modality());
        }

        [Fact]
        public async Task ExistsByNameAsync_NoExiste_RetornaFalse()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new EventRepository(context);

            // Act
            var result = await repo.ExistsByNameAsync("NoExiste");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExistsByNameAsync_Existe_RetornaTrue()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new EventRepository(context);
            var evento = CreateEvent("Mi Evento");
            await repo.AddAsync(evento);
            await repo.SaveChangesAsync();

            // Act
            var result = await repo.ExistsByNameAsync("mi evento");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetAllAsync_RetornaTodosLosEventos()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new EventRepository(context);
            await repo.AddAsync(CreateEvent("E1"));
            await repo.AddAsync(CreateEvent("E2"));
            await repo.SaveChangesAsync();

            // Act
            var result = await repo.GetAllAsync();

            // Assert
            Assert.True(result.Count >= 2);
        }

        [Fact]
        public async Task GetCategoriesWithDetailsAsync_ConCategorias_RetornaConSesiones()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new EventRepository(context);

            var evento = CreateEvent("E1");
            await repo.AddAsync(evento);
            await repo.SaveChangesAsync();

            var catRepo = new CategoryRepository(context);
            var category = new Category(evento.Id, "Cat1");
            await catRepo.AddAsync(category);
            await catRepo.SaveChangesAsync();

            var session = new VotingSession(
                category.Id, "Sesión1", VoterType.Public, EvaluationType.TopN,
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5))
            { TopN = 3 };
            context.VotingSessions.Add(session);
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetCategoriesWithDetailsAsync(evento.Id);

            // Assert
            Assert.Single(result);
            Assert.Equal("Cat1", result[0].Name);
            Assert.Single(result[0].VotingSessions);
        }

        [Fact]
        public async Task DeleteAsync_EliminaEventoYRelacionados()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new EventRepository(context);
            var evento = CreateEvent("ToDelete");
            await repo.AddAsync(evento);
            await repo.SaveChangesAsync();

            // Act
            await repo.DeleteAsync(evento.Id);
            await repo.SaveChangesAsync();

            var result = await repo.GetByIdAsync(evento.Id);

            // Assert
            Assert.Null(result);
        }
    }
}
