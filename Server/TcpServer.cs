using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Helpers;
using Server;
using RabbitMQ.Client;

public class TcpServer
{
  static readonly SettingsManager settingsMngr = new SettingsManager();
  private static List<TcpClient> socketList = new List<TcpClient>();
  private static List<Task> clientTasks = new List<Task>();
  private static UserController UsersHandler = new UserController();
  private static TripController TripsHandler = new TripController();
  private static bool serverOpen = true;
  public async Task InitServer()
  {
    User user = new User()
    {
      UserName = "ServerAdmin", Password = "ServerAdmin123", TripJoinedAsPassenger = new List<Trip>(),
      Rating = new List<FeedBack>()
    };
    UsersHandler.Users.Add(user);
    var connection = await setupServer();
    await HandleClients(connection);
    Console.WriteLine("El servidor se ha cerrado correctamente");
  }

  private static async Task<TcpListener> setupServer()
  {
    Console.WriteLine("Iniciando Servidor!");
    await QueueHandler.SetupQueues();
    Console.WriteLine("Server is starting...");
    var ipEndPoint = new IPEndPoint(
      IPAddress.Parse("127.0.0.1"), 6000);
    var tcpListener = new TcpListener(ipEndPoint);

    tcpListener.Start(100);
    Console.WriteLine("Server started listening connections on {0}-{1}",settingsMngr.ReadSettings(ServerConfig.serverIPconfigkey),settingsMngr.ReadSettings(ServerConfig.serverPortconfigkey));

    Console.WriteLine("Server will start displaying messages from the clients");

    return tcpListener;
  }

  private static async Task HandleClients(TcpListener tcpListener)
  {
    var source = new CancellationTokenSource();
    var token = source.Token;
    var menuTask = Task.Run(async () => await ServerMenu(tcpListener, source));
    while (serverOpen) 
    {
      try
      {
        TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
        if (!source.IsCancellationRequested)
        {
          socketList.Add(tcpClient);
          Console.WriteLine("Acepte una conexion de un cliente");
          var task = Task.Run(async () => await HandlerGestor.Call(tcpClient, UsersHandler, TripsHandler, token), token);
          clientTasks.Add(task);
        }
        else
        {
          tcpClient.GetStream().Close();
          tcpClient.Close();
        }
      }
      catch (SocketException)
      {
        Console.WriteLine("Se cerro comunicacion con clientes");
        serverOpen = false;
      }
    }
  }
  
  private static async Task ServerMenu(TcpListener tcpListener, CancellationTokenSource source)
  {
    string response = "";
    bool success = true;
    while (success)
    {
      Console.WriteLine("Seleccione 1 para forzar cierre");
      Console.WriteLine("Seleccione 2 para esperar que los clientes terminen");
      response = Console.ReadLine();
      if (response == "1")
      {
        foreach (var client in socketList)
        {
          success = false;
          client.GetStream().Close();
          client.Close();
        }
        serverOpen = false;
        tcpListener.Stop();
      }
      else if (response == "2")
      {
        success = false;
        source.Cancel();
        await Task.WhenAll(clientTasks);
        tcpListener.Stop();
      }
      else
      {
        Console.WriteLine("Comando no valido");
      }
    }
  }
  
  
  public async Task AddAdminTrip(string input_data)
  {
    User user = UsersHandler.Get("ServerAdmin");
    TripsHandler.AddTrip(input_data, user);
  }

  public async Task ModifyTrip(string id, Trip input_data)
  {
      User user = UsersHandler.Get("ServerAdmin");
      await TripsHandler.UpdateTrip(id, input_data, user);
  }

  public async Task DeleteTrip(string id)
  {
    TripsHandler.DeleteTrip(id);
  }
  
  public async Task<List<int>> GetTripRatings(string userName)
  {
    List<FeedBack> feedbacks = UsersHandler.GetRating(userName);
    if (feedbacks != null)
    {
      return feedbacks.Select(fb => int.Parse(fb.NumberOfStars)).ToList();
    }
    return new List<int>();
  }
}