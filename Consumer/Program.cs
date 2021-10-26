using Common;
using Kafka;

namespace CentralHub
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
#if DEBUG
            Environment.SetEnvironmentVariable("KAFKA_HOST", "localhost:9091,localhost:9092,localhost:9093");
#endif

            CancellationTokenSource cts = new CancellationTokenSource();

            new Consumer().Consume(
                Topic.City_Current_Weather, 
                async _ => await Task.CompletedTask,
                cts.Token);

            await Task.Delay(-1);
        }
    }
}