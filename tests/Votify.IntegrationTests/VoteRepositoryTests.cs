using Xunit;
using Votify.Domain.VoteFolder;
using Votify.Domain.EventFolder;
using Votify.Domain.CategoryFolder;
using Votify.Infrastructure.Repositories;

namespace Votify.IntegrationTests
{
    public class VoteRepositoryTests : IClassFixture<TestDatabaseFixture>, IAsyncLifetime
    {
        private readonly TestDatabaseFixture _fixture;

        public VoteRepositoryTests(TestDatabaseFixture fixture) => _fixture = fixture;

        public Task InitializeAsync() => _fixture.ClearDatabaseAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private static PublicVote CreateVote(string session, string project, string user, string category, int position, string? comment = null)
        {
            var v = new PublicVote(session, project, user, category, position, comment);
            v.GenerateIntegrityHash();
            return v;
        }

        [Fact]
        public async Task AddAsync_GuardaVoto_YGetLoRecupera()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new VoteRepository(context);
            var vote = CreateVote("session-1", "proj-1", "user-1", "cat-1", 1, "Bueno");

            // Act
            var result = await repo.AddAsync(vote);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("proj-1", result.VotedProjectId);
            Assert.Equal("user-1", result.UserId);
        }

        [Fact]
        public async Task GetByUserIdAndCategoryAsync_RetornaVotosOrdenados()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new VoteRepository(context);
            await repo.AddAsync(CreateVote("s1", "p2", "u1", "c1", 2));
            await repo.AddAsync(CreateVote("s1", "p1", "u1", "c1", 1));
            await repo.SaveChangesAsync();

            // Act
            var result = await repo.GetByUserIdAndCategoryAsync("u1", "c1");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].TopPosition);
            Assert.Equal(2, result[1].TopPosition);
        }

        [Fact]
        public async Task CountVotesByUserInCategoryAsync_CuentaCorrectamente()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new VoteRepository(context);
            await repo.AddAsync(CreateVote("s1", "p1", "u1", "c1", 1));
            await repo.AddAsync(CreateVote("s1", "p2", "u1", "c1", 2));
            await repo.AddAsync(CreateVote("s1", "p3", "u1", "c1", 3));
            await repo.SaveChangesAsync();

            // Act
            var count = await repo.CountVotesByUserInCategoryAsync("u1", "c1");

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task HasUserVotedForProjectAsync_SiVoto_RetornaTrue()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new VoteRepository(context);
            await repo.AddAsync(CreateVote("s1", "proj-1", "u1", "c1", 1));
            await repo.SaveChangesAsync();

            // Act
            var result = await repo.HasUserVotedForProjectAsync("u1", "proj-1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasUserVotedForProjectAsync_NoVoto_RetornaFalse()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new VoteRepository(context);

            // Act
            var result = await repo.HasUserVotedForProjectAsync("u1", "proj-999");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetByProjectAsync_RetornaVotosDelProyecto()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new VoteRepository(context);
            await repo.AddAsync(CreateVote("s1", "proj-1", "u1", "c1", 1));
            await repo.AddAsync(CreateVote("s1", "proj-1", "u2", "c1", 2));
            await repo.AddAsync(CreateVote("s1", "proj-2", "u3", "c1", 1));
            await repo.SaveChangesAsync();

            // Act
            var result = await repo.GetByProjectAsync("proj-1");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, v => Assert.Equal("proj-1", v.VotedProjectId));
        }

        [Fact]
        public async Task GetByProjectIdsAsync_RetornaVotosDeMultiplesProyectos()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new VoteRepository(context);
            await repo.AddAsync(CreateVote("s1", "proj-1", "u1", "c1", 1));
            await repo.AddAsync(CreateVote("s1", "proj-2", "u2", "c1", 1));
            await repo.AddAsync(CreateVote("s1", "proj-3", "u3", "c1", 1));
            await repo.SaveChangesAsync();

            // Act
            var result = await repo.GetByProjectIdsAsync(new[] { "proj-1", "proj-3" });

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task RemoveByUserInCategoryAsync_EliminaVotosDelUsuario()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new VoteRepository(context);
            await repo.AddAsync(CreateVote("s1", "p1", "u1", "c1", 1));
            await repo.AddAsync(CreateVote("s1", "p2", "u1", "c1", 2));
            await repo.AddAsync(CreateVote("s1", "p3", "u2", "c1", 1));
            await repo.SaveChangesAsync();

            // Act
            await repo.RemoveByUserInCategoryAsync("u1", "c1");
            await repo.SaveChangesAsync();

            var remaining = await repo.GetByUserIdAndCategoryAsync("u1", "c1");
            var otherVotes = await repo.GetByUserIdAndCategoryAsync("u2", "c1");

            // Assert
            Assert.Empty(remaining);
            Assert.Single(otherVotes);
        }

        [Fact]
        public async Task RemoveByUserInCategoryAsync_ConSessionId_EliminaSoloEsaSesion()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new VoteRepository(context);
            await repo.AddAsync(CreateVote("s1", "p1", "u1", "c1", 1));
            await repo.AddAsync(CreateVote("s2", "p2", "u1", "c1", 1));
            await repo.SaveChangesAsync();

            // Act
            await repo.RemoveByUserInCategoryAsync("u1", "c1", "s1");
            await repo.SaveChangesAsync();

            var s1Votes = (await repo.GetByUserIdAndCategoryAsync("u1", "c1"))
                .Where(v => v.VotingSessionId == "s1").ToList();
            var s2Votes = (await repo.GetByUserIdAndCategoryAsync("u1", "c1"))
                .Where(v => v.VotingSessionId == "s2").ToList();

            // Assert
            Assert.Empty(s1Votes);
            Assert.Single(s2Votes);
        }

        [Fact]
        public async Task AddRangeAsync_AgregaMultiplesVotos()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var repo = new VoteRepository(context);
            var votes = new List<Vote>
            {
                CreateVote("s1", "p1", "u1", "c1", 1),
                CreateVote("s1", "p2", "u1", "c1", 2),
                CreateVote("s1", "p3", "u1", "c1", 3)
            };

            // Act
            await repo.AddRangeAsync(votes);
            await repo.SaveChangesAsync();

            var result = await repo.GetByUserIdAndCategoryAsync("u1", "c1");

            // Assert
            Assert.Equal(3, result.Count);
        }
    }
}
