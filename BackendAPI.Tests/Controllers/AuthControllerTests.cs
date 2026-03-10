using API.Services;
using BackendAPI.Controllers;
using BackendAPI.Models.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using System.Text.Json;

namespace BackendAPI.Tests.Controllers
{
    public class AuthControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher<UserModel> _hasher = new();

        public AuthControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
        }

        public void Dispose() => _db.Dispose();

        private UserModel CreateUser(string username, string plainPassword, string role = "User")
        {
            var user = new UserModel { Username = username, Role = role, PasswordHash = "" };
            user.PasswordHash = _hasher.HashPassword(user, plainPassword);
            return user;
        }

        private AuthController BuildController(HttpContext? httpContext = null)
        {
            var controller = new AuthController(_db);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext ?? new DefaultHttpContext()
            };
            return controller;
        }

        private HttpContext BuildHttpContextWithAuthService(
            Mock<IAuthenticationService>? authServiceMock = null,
            ClaimsPrincipal? user = null)
        {
            authServiceMock ??= new Mock<IAuthenticationService>();

            authServiceMock
                .Setup(s => s.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            authServiceMock
                .Setup(s => s.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var servicesMock = new Mock<IServiceProvider>();
            servicesMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            var context = new DefaultHttpContext { RequestServices = servicesMock.Object };
            if (user != null)
                context.User = user;

            return context;
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithRole()
        {
            var user = CreateUser("alice", "Secret1!", "Admin");
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var httpContext = BuildHttpContextWithAuthService();
            var controller = BuildController(httpContext);

            var result = await controller.Login(new AuthController.LoginRequest("alice", "Secret1!"));

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Admin", doc.RootElement.GetProperty("role").GetString());
        }

        [Fact]
        public async Task Login_ValidCredentials_CallsSignIn()
        {
            var user = CreateUser("bob", "Pass123!");
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(s => s.SignInAsync(
                    It.IsAny<HttpContext>(),
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var httpContext = BuildHttpContextWithAuthService(authServiceMock);
            var controller = BuildController(httpContext);

            await controller.Login(new AuthController.LoginRequest("bob", "Pass123!"));

            authServiceMock.Verify(
                s => s.SignInAsync(
                    It.IsAny<HttpContext>(),
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    It.Is<ClaimsPrincipal>(p => p.Identity!.Name == "bob"),
                    It.IsAny<AuthenticationProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task Login_UnknownUsername_ReturnsUnauthorized()
        {
            var controller = BuildController(BuildHttpContextWithAuthService());

            var result = await controller.Login(new AuthController.LoginRequest("nobody", "any"));

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var json = JsonSerializer.Serialize(unauthorized.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Invalid username/password", doc.RootElement.GetProperty("message").GetString());
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            var user = CreateUser("carol", "CorrectPass1!");
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var controller = BuildController(BuildHttpContextWithAuthService());

            var result = await controller.Login(new AuthController.LoginRequest("carol", "WrongPass!"));

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var json = JsonSerializer.Serialize(unauthorized.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Invalid username/password", doc.RootElement.GetProperty("message").GetString());
        }

        [Fact]
        public async Task Login_ValidCredentials_ClaimsContainUsernameAndRole()
        {
            var user = CreateUser("dave", "MyPass1!", "Manager");
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            ClaimsPrincipal? capturedPrincipal = null;
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(s => s.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>(
                    (_, _, principal, _) => capturedPrincipal = principal)
                .Returns(Task.CompletedTask);

            var servicesMock = new Mock<IServiceProvider>();
            servicesMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);
            var httpContext = new DefaultHttpContext { RequestServices = servicesMock.Object };

            var controller = BuildController(httpContext);

            await controller.Login(new AuthController.LoginRequest("dave", "MyPass1!"));

            Assert.NotNull(capturedPrincipal);
            Assert.Equal("dave", capturedPrincipal!.Identity!.Name);
            Assert.Equal("Manager", capturedPrincipal.FindFirstValue(ClaimTypes.Role));
        }

        [Fact]
        public async Task Logout_ReturnsOk()
        {
            var httpContext = BuildHttpContextWithAuthService();
            var controller = BuildController(httpContext);

            var result = await controller.Logout();

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Logout_CallsSignOut()
        {
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(s => s.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var httpContext = BuildHttpContextWithAuthService(authServiceMock);
            var controller = BuildController(httpContext);

            await controller.Logout();

            authServiceMock.Verify(
                s => s.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    It.IsAny<AuthenticationProperties>()),
                Times.Once);
        }

        [Fact]
        public void Me_Authenticated_ReturnsOkWithUsernameAndRole()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "eve"),
                new Claim(ClaimTypes.Role, "User"),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var httpContext = BuildHttpContextWithAuthService(user: principal);
            var controller = BuildController(httpContext);

            var result = controller.Me();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("eve", doc.RootElement.GetProperty("username").GetString());
            Assert.Equal("User", doc.RootElement.GetProperty("role").GetString());
        }

        [Fact]
        public void Me_NotAuthenticated_ReturnsUnauthorized()
        {
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // no authenticationType → not authenticated
            };
            var controller = BuildController(httpContext);

            var result = controller.Me();

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public void Me_Authenticated_RoleClaimIsCorrect()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "frank"),
                new Claim(ClaimTypes.Role, "Admin"),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var httpContext = BuildHttpContextWithAuthService(user: principal);
            var controller = BuildController(httpContext);

            var result = controller.Me();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Admin", doc.RootElement.GetProperty("role").GetString());
        }
    }
}
