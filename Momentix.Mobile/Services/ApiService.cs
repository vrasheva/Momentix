using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Momentix.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public string LastErrorMessage { get; private set; } = string.Empty;
    public static string ToDeviceUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        if (url.StartsWith("/", StringComparison.Ordinal))
            return $"http://10.0.2.2:5036{url}";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var shouldRewrite = string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(uri.Host, "0.0.0.0", StringComparison.OrdinalIgnoreCase);

        if (!shouldRewrite)
            return url;

        var builder = new UriBuilder(uri)
        {
            Scheme = "http",
            Host = "10.0.2.2",
            Port = 5036
        };

        return builder.Uri.ToString();
    }

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5036/api/"),
            Timeout = TimeSpan.FromSeconds(120)
        };

        var savedToken = Preferences.Get("auth_token", string.Empty);
        if (!string.IsNullOrWhiteSpace(savedToken))
            SetToken(savedToken);
    }

    public void SetToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        LastErrorMessage = string.Empty;
        var response = await _httpClient.PostAsJsonAsync(endpoint, data);

        if (!response.IsSuccessStatusCode)
        {
            LastErrorMessage = await response.Content.ReadAsStringAsync();
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest data)
    {
        LastErrorMessage = string.Empty;
        var response = await _httpClient.PostAsJsonAsync(endpoint, data);
        if (!response.IsSuccessStatusCode)
            LastErrorMessage = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PostAsync(string endpoint)
    {
        LastErrorMessage = string.Empty;
        var response = await _httpClient.PostAsync(endpoint, null);
        if (!response.IsSuccessStatusCode)
            LastErrorMessage = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode;
    }
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        LastErrorMessage = string.Empty;
        var response = await _httpClient.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            LastErrorMessage = await response.Content.ReadAsStringAsync();
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> PostFileAsync<T>(
        string endpoint,
        Stream fileStream,
        string fileName,
        string contentType)
    {
        LastErrorMessage = string.Empty;

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync(endpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            LastErrorMessage = await response.Content.ReadAsStringAsync();
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<bool> PutAsync<TRequest>(string endpoint, TRequest data)
    {
        LastErrorMessage = string.Empty;
        var response = await _httpClient.PutAsJsonAsync(endpoint, data);
        if (!response.IsSuccessStatusCode)
            LastErrorMessage = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PatchAsync<TRequest>(string endpoint, TRequest data)
    {
        LastErrorMessage = string.Empty;
        var response = await _httpClient.PatchAsJsonAsync(endpoint, data);
        if (!response.IsSuccessStatusCode)
            LastErrorMessage = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        LastErrorMessage = string.Empty;
        var response = await _httpClient.DeleteAsync(endpoint);
        if (!response.IsSuccessStatusCode)
            LastErrorMessage = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode;
    }
}
