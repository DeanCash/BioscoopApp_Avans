namespace BackendAPI.Services
{
    public class LocationService
    {
        public const string CinemaAddress = "Lovensdijkstraat 61, 4818 AJ Breda";

        /// <summary>
        /// Geeft een Google Maps routebeschrijving-URL terug naar de bioscoop.
        /// </summary>
        public string GetGoogleMapsDirectionsUrl()
        {
            var encoded = Uri.EscapeDataString(CinemaAddress);
            return $"https://www.google.com/maps/dir/?api=1&destination={encoded}";
        }
    }
}
