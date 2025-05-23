using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace StatisticalDataServer;

[ApiController]
[Route("api/statistics")]
public class StatisticsController : ControllerBase
{
    
    [HttpGet("/users")]
    public async Task<IActionResult> GetUsersCount()
    {
        int count = MemoryDataBase.GetUsersCount();
        return Ok(count);
    }
    
    [HttpGet("/trips")]
    public async Task<IActionResult> FilteredTrips([FromQuery] int? maxPrice, string origin = "", string destination = "")
    {
        var trips = await MemoryDataBase.FilterLists(maxPrice, origin, destination);
        return Ok(trips);
    }
    
    [HttpPost("/reports")]
    public async Task<IActionResult> GenerateReport([FromQuery] int numberOfTrips)
    {
        if (numberOfTrips <= 0)
        {
            return BadRequest(new { Message = "The number of trips must be greater than 0." });
        }
        int tripId = await MemoryDataBase.AddReport(numberOfTrips);
        return Ok(new {Message = $"The report was created successfully with id: {tripId}"});
    }
    
    [HttpGet("/reports/{id}")]
    public async Task<IActionResult> GetReport(int id)
    {
        var report = await MemoryDataBase.GetReport(id);
        if (report == null)
        {
            return NotFound(new { Message = "There is no report with that ID" });
        }
        else if (! await report.IsReportCompleted())
        {
            return BadRequest(new { Message = "The report is not ready yet." });
        }
        return Ok(report);
    }
}