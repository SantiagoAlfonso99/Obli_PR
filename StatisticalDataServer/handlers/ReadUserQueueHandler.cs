using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Server.Handlers;
using StatisticalDataServer;
using StatisticalDataServer.Models;


public class ReadUserQueueHandler : IHandler
{
  public async Task Call(IModel channel, CancellationToken token)
  {
    Console.WriteLine("Reading Users");
    channel.QueueDeclare(queue: "users",
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null);
    while (!token.IsCancellationRequested)
    {
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async(model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            var user = JsonConvert.DeserializeObject<User>(message);
            
            await MemoryDataBase.AddUser(user);
            
            Console.WriteLine($"UserQueue [x] Received {message}");
        };
        channel.BasicConsume(queue: "users",
            autoAck: true,
            consumer: consumer);
    }
    Console.WriteLine("Stopping user queue reading");
  }
}