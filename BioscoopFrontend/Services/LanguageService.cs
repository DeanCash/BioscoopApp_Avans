using Microsoft.JSInterop;

namespace BioscoopFrontend.Services;

public class LanguageService
{
    private readonly IJSRuntime _js;
    public event Action? OnChange;
    public string Lang { get; private set; } = "nl";

    public LanguageService(IJSRuntime js) => _js = js;

    public async Task InitAsync()
    {
        var saved = await _js.InvokeAsync<string?>("localStorage.getItem", "lang");
        if (saved is "en" or "nl") Lang = saved;
    }

    public async Task SetAsync(string lang)
    {
        Lang = lang;
        await _js.InvokeVoidAsync("localStorage.setItem", "lang", lang);
        OnChange?.Invoke();
    }

    public string T(string key) => Translations.Get(key, Lang);
}
