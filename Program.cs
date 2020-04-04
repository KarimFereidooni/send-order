namespace SendOrder
{
    using Newtonsoft.Json;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;

    internal static class Program
    {
        private static StringContent orderContent = null;

        internal static void Main(string[] args)
        {
            Log($"Start running app");

            var nonceUrl = "https://online.agah.com/Order/GenerateNonce";
            var sendOrderUrl = "https://online.agah.com/Order/SendOrder";
            using var httpClient = GetHttpClient();
            var emptyContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");

            // Testing connection
            Log("Testing connection...");
            var testStopwatch = Stopwatch.StartNew();
            try
            {
                using var nonceResponse = httpClient.PostAsync(nonceUrl, emptyContent).Result;
                if (nonceResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(nonceResponse.StatusCode.ToString());
                }

                nonceResponse.Content.ReadAsStringAsync().Wait();
            }
            catch (Exception ex)
            {
                LogError("Error: " + ex.Message);
                Console.ReadKey();
                return;
            }
            finally
            {
                testStopwatch.Stop();
            }

            Log($"Connection is OK. A nonce was taken in {testStopwatch.Elapsed.TotalSeconds} seconds");

            TimeSpan startsAt = TimeSpan.ParseExact(Data.Instance.StartsAt, @"h\:m\:s\:fff", CultureInfo.InvariantCulture);
            TimeSpan endsAt = TimeSpan.ParseExact(Data.Instance.EndsAt, @"h\:m\:s\:fff", CultureInfo.InvariantCulture);
            DateTime startDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, startsAt.Hours, startsAt.Minutes, startsAt.Seconds, startsAt.Milliseconds);
            DateTime endDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, endsAt.Hours, endsAt.Minutes, endsAt.Seconds, endsAt.Milliseconds);

            void GetNonceAndSetOrderContent()
            {
                Log($"Getting nonce ...");
                using var nonceResponse = httpClient.PostAsync(nonceUrl, emptyContent).Result;
                if (nonceResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Error while getting nonce: " + nonceResponse.StatusCode.ToString());
                }

                string nonceValue = nonceResponse.Content.ReadAsStringAsync().Result;
                if (nonceValue.StartsWith("\"") && nonceValue.EndsWith("\""))
                {
                    nonceValue = nonceValue.Substring(1, nonceValue.Length - 2);
                }

                Log("Nonce is: " + nonceValue);
                string orderPayload = JsonConvert.SerializeObject(new { orderModel = Data.Instance.Order, nonce = nonceValue });
                orderContent = new StringContent(orderPayload, Encoding.UTF8, "application/json");
            }

            Thread nonceThread = new Thread(new ThreadStart(() =>
            {
                Log("Nonce thread started");
                do
                {
                    Thread.Sleep(Data.Instance.NonceInterval);
                    if (DateTime.Now >= endDateTime)
                    {
                        break;
                    }

                    try
                    {
                        GetNonceAndSetOrderContent();
                    }
                    catch (Exception ex)
                    {
                        LogError("Error: " + ex.Message);
                    }
                }
                while (true);
            }));
            Thread orderThread = new Thread(new ThreadStart(() =>
            {
                Log("Order thread started");
                Log($"Start send orders at {startsAt.Hours}:{startsAt.Minutes}:{startsAt.Seconds}:{startsAt.Milliseconds}");

                // Get nonce 3 seconds before start send orders.
                var x = startDateTime.AddSeconds(-3).Subtract(DateTime.Now);
                if (x.TotalMilliseconds > 0)
                {
                    Log($"Waiting {x.TotalSeconds} seconds for start getting nonce...");
                    Thread.Sleep((int)x.TotalMilliseconds);
                }

                try
                {
                    GetNonceAndSetOrderContent();
                }
                catch (Exception ex)
                {
                    LogError("Error: " + ex.Message);
                    return;
                }

                nonceThread.Start();

                x = startDateTime.Subtract(DateTime.Now);
                if (x.TotalMilliseconds > 0)
                {
                    Log($"Waiting {x.TotalSeconds} seconds for start sending orders...");
                    Thread.Sleep((int)x.TotalMilliseconds);
                }

                do
                {
                    try
                    {
                        // var tasks = Enumerable.Range(0, Data.Instance.SendCount).Select(i =>
                        // {
                        //     return httpClient.PostAsync(sendOrderUrl, orderContent);
                        // });
                        Log($"Sending {Data.Instance.SendCount} order...");
                        var ordersStopwatch = Stopwatch.StartNew();
                        for (int i = 0; i < Data.Instance.SendCount; i++)
                        {
                            httpClient.PostAsync(sendOrderUrl, orderContent);
                        }

                        // Task.WhenAll(tasks).Wait();
                        ordersStopwatch.Stop();
                        Log($"{Data.Instance.SendCount} order(s) were sent in {ordersStopwatch.Elapsed.TotalSeconds} second");
                    }
                    catch (Exception ex)
                    {
                        while (ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                        }

                        LogError("Error: " + $"Error while sending orders: " + ex.Message);
                    }

                    if (DateTime.Now >= endDateTime)
                    {
                        break;
                    }

                    Log($"Waiting {Data.Instance.SendInterval} milliseconds...");
                    Thread.Sleep(Data.Instance.SendInterval);
                }
                while (true);
                Log($"Finish");
                Log($"Press any key to exit...");
            }));
            orderThread.Priority = ThreadPriority.Highest;
            orderThread.Start();
            Console.ReadKey();
        }

        private static void Log(string text)
        {
            Console.WriteLine($"{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}:{DateTime.Now.Millisecond}\t{text}");
        }

        private static void LogError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}:{DateTime.Now.Millisecond}\t{text}");
            Console.ResetColor();
        }

        private static HttpClient GetHttpClient()
        {
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 1000,
                Proxy = Data.Instance.Proxy.Enabled ? new WebProxy(Data.Instance.Proxy.Value, false) : null,
            };
            HttpClient client = new HttpClient(socketsHandler);
            client.DefaultRequestHeaders.Add("Host", "online.agah.com");
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            client.DefaultRequestHeaders.Add(
              "User-Agent",
              "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.132 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.Add("Origin", "https://online.agah.com");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            client.DefaultRequestHeaders.Add("Referer", "https://online.agah.com/");
            client.DefaultRequestHeaders.Add("Cookie", Data.Instance.Cookie);
            return client;
        }
    }
}
