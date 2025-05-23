namespace StatisticalDataServer.Models;

public class Trip
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
        
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        Trip otherTrip = (Trip)obj;
        return Id.Equals(otherTrip.Id);
    }

    public override string ToString()
    {
        return Id.ToString() + '#' + Origin + '#' + Destination + '#' + DepartureDate + '#' + DepartureTime + '#' + AvailableSeats + '#' 
               + PricePerPerson + '#' + PetFriendly + '#' + ConductorName ;
    }
}