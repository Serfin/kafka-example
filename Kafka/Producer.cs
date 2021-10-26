using System.Net;
using System.Text.Json;
using Common;
using Confluent.Kafka;

namespace Kafka
{
    public class Producer
    {
        private const string ENV_KAFKA_HOST = "KAFKA_HOST";

        public async Task ProduceAsync<T>(Topic topic, T data)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = GetKafkaHostEnv(),
                ClientId = Dns.GetHostName(),
                Partitioner = Partitioner.Random,
                Acks = Acks.All
            };

            using var producer = new ProducerBuilder<Null, string>(config).Build();
            
            try
            {
                var delivery = await producer.ProduceAsync(topic.GetTopicName(),
                    new Message<Null, string>
                    {
                        Value = JsonSerializer.Serialize(data)
                    });

                Console.WriteLine($"[KAFKA] {delivery.Status} on partition [{delivery.TopicPartition.Partition.Value}] with offset " +
                    delivery.TopicPartitionOffset.Offset.Value);
            }
            catch (ProduceException<Null, string> produceException)
            {
                Console.WriteLine($"[KAFKA] Delivery failed {produceException.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static string GetKafkaHostEnv() 
            => Environment.GetEnvironmentVariable(ENV_KAFKA_HOST);
    }
}
