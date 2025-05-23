using System.Net.Sockets;
using System.Text;
using Helpers;
using Server.Handlers;

public class LoginHandler: IHandler
{
  public async Task Call(TcpClient clientSocket, UserController userController, TripController tripController,UserContainer loggedUser)
  {
    var mensaje = await Connections.ReceiveMessage(clientSocket);
    loggedUser.user = userController.LogIn(mensaje, clientSocket);
    mensaje = (loggedUser.user != null) ? "true" : "false";
    await Connections.SendMessage(mensaje, clientSocket);
  }

}