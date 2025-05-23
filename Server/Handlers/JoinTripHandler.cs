using System.Net.Sockets;
using System.Text;
using Helpers;
using Microsoft.VisualBasic;
using Server.Handlers;

namespace Handlers;
public class JoinTripHandler: IHandler
{
  public async Task Call(TcpClient clientSocket, UserController userController, TripController tripController, UserContainer loggedUser)
  {
    int maxPrice = int.Parse(await Connections.ReceiveMessage(clientSocket));
    User localUser = loggedUser.user;
    List<Trip> filteredList = tripController.RegisteredTrips;
    filteredList = tripController.RegisteredTrips
        .Where(trip => trip.Conductor.UserName != localUser.UserName && (trip.PricePerPerson <= maxPrice || maxPrice <= 0))
        .ToList();

    var count = filteredList.Count;
    await Connections.SendMessage(filteredList.Count.ToString(), clientSocket);
    
    foreach (Trip trip in filteredList)
    {
        await Connections.SendMessage(trip.ToString(), clientSocket);
    }
    
    string tripId = await Connections.ReceiveMessage(clientSocket);

    Trip tripToJoin = tripController.RegisteredTrips.Find(trip => trip.Id == int.Parse(tripId));
    string msg = "true";
    if(tripToJoin == null) msg = "No puedes unirte al viaje. Viaje no encontrado";
    else if(tripToJoin.Conductor.UserName == localUser.UserName) msg = "No puedes unirte al viaje. No puedes unirte a tu propio viaje";
    else if(tripToJoin.AvailableSeats <= 0) msg = "No puedes unirte al viaje. No hay asientos disponibles";
    
    await Connections.SendMessage(msg, clientSocket);
    
    if (msg == "true"){

    tripController.JoinTrip(tripId);

    string comment = await Connections.ReceiveMessage(clientSocket);
    string stars = await Connections.ReceiveMessage(clientSocket);

    Trip tripJoined = tripController.RegisteredTrips.Find(trip => trip.Id == int.Parse(tripId));

    userController.CreateConductorFeedBack(comment, stars, tripJoined.Conductor.UserName);
    }
  }
}