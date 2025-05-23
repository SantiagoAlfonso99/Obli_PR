using System.Net.Sockets;
using System.Text;
using Helpers;
using Server.Handlers;

public class UpdateTripHandler: IHandler
{
  public async Task Call(TcpClient clientSocket, UserController userController, TripController tripController, UserContainer loggedUser)
  { 
    int maxPrice = int.Parse(await Connections.ReceiveMessage(clientSocket));
    User localUser = loggedUser.user;
    List<Trip> filteredList = tripController.RegisteredTrips;
    filteredList = tripController.RegisteredTrips
        .Where(trip => trip.Conductor.UserName == localUser.UserName && (trip.PricePerPerson <= maxPrice || maxPrice <= 0))
        .ToList();
    await Connections.SendMessage(filteredList.Count.ToString(), clientSocket);
    
    foreach (Trip trip in filteredList)
    {
        await Connections.SendMessage(trip.ToString(), clientSocket);
    }
    string tripId = await Connections.ReceiveMessage(clientSocket);
    Trip tripToUpdate = tripController.RegisteredTrips.Find(trip => trip.Id == int.Parse(tripId));
    
    string msg = "true";
    if(tripToUpdate == null) msg = "No puedes actualizar el viaje. Viaje no encontrado";
    else if(tripToUpdate.Conductor.UserName != localUser.UserName) msg = "No puedes actualizar el viaje. No eres el conductor de este viaje";

    await Connections.SendMessage(msg, clientSocket);
    if(msg == "true"){
      string newAttributes = await Connections.ReceiveMessage(clientSocket);
      Trip newAttributesTrip = tripController.ReceiveNewAttributes(newAttributes);
      await tripController.UpdateTrip(tripId, newAttributesTrip, loggedUser.user, clientSocket);
    }
  }
}