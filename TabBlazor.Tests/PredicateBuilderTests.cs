using System;
using System.Linq.Expressions;
using TabBlazor.Dashboards;

namespace TabBlazor.Tests
{
    public class PredicateBuilderTests
    {
        [Fact]
        public void True_matches_everything()
        {
            Assert.True(PredicateBuilder.True<int>().Compile()(42));
        }

        [Fact]
        public void False_matches_nothing()
        {
            Assert.False(PredicateBuilder.False<int>().Compile()(42));
        }

        [Fact]
        public void And_requires_both_predicates()
        {
            Expression<Func<int, bool>> positive = x => x > 0;
            Expression<Func<int, bool>> even = x => x % 2 == 0;
            var predicate = positive.And(even).Compile();

            Assert.True(predicate(4));
            Assert.False(predicate(3));
            Assert.False(predicate(-2));
        }

        [Fact]
        public void Or_requires_either_predicate()
        {
            Expression<Func<int, bool>> negative = x => x < 0;
            Expression<Func<int, bool>> even = x => x % 2 == 0;
            var predicate = negative.Or(even).Compile();

            Assert.True(predicate(-3));
            Assert.True(predicate(4));
            Assert.False(predicate(3));
        }

        [Fact]
        public void Not_inverts_predicate()
        {
            Expression<Func<int, bool>> positive = x => x > 0;
            var predicate = positive.Not().Compile();

            Assert.False(predicate(1));
            Assert.True(predicate(-1));
        }
    }
}
