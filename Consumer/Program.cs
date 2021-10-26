using Common;
using Kafka;

namespace CentralHub
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine($"[APP] KAFKA_HOST={Environment.GetEnvironmentVariable("KAFKA_HOST")}");

            new Consumer().Consume(
                Topic.City_Current_Weather, 
                async _ => await Task.CompletedTask,
                CancellationToken.None);

            await Task.Delay(-1);
        }
    }
}