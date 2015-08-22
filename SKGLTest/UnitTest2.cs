using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;

namespace SKGLTest
{
    [TestClass]
    public class UnitTest2
    {
        [TestMethod]
        public void TestMethod1()
        {
            string stringSignature;
            Bob bob = new Bob();
            using (ECDsaCng dsa = new ECDsaCng(100))
            {
                dsa.HashAlgorithm = CngAlgorithm.MD5;
                bob.key = dsa.Key.Export(CngKeyBlobFormat.EccPublicBlob);

                byte[] data = new byte[] { 21, 5, 8, 12, 207 };

                byte[] signature = dsa.SignData(data);
                stringSignature = Convert.ToBase64String(signature);

                bob.Receive(data, signature);
            }


        }


    }

    public class Bob
    {
        public byte[] key;

        public void Receive(byte[] data, byte[] signature)
        {
            using (ECDsaCng ecsdKey = new ECDsaCng(CngKey.Import(key, CngKeyBlobFormat.EccPublicBlob)))
            {
                if (ecsdKey.VerifyData(data, signature))
                    Console.WriteLine("Data is good");
                else
                    Console.WriteLine("Data is bad");
            }
        }
    }
}
