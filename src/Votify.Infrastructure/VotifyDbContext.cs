using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Votify.Domain;

namespace Votify.Infrastructure;

public class VotifyDbContext : DbContext
{
    public VotifyDbContext(DbContextOptions<VotifyDbContext> options) : base(options) { }
}
