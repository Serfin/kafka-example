using System.Collections.Concurrent;
using System.Text.Json;
using Common;
using Confluent.Kafka;

namespace WeatherStation
{
	public class Program
	{
		private static readonly ConcurrentDictionary<Topic, IProducer<Null, string>> _producerStore = new();

		public static async Task Main(string[] args)
		{
			Environment.SetEnvironmentVariable("KAFKA_HOST", "localhost:9091,localhost:9092,localhost:9093");

			Produce(Topic.City_Current_Weather, "DATA");

			await Task.Delay(-1);
		}

		private static void Produce(Topic topic, string data)
		{
			var config = new ProducerConfig
			{
				BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_HOST"),
				Acks = Acks.All,
				MessageTimeoutMs = 3_000
			};

			DisplayPartitionsInfo(config);

			_ = Task.Run(async () =>
			{
				var semaphore = new SemaphoreSlim(3, 3);
				var producer = _producerStore.GetOrAdd(topic,
					_ => new ProducerBuilder<Null, string>(config)
							.SetErrorHandler(HandleError)
							.Build());

				while (true)
				{
					await semaphore.WaitAsync();

					_ = Task.Run(async () =>
					{
						try
						{
							var delivery = await producer.ProduceAsync(topic.GetTopicName(),
								new Message<Null, string>
								{
									Value = JsonSerializer.Serialize(data)
								});

							Console.WriteLine($"[KAFKA] {delivery.Status} on partition [{delivery.Partition.Value}] " +
								$"with offset " + delivery.Offset.Value);
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

		private static void HandleError(IProducer<Null, string> producer, Error error)
		{
			Console.WriteLine($"Proucer {producer.Name} error {error.Reason} code {error.Code}");
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