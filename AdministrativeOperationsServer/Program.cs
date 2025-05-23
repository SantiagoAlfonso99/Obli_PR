using System.Globalization;
using Grpc.Net.Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using Grpc.Core;

namespace AdministrativeOperationsServer;

class Program
{
    private static Trips.TripsClient client;

    static async Task Main(string[] args)
    {
        var channel = GrpcChannel.ForAddress("http://localhost:5017");
        client = new Trips.TripsClient(channel);
        await ServerMenu();
    }
    
    private static async Task ServerMenu()
    {
        string response = "";
        while (response != "exit")
        {
            Console.WriteLine("Seleccione 1 para dar de alta un viaje");
            Console.WriteLine("Seleccione 2 para dar de baja un viaje");
            Console.WriteLine("Seleccione 3 para modificar un viaje");
            Console.WriteLine("Seleccione 4 consultar las calificaciones de un viaje");
            Console.WriteLine("Seleccione 5 para ver los proximos N viajes");
            response = Console.ReadLine();
            if (response == "1")
            {
                await AddTrip();
            }
            if (response == "2")
            {
                await DeleteTrip();
            }
            if (response == "3")
            {
                await UpdateTrip();
            }
            if (response == "4")
            {
                await GetTripRatings();
            }
            if (response == "5")
            {
                int numberOfTrips;
                Console.WriteLine("Por favor ingrese el numero de viajes:");
                while (!int.TryParse(Console.ReadLine(), out numberOfTrips))
                {
                    Console.WriteLine("Ingreso no válido, debe ingresar un número:");
                }
                await GetNextTrips(numberOfTrips);
                await ServerMenu();
            }
        }
    }
    
    private static async Task AddTrip()
    {
        var input = PublishTrip();
        var data = input.Split('#');
        var request = new AddTripRequest
        {
            Origin = data[0],
            Destination = data[1],
            DepartureDate = data[2],
            DepartureTime = data[3],
            Seats = int.Parse(data[4]),
            PricePerPerson = int.Parse(data[5]),
            PetFriendly = data[6]
        };
        var response = await client.AddTripAsync(request);
        Console.WriteLine(response.Message);
    }

    private static async Task UpdateTrip()
    {
        
        Console.WriteLine("Ingrese el ID del viaje que desea modificar:");
        var tripId = int.Parse(Console.ReadLine());
        
        var input = PublishTrip();
        var data = input.Split('#');

        var request = new UpdateTripRequest
        {
            TripId = tripId,
            Origin = data[0],
            Destination = data[1],
            DepartureDate = data[2],
            DepartureTime = data[3],
            Seats = int.Parse(data[4]),
            PricePerPerson = int.Parse(data[5]),
            PetFriendly = data[6]
        };
        var response = await client.UpdateTripAsync(request);
        Console.WriteLine(response.Message);
    }

    private static async Task DeleteTrip()
    {
        Console.WriteLine("Ingrese el ID del viaje que desea eliminar:");
        var tripId = int.Parse(Console.ReadLine());
        var request = new DeleteTripRequest { TripId = tripId };
        var response = await client.DeleteTripAsync(request);
        Console.WriteLine(response.Message);
    }

