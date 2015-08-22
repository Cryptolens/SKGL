
using System;
using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;

using System.Text;
using System.Management;
using System.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SKGLTest
{
    [TestClass]
    public class SKGLPlusTest
    {
        [TestMethod]
        public void test()
        {
            SKGL.Plus.Generate gen = new SKGL.Plus.Generate();

            var a = gen.doKey(3, 3, 1, 2);

            SKGL.Plus.Validate val = new SKGL.Plus.Validate();

            val.Key = a;

            //problem with max users. 1 -> 10?
            
           
        }
    }
}
