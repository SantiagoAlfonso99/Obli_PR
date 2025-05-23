using System.Net.Sockets;
using System.Text;
using Helpers;
using Server.Handlers;

public class SignupHandler: IHandler
{
  public async Task Call(TcpClient clientSocket, UserController userController, TripController tripController, UserContainer loggedUser)
  {
    var mensaje = await Connections.ReceiveMessage(clientSocket);
    bool success = userController.SignUp(mensaje);
    mensaje = success ? "true" : "false";
    await Connections.SendMessage(mensaje, clientSocket);
  }
}
