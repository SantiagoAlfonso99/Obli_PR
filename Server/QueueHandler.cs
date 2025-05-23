using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using static System.Runtime.InteropServices.JavaScript.JSType;

public static class QueueHandler
{
    public static void EnqueueUser(User user)
    {
        UserDTO userToEnqueue = new UserDTO(user);
        var jsonMessage = JsonConvert.SerializeObject(userToEnqueue);
    
        var body = Encoding.UTF8.GetBytes(jsonMessage);
            
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
            
        channel.BasicPublish(exchange: string.Empty,
            routingKey: "users",
            mandatory:false,
            basicProperties: null,
            body: body);
    }
    
    public static void EnqueueTrip(Trip newTrip)
    {
        TripDTO tripDto = new TripDTO(newTrip);
        var jsonMessage = JsonConvert.SerializeObject(tripDto);
    
        var body = Encoding.UTF8.GetBytes(jsonMessage);
            
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
            
        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: "trips",
            mandatory: false,
            basicProperties: null,
            body: body
        );
        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: "adminTripsQueue",
            mandatory: false,
            basicProperties: null,
            body: body
        );
    }
    
    public static async Task SetupQueues()
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "users",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            Console.WriteLine("Init users queue");
            channel.QueueDeclare(queue: "trips",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            Console.WriteLine("Init trip queue");
            channel.QueueDeclare(queue: "adminTripsQueue",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            Console.WriteLine("Init adminTrips queue");
        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
        {
            Console.WriteLine("Trying to connect with RabbitMQ...");
            Thread.Sleep(3000);
            SetupQueues();
        }
    }
}