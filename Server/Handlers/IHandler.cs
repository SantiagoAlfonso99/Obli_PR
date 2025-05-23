using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace Server.Handlers
{
    public interface IHandler
    {
        public Task Call(TcpClient clientSocket, UserController userController, TripController tripController,UserContainer userContainer);
    }
}
