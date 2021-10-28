using System.Text.Json;
using Common;
using Confluent.Kafka;

namespace WeatherStation
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Produce(Topic.City_Current_Weather, "DATA");

            await Task.Delay(-1);
        }

        private static void Produce(Topic topic, string data)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_HOST"),
                Acks = Acks.All,
                Partitioner = Partitioner.Random,
                MessageTimeoutMs = 3_000
            };

            DisplayPartitionsInfo(config);

            _ = Task.Run(async () =>
            {
                using var producer = new ProducerBuilder<Null, string>(config).Build();
                var semaphore = new SemaphoreSlim(1, 1);

                while (true)
                {
                    await semaphore.WaitAsync();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var delivery = await producer.ProduceAsync(Topic.City_Current_Weather.GetTopicName(),
                                new Message<Null, string>
                                {
                                    Value = JsonSerializer.Serialize("DATA")
                                });

                            Console.WriteLine($"[KAFKA] {delivery.Status} on partition [{delivery.TopicPartition.Partition.Value}] " +
                                $"with offset " + delivery.TopicPartitionOffset.Offset.Value);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[KAFKA] Delivery failed {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                }
            });
        }

        private static void DisplayPartitionsInfo(ProducerConfig config)
        {
            using var admin = new AdminClientBuilder(config).Build();
            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(30));

            var topic = metadata.Topics.FirstOrDefault(x => x.Topic == Topic.City_Current_Weather.GetTopicName());
            topic.Partitions.ForEach(x =>
            {
                Console.WriteLine($"Partition {x.PartitionId} Replicas {x.Replicas.Length}");
            });

            Console.WriteLine();
        }
    }
}