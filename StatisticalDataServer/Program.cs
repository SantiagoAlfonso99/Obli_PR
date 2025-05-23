using RabbitMQ.Client;
using Server.Handlers;

namespace StatisticalDataServer;

public class Program
{
    public static void Main(string[] args)
    {
        MemoryDataBase.InitLists();
        var app = SetupApi(args);
        var source = SetupRabbitQM();

        app.Run();
        source.Cancel();
    }

    public static CancellationTokenSource SetupRabbitQM()
    {
        var connection = RabbitMQConnection();
        
        var source = new CancellationTokenSource();
        var token = source.Token;
        
        IHandler[] handlers = [new ReadTripQueueHandler(), new ReadUserQueueHandler()];
        foreach (var handler in handlers)
        {
          var channel = connection.CreateModel();
          Task.Run(async () => await handler.Call(channel, token), token);
        }
        return source;
    }

    public static IConnection? RabbitMQConnection()
    {
        IConnection? connection = null;
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            connection = factory.CreateConnection();
        }
        catch (System.Exception)
        {
            Console.WriteLine("Trying to connect with RabbitMQ...");
            Thread.Sleep(5000);
            RabbitMQConnection();
        }
        return connection;
    }
    
    public static WebApplication SetupApi(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      builder.Services.AddAuthorization();
      builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddControllers();
        
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();
        return app;
    }
}