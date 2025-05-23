using System.Net.Sockets;
using System.Text;
using Helpers;
using Server.Handlers;

namespace Server.Handlers;

public class ReturnTripByIdHandler : IHandler
{
    public async Task Call(TcpClient clientSocket, UserController userController, TripController tripController,
        UserContainer loggedUser)
    {
        int tripId = int.Parse(await Connections.ReceiveMessage(clientSocket));
        Trip consultedTrip = tripController.RegisteredTrips.FirstOrDefault(trip => trip.Id == tripId);

        if (consultedTrip == null)
        {
            await Connections.SendMessage("", clientSocket);
        }
        else
        {
            await Connections.SendMessage(consultedTrip.ToString(), clientSocket);
        }
        string command = await Connections.ReceiveMessage(clientSocket);

        if (command == "1")
        {
            string folderPath = @"C:\FotosPr";
            string type = consultedTrip.FileType;
            string fileName = $"{tripId}.{type}";
            string abspath = Path.Combine(folderPath, fileName);
            var fileCommonHandler = new FileCommsHandler();
            await fileCommonHandler.SendFile(abspath, clientSocket);
        }
    }
}