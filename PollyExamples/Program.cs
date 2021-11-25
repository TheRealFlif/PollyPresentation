namespace PollyExamples
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Polly;
    using Polly.Timeout;

    static class Program
    {
        private static readonly string[] Uris =
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

            foreach (var t in Uris)
            {
                //Console.WriteLine(CreateFunc(t).Invoke()); //"Normal" call
                // Console.WriteLine(UseTimeOutWithTryCatch(_uris[i])); //Call using Time out policy, explicit try ... catch
                // Console.WriteLine(UseTimeOut(_uris[i])); //Call using Time out policy, implicit try ... catch
                //Console.WriteLine(RetryOnce(t));
                //Console.WriteLine(RetryTimes(t, 3));
                //Console.WriteLine(RetryTimesWithWait(t, 3));
                Console.WriteLine(Fallback(t, Uris[0]));
                //Console.ReadLine();
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

            return result.Result ?? string.Empty;
        }

        private static void OnTimeOut(Context arg1,
                                      TimeSpan arg2,
                                      Task arg3)
        {
            Console.WriteLine("Call timed out");
        }

        #endregion

        #region Retry
        private static string RetryOnce(string uri)
        {
            Policy<HttpResponseMessage> policy = Policy
                .HandleResult<HttpResponseMessage>(m => m.StatusCode != HttpStatusCode.OK)
                .Retry(OnRetry);

            var message = policy.Execute(CreateResponseMessageFunc(uri));
            return message.Content.ReadAsStringAsync().Result;
        }

        private static string RetryTimes(string uri, int times)
        {
            Policy<HttpResponseMessage> policy = Policy
                .HandleResult<HttpResponseMessage>(m => m.StatusCode != HttpStatusCode.OK)
                .Retry(times, OnRetry);

            var message = policy.Execute(CreateResponseMessageFunc(uri));
            return message.Content.ReadAsStringAsync().Result;
        }

        private static string RetryTimesWithWait(string uri, int times)
        {
            Policy<HttpResponseMessage> policy = Policy
                .HandleResult<HttpResponseMessage>(m => m.StatusCode != HttpStatusCode.OK)
                .WaitAndRetry(times, SleepDurationProvider, OnRetry);

            var message = policy.Execute(CreateResponseMessageFunc(uri));
            return message.Content.ReadAsStringAsync().Result;
        }

        private static void OnRetry(DelegateResult<HttpResponseMessage> message,
                                    TimeSpan timeSpan,
                                    int retryCount,
                                    Context context)
        {
            OnRetry(message, retryCount);
        }

        private static TimeSpan SleepDurationProvider(int retryCount)
        {
            Console.WriteLine($"SleepDurationProvider: {retryCount}");
            return TimeSpan.FromMilliseconds(Math.Pow(2, retryCount)*1000+new Random().Next(0,499));
        }

        private static void OnRetry(DelegateResult<HttpResponseMessage> message,
                                    int retryCount)
        {
            Console.WriteLine($"Received status '{message.Result.StatusCode}' on call #{retryCount}");
        }
        #endregion

        #region Fallback
        private static string Fallback(string uri, string fallbackUri)
        {
            var policy = Polly.Fallback.FallbackPolicy<HttpResponseMessage>
                .HandleResult(s => !s.IsSuccessStatusCode)
                .Fallback(() => CreateResponseMessageFunc(fallbackUri).Invoke());

            var message = policy.Execute(() => CreateResponseMessageFunc(uri).Invoke());
            return message.Content.ReadAsStringAsync().Result;
        }
        #endregion
        
        private static Func<string> CreateFunc(string uri)
        {
            return () =>
            {
                var content = CreateResponseMessageFunc(uri).Invoke().Content;
                return content.ReadAsStringAsync().Result;
            };
        }

        private static Func<HttpResponseMessage> CreateResponseMessageFunc(string uri)
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
                return task.Result;
            };
        }
    }
}