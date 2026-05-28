using Xunit;
using Votify.Domain.UserFolder;
using Votify.Infrastructure.Repositories;

namespace Votify.IntegrationTests
{
    public class UserRepositoryTests : IClassFixture<TestDatabaseFixture>, IAsyncLifetime
    {
        private readonly TestDatabaseFixture _fixture;

        public UserRepositoryTests(TestDatabaseFixture fixture) => _fixture = fixture;

        public Task InitializeAsync() => _fixture.ClearDatabaseAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task AddAsync_YGetByIdAsync_GuardaYRecuperaUsuario()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new UserRepository(context);
            var user = new GeneralUser("TestUser", "test@test.com", "pass123");

            // Act
            await repo.AddAsync(user);
            await repo.SaveChangesAsync();

            var result = await repo.GetByIdAsync(user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestUser", result.Name);
            Assert.Equal("test@test.com", result.Email);
        }

        [Fact]
        public async Task GetByEmailAsync_Existe_RetornaUsuario()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new UserRepository(context);
            var user = new Organizer("Organizer", "org@test.com", "pass");
            await repo.AddAsync(user);
            await repo.SaveChangesAsync();

            // Act
            var result = await repo.GetByEmailAsync("org@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Organizer", result.Name);
            Assert.IsType<Organizer>(result);
        }

        [Fact]
        public async Task GetByEmailAsync_NoExiste_RetornaNull()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new UserRepository(context);

            // Act
            var result = await repo.GetByEmailAsync("noexiste@test.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CheckForDuplicatesAsync_SinDuplicados_RetornaFalse()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new UserRepository(context);

            // Act
            var (nameExists, emailExists) = await repo.CheckForDuplicatesAsync("Unique", "unique@test.com");

            // Assert
            Assert.False(nameExists);
            Assert.False(emailExists);
        }

        [Fact]
        public async Task CheckForDuplicatesAsync_EmailDuplicado_RetornaTrue()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new UserRepository(context);
            var user = new GeneralUser("User1", "dup@test.com", "pass");
            await repo.AddAsync(user);
            await repo.SaveChangesAsync();

            // Act
            var (nameExists, emailExists) = await repo.CheckForDuplicatesAsync("OtroNombre", "dup@test.com");

            // Assert
            Assert.False(nameExists);
            Assert.True(emailExists);
        }

        [Fact]
        public async Task CheckForDuplicatesAsync_NombreDuplicado_RetornaTrue()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new UserRepository(context);
            var user = new GeneralUser("DupName", "other@test.com", "pass");
            await repo.AddAsync(user);
            await repo.SaveChangesAsync();

            // Act
            var (nameExists, emailExists) = await repo.CheckForDuplicatesAsync("DupName", "new@test.com");

            // Assert
            Assert.True(nameExists);
            Assert.False(emailExists);
        }

        [Fact]
        public async Task CountAsync_RetornaNumeroUsuarios()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new UserRepository(context);
            await repo.AddAsync(new GeneralUser("U1", "u1@test.com", "p"));
            await repo.AddAsync(new GeneralUser("U2", "u2@test.com", "p"));
            await repo.SaveChangesAsync();

            // Act
            var count = await repo.CountAsync();

            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetByEmailAsync_CasoInsensitive()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new UserRepository(context);
            var user = new GeneralUser("User", "Case@Test.Com", "pass");
            await repo.AddAsync(user);
            await repo.SaveChangesAsync();

            // Act
            var result = await repo.GetByEmailAsync("case@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
        }
    }
}
