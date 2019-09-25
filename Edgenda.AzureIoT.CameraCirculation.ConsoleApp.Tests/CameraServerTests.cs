using Edgenda.AzureIoT.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Edgenda.AzureIoT.CameraCirculation.ConsoleApp.Tests
{
    [TestClass]
    public class CameraServerTests
    {
        [TestMethod]
        public void CanProcessGetByCoordinatesCommandFromString()
        {
            var cmd = new GetByCoordinatesCommand() { Name = "get-by-coordinates", Parameters = new double[] { -73.532344350311, 45.600982799511 } };
            var strCmd = JsonConvert.SerializeObject(cmd);
            var target = new CameraServer();
            var val = target.ProcessCommand(strCmd);
            Assert.IsNotNull(val);
            Assert.IsTrue(!string.IsNullOrEmpty(val));
        }
    }
}
