namespace BackendAPI.Services
{
    public class SecretMovieService
    {
        public const decimal Discount = 2.50m;

        /// <summary>
        /// Berekent de prijs voor een geheime film ticket:
        /// de reguliere tarifprijs minus €2,50 korting (minimaal €0).
        /// </summary>
        public decimal GetDiscountedPrice(decimal regularPrice)
        {
            var discounted = regularPrice - Discount;
            return discounted < 0 ? 0 : discounted;
        }
    }
}
