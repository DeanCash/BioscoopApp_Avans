using API.Services;
using BackendAPI.Controllers;
using BackendAPI.Models.Arrangement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BackendAPI.Tests.Controllers
{
    public class ArrangementsControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;

        public ArrangementsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
        }

        public void Dispose() => _db.Dispose();

        private ArrangementModel CreateArrangement(
            string name,
            ArrangementCategory category,
            decimal price = 5.00m,
            int sortOrder = 0,
            bool isActive = true)
        {
            return new ArrangementModel
            {
                ArrangementId = Guid.NewGuid(),
                Name = name,
                Description = $"{name} description",
                Category = category,
                Price = price,
                SortOrder = sortOrder,
                IsActive = isActive,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        private ArrangementsController BuildController()
        {
            return new ArrangementsController(_db);
        }

        [Fact]
        public async Task GetArrangements_NoFilter_ReturnsAllActive()
        {
            _db.Arrangements.Add(CreateArrangement("Popcorn Small", ArrangementCategory.Popcorn));
            _db.Arrangements.Add(CreateArrangement("Cola", ArrangementCategory.Drank));
            _db.Arrangements.Add(CreateArrangement("Inactive Item", ArrangementCategory.Snack, isActive: false));
            await _db.SaveChangesAsync();

            var controller = BuildController();

            var result = await controller.GetArrangements();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(2, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public async Task GetArrangements_FilterByCategory_ReturnsOnlyThatCategory()
        {
            _db.Arrangements.Add(CreateArrangement("Popcorn Small", ArrangementCategory.Popcorn));
            _db.Arrangements.Add(CreateArrangement("Popcorn Large", ArrangementCategory.Popcorn));
            _db.Arrangements.Add(CreateArrangement("Cola", ArrangementCategory.Drank));
            await _db.SaveChangesAsync();

            var controller = BuildController();

            var result = await controller.GetArrangements(category: ArrangementCategory.Popcorn);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(2, doc.RootElement.GetArrayLength());

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                Assert.Equal("Popcorn", item.GetProperty("Category").GetString());
            }
        }

        [Fact]
        public async Task GetArrangements_InactiveExcluded()
        {
            var inactive = CreateArrangement("Old Snack", ArrangementCategory.Snack, isActive: false);
            _db.Arrangements.Add(inactive);
            await _db.SaveChangesAsync();

            var controller = BuildController();

            var result = await controller.GetArrangements();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(0, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public async Task GetArrangements_OrderedByCategoryThenSortOrder()
        {
            // Drank (enum 1) with sortOrder 2
            _db.Arrangements.Add(CreateArrangement("Cola", ArrangementCategory.Drank, sortOrder: 2));
            // Drank (enum 1) with sortOrder 1
            _db.Arrangements.Add(CreateArrangement("Fanta", ArrangementCategory.Drank, sortOrder: 1));
            // Popcorn (enum 0) with sortOrder 1
            _db.Arrangements.Add(CreateArrangement("Popcorn Small", ArrangementCategory.Popcorn, sortOrder: 1));
            await _db.SaveChangesAsync();

            var controller = BuildController();

            var result = await controller.GetArrangements();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.EnumerateArray().ToArray();

            Assert.Equal(3, items.Length);
            // Popcorn first (category 0), then Drank sorted by sortOrder
            Assert.Equal("Popcorn Small", items[0].GetProperty("Name").GetString());
            Assert.Equal("Fanta", items[1].GetProperty("Name").GetString());
            Assert.Equal("Cola", items[2].GetProperty("Name").GetString());
        }

        [Fact]
        public async Task GetArrangement_ValidId_ReturnsArrangement()
        {
            var arrangement = CreateArrangement("Nachos", ArrangementCategory.Snack, price: 4.50m);
            _db.Arrangements.Add(arrangement);
            await _db.SaveChangesAsync();

            var controller = BuildController();

            var result = await controller.GetArrangement(arrangement.ArrangementId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Nachos", doc.RootElement.GetProperty("Name").GetString());
            Assert.Equal("Snack", doc.RootElement.GetProperty("Category").GetString());
            Assert.Equal(4.50m, doc.RootElement.GetProperty("Price").GetDecimal());
        }

        [Fact]
        public async Task GetArrangement_InvalidId_ReturnsNotFound()
        {
            var controller = BuildController();

            var result = await controller.GetArrangement(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void GetCategories_ReturnsAllEnumValues()
        {
            var controller = BuildController();

            var result = controller.GetCategories();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var categories = doc.RootElement;

            var expectedCount = Enum.GetValues<ArrangementCategory>().Length;
            Assert.Equal(expectedCount, categories.GetArrayLength());

            var names = categories.EnumerateArray()
                .Select(c => c.GetProperty("Name").GetString())
                .ToList();
            Assert.Contains("Popcorn", names);
            Assert.Contains("Drank", names);
            Assert.Contains("Snack", names);
            Assert.Contains("ComboPackage", names);
            Assert.Contains("HorecaSpecial", names);
        }
    }
}
