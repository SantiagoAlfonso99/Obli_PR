using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class UserController
    {
        public List<User> Users { get; set; }

    public UserController()
    {
        Users = new List<User>();
    }

    public bool SignUp(string singInMessage)
    {
        lock(this)
        {
            string[] inputs = singInMessage.Split('#');
            string userName = inputs[0];
            string password = inputs[1];
            try
            {
                if (Users.Exists(user => user.UserName == userName))
                {
                    return false; 
                }
            }
            catch(Exception e) 
            {
                Console.WriteLine(e.Message);
            }
            Users.Add(new User(){UserName = userName, Password = password, TripJoinedAsPassenger = new List<Trip>(), Rating = new List<FeedBack>()});
            return true;
        }
    }

        public User LogIn(string userData, TcpClient socketClient)
        {
            lock(this)
            {
                string[] inputs = userData.Split('#');
                string userName = inputs[0];
                string password = inputs[1];
                User registeredUser = Users.Find(user => (user.UserName == userName && user.Password == password));
                if (registeredUser !=null)
                {
                    QueueHandler.EnqueueUser(registeredUser);
                }
                return registeredUser;
            }
        }
        
        public User Get(string username)
        {
            var user = Users.Find(user => user.UserName == username);
            return user;
        }
        
        public void CreateConductorFeedBack(string comment, string stars, string conductorUserName)
        {
            lock(this)
            {
                User conductor = Users.Find(user => user.UserName == conductorUserName);
                if (conductor != null)
                {
                    FeedBack newFeedBack = new FeedBack() { Comment = comment, NumberOfStars = stars };
                    conductor.Rating.Add(newFeedBack);
                }
            }
        }
        
        
        public List<FeedBack> GetRating(string conductorUserName)
        {
            lock(this)
            {
                List<FeedBack> return_list = null;
                User conductor = Users.Find(user => user.UserName == conductorUserName);
                if (conductor != null)
                {
                    return_list = conductor.Rating;
                }
                return return_list;
            }
        }
    }