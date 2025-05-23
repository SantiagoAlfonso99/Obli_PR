using System.Security.Cryptography;

namespace StatisticalDataServer.Models;

public class Report
{
    public List<Trip> Trips { get; set; }
    public int RequestedAmount { get; set; }
    public int Id { get; set; }

    public async Task<bool> IsReportCompleted()
    {
        return Trips.Count == RequestedAmount;
    }
}