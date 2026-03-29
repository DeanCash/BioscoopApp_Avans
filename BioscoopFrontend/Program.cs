using BioscoopFrontend;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BioscoopFrontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<CookieHandler>();

// Default HttpClient � sends cookies for authenticated admin endpoints
builder.Services.AddHttpClient("AuthClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7120/");
}).AddHttpMessageHandler<CookieHandler>();

// Public client � no credentials, used for anonymous endpoints like movies
builder.Services.AddHttpClient("PublicClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7120/");
});

// @inject HttpClient Http ? uses the auth (credentialed) client for admin pages
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("AuthClient"));

builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
