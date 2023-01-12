using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace NasaApiWrapper;

public class Apod
{
    private          RateLimiter _limiter;
    private readonly HttpClient  _httpClient;
    private readonly string      _token;
    private const    string      Route = "planetary/apod";


    public Apod(HttpClient client, string apiToken)
    {
        _httpClient = client;
        _token = apiToken;
        var options = new SlidingWindowRateLimiterOptions
        {
            SegmentsPerWindow = 20,
            Window = TimeSpan.FromHours(1),
            PermitLimit = 2000,
            QueueLimit = 1,
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        };
        
        _limiter = new SlidingWindowRateLimiter(options);
    }
    
    /// <summary>
    /// Get todays APOD
    /// </summary>
    /// <returns></returns>
    /// <exception cref="HttpRequestException">Thrown when ratelimit is hit</exception>
    public async Task<ApodEntity> GetApodToday() => (await GetApodInternal(count: 1)).First();
    
    
    /// <summary>
    /// Get a list of random APODs
    /// </summary>
    /// <param name="count">Number of APODs</param>
    /// <returns>List of APODs</returns>
    /// <exception cref="HttpRequestException">Thrown when ratelimit is hit</exception>
    public async Task<List<ApodEntity>> GetRandom(int count)
    {
        List<ApodEntity> response = new List<ApodEntity>();

        if (count == 1)
        {
            response.Add( (await GetApodInternal(count: 2)).First());
        }
        else
        {
            response.AddRange(await GetApodInternal(count:count));
        }
        
        return response;
    }

    private async Task<List<ApodEntity>> GetApodInternal(DateOnly? date = null, DateOnly? startDate = null, DateOnly? endDate = null, int count = 1, bool thumb = false)
    {
        var lease = _limiter.AttemptAcquire(1);
        if (!lease.IsAcquired) throw new HttpRequestException("Ratelimit hitet");
        
        var query = new StringBuilder();
        
        if (date is not null) query.Append($"date={date.Value.ToString(Utility.DateFormat)}&");
        if (startDate is not null) query.Append($"start_date={startDate.Value.ToString(Utility.DateFormat)}&");
        if (endDate is not null) query.Append($"end_date={endDate.Value.ToString(Utility.DateFormat)}&");
        if (count != 1) query.Append($"count={count}&");
        if (thumb) query.Append($"thumb=true&");
        query.Append($"api_key={_token}");
        
        var requestUri = $"{Route}?{query}";
        
        var res = await _httpClient.GetAsync(requestUri);
        
        var resText = await res.Content.ReadAsStringAsync();
        List<ApodEntity> response = new();
        
        
        if (count == 1 && startDate == null && endDate == null)
        {
            var resJson = JsonSerializer.Deserialize<ApodJson>(resText, new JsonSerializerOptions{PropertyNameCaseInsensitive = false});
            if (resJson is null) return response;
            var resList = new ApodEntity(resJson);
            response.Add(resList);
        }
        else
        {
            var resJson = JsonSerializer.Deserialize<ApodJson[]>(resText, new JsonSerializerOptions{PropertyNameCaseInsensitive = false});
            if (resJson is null) return response;
            var resList = resJson.Select(x => new ApodEntity(x));
            response.AddRange(resList);
        }
        return response;
    }
}

internal class ApodJson
{
    [JsonPropertyName("copyright")]
    public string Copyright { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; }

    [JsonPropertyName("hdurl")]
    public string HdUrl { get; set; }
    
    [JsonPropertyName("media_type")]
    public string MediaType { get; set; }
    
    [JsonPropertyName("service_version")]
    public string Version { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class ApodEntity
{
    /// <summary>
    /// Copyrightinformation
    /// </summary>
    public string? Copyright;
    
    /// <summary>
    /// Date on which this photo was APOD
    /// </summary>
    public DateOnly Date;
    
    /// <summary>
    /// Imageexplanation
    /// </summary>
    public string? Explanation;
    
    /// <summary>
    /// Url to a high definition image source
    /// </summary>
    public string? HdUrl;
    
    /// <summary>
    /// Url to a standard definition image source
    /// </summary>
    public string? Url;
    
    /// <summary>
    /// Imagetitle
    /// </summary>
    public string? Title;
    
    public bool IsPublicDomain;

    public ApodEntity(string? copyright, DateOnly date, string? explanation, string? hdUrl, string? url, string? title)
    {
        if (copyright is not null)
        {
            Copyright = copyright;
            IsPublicDomain = false;
        }
        else
        {
            IsPublicDomain = true;
        }
        
        Date = date;
        Explanation = explanation;
        HdUrl = hdUrl;
        Url = url;
        Title = title;
    }
    
    internal ApodEntity(ApodJson json)
    {
        Copyright = json.Copyright;
        IsPublicDomain = false;

        Date = DateOnly.Parse(json.Date);

        Explanation = json.Explanation;
        HdUrl = json.HdUrl;
        Url = json.Url;
        Title = json.Title;
    }
}