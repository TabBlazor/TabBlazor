using System.Linq;

namespace TabBlazor.Tests
{
    public class EnumHelperTests
    {
        private enum Sample { First, Second, Third }

        [Fact]
        public void GetList_returns_all_enum_values()
        {
            var result = EnumHelper.GetList<Sample>();
            Assert.Equal(new[] { Sample.First, Sample.Second, Sample.Third }, result);
        }

        [Fact]
        public void GetNullableList_returns_all_values_as_nullable()
        {
            var result = EnumHelper.GetNullableList<Sample>();
            Assert.Equal(3, result.Count);
            Assert.Contains((Sample?)Sample.Second, result);
        }
    }
}
