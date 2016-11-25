using System;
using Xunit;
using RT.ValueFilter;
using RT.ValueFilter.Struct;

namespace Tests {

    public class Tests {
        [Fact]
        public void Test1() {
            Assert.True(true);
        }

        [Fact]
        public void TestRemoveNonDigits() {
            var t = "Hello world 123";
            var f = new Filtered<string>(StringFilters.RemoveNonDigits, t);
            Assert.True(f.Value=="123");
        }

    }

}