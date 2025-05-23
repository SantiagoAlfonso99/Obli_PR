using GrpcServer.Services;
using Server;

namespace GrpcServer;

public class Program
{
    public static void Main(string[] args)
    {
        TcpServer tcpServer = new TcpServer();
        
        var tcpServerTask = Task.Run(async () => await tcpServer.InitServer());

        var builder = WebApplication.CreateBuilder(args);
        
        // Registrar TcpServer como un servicio singleton
        builder.Services.AddSingleton(tcpServer);
        // Add services to the container.
        builder.Services.AddGrpc();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<TripsService>();
        app.MapGet("/", () =>
            "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        app.Run();
    }
}