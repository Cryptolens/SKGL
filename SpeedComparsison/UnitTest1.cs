using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace SpeedComparsison
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CreateAndValidateSimple()
        {
            SKGL.Generate gen = new SKGL.Generate();
            string a = gen.doKey(30);

            SKGL.Validate val = new SKGL.Validate();

            val.Key = a;

            Assert.IsTrue(val.IsValid == true);
            Assert.IsTrue(val.IsExpired == false);
            Assert.IsTrue(val.SetTime == 30);

        }
    }
}
