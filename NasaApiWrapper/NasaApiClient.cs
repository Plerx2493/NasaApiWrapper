using System.Net.Http.Headers;

namespace NasaApiWrapper;


public class NasaApiClient
{
    private HttpClient _httpClient;
    public Apod Apod;

    public NasaApiClient(string token)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(@"https://api.nasa.gov/");
        //_httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Plerx2493 NasaApiClient"));
        
        Apod = new Apod(_httpClient, token);
    }
}