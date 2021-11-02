using System.Collections.Concurrent;
using Common;
using Confluent.Kafka;

namespace CentralHub
{
    public class Program
    {
        private const string ENV_KAFKA_HOST = "KAFKA_HOST";
        private const string _consumerGroup = "-consumer_grp";

        private static readonly ConcurrentDictionary<string, IConsumer<Null, string>> _consumers = new();

        public static async Task Main(string[] args)
        {
            Environment.SetEnvironmentVariable("KAFKA_HOST", "localhost:9091,localhost:9092,localhost:9093");

            Consume(Topic.City_Current_Weather, async message => await HandleMessage(message), CancellationToken.None);
            Consume(Topic.Test_Topic_2, async message => await HandleMessage(message), CancellationToken.None);

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
                var consumer = _consumers.GetOrAdd(topicName, _ => 
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
                            // This message should go to DLQ
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                }
            }, cancellationToken);
        }

        public static void Dispose()
        {
            foreach (var consumer in _consumers)
            {
                _consumers.Remove(consumer.Key, out _);

                consumer.Value.Unsubscribe();
                consumer.Value.Dispose();
            }
        }

        private static void HandleError(IConsumer<Null, string> consumer, Error error)
        {
            if (error.IsLocalError)
            {
                // For local error just log, client will try to recover
                Console.WriteLine($"Consumer {consumer.Subscription.First()} \nError {error.Reason} \nCode {error.Code}");
                return;
            }

            // Handle fatal errors
            Console.WriteLine($"Consumer {consumer.Subscription.First()} \nFatal error {error.Reason} \nCode {error.Code}");
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