using System.Net.Http.Json;

namespace BioscoopFrontend.Services;

public class AuthService(HttpClient http)
{
    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var response = await http.GetAsync("/api/auth/me");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetRoleAsync()
    {
        try
        {
            var response = await http.GetAsync("/api/auth/me");
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadFromJsonAsync<MeResponse>();
            return data?.Role;
        }
        catch
        {
            return null;
        }
    }

    private class MeResponse
    {
        public string? Username { get; set; }
        public string? Role { get; set; }
    }
}
