using API.Services;
using BackendAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using BackendAPI.Services.Newsletter;

namespace BackendAPI.Tests.Controllers
{
    public class NewsletterControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<IEmailService> _emailMock = new();

        public NewsletterControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
        }

        public void Dispose() => _db.Dispose();

        private NewsletterController BuildController()
            => new NewsletterController(_db, _emailMock.Object);

        [Fact]
        public async Task Subscribe_GeldigEmail_GeeftOkResultaat()
        {
            // Arrange
            var controller = BuildController();

            // Act
            var result = await controller.Subscribe(new SubscribeRequest { Email = "bezoeker@example.com" });

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Subscribe_GeldigEmail_SlaatAbonneeOp()
        {
            // Arrange
            var controller = BuildController();

            // Act
            await controller.Subscribe(new SubscribeRequest { Email = "bezoeker@example.com" });

            // Assert
            var opgeslagen = await _db.NewsletterSubscribers.AnyAsync(s => s.Email == "bezoeker@example.com");
            Assert.True(opgeslagen);
        }

        [Fact]
        public async Task Subscribe_DubbeleEmail_GeeftConflict()
        {
            // Arrange
            var controller = BuildController();
            await controller.Subscribe(new SubscribeRequest { Email = "tweemaal@example.com" });

            // Act
            var result = await controller.Subscribe(new SubscribeRequest { Email = "tweemaal@example.com" });

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Subscribe_EmailWordtKleinLetterOpgeslagen()
        {
            // Arrange
            var controller = BuildController();

            // Act
            await controller.Subscribe(new SubscribeRequest { Email = "Test@Example.COM" });

            // Assert
            var opgeslagen = await _db.NewsletterSubscribers.AnyAsync(s => s.Email == "test@example.com");
            Assert.True(opgeslagen);
        }

        [Fact]
        public async Task Subscribe_OngeldigModel_GeeftBadRequest()
        {
            // Arrange
            var controller = BuildController();
            controller.ModelState.AddModelError("Email", "Het e-mailadres is verplicht.");

            // Act
            var result = await controller.Subscribe(new SubscribeRequest { Email = "" });

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
