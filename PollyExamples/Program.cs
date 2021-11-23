namespace PollyExamples
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Polly;
    using Polly.Timeout;

    static class Program
    {
        private static string[] _uris =
        {
            "https://localhost:44383/WeatherForecast",
            "https://localhost:44383/NoneExistingWeatherForecast",
            "https://localhost:44383/InternalServerErrorWeatherForecast",
            "https://localhost:44383/SlowWeatherForecast/10",
            "https://localhost:44383/NumberOfExceptionWeatherForecast/3"
        };

        static void Main(string[] args)
        {
            Console.WriteLine("Calling api:s");

            for (int i = 0; i < _uris.Length; i++)
            {
                //Console.WriteLine( CreateFunc(_uris[i]).Invoke()); //"Normal" call
                Console.WriteLine(UseTimeOutWithTryCatch(_uris[i])); //Call using Time out policy, explicit try ... catch
                Console.WriteLine(UseTimeOut(_uris[i])); //Call using Time out policy, implicit try ... catch 
                Console.ReadLine();
            }

            Console.WriteLine("Called api:s");
            Console.ReadLine();
        }

        #region Time out policy
        private static string UseTimeOutWithTryCatch(string uri)
        {
            var policy = Policy.Timeout<string>(1, TimeoutStrategy.Pessimistic, OnTimeOut);
            var returnValue = string.Empty;
            try
            {
                returnValue = policy.Execute(() => CreateFunc(uri).Invoke());
            }
            catch (TimeoutRejectedException)
            {
                //Handle time out
            }

            return returnValue;
        }

        private static string UseTimeOut(string uri)
        {
            var policy = Policy.Timeout<string>(1, TimeoutStrategy.Pessimistic, OnTimeOut);
            PolicyResult<string> result = policy.ExecuteAndCapture(() => CreateFunc(uri).Invoke());

            if (result.FinalException != null)
            {
                //Handel exception if any
                //Console.WriteLine(result.FinalException.Message);
            }

            return result.Result??string.Empty;
        }

        private static void OnTimeOut(Context arg1,
                                      TimeSpan arg2,
                                      Task arg3)
        {
            Console.WriteLine("Call timed out");
        }
        
        #endregion
        
        private static Func<string> CreateFunc(string uri)
        {
            return () =>
            {
                var httpMessageHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message,
                                                                 certificate2,
                                                                 arg3,
                                                                 arg4) => true
                };
                var client = new HttpClient(httpMessageHandler);
                Console.WriteLine(uri);
                var task = client.GetAsync(uri);
                var content = task.Result.Content;
                return content.ReadAsStringAsync().Result;
            };
        }
    }
}