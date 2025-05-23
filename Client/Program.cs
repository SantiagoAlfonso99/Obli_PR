using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using Helpers;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace Client
{
    internal class Program
    {
        static readonly SettingsManager settingsMngr = new SettingsManager();
        static async Task Main(string[] args)
        {
            Console.WriteLine("Iniciando aplicacion Client!");
            
            string ipServer = settingsMngr.ReadSettings(ClientConfig.serverIPconfigkey);
            string ipClient = settingsMngr.ReadSettings(ClientConfig.clientIPconfigkey);
            int serverPort = int.Parse(settingsMngr.ReadSettings(ClientConfig.serverPortconfigkey));
            var localEndPoint = new IPEndPoint(IPAddress.Parse(ipClient), 0);
            var serverEndpoint = new IPEndPoint(IPAddress.Parse(ipServer), serverPort);
            var socketClient = new TcpClient(localEndPoint);
            try
            {
                await socketClient.ConnectAsync(serverEndpoint);
            }
            catch (SocketException)
            {
               Console.WriteLine("No se pudo conectar con servidor");
               return;
            }
            Console.WriteLine("Client Conectado al Servidor");
            UserController userController = new UserController();
            try
            {
                await userController.InitialMenu(socketClient);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("El servidor esta apagado, vuelva mas tarde");
            }
        }
    }
    
    internal class UserController //Encargado del inicio de sesion y toda esas cosas
    {
        public async Task InitialMenu(TcpClient socketClient)
        {
            Console.WriteLine("Bienvenido");
            Console.WriteLine("Seleccione 1 para iniciar sesion");
            Console.WriteLine("Seleccione 2 para registrarse");
            Console.WriteLine("Seleccione 3 para salir");
            string message = Console.ReadLine();
            if (message == "1")
            {
                await LogIn(socketClient);
            }
            else if (message == "2")
            {
                await SignIn(socketClient);
            }
            else if (message == "3")
            {
                Console.WriteLine("Voy a Cerrar la conexion");
                socketClient.Close();
            }
            else
            {
                Console.WriteLine("Ingrese un comando valido");
                await InitialMenu(socketClient);
            }
        }
        
        public async Task SignIn(TcpClient socketClient)
        {
            Console.WriteLine("Ingrese un nombre para su cuenta de usuario");
            string userNameIn = Console.ReadLine();
            Console.WriteLine("Ingrese una contraseña para su cuenta de usuario");
            string passwordIn = Console.ReadLine();
            string message = userNameIn +'#'+ passwordIn;
            
            await Connections.SendMessage("2", socketClient);
            await Connections.SendMessage(message,socketClient);

            string mensajeRecibido = await Connections.ReceiveMessage(socketClient);
            if (mensajeRecibido == "true")
            {
                Console.WriteLine("Se registro correctamente en el sistema");
                await InitialMenu(socketClient);
            }
            else
            {
                Console.WriteLine("Ese nombre de usuario no esta disponible");
                await SignIn(socketClient);
            }
        }

        public async Task LogIn(TcpClient socketClient)
        {
            Console.WriteLine("Ingrese su nombre de usuario");
            string userNameIn = Console.ReadLine();
            Console.WriteLine("Ingrese su contraseña");
            string passwordIn = Console.ReadLine();
            string message = userNameIn +'#'+ passwordIn;
            
            await Connections.SendMessage("1", socketClient);
            await Connections.SendMessage(message,socketClient);

            string mensajeRecibido = await Connections.ReceiveMessage(socketClient);
            if (mensajeRecibido == "true")
            {
                Console.WriteLine("Se inicio sesion con exito");
                await OptionsMenu(socketClient);
            }
            else
            {
                Console.WriteLine("No se pudo iniciar sesion");
                Console.WriteLine("El usuario no existe o la contraseña es incorrecta");
                await InitialMenu(socketClient);
            }
        }

        public async Task OptionsMenu(TcpClient clientSocket)
        {
            Console.WriteLine("Bienvenido al menu de usuario");
            Console.WriteLine("Seleccione una opcion a ejecutar:");
            
            Console.WriteLine("Seleccione 3 para publicar un viaje");
            Console.WriteLine("Seleccione 4 para unirse a un viaje");
            Console.WriteLine("Seleccione 5 para modificar un viaje");
            Console.WriteLine("Seleccione 6 para dar de baja un viaje");
            Console.WriteLine("Seleccione 7 para buscar viajes disponibles");
            Console.WriteLine("Seleccione 8 para consultar informacion de un viaje en especifico");
            Console.WriteLine("Para cerrar el menu presione salir");
            string selectedOption = Console.ReadLine();
            if (selectedOption == "3")
            {
                await Connections.SendMessage("3", clientSocket);
                string tripData = PublishTrip();
                await Connections.SendMessage(tripData, clientSocket);
                try
                {
                    Console.WriteLine("Ingrese la ruta completa de la foto a enviar");
                    string abspath = Console.ReadLine();
                    var fileCommonHandler = new FileCommsHandler();
                    await fileCommonHandler.SendFile(abspath, clientSocket);
                    Console.WriteLine("Se envio el archivo al Servidor");
                }
                catch (Exception e)
                {
                    Console.WriteLine("El archivo no existe, se publicara el viaje sin foto");
                    await OptionsMenu(clientSocket);
                }
                await OptionsMenu(clientSocket);
            }
            if (selectedOption == "4")
            {
                await Connections.SendMessage("4", clientSocket);
                Console.WriteLine("Viajes publicados: ");
                await ShowTrips(clientSocket);
                Console.WriteLine("Escriba el identificador del viaje que desea unirse");
                string tripId = Console.ReadLine();
                await Connections.SendMessage(tripId,clientSocket);
                var msg = await Connections.ReceiveMessage(clientSocket);
                if(msg == "true")
                { 
                    Console.WriteLine("Se unio al viaje con exito");
                  Console.WriteLine("Escriba un comentario acerca de su conductor");
                  string comment = Console.ReadLine();
                  await Connections.SendMessage(comment,clientSocket);
                  string stars;
                  int starValue;
                  do
                  {
                      Console.WriteLine("Califique a su conductor del 1 al 5:");
                      stars = Console.ReadLine();
                      if (!int.TryParse(stars, out starValue) || starValue < 1 || starValue > 5)
                      {
                          Console.WriteLine("Por favor, introduzca un número válido entre 1 y 5.");
                      }
                  } while (!int.TryParse(stars, out starValue) || starValue < 1 || starValue > 5);
                  await Connections.SendMessage(stars, clientSocket);
                }
                else
                {
                  Console.WriteLine(msg);
                }
                await OptionsMenu(clientSocket);
            }
            if (selectedOption == "5")
            {
                await Connections.SendMessage("5", clientSocket);
                Console.WriteLine("Viajes publicados por usted:");
                await ShowTrips(clientSocket);
                Console.WriteLine("Escriba el identificador del viaje que quieres modificar");
                string tripId = Console.ReadLine();
                await Connections.SendMessage(tripId,clientSocket);
                string msg = await Connections.ReceiveMessage(clientSocket);
                if (msg == "true")
                {
                  Console.WriteLine("Editando Viaje!");
                  await Connections.SendMessage(SendNewAttributes(),clientSocket);
                  try
                  {
                      Console.WriteLine("Ingrese la ruta completa de la foto a enviar");
                      string abspath = Console.ReadLine();
                      var fileCommonHandler = new FileCommsHandler();
                      await fileCommonHandler.SendFile(abspath, clientSocket);
                      Console.WriteLine("Se envio el archivo al Servidor");
                  }
                  catch (Exception e)
                  {
                      Console.WriteLine("El archivo no existe, se publicara el viaje sin foto");
                      await OptionsMenu(clientSocket);
                  }
                }
                else
                {
                    Console.WriteLine(msg);
                }
                await OptionsMenu(clientSocket);
            }
            if (selectedOption == "6")
            {
                await Connections.SendMessage("6", clientSocket);
                Console.WriteLine("Viajes publicados por usted:");
                await ShowTrips(clientSocket);
                Console.WriteLine("Escriba el identificador del viaje que quieres modificar");
                string tripId = Console.ReadLine();
                await Connections.SendMessage(tripId, clientSocket);
                string msg = await Connections.ReceiveMessage(clientSocket);
                Console.WriteLine(msg);
                await OptionsMenu(clientSocket);
            }
            if (selectedOption == "7")
            {
                await Connections.SendMessage("7", clientSocket);
                await ShowTrips(clientSocket);
                Console.WriteLine("Escriba '9' para ver mas informacion acerca de un conductor o cualquier otra tecla para volver al menu principal");
                string command = Console.ReadLine();
                await Connections.SendMessage(command, clientSocket);
                if (command == "9")
                {
                    Console.WriteLine("Escriba el nombre de usuario del conductor");
                    string userNameConductor = Console.ReadLine();
                    await Connections.SendMessage(userNameConductor,clientSocket);
                    int feedBackCount = int.Parse(await Connections.ReceiveMessage(clientSocket));
                    for (int i = 0; i < feedBackCount; i++)
                    {
                        string feedBack = await Connections.ReceiveMessage(clientSocket);
                        string[] inputs = feedBack.Split("#");
                        Console.WriteLine("Comentario: "+ inputs[0]+ "   //   " + "Estrellas: "+ inputs[1]);
                    }
                }
                await OptionsMenu(clientSocket);
            }
            if (selectedOption == "8")
            {
                await Connections.SendMessage("8", clientSocket);
                Console.WriteLine("Ingrese el identificador del viaje a visualizar");
                string tripId = Console.ReadLine();
                await Connections.SendMessage(tripId, clientSocket);
                string returnedTrip = await Connections.ReceiveMessage(clientSocket);
                await ShowTrip(returnedTrip, clientSocket);
                await OptionsMenu(clientSocket);
            }
            if (selectedOption == "salir")
            {
                await InitialMenu(clientSocket);
            }
        }

        public async Task ShowTrip(string returnedTrip, TcpClient clientSocket)
        {
            ShowTripAttributes(returnedTrip);
            Console.WriteLine("Presione '1' si desea descargar la imagen o cualquier otra tecla para volver al menu principal");
            string response = Console.ReadLine();
            await Connections.SendMessage(response, clientSocket);
            if (response == "1")
            {
                try
                {
                    var fileCommonHandler = new FileCommsHandler();
                    await fileCommonHandler.ReceiveFile(clientSocket, "-1");
                    Console.WriteLine("Archivo recibido!!");
                }
                catch(Exception e)
                {
                    Console.WriteLine("El viaje solicitado no tiene una imagen del vehiculo");
                }
            }
            await OptionsMenu(clientSocket);
        }
        
        public static string PublishTrip()
        {
            Console.WriteLine("Ingrese un origen de salida");
            string origin = Console.ReadLine();

            Console.WriteLine("Ingrese una destino");
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


            return origin + '#' + destination + '#' + departureDate + '#' + departureTime + '#' + availableSeats + '#' + pricePerPerson + '#' + petFriendly;
        }
        
        public static string SendNewAttributes()
        {
            Console.WriteLine("Ingrese un origen de salida");
            string origin = Console.ReadLine();
            Console.WriteLine("Ingrese una destino");
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
            return origin + '#' + destination + '#' + departureDate + '#' + departureTime + '#' + pricePerPerson +
                   '#' + petFriendly;
        }

        public static async Task ShowTrips(TcpClient clientSocket)
        {
            Console.WriteLine("Desea filtrar por precio?,Escriba 'si' en caso afirmativo");
            string respuesta = Console.ReadLine();
            int maxPrice = 0;
            if (respuesta == "si")
            {
                Console.WriteLine("Escriba un precio maximo");
                maxPrice = int.Parse(Console.ReadLine());
            }

            await Connections.SendMessage(maxPrice.ToString(), clientSocket);
            int tripsLength = int.Parse(await Connections.ReceiveMessage(clientSocket));
            if (tripsLength == 0) Console.WriteLine("No hay viajes disponibles");
            for (int i = 0; i < tripsLength; i++)
            {
                Console.WriteLine("viaje: " + i);
                string message = await Connections.ReceiveMessage(clientSocket);
                Console.WriteLine("/////////////TRIP" + " " + (i+1) + "/////////////");
                ShowTripAttributes(message);
            }
        }
        
        public static void ShowTripAttributes(string trip)
        {
            string[] inputs = trip.Split('#');
            string id = inputs[0];
            Console.WriteLine("Id: " + id);
            string origin = inputs[1];
            Console.WriteLine("Origen: " + origin);
            string destination = inputs[2];
            Console.WriteLine("Destination: " + destination);
            string departureDate = inputs[3];
            Console.WriteLine("Departure date: " + departureDate);
            string departureTime = inputs[4];
            Console.WriteLine("Departure time: " + departureTime);
            string availableSeats = inputs[5];
            Console.WriteLine("Available seats: " + availableSeats);
            string pricePerPerson = inputs[6];
            Console.WriteLine("Price per person: " + pricePerPerson);
            string petFriendly = inputs[7];
            Console.WriteLine("PetFriendly: " + petFriendly);
            string conductorUserName = inputs[8];
            Console.WriteLine("Conductor user name: " + conductorUserName);
        }
    }
}