using Microsoft.VisualStudio.TestTools.UnitTesting;
using Biobanking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Settings;

namespace Biobanking.Tests
{
    [TestClass()]
    public class MappingCalculatorTests
    {
        static MappingCalculator mappingCalculator;
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            string sFile = Utility.GetExeFolder() + "calibrations.xml";
            mappingCalculator = new MappingCalculator(sFile);
        }

        [TestMethod()]
        public void GetVolumeTest()
        {
            var v10 = mappingCalculator.GetVolumeFromHeight(10);
            //Assert.AreEqual(1200, v10);
            var v15 = mappingCalculator.GetVolumeFromHeight(15);
            //Assert.AreEqual(1700, v15);
            var v20 = mappingCalculator.GetVolumeFromHeight(20);

            var v29 = mappingCalculator.GetVolumeFromHeight(29.9);
            //Assert.AreEqual( 2200, v20);
            var v50 = mappingCalculator.GetVolumeFromHeight(50);
            //Assert.AreEqual( 5200, v50);
            var v577 = mappingCalculator.GetVolumeFromHeight(57.7);
            var tipV10 = mappingCalculator.GetTipVolumeFromHeight(10);
            //Assert.AreEqual(1300, tipV10);

            var tipV15 = mappingCalculator.GetTipVolumeFromHeight(15);
            //Assert.AreEqual(1800, tipV15);

            var tipV25 = mappingCalculator.GetTipVolumeFromHeight(25);
            //Assert.AreEqual(2850, tipV25);

            var tipV40 = mappingCalculator.GetTipVolumeFromHeight(40);
            //Assert.AreEqual(4500, tipV40);
            double h16 = mappingCalculator.GetHeightFromVolume(1600);
            double h21 = mappingCalculator.GetHeightFromVolume(2100);
            double h19 = mappingCalculator.GetHeightFromVolume(1900);
            double h70 = mappingCalculator.GetHeightFromVolume(7000);

            double tipV16 = mappingCalculator.GetTipVolumeFromVolume(1600);
            double tipV21 = mappingCalculator.GetTipVolumeFromVolume(2100);
            double tipV50 = mappingCalculator.GetTipVolumeFromVolume(5000);
            
        }
    }
}