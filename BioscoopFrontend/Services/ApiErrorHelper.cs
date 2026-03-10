using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BioscoopFrontend.Services;

public static class ApiErrorHelper
{
    private class ValidationProblem
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("errors")]
        public Dictionary<string, string[]>? Errors { get; set; }
    }

    public static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync();

            var problem = JsonSerializer.Deserialize<ValidationProblem>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (problem?.Errors is { Count: > 0 })
            {
                var messages = problem.Errors.Values
                    .SelectMany(e => e)
                    .ToList();

                return string.Join(" ", messages);
            }

            if (!string.IsNullOrWhiteSpace(problem?.Title))
                return problem.Title;

            return "An unexpected error occurred. Please try again.";
        }
        catch
        {
            return "An unexpected error occurred. Please try again.";
        }
    }
}