    private static async Task GetTripRatings()
    {
        Console.WriteLine("Ingrese el nombre del conductor:");
        var name = Console.ReadLine();
        var request = new GetRatingsRequest { Name = name };
        var response = await client.GetTripRatingsAsync(request);
        Console.WriteLine("Calificaciones:");
        foreach (var rating in response.Ratings)
        {
            Console.WriteLine(rating);
        }
    }
    private static async Task TestConnection()
    {
        try
        {
            var response = await client.SayHelloAsync(new HelloRequest { Name = "Test" });
            Console.WriteLine(response.Message);
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"gRPC Error: {ex.Status.Detail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected Error: {ex.Message}");
        }
    }
    static async Task GetNextTrips(int numberOfTrips)
    {
        List<Trip> returnedTrips = await ReadTripQueue(numberOfTrips);
        int count = 1;
        foreach (var trip in returnedTrips)
        {
            Console.WriteLine("Viaje: "+ count.ToString());
            Console.WriteLine("Id: "+ trip.Id.ToString());
            Console.WriteLine($"Origen: {trip.Origin}");
            Console.WriteLine($"Destino: {trip.Destination}");
            Console.WriteLine($"Fecha de salida: {trip.DepartureDate}");
            Console.WriteLine($"Hora de salida: {trip.DepartureTime}");
            Console.WriteLine($"Asientos disponibles: {trip.AvailableSeats}");
            Console.WriteLine($"Precio por persona: {trip.PricePerPerson}");
            Console.WriteLine($"Permite mascotas: {trip.PetFriendly}");
            Console.WriteLine("//////////////////////////////////////////////////////");
            count++;
        }
    }
    
    public static async Task<List<Trip>> ReadTripQueue(int numberOfTrips)
    {
        List<Trip> trips = new List<Trip>();
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        Console.WriteLine("Reading Trips");
        channel.QueuePurge("adminTripsQueue");
        channel.QueueDeclare(queue: "adminTripsQueue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        while (trips.Count < numberOfTrips)
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                var trip = JsonConvert.DeserializeObject<Trip>(message);
                
                trips.Add(trip);
                Console.WriteLine($"TripQueue [x] Received {message}");
            };
            channel.BasicConsume(queue: "adminTripsQueue",
                autoAck: true,
                consumer: consumer);
        }
        Console.WriteLine("Stopping trip queue reading");
        return trips;
    }
    
    public static string PublishTrip()
{
    Console.WriteLine("Ingrese un origen de salida");
    string origin = Console.ReadLine();
    Console.WriteLine("Ingrese un destino");
    string destination = Console.ReadLine();
    
    string departureDate;
    Regex dateRegex = new Regex(@"^\d{2}/\d{2}/\d{4}$"); 
    DateTime dateValue;
    DateTime currentDate = DateTime.Today;
    int currentYear = currentDate.Year; 
    do
    {
        Console.WriteLine("Ingrese fecha de salida en formato: dd/MM/yyyy");
        departureDate = Console.ReadLine();
        if (!dateRegex.IsMatch(departureDate) ||
            !DateTime.TryParseExact(departureDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue) ||
            dateValue <= currentDate || 
            dateValue.Year != currentYear)
        {
            Console.WriteLine("Fecha inválida, formato incorrecto, o la fecha no está en el futuro dentro del mismo año. Por favor intente de nuevo.");
        }
    } while (!dateRegex.IsMatch(departureDate) ||
             !DateTime.TryParseExact(departureDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue) ||
             dateValue <= currentDate ||
             dateValue.Year != currentYear);

    string departureTime;
    Regex timeRegex = new Regex(@"^\d{2}:\d{2}$"); 
    TimeSpan timeValue;
    do
    {
        Console.WriteLine("Ingrese hora de salida en formato: hh:mm");
        departureTime = Console.ReadLine();
        if (!timeRegex.IsMatch(departureTime) || !TimeSpan.TryParse(departureTime, out timeValue))
        {
            Console.WriteLine("Hora inválida o formato incorrecto, por favor intente de nuevo.");
        }
    } while (!timeRegex.IsMatch(departureTime) || !TimeSpan.TryParse(departureTime, out timeValue));

    Console.WriteLine("Ingrese cantidad de asientos disponibles");
    string availableSeats = Console.ReadLine();

    string pricePerPerson;
    decimal price;
    do
    {
        Console.WriteLine("Ingrese precio por persona");
        pricePerPerson = Console.ReadLine();
        if (!decimal.TryParse(pricePerPerson, out price) || price <= 0)
        {
            Console.WriteLine("Por favor, introduzca un precio válido (número positivo).");
        }
    } while (!decimal.TryParse(pricePerPerson, out price) || price <= 0);

    string petFriendly;
    do
    {
        Console.WriteLine("Ingrese 'si' en caso de permitir mascotas y en caso contrario ingrese 'no'");
        petFriendly = Console.ReadLine().ToLower(); // Convert input to lowercase to handle case insensitivity

        if (petFriendly != "si" && petFriendly != "no")
        {
            Console.WriteLine("Entrada inválida. Por favor, ingrese 'si' o 'no'.");
        }
    } while (petFriendly != "si" && petFriendly != "no");

    string result = $"{origin}#{destination}#{departureDate}#{departureTime}#{availableSeats}#{pricePerPerson}#{petFriendly}";
    return result;
}

}