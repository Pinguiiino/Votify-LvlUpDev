using Microsoft.Extensions.DependencyInjection;
using Votify.Domain.VoteFolder.Strategies;

namespace Votify.Infrastructure;

public static class VotingStrategyServiceExtensions
{
    public static IServiceCollection AddVotingStrategies(this IServiceCollection services)
    {
        services.AddScoped<IVotingStrategy, TopNVotingStrategy>();
        services.AddScoped<IVotingStrategy, WeightedVotingStrategy>();
        services.AddScoped<IVotingStrategy, PointDistributionVotingStrategy>();
        services.AddScoped<VotingStrategyResolver>();

        return services;
    }
}
