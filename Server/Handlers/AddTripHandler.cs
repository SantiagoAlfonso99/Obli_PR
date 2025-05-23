using System.Net.Sockets;
using System.Text;
using Helpers;
using Server.Handlers;

public class AddTripHandler: IHandler
{
  public async Task Call(TcpClient clientSocket, UserController userController, TripController tripController, UserContainer loggedUser)
  {
    Console.WriteLine("AddTripHandler");
    var mensaje = await Connections.ReceiveMessage(clientSocket);
    int returnedId = tripController.AddTrip(mensaje, loggedUser.user);
    try
    {
      var fileCommonHandler = new FileCommsHandler();
      string fileName = await fileCommonHandler.ReceiveFile(clientSocket, returnedId.ToString());
      Trip trip = tripController.RegisteredTrips.FirstOrDefault(trip => trip.Id == returnedId);
      trip.FileType = fileName.Substring(fileName.LastIndexOf('.') + 1);
      Console.WriteLine("Archivo recibido!!");
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }
  }
}