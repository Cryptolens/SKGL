using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SKGLTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void MachineCodeTest()
        {
            SKGL.Generate gen = new SKGL.Generate();
            string a= gen.MachineCode.ToString();
        }

        [TestMethod]
        public void CreateAndValidateSimple()
        {
            SKGL.Generate gen = new SKGL.Generate();
            string a  = gen.doKey(30);

            SKGL.Validate val = new SKGL.Validate();

            val.Key = a;
            
            Assert.IsTrue(val.IsValid == true);
            Assert.IsTrue(val.IsExpired ==false);
            Assert.IsTrue(val.SetTime == 30);

        }
        [TestMethod]
        public void CreateAndValidateA()
        {

            SKGL.Validate val = new SKGL.Validate();

            val.Key = "MXNBF-ITLDZ-WPOBY-UCHQW";
            val.secretPhase = "567";

            Assert.IsTrue(val.IsValid == true);
            Assert.IsTrue(val.IsExpired == true);
            Assert.IsTrue(val.SetTime == 30);

        }
        [TestMethod]
        public void CreateAndValidateC()
        {
            SKGL.SerialKeyConfiguration skm = new SKGL.SerialKeyConfiguration();

            SKGL.Generate gen = new SKGL.Generate(skm);
            skm.Features[0] = true;
            gen.secretPhase = "567";
            string a = gen.doKey(37);


            SKGL.Validate val = new SKGL.Validate();

            val.Key = a;
            val.secretPhase = "567";

            Assert.IsTrue(val.IsValid == true);
            Assert.IsTrue(val.IsExpired == false);
            Assert.IsTrue(val.SetTime == 37);
            Assert.IsTrue(val.Features[0] == true);
            Assert.IsTrue(val.Features[1] == false);

        }


        [TestMethod]
        public void CreateAndValidateCJ()
        {


            SKGL.Validate val = new SKGL.Validate();

            val.Key = "LZWXQ-SMBAS-JDVDL-XTEHB";
            val.secretPhase = "567";

            int timeLeft = val.DaysLeft;

            Assert.IsTrue(val.IsValid == true);
            Assert.IsTrue(val.IsExpired == true);
            Assert.IsTrue(val.SetTime == 30);
            Assert.IsTrue(val.Features[0] == true);
            //Assert.IsTrue(val.Features[1] == false);

        }

        [TestMethod]
        public void CreateAndValidateAM()
        {
            SKGL.Generate gen = new SKGL.Generate();
            string a = gen.doKey(30);

            SKGL.Validate ValidateAKey = new SKGL.Validate();

            ValidateAKey.Key = a;

            Assert.IsTrue(ValidateAKey.IsValid == true);
            Assert.IsTrue(ValidateAKey.IsExpired == false);
            Assert.IsTrue(ValidateAKey.SetTime == 30);

            if (ValidateAKey.IsValid)
            {
                // displaying date
                // remember to use .ToShortDateString after each date!
                Console.WriteLine("This key is created {0}", ValidateAKey.CreationDate.ToShortDateString());
                Console.WriteLine("This key will expire {0}", ValidateAKey.ExpireDate.ToShortDateString());

                Console.WriteLine("This key is set to be valid in {0} day(s)", ValidateAKey.SetTime);
                Console.WriteLine("This key has {0} day(s) left", ValidateAKey.DaysLeft);

            }
            else
            {
                // if invalid
                Console.WriteLine("Invalid!");
            }

        }
    }
}
