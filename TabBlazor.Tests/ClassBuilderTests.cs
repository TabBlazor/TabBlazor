using System.Collections.Generic;

namespace TabBlazor.Tests
{
    public class ClassBuilderTests
    {
        [Fact]
        public void Add_splits_and_deduplicates_class_names()
        {
            var result = new ClassBuilder("btn btn primary").ToString();
            Assert.Equal("btn primary", result);
        }

        [Fact]
        public void Add_ignores_null_and_whitespace()
        {
            var result = new ClassBuilder().Add(null).Add("   ").Add("btn").ToString();
            Assert.Equal("btn", result);
        }

        [Fact]
        public void AddIf_adds_only_when_condition_true()
        {
            Assert.Equal("a b", new ClassBuilder("a").AddIf("b", true).ToString());
            Assert.Equal("a", new ClassBuilder("a").AddIf("b", false).ToString());
        }

        [Fact]
        public void AddCompare_adds_class_when_values_match()
        {
            Assert.Equal("a active", new ClassBuilder("a").AddCompare("active", 1, 1).ToString());
            Assert.Equal("a", new ClassBuilder("a").AddCompare("active", 1, 2).ToString());
        }

        [Fact]
        public void AddCompare_with_dictionary_adds_matching_entry()
        {
            var map = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } };
            Assert.Equal("two", new ClassBuilder().AddCompare(2, map).ToString());
        }

        [Fact]
        public void Remove_is_case_insensitive()
        {
            Assert.Equal("b", new ClassBuilder("a b").Remove("A").ToString());
        }

        [Fact]
        public void ToString_returns_null_when_empty()
        {
            Assert.Null(new ClassBuilder().ToString());
        }
    }
}
