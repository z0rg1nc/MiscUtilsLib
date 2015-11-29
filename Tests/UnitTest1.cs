using BtmI2p.MiscUtils;
using Xunit;
using Xunit.Abstractions;

namespace BtmI2p.TestMiscUtilsNs
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _helper;
        public UnitTest1(ITestOutputHelper helper)
        {
            _helper = helper;
        }

        [Fact]
        public void T1()
        {
            var obj1 = new object();
            object objNull = null;
            _helper.WriteLine($"{obj1.WriteObjectToJson()} {objNull.WriteObjectToJson()}");
        }
    }
}
