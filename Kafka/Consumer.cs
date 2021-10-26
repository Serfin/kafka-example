using Common;
using Confluent.Kafka;

namespace Kafka
{
    public class Consumer
    {
        private const string ENV_KAFKA_HOST = "KAFKA_HOST";
        private const string _consumerGroup = "-consumer_grp";

        public void Consume(Topic topic, Func<string, Task> handle, CancellationToken cancellationToken)
        {
            var topicName = topic.GetTopicName();

            var config = new ConsumerConfig
            {
                BootstrapServers = GetKafkaHostEnv(),
                GroupId = string.Concat(topicName, _consumerGroup),
                AutoOffsetReset = AutoOffsetReset.Latest
            };

            _ = Task.Run(async () =>
            {
                using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
                consumer.Subscribe(topicName);

                Console.WriteLine($"[{config.GroupId}] Started consumer on topic '{topicName}'");

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

                Console.WriteLine($"[{config.GroupId}] Closing consumer on topic {topicName}");
                consumer.Close();
            }, cancellationToken);
        }

        private string GetKafkaHostEnv()
            => Environment.GetEnvironmentVariable(ENV_KAFKA_HOST);
    }
}
