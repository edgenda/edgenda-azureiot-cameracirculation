using System;
using System.Linq;
using Edgenda.AzureIoT.Common;
using LazyCache;
using Newtonsoft.Json;
using RestSharp;

namespace Edgenda.AzureIoT.CameraCirculation.ConsoleApp
{
    /// <summary>
    /// Get camera properties by coordinates handler
    /// </summary>
    public class GetByCoordinatesHandler
    {
        private readonly CachingService cache;

        /// <summary>
        /// Default constructor
        /// </summary>
        public GetByCoordinatesHandler()
        {
            this.cache = new LazyCache.CachingService();
        }

        /// <summary>
        /// Gets camera data as a string for deserialization
        /// </summary>
        /// <returns></returns>
        private string GetCameraData()
        {
            var item = cache.GetOrAdd("camera-data", cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromSeconds(10);
                var client = new RestClient("http://ville.montreal.qc.ca/circulation/sites/ville.montreal.qc.ca.circulation/files/cameras-de-circulation.json");
                var request = new RestRequest(Method.GET);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("Connection", "keep-alive");
                request.AddHeader("Accept-Encoding", "gzip, deflate");
                request.AddHeader("Host", "ville.montreal.qc.ca");
                request.AddHeader("Accept", "application/json");
                IRestResponse response = client.Execute(request);
                return response.Content;
            });
            return item;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private double DistanceInKmBetweenEarthCoordinates(double lat1, double lon1, double lat2, double lon2)
        {
            var earthRadiusKm = 6371;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            lat1 = DegreesToRadians(lat1);
            lat2 = DegreesToRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        private IRestResponse GetImage(string url)
        {
            var uri = new Uri(url);
            var client = new RestClient(url);
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Accept-Encoding", "gzip, deflate");
            request.AddHeader("Host", "www1.ville.montreal.qc.ca");
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Accept", "*/*");
            IRestResponse response = client.Execute(request);
            return response;
        }

        /// <summary>
        /// Gets camera properties by coordinates
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public CameraProperties[] GetByCoordinates(double longitude, double latitude, int count = 1)
        {
            var data = this.GetCameraData();
            GeometryFeatures features = JsonConvert.DeserializeObject<GeometryFeatures>(data);
            var cameras = features.Features
                .OrderBy((feature) => { return DistanceInKmBetweenEarthCoordinates(latitude, longitude, feature.Geometry.Coordinates[1], feature.Geometry.Coordinates[0]); })
                .Take(count)
                .Select((feature) =>
                {
                    var properties = feature.Properties;
                    var response = GetImage(properties.LiveImageUrl.AbsoluteUri);
                    var contentTypeHeader = response.Headers.FirstOrDefault(h => h.Name == "Content-Type");
                    var contentType = contentTypeHeader != null ? contentTypeHeader.Value.ToString() : "";
                    var imageData = response.RawBytes;
                    var imageString = Convert.ToBase64String(imageData);
                    properties.ImageType = contentType;
                    properties.ImageData = imageString;
                    return feature.Properties;
                })
                .ToArray();
            return cameras;
        }
    }
}
