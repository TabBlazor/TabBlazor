#nullable enable
using System.ComponentModel;
using System.Linq;

namespace TabBlazor.Tests.Components.DataViews
{
    public class EnumDisplayTests
    {
        private enum Status
        {
            [Description("Waiting")] Open,
            Closed,
            __Unknown
        }

        private enum Hidden
        {
            Open,
            [Browsable(false)] Legacy,
            [Browsable(true)] Closed
        }

        [Fact]
        public void GetValues_leaves_out_members_with_a_leading_double_underscore() =>
            Assert.Equal([Status.Open, Status.Closed], EnumDisplay.GetValues(typeof(Status)).Cast<Status>());

        [Fact]
        public void GetValues_leaves_out_members_marked_not_browsable() =>
            Assert.Equal([Hidden.Open, Hidden.Closed], EnumDisplay.GetValues(typeof(Hidden)).Cast<Hidden>());

        [Fact]
        public void GetValues_returns_an_array_of_the_enum_type() =>
            Assert.IsType<Status[]>(EnumDisplay.GetValues(typeof(Status)));

        private enum Kind
        {
            SapS4,
            [Description("Other system")] Other
        }

        [Fact]
        public void GetName_uses_the_description() =>
            Assert.Equal("Waiting", EnumDisplay.GetName(Status.Open));

        [Fact]
        public void GetName_asks_the_formatter_first_and_falls_back_when_it_returns_null()
        {
            EnumDisplay.Formatter = e => e is Kind.SapS4 ? "SAP S/4HANA" : null;
            try
            {
                Assert.Equal("SAP S/4HANA", EnumDisplay.GetName(Kind.SapS4));
                Assert.Equal("Other system", EnumDisplay.GetName(Kind.Other));
            }
            finally
            {
                EnumDisplay.Formatter = null;
            }

            Assert.Equal("SapS4", EnumDisplay.GetName(Kind.SapS4));
        }
    }
}
