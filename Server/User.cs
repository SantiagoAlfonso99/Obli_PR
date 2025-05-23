public class User
    {
        public string  UserName { get; set; }
        public string Password { get; set; }
        public List<FeedBack> Rating { get; set; }
        public List<Trip> TripJoinedAsPassenger { get; set; }
    }