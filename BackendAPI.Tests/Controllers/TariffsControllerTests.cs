using API.Services;
using BackendAPI.Controllers;
using BackendAPI.DTOs.Tariffs;
using BackendAPI.Models.Tariff;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BackendAPI.Tests.Controllers
{
    public class TariffsControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;

        public TariffsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
        }

        public void Dispose() => _db.Dispose();

        private TariffModel CreateTariff(string type, string displayName, decimal price = 5.00m, int sortOrder = 0)
        {
            return new TariffModel
            {
                TariffId = Guid.NewGuid(),
                TariffType = type,
                DisplayName = displayName,
                Price = price,
                SortOrder = sortOrder
            };
        }

        private TariffsController BuildController()
        {
            return new TariffsController(_db);
        }

        [Fact]
        public async Task GetTariffs_ReturnsAllOrderedBySortOrder()
        {
            _db.Tariffs.Add(CreateTariff("Adult", "Volwassene", price: 8.50m, sortOrder: 2));
            _db.Tariffs.Add(CreateTariff("Child", "Kind", price: 5.00m, sortOrder: 1));
            _db.Tariffs.Add(CreateTariff("Senior", "Senior", price: 6.50m, sortOrder: 3));
            await _db.SaveChangesAsync();

            var controller = BuildController();

            var result = await controller.GetTariffs();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.EnumerateArray().ToArray();

            Assert.Equal(3, items.Length);
            Assert.Equal("Child", items[0].GetProperty("TariffType").GetString());
            Assert.Equal("Adult", items[1].GetProperty("TariffType").GetString());
            Assert.Equal("Senior", items[2].GetProperty("TariffType").GetString());
        }

        [Fact]
        public async Task GetTariff_ValidId_ReturnsTariff()
        {
            var tariff = CreateTariff("Student", "Student", price: 4.25m);
            _db.Tariffs.Add(tariff);
            await _db.SaveChangesAsync();

            var controller = BuildController();

            var result = controller.GetTariff(tariff.TariffId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Student", doc.RootElement.GetProperty("TariffType").GetString());
            Assert.Equal("Student", doc.RootElement.GetProperty("DisplayName").GetString());
            Assert.Equal(4.25m, doc.RootElement.GetProperty("Price").GetDecimal());
        }

        [Fact]
        public void GetTariff_InvalidId_ReturnsNotFound()
        {
            var controller = BuildController();

            var result = controller.GetTariff(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateTariff_DuplicateTariffType_ReturnsBadRequest()
        {
            _db.Tariffs.Add(CreateTariff("Family", "Familie", price: 12.00m));
            await _db.SaveChangesAsync();

            var controller = BuildController();

            var dto = new TariffDto
            {
                TariffType = "Family",
                DisplayName = "Gezin",
                Price = 12.00m,
                SortOrder = 1
            };

            var result = controller.CreateTariff(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
