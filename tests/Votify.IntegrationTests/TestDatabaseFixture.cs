using Microsoft.EntityFrameworkCore;
using Votify.Infrastructure;

namespace Votify.IntegrationTests;

public class TestDatabaseFixture : IDisposable
{
    private readonly string _dbName = $"db_{Guid.NewGuid():N}";
    private Microsoft.Data.Sqlite.SqliteConnection? _connection;

    public VotifyDbContext CreateContext()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection = new Microsoft.Data.Sqlite.SqliteConnection($"DataSource={_dbName}");
            _connection.Open();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = OFF;";
                cmd.ExecuteNonQuery();
            }

            using var ctx = new VotifyDbContext(
                new DbContextOptionsBuilder<VotifyDbContext>()
                    .UseSqlite(_connection).Options);
            ctx.Database.EnsureCreated();
        }

        return new VotifyDbContext(
            new DbContextOptionsBuilder<VotifyDbContext>()
                .UseSqlite(_connection).Options);
    }

    public async Task ClearDatabaseAsync()
    {
        using var ctx = CreateContext();
        ctx.Votes.RemoveRange(ctx.Votes);
        ctx.WeightedVotes.RemoveRange(ctx.WeightedVotes);
        ctx.WeightedCriterionScores.RemoveRange(ctx.WeightedCriterionScores);
        ctx.CriterionScores.RemoveRange(ctx.CriterionScores);
        ctx.AuditRequests.RemoveRange(ctx.AuditRequests);
        ctx.Criteria.RemoveRange(ctx.Criteria);
        ctx.Prizes.RemoveRange(ctx.Prizes);
        ctx.VotingSessions.RemoveRange(ctx.VotingSessions);
        ctx.ProjectCategories.RemoveRange(ctx.ProjectCategories);
        ctx.Projects.RemoveRange(ctx.Projects);
        ctx.Categories.RemoveRange(ctx.Categories);
        ctx.Users.RemoveRange(ctx.Users);
        ctx.Events.RemoveRange(ctx.Events);
        await ctx.SaveChangesAsync();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
