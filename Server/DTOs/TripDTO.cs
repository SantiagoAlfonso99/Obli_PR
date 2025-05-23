
public class TripDTO
{
    public int Id { get; set; } 
    public string ConductorName { get; set; }
    public string Origin { get; set; }
    public string Destination { get; set; }
    public string DepartureDate { get; set; }
    public string DepartureTime { get; set; }
    public int AvailableSeats { get; set; }
    public int PricePerPerson { get; set; }
    public string PetFriendly { get; set; }

    public TripDTO(Trip trip)
    {
        Id = trip.Id;
        ConductorName = trip.Conductor.UserName;
        Console.WriteLine(ConductorName);
        Origin = trip.Origin;
        Destination = trip.Destination;
        DepartureDate = trip.DepartureDate;
        DepartureTime = trip.DepartureTime;
        AvailableSeats = trip.AvailableSeats;
        PricePerPerson = trip.PricePerPerson;
        PetFriendly = trip.PetFriendly;
    }
}