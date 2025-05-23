using System.Net;
using System.Net.Sockets;
using Helpers;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;

public class TripController
    {
        private int Counter = 1;
        public List<Trip> RegisteredTrips { get; set; } = new List<Trip>();
        public int AddTrip(string inputData, User loggedUser)
        {
            lock(this)
            {
                int id = Counter;
                string[] inputs = inputData.Split('#');
                string origin = inputs[0];
                string destination = inputs[1];
                string departureDate = inputs[2];
                string departureTime = inputs[3];
                string availableSeats = inputs[4];
                string pricePerPerson = inputs[5];
                string petFriendly = inputs[6];
                Trip newTrip = new Trip()
                {
                    Id = id, Origin = origin, Destination = destination, DepartureTime = departureTime,
                    DepartureDate = departureDate, AvailableSeats = int.Parse(availableSeats), PricePerPerson = int.Parse(pricePerPerson), PetFriendly = petFriendly, Conductor = loggedUser
                };
                Console.WriteLine(loggedUser.UserName);
                RegisteredTrips.Add(newTrip);
                QueueHandler.EnqueueTrip(newTrip);
                Counter++;
                return id;
            }
        }

        public Trip ReceiveNewAttributes(string inputData)
        {
            string[] inputs = inputData.Split('#');
            string origin = inputs[0];
            string destination = inputs[1];
            string departureDate = inputs[2];
            string departureTime = inputs[3];
            string pricePerPerson = inputs[4];
            string petFriendly = inputs[5];
            Trip newTrip = new Trip()
            {
                Origin = origin, Destination = destination, DepartureTime = departureTime,
                DepartureDate = departureDate, PricePerPerson = int.Parse(pricePerPerson), PetFriendly = petFriendly
            };
            return newTrip;
        }
        public void DeleteTrip(string id)
        {
            lock(this){
                Trip tripToDelete = RegisteredTrips.Find(trip => trip.Id == int.Parse(id));
                string currentImageFileName = id + "." + tripToDelete?.FileType;
                if (tripToDelete == null)
                {
                    throw new ArgumentException();
                }

                if (tripToDelete != null)
                {
                        Console.WriteLine("Existe");
                        RegisteredTrips.Remove(tripToDelete);
                        var fileCommonHandler = new FileCommsHandler();
                        fileCommonHandler.DeleteImage(currentImageFileName);
                }
            }
        }
        
        public void JoinTrip(string id)
        {
            lock(this)
            {
                Trip tripToJoin = RegisteredTrips.Find(trip => trip.Id == int.Parse(id));
                if (tripToJoin != null)
                {
                    if (tripToJoin.AvailableSeats != 0)
                    {
                        tripToJoin.AvailableSeats--;
                    }
                }
            }
        }

        public async Task UpdateTrip(string tripId, Trip newData, User loggedUser, TcpClient clientSocket=null)
        {
            // throw new NotImplementedException();
            Trip tripToUpdate = RegisteredTrips.Find(trip => trip.Id == int.Parse(tripId) && trip.Conductor.UserName == loggedUser.UserName);
            if (tripToUpdate != null)
            {
                lock (this)
                {
                    tripToUpdate.Origin = newData.Origin;
                    tripToUpdate.Destination = newData.Destination;
                    tripToUpdate.DepartureDate = newData.DepartureDate;
                    tripToUpdate.DepartureTime = newData.DepartureTime;
                    tripToUpdate.PricePerPerson = newData.PricePerPerson;
                    tripToUpdate.PetFriendly = newData.PetFriendly;
                }
                try
                {
                    string currentImageFileName = tripId + "." + tripToUpdate.FileType;
                    var fileCommonHandler = new FileCommsHandler();
                    string fileName = await fileCommonHandler.ReceiveFile(clientSocket, tripId.ToString());
                    tripToUpdate.FileType = fileName.Substring(fileName.LastIndexOf('.') + 1);
                    fileCommonHandler.DeleteImage(currentImageFileName);
                    Console.WriteLine("Archivo recibido!!");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else throw new ArgumentException();
        }
    }