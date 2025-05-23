using System.Net.Sockets;
using System.Text;
using Helpers;
using Server.Handlers;

public class LookTripHandler: IHandler
{
  public async Task Call(TcpClient clientSocket, UserController userController, TripController tripController, UserContainer loggedUser)
  {
    int maxPrice = int.Parse(await Connections.ReceiveMessage(clientSocket));
    List<Trip> filteredList = tripController.RegisteredTrips;
    if (maxPrice > 0)
    {
        filteredList = tripController.RegisteredTrips
            .Where(trip => trip.PricePerPerson <= maxPrice)
            .ToList();
    }
    await Connections.SendMessage(filteredList.Count.ToString(), clientSocket);
    
    foreach (Trip trip in filteredList)
    {
        await Connections.SendMessage(trip.ToString(), clientSocket);
    }

    string command = await Connections.ReceiveMessage(clientSocket);
    if (command == "9")
    {
        string conductorUserName = await Connections.ReceiveMessage(clientSocket);
        List<FeedBack> conductorComments =
            userController.Users.Find(user => user.UserName == conductorUserName).Rating;
        await Connections.SendMessage(conductorComments.Count.ToString(), clientSocket);
        foreach (FeedBack feedBack in conductorComments)
        {
            string conductorFeedBack = feedBack.Comment + "#" + feedBack.NumberOfStars;
            await Connections.SendMessage(conductorFeedBack, clientSocket);
        }
    }
  }

}