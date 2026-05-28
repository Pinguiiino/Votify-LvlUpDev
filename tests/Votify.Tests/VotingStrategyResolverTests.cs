using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using Votify.Domain.VoteFolder;
using Votify.Domain.VoteFolder.Strategies;

namespace Votify.Tests
{
    public class VotingStrategyResolverTests
    {
        [Fact]
        public void Resolve_TopN_RetornaTopNVotingStrategy()
        {
            // Arrange
            var topN = new Mock<IVotingStrategy>();
            topN.Setup(s => s.SupportedType).Returns(EvaluationType.TopN);
            var resolver = new VotingStrategyResolver(new[] { topN.Object });

            // Act
            var result = resolver.Resolve(EvaluationType.TopN);

            // Assert
            Assert.Same(topN.Object, result);
        }

        [Fact]
        public void Resolve_PointDistribution_RetornaPointDistributionStrategy()
        {
            // Arrange
            var pointDist = new Mock<IVotingStrategy>();
            pointDist.Setup(s => s.SupportedType).Returns(EvaluationType.PointDistribution);
            var resolver = new VotingStrategyResolver(new[] { pointDist.Object });

            // Act
            var result = resolver.Resolve(EvaluationType.PointDistribution);

            // Assert
            Assert.Same(pointDist.Object, result);
        }

        [Fact]
        public void Resolve_WeightedScale_RetornaWeightedStrategy()
        {
            // Arrange
            var weighted = new Mock<IVotingStrategy>();
            weighted.Setup(s => s.SupportedType).Returns(EvaluationType.WeightedScale);
            var resolver = new VotingStrategyResolver(new[] { weighted.Object });

            // Act
            var result = resolver.Resolve(EvaluationType.WeightedScale);

            // Assert
            Assert.Same(weighted.Object, result);
        }

        [Fact]
        public void Resolve_TipoNoRegistrado_LanzaNotSupportedException()
        {
            // Arrange
            var resolver = new VotingStrategyResolver(Array.Empty<IVotingStrategy>());

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(
                () => resolver.Resolve(EvaluationType.TopN));
            Assert.Contains("No hay ninguna estrategia registrada", ex.Message);
        }

        [Fact]
        public void Constructor_MultiplesEstrategias_TodasResolubles()
        {
            // Arrange
            var topN = new Mock<IVotingStrategy>();
            topN.Setup(s => s.SupportedType).Returns(EvaluationType.TopN);
            var pointDist = new Mock<IVotingStrategy>();
            pointDist.Setup(s => s.SupportedType).Returns(EvaluationType.PointDistribution);
            var weighted = new Mock<IVotingStrategy>();
            weighted.Setup(s => s.SupportedType).Returns(EvaluationType.WeightedScale);

            var resolver = new VotingStrategyResolver(new[] { topN.Object, pointDist.Object, weighted.Object });

            // Act & Assert
            Assert.Same(topN.Object, resolver.Resolve(EvaluationType.TopN));
            Assert.Same(pointDist.Object, resolver.Resolve(EvaluationType.PointDistribution));
            Assert.Same(weighted.Object, resolver.Resolve(EvaluationType.WeightedScale));
        }

        [Fact]
        public void Constructor_EstrategiasDuplicadas_UltimaPrevalece()
        {
            // Arrange
            var topN1 = new Mock<IVotingStrategy>();
            topN1.Setup(s => s.SupportedType).Returns(EvaluationType.TopN);
            var topN2 = new Mock<IVotingStrategy>();
            topN2.Setup(s => s.SupportedType).Returns(EvaluationType.TopN);

            var resolver = new VotingStrategyResolver(new[] { topN1.Object, topN2.Object });

            // Act
            var result = resolver.Resolve(EvaluationType.TopN);

            // Assert
            Assert.Same(topN2.Object, result);
        }
    }
}
