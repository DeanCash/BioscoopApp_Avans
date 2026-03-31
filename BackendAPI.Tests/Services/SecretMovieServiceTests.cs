using BackendAPI.Services;

namespace BackendAPI.Tests.Services
{
    public class SecretMovieServiceTests
    {
        private readonly SecretMovieService _service = new();

        [Fact]
        public void GetDiscountedPrice_Volwassene_GeeftKortingVanTweeEuroVijftig()
        {
            // Arrange: regulier tarief Volwassene = €12,50
            decimal regulierePrijs = 12.50m;

            // Act
            decimal kortingsPrijs = _service.GetDiscountedPrice(regulierePrijs);

            // Assert: €12,50 - €2,50 = €10,00
            Assert.Equal(10.00m, kortingsPrijs);
        }

        [Fact]
        public void GetDiscountedPrice_Kind_GeeftKortingVanTweeEuroVijftig()
        {
            // Arrange: regulier tarief Kind = €8,50
            decimal regulierePrijs = 8.50m;

            // Act
            decimal kortingsPrijs = _service.GetDiscountedPrice(regulierePrijs);

            // Assert: €8,50 - €2,50 = €6,00
            Assert.Equal(6.00m, kortingsPrijs);
        }

        [Fact]
        public void GetDiscountedPrice_Senior_GeeftKortingVanTweeEuroVijftig()
        {
            // Arrange: regulier tarief Senioren = €10,00
            decimal regulierePrijs = 10.00m;

            // Act
            decimal kortingsPrijs = _service.GetDiscountedPrice(regulierePrijs);

            // Assert: €10,00 - €2,50 = €7,50
            Assert.Equal(7.50m, kortingsPrijs);
        }

        [Fact]
        public void GetDiscountedPrice_Student_GeeftKortingVanTweeEuroVijftig()
        {
            // Arrange: regulier tarief Student = €9,50
            decimal regulierePrijs = 9.50m;

            // Act
            decimal kortingsPrijs = _service.GetDiscountedPrice(regulierePrijs);

            // Assert: €9,50 - €2,50 = €7,00
            Assert.Equal(7.00m, kortingsPrijs);
        }

        [Fact]
        public void Discount_IsExactTweeEuroVijftig()
        {
            Assert.Equal(2.50m, SecretMovieService.Discount);
        }

        [Fact]
        public void GetDiscountedPrice_PrijsKleinerDanKorting_GeeftNul()
        {
            // Arrange: een prijs lager dan de korting mag niet negatief worden
            decimal regulierePrijs = 1.00m;

            // Act
            decimal kortingsPrijs = _service.GetDiscountedPrice(regulierePrijs);

            // Assert: nooit negatief
            Assert.Equal(0m, kortingsPrijs);
        }
    }
}
