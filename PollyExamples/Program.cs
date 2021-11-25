namespace PollyExamples
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Memory;
    using Polly;
    using Polly.Caching;
    using Polly.Caching.Memory;
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
                //Console.WriteLine(CreateFunc(Uris[3]).Invoke()); //"Normal" call
                // Console.WriteLine(UseTimeOutWithTryCatch(_uris[i])); //Call using Time out policy, explicit try ... catch
                // Console.WriteLine(UseTimeOut(_uris[i])); //Call using Time out policy, implicit try ... catch
                //Console.WriteLine(RetryOnce(t));
                //Console.WriteLine(RetryTimes(t, 3));
                //Console.WriteLine(RetryTimesWithWait(t, 3));
                //Console.WriteLine(Fallback(t, Uris[0]));
                //Console.WriteLine(Cache(Uris[3]));
                //Console.WriteLine(Cache(Uris[4]));
                //Console.WriteLine(CacheWithFilter(Uris[4]));
                Console.WriteLine(CircuitBreaker(Uris[4]));
                //Thread.Sleep(TimeSpan.FromSeconds(3));
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

        #region Cache
        static readonly MemoryCache MemoryCache = new MemoryCache(new MemoryCacheOptions());
        static readonly MemoryCacheProvider MemoryCacheProvider = new MemoryCacheProvider(MemoryCache);
        
        static readonly Func<Context, HttpResponseMessage, Ttl> CacheFilter =
            (ctx, msg) => new Ttl(
                timeSpan:msg.StatusCode == HttpStatusCode.OK ? TimeSpan.FromMinutes(30) : TimeSpan.Zero,
                slidingExpiration:true);
            
        static readonly IAsyncPolicy<HttpResponseMessage> CacheFilterPolicy =
            Policy.CacheAsync<HttpResponseMessage>(
                MemoryCacheProvider.AsyncFor<HttpResponseMessage>(), //note the .AsyncFor<HttpResponseMessage>
                new ResultTtl<HttpResponseMessage>(CacheFilter),
                onCacheError: null
            );
        
        static CachePolicy<HttpResponseMessage> cachePolicy = Policy.Cache<HttpResponseMessage>(
            MemoryCacheProvider,
            new RelativeTtl(TimeSpan.FromMinutes(5)));
        
        private static string Cache(string uri)
        {
            var message = cachePolicy.Execute(ctx => CreateResponseMessageFunc(uri).Invoke(), new Context(uri));
            return message.Content.ReadAsStringAsync().Result;
        }
        
        private static string CacheWithFilter(string uri)
        {
            var message = CacheFilterPolicy.ExecuteAsync(ctx => Task.FromResult(CreateResponseMessageFunc(uri).Invoke()), new Context(uri));

            return message.Result.Content.ReadAsStringAsync().Result;
        }
        #endregion
        
        
        static Polly.CircuitBreaker.CircuitBreakerPolicy<HttpResponseMessage> BasicCircuitBreakerPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .CircuitBreaker(2, TimeSpan.FromSeconds(5));

        private static string CircuitBreaker(string uri)
        {
            HttpResponseMessage message = null;
            try
            {
                message = BasicCircuitBreakerPolicy.Execute(CreateResponseMessageFunc(uri));
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException circuitException)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                return circuitException.Message;
            }

            return message.Content.ReadAsStringAsync().Result;
        }
        
        
        
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