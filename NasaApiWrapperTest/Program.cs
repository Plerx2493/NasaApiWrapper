using NasaApiWrapper;

namespace NasaApiWrapperTest;

public static class Programm
{
    public static void Main()
    {
        var nasa = new NasaApiClient("ZsIKgYsYUBmqqRVUwHR8MCTsVmcfuhJBIZFTVh8A");
        
        var apodtoday = nasa.Apod.GetApodToday().GetAwaiter().GetResult();
        
        Console.WriteLine("test");
        
    }
}