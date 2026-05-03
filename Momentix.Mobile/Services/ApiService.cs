using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Momentix.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public string LastErrorMessage { get; private set; } = string.Empty;

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5036/api/")
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

    public async Task<bool> PutAsync<TRequest>(string endpoint, TRequest data)
    {
        LastErrorMessage = string.Empty;
        var response = await _httpClient.PutAsJsonAsync(endpoint, data);
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
