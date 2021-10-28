using Common;
using Confluent.Kafka;

namespace CentralHub
{
    public class Program
    {
        private const string ENV_KAFKA_HOST = "KAFKA_HOST";
        private const string _consumerGroup = "-consumer_grp";

        public static async Task Main(string[] args)
        {
            Consume(Topic.City_Current_Weather, async _ => await Task.CompletedTask, CancellationToken.None);

            await Task.Delay(-1);
        }

        public static void Consume(Topic topic, Func<string, Task> handle, CancellationToken cancellationToken)
        {
            var topicName = topic.GetTopicName();

            var config = new ConsumerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable(ENV_KAFKA_HOST),
                GroupId = string.Concat(topicName, _consumerGroup),
                AutoOffsetReset = AutoOffsetReset.Latest
            };

            _ = Task.Run(async () =>
            {
                using var consumer = new ConsumerBuilder<Null, string>(config).Build();
                consumer.Subscribe(topicName);

                var semaphore = new SemaphoreSlim(10, 10);
                while (!cancellationToken.IsCancellationRequested)
                {
                    await semaphore.WaitAsync(cancellationToken);
                    var consumeResult = consumer.Consume(cancellationToken);

                    Console.WriteLine($"[{config.GroupId}] Received message from topic {topicName} " +
                        $"on partition [{consumeResult.Partition.Value}] with offset {consumeResult.Offset.Value}");

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await handle(consumeResult.Message.Value);
                        }
                        catch (ConsumeException consumeException)
                        {
                            Console.WriteLine($"Exception during consume {consumeException.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                }

                consumer.Close();
            }, cancellationToken);
        }
    }
}