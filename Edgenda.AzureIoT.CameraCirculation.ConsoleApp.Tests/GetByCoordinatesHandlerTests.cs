using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Edgenda.AzureIoT.CameraCirculation.ConsoleApp.Tests
{
    [TestClass]
    public class GetByCoordinatesHandlerTests
    {
        [TestMethod]
        public void CanLoadFeatureCollectionFromJson()
        {
            GetByCoordinatesHandler target = new GetByCoordinatesHandler();
            var features = target.GetByCoordinates(-73.532344350311, 45.600982799511, 5);
            Assert.AreEqual(5, features.Length);
            Assert.AreEqual(4, features[0].CameraId);
        }
    }
}
