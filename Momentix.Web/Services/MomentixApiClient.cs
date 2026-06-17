using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace Momentix.Web.Services;

public class MomentixApiClient
{
    private const long MaxUploadSize = 20 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly AuthSession _authSession;

    public MomentixApiClient(HttpClient httpClient, AuthSession authSession)
    {
        _httpClient = httpClient;
        _authSession = authSession;
    }

    public string? LastError { get; private set; }

    public string ToBrowserUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        if (!string.Equals(uri.Host, "10.0.2.2", StringComparison.OrdinalIgnoreCase) || _httpClient.BaseAddress == null)
            return url;

        var builder = new UriBuilder(uri)
        {
            Scheme = _httpClient.BaseAddress.Scheme,
            Host = _httpClient.BaseAddress.Host,
            Port = _httpClient.BaseAddress.Port
        };

        return builder.Uri.ToString();
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        using var request = CreateRequest(HttpMethod.Get, endpoint);
        using var response = await _httpClient.SendAsync(request);
        return await ReadResponse<T>(response);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload)
    {
        using var request = CreateRequest(HttpMethod.Post, endpoint);
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await _httpClient.SendAsync(request);
        return await ReadResponse<TResponse>(response);
    }

    public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest payload)
    {
        using var request = CreateRequest(HttpMethod.Post, endpoint);
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await _httpClient.SendAsync(request);
        return await ReadResponse(response);
    }

    public async Task<bool> PostAsync(string endpoint)
    {
        using var request = CreateRequest(HttpMethod.Post, endpoint);
        using var response = await _httpClient.SendAsync(request);
        return await ReadResponse(response);
    }

    public async Task<bool> PutAsync<TRequest>(string endpoint, TRequest payload)
    {
        using var request = CreateRequest(HttpMethod.Put, endpoint);
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await _httpClient.SendAsync(request);
        return await ReadResponse(response);
    }

    public async Task<bool> PatchAsync<TRequest>(string endpoint, TRequest payload)
    {
        using var request = CreateRequest(HttpMethod.Patch, endpoint);
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await _httpClient.SendAsync(request);
        return await ReadResponse(response);
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        using var request = CreateRequest(HttpMethod.Delete, endpoint);
        using var response = await _httpClient.SendAsync(request);
        return await ReadResponse(response);
    }

    public async Task<TResponse?> PostFileAsync<TResponse>(string endpoint, IBrowserFile file)
    {
        LastError = null;

        if (file.Size > MaxUploadSize)
        {
            LastError = "File must be up to 20 MB.";
            return default;
        }

        await using var stream = file.OpenReadStream(MaxUploadSize);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.Name);

        using var request = CreateRequest(HttpMethod.Post, endpoint);
        request.Content = content;
        using var response = await _httpClient.SendAsync(request);
        return await ReadResponse<TResponse>(response);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        LastError = null;

        var request = new HttpRequestMessage(method, endpoint);
        if (!string.IsNullOrWhiteSpace(_authSession.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authSession.Token);
        }

        return request;
    }

    private async Task<T?> ReadResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            LastError = await ReadError(response);
            return default;
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }
        catch (JsonException)
        {
            LastError = "The server returned an unexpected response.";
            return default;
        }
    }

    private async Task<bool> ReadResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            LastError = null;
            return true;
        }

        LastError = await ReadError(response);
        return false;
    }

    private static async Task<string> ReadError(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            return $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}";

        return body.Length > 500 ? body[..500] : body;
    }
}

