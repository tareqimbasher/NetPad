using NetPad.Utilities;
using System.Text;
using Xunit;

namespace NetPad.Domain.Tests.Utilities
{
    public class StringUtilsTests
    {
        [Fact]
        public void JoinToString_JoinsToString()
        {
            var collection = new[] { "Test1", "Test2" };

            var joinedStr = StringUtils.JoinToString(collection, ",");

            Assert.Equal("Test1,Test2", joinedStr);
        }

        [Fact]
        public void RemoveLeadingBOMString_RemovesLeadingBOMString()
        {
            var strWithBOM = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble()) + "test";

            var strWithoutBOM = StringUtils.RemoveLeadingBOMString(strWithBOM);

            Assert.Equal("test", strWithoutBOM);
        }

        [Fact]
        public void RemoveLeadingBOMString_DoesNotRemoveNonLeadingBOMString()
        {
            var strWithBOM = "test" + Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

            var result = StringUtils.RemoveLeadingBOMString(strWithBOM);

            Assert.Equal(strWithBOM, result);
        }
    }
}
