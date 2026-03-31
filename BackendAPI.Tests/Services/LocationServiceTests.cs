using BackendAPI.Services;

namespace BackendAPI.Tests.Services
{
    public class LocationServiceTests
    {
        private readonly LocationService _service = new();

        [Fact]
        public void GetGoogleMapsDirectionsUrl_GeeftNietLegeUrl()
        {
            // Act
            string url = _service.GetGoogleMapsDirectionsUrl();

            // Assert
            Assert.False(string.IsNullOrEmpty(url));
        }

        [Fact]
        public void GetGoogleMapsDirectionsUrl_BegintMetGoogleMapsUrl()
        {
            // Act
            string url = _service.GetGoogleMapsDirectionsUrl();

            // Assert
            Assert.StartsWith("https://www.google.com/maps/dir/", url);
        }

        [Fact]
        public void GetGoogleMapsDirectionsUrl_BevatGecodeerdesBioscoopAdres()
        {
            // Arrange
            string verwachtGecodeerdAdres = Uri.EscapeDataString(LocationService.CinemaAddress);

            // Act
            string url = _service.GetGoogleMapsDirectionsUrl();

            // Assert
            Assert.Contains(verwachtGecodeerdAdres, url);
        }

        [Fact]
        public void GetGoogleMapsDirectionsUrl_BevatApiParameter()
        {
            // Act
            string url = _service.GetGoogleMapsDirectionsUrl();

            // Assert
            Assert.Contains("api=1", url);
        }

        [Fact]
        public void GetGoogleMapsDirectionsUrl_BevatDestinationParameter()
        {
            // Act
            string url = _service.GetGoogleMapsDirectionsUrl();

            // Assert
            Assert.Contains("destination=", url);
        }

        [Fact]
        public void CinemaAddress_IsNietLeeg()
        {
            // Assert
            Assert.False(string.IsNullOrEmpty(LocationService.CinemaAddress));
        }
    }
}
