using System.Net.Sockets;
using Handlers;
using Helpers;
using Server.Handlers;

public class HandlerGestor {
  public static async Task Call(TcpClient clientSocket, UserController userController, TripController tripController, CancellationToken token){
    UserContainer loggedUserContainer = new UserContainer();
    bool clientIsConected = true;

    var services = new List<IHandler>(){
        new LoginHandler(),
        new SignupHandler(),
        new AddTripHandler(),
        new JoinTripHandler(),
        new UpdateTripHandler(),
        new LeaveTripHandler(),
        new LookTripHandler(),
        new ReturnTripByIdHandler()
    };

    while (clientIsConected){
      try{
        string mensaje = await Connections.ReceiveMessage(clientSocket);
        int pos = Int32.Parse(mensaje) - 1;
        var service = services[pos];
        await service.Call(clientSocket, userController, tripController,loggedUserContainer);
      }
      catch (Exception e) { 
          clientIsConected = false;
      }
    }
    Console.WriteLine("Se desconecto un cliente");
  }
}
