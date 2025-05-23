using System.Net.Sockets;
using System.Text;
using Helpers;
using Server.Handlers;

public class LeaveTripHandler: IHandler
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
    Trip tripToDelete = tripController.RegisteredTrips.Find(trip => trip.Id == int.Parse(tripId));

    string msg = "Viaje Eliminado Exitosamente";
    if(tripToDelete == null) msg = "No puedes eliminar el viaje. Viaje no encontrado";
    else if(tripToDelete.Conductor.UserName != localUser.UserName) msg = "No puedes eliminar el viaje. No eres el conductor de este viaje";

    await Connections.SendMessage(msg, clientSocket);

    if(msg == "Viaje Eliminado Exitosamente"){
      tripController.DeleteTrip(tripId);
    }
  }
}