using Grpc.Core;
using GrpcServer;

namespace GrpcServer.Services
{
    public class TripsService : Trips.TripsBase
    {
        private readonly TcpServer _tcpServer;

        public TripsService(TcpServer tcpServer)
        {
            _tcpServer = tcpServer;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
        }
        
        public override async Task<TripResponse> AddTrip(AddTripRequest request, ServerCallContext context)
        {
            await _tcpServer.AddAdminTrip(request.Origin + "#" + request.Destination + "#" + request.DepartureDate + "#" + request.DepartureTime + "#" + request.Seats + "#" + request.PricePerPerson + "#" + request.PetFriendly);
            return new TripResponse { Message = "Trip added successfully"};
        }
        
        public override async Task<TripResponse> UpdateTrip(UpdateTripRequest request, ServerCallContext context)
        {
          try {
            var trip = new Trip
            {
                Origin = request.Origin,
                Destination = request.Destination,
                DepartureDate = request.DepartureDate,
                DepartureTime = request.DepartureTime,
                AvailableSeats = request.Seats,
                PricePerPerson = request.PricePerPerson,
                PetFriendly = request.PetFriendly
            };
            await _tcpServer.ModifyTrip(request.TripId.ToString(), trip);
            return new TripResponse { Message = "Trip updated successfully", TripId = request.TripId };
          }
          catch (Exception e)
          {
            return new TripResponse { Message = "Couldn't update trip" };
          }
        }

        public override async Task<TripResponse> DeleteTrip(DeleteTripRequest request, ServerCallContext context)
        {
             try
            {
                await _tcpServer.DeleteTrip(request.TripId.ToString());
                return new TripResponse { Message = "Trip deleted successfully" };
            }
            catch (Exception e)
            {
                return new TripResponse { Message = "Couldn't delete trip" };
            }
        }

        public override async Task<TripRatingsResponse> GetTripRatings(GetRatingsRequest request, ServerCallContext context)
        {
          var ratings = await _tcpServer.GetTripRatings(request.Name);
          return new TripRatingsResponse { Ratings = { ratings } };
        }
    }
}