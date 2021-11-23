namespace PollyExamples
{
    using System;

    using System.Net.Http;

    static class Program
    {
        private static string[] _uris =
        {
            "https://localhost:44383/WeatherForecast",
            "https://localhost:44383/InternalWeatherForecast",
            "https://localhost:44383/SlowWeatherForecast/10",
            "https://localhost:44383/NumberOfExceptionWeatherForecast/3"
        };
            
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var httpMessageHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message,
                                                             certificate2,
                                                             arg3,
                                                             arg4) => true
            };
            var client = new HttpClient(httpMessageHandler);
            var task = client.GetAsync(_uris[1]);
            var content = task.Result.Content;
            
            Console.WriteLine(content.ReadAsStringAsync().Result);
            Console.ReadLine();
        }
    }
    
    
}