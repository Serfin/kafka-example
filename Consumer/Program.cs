using System.Collections.Concurrent;
using Common;
using Confluent.Kafka;

namespace CentralHub
{
    public class Program
    {
        private const string ENV_KAFKA_HOST = "KAFKA_HOST";
        private const string _consumerGroup = "-consumer_grp";

        private static readonly ConcurrentDictionary<string, IConsumer<Null, string>> _consumerStore = new();

        public static async Task Main(string[] args)
        {
            Environment.SetEnvironmentVariable("KAFKA_HOST", "localhost:9091,localhost:9092,localhost:9093");

            Consume(Topic.City_Current_Weather, async message => await HandleMessage(message), CancellationToken.None);

            await Task.Delay(-1);
        }

        public static void Consume(Topic topic, Func<ConsumeResult<Null, string>, Task> handle, CancellationToken cancellationToken)
        {
            var topicName = topic.GetTopicName();
            var config = new ConsumerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable(ENV_KAFKA_HOST),
                GroupId = string.Concat(topicName, _consumerGroup),
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _ = Task.Run(async () =>
            {
                var semaphore = new SemaphoreSlim(10, 10);
                var consumer = _consumerStore.GetOrAdd(topicName, _ => 
                    new ConsumerBuilder<Null, string>(config)
                        .SetErrorHandler(HandleError)
                        .Build());

                consumer.Subscribe(topicName);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await semaphore.WaitAsync(cancellationToken);
                    var consumeResult = consumer.Consume(cancellationToken); // What if this throws fatal error, semaphore stays locked

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await handle(consumeResult);
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
            }, cancellationToken);
        }

        private static void HandleError(IConsumer<Null, string> consumer, Error error)
        {
            if (error.IsLocalError)
            {
                // For local error just log, client will try to recover
                Console.WriteLine($"Consumer {consumer.Subscription.First()} error {error.Reason} code {error.Code}");
                return;
            }

            // Handle fatal errors
            Console.WriteLine($"Consumer {consumer.Subscription.First()} fatal error {error.Reason} code {error.Code}");
        }

        public static async Task HandleMessage(ConsumeResult<Null, string> consumeResult)
        {
            Console.WriteLine($"Received message from topic {consumeResult.Topic} " +
                $"on partition [{consumeResult.Partition.Value}] with offset {consumeResult.Offset.Value}");

            await Task.CompletedTask;
            return;
        }
    }
}