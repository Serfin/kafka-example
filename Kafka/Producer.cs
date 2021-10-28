using System.Net;
using System.Text.Json;
using Common;
using Confluent.Kafka;

namespace Kafka
{
    public class Producer
    {
        private readonly ProducerConfig _config;

        public Producer(ProducerConfig config)
        {
            _config = config;
        }

        public async Task ProduceAsync<T>(Topic topic, T data)
        {
            using var producer = new ProducerBuilder<Null, string>(_config).Build();
            
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
            catch (ProduceException<Ignore, string> produceException)
            {
                Console.WriteLine($"[KAFKA] Delivery failed {produceException.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
