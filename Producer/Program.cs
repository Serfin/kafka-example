using Common;
using Kafka;

namespace WeatherStation
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine($"[APP] KAFKA_HOST={Environment.GetEnvironmentVariable("KAFKA_HOST")}");

            _ = Task.Run(async () =>
            {
                var producer = new Producer();

                while (true)
                {
                    await producer.ProduceAsync(Topic.City_Current_Weather, "TEST_DATA");
                }
            });

            await Task.Delay(-1);
        }
    }
}