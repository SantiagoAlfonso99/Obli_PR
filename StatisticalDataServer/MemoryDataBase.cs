using System.Runtime.CompilerServices;
using StatisticalDataServer.Models;

namespace StatisticalDataServer;

public static class MemoryDataBase
{
    private static readonly object lockObj = new object();
    public static List<Trip> RegisteredTrips { get; set; } 
    public static  List<User> RegisteredUsers { get; set; }
    public static List<Report> Reports { get; set; }

    private static int  nextReportId = 0; 

    public static void InitLists()
    {
        RegisteredTrips = new List<Trip>();
        RegisteredUsers = new List<User>();
        Reports = new List<Report>();
    }

    public static int GetUsersCount()
    {
        return RegisteredUsers.Count;
    }

    public static async Task AddTrip(Trip newTrip)
    {
        RegisteredTrips.Add(newTrip);
        await AddTripsToReports(newTrip);
    }
    
    public static async Task AddUser(User newUser)
    {
        RegisteredUsers.Add(newUser);
    }
    
    public static async Task<int> AddReport(int RequestedTrips)
    {
        lock (lockObj)
        {
            nextReportId++;
            Report newReport = new Report()
            {
                Trips = new List<Trip>(),
                Id = nextReportId,
                RequestedAmount = RequestedTrips
            };
            Reports.Add(newReport);
            return nextReportId;   
        }
    }

    public static async Task<Report> GetReport(int reportId)
    {
        return Reports.FirstOrDefault(report => report.Id == reportId);
    }
    
    public static async Task<List<Trip>> FilterLists(int? maxPrice, string origin, string destination)
    {
        List<Trip> trips = RegisteredTrips;
        if (maxPrice.HasValue)
        {
            trips = trips.FindAll(trip => trip.PricePerPerson <= maxPrice.Value);
        }
        if (origin != "")
        {
            trips = trips.FindAll(trip => trip.Origin == origin);
        }
        if (destination != "")
        {
            trips = trips.FindAll(trip => trip.Destination == destination);
        }
        return trips;
    }
    
    private static async Task AddTripsToReports(Trip trip)
    {
        foreach (var report in Reports)
        {
            bool completedReport = await report.IsReportCompleted();
            if (!completedReport)
            {
                report.Trips.Add(trip);   
            }
        }
    }
};