using System.Net;
using System.Net.Http.Headers;
using ForaChallenge.Application.Services;
using ForaChallenge.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace ForaChallenge.Infrastructure.Edgar;

public class SecEdgarApiClient : ISecEdgarApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public SecEdgarApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> GetCompanyFactsAsync(Cik cik, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["SecEdgar:BaseUrl"]?.TrimEnd('/') ?? "https://data.sec.gov/api/xbrl/companyfacts";
        var userAgent = _configuration["SecEdgar:UserAgent"] ?? "ForaChallenge/1.0 (contact@example.com)";
        var accept = _configuration["SecEdgar:Accept"] ?? "application/json";

        var client = _httpClientFactory.CreateClient("SecEdgar");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        client.DefaultRequestHeaders.Accept.ParseAdd(accept);

        var url = $"{baseUrl}/CIK{cik.Value}.json";
        var response = await client.GetAsync(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new HttpRequestException("The specified key does not exist.", null, HttpStatusCode.NotFound);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
