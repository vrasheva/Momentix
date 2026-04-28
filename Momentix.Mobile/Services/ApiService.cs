using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Momentix.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5036/api/")
        };
    }

    public void SetToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, data);

        if (!response.IsSuccessStatusCode)
            return default;

        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
            return default;

        return await response.Content.ReadFromJsonAsync<T>();
    }
}