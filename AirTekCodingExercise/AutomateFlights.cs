
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AirTekCodingExercise
{

    // enums
    public enum Airport
    {
        YUL,
        YYZ,
        YYC,
        YVR,
        YYE
        // add airports as necessary
    }

    // interfaces

    // abstract class to force derived classes to implement ToString
    public abstract class IFlight
    {
        uint FlightNumber { get; set; }
        Airport DepartureAirport { get; set; }
        Airport ArrivalAirport { get; set; }
        uint DepartureDay { get; set; }
        uint ArrivalDay { get; set; } // for extension to overnight flights
        public abstract override string ToString();
    }

    // classes
    public class Flight : IFlight
    {
        public uint FlightNumber { get; set; }
        public Airport DepartureAirport { get; set; }
        public Airport ArrivalAirport { get; set; }
        public uint DepartureDay { get; set; }
        public uint ArrivalDay { get; set; }
        public uint FreeCapacity { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassName"/> class.
        /// </summary>
        public Flight(uint flightNumber, Airport departureAirport, Airport arrivalAirport, uint departureDay, uint arrivalDay)
        {
            FlightNumber = flightNumber;
            DepartureAirport = departureAirport;
            ArrivalAirport = arrivalAirport;
            DepartureDay = departureDay;
            ArrivalDay = arrivalDay;
            FreeCapacity = 20;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassName"/> class, assuming a same day flight.
        /// </summary>
        public Flight(uint flightNumber, Airport departureAirport, Airport arrivalAirport, uint departureDay)
        {
            FlightNumber = flightNumber;
            DepartureAirport = departureAirport;
            ArrivalAirport = arrivalAirport;
            DepartureDay = departureDay;
            ArrivalDay = departureDay;
            FreeCapacity = 20;
        }

        public override string ToString()
        {
            // extension: modify to print arrival day as well if this is an overnight flight
            return string.Format("Flight: {0}, departure: {1}, arrival: {2}, day: {3}", FlightNumber, DepartureAirport, ArrivalAirport, DepartureDay);
        }
    }

    public static class FlightPrinter
    {
        public static void printFlightToConsole(IFlight flight)
        {
            Console.WriteLine(flight.ToString());
        }
    }

    public class OrderItem
    {
        public Airport Destination { get; set; }

    }

    public class Order
    {
        public string OrderName { get; set; }
        public uint? FlightNumber { get; set; } // null if not yet scheduled

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassName"/> class.
        /// </summary>
        public Order(string orderName, uint? flightNumber = null)
        {
            OrderName = orderName;
            FlightNumber = flightNumber;
        }
    }

    class AutomateFlights
    {
        static void Main(string[] args)
        {

            // user story 1 functionality
            Console.WriteLine("The current flight schedule is as follows: ");
            Console.Write(Environment.NewLine);

            // populate list of scheduled flights
            HashSet<Flight> flightsList = new HashSet<Flight>();
            Dictionary<Airport, uint> destToNextAvailableFlight = new Dictionary<Airport, uint>();

            // enhancement: use a factory to ensure flight numbers are generated in an error proof manner (can't be repeated)
            flightsList.Add(new Flight(1, Airport.YUL, Airport.YYZ, 1));
            flightsList.Add(new Flight(2, Airport.YUL, Airport.YYC, 1));
            flightsList.Add(new Flight(3, Airport.YUL, Airport.YVR, 1));

            flightsList.Add(new Flight(4, Airport.YUL, Airport.YYZ, 2));
            flightsList.Add(new Flight(5, Airport.YUL, Airport.YYC, 2));
            flightsList.Add(new Flight(6, Airport.YUL, Airport.YVR, 2));

            // populate for user story 2
            destToNextAvailableFlight.Add(Airport.YYZ, 1);
            destToNextAvailableFlight.Add(Airport.YYC, 2);
            destToNextAvailableFlight.Add(Airport.YVR, 3);

            // print all flights in the list
            foreach (var flight in flightsList)
            {
                FlightPrinter.printFlightToConsole(flight);
            }


            // user story 2 functionality
            Console.Write(Environment.NewLine);
            Console.WriteLine("Generating the flight itinerary by assigning orders to flights...");

            // assume json file is in same directory as this program
            string fileName = "coding-assigment-orders.json";
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            string jsonContent;
            try
            {
                jsonContent = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse the following file: {0}, please check the file exists and is valid. More info: {1}",
                    filePath, ex.Message);
                return;
            }

            if (jsonContent == null)
            {
                Console.WriteLine("Failed to access data from the following file: {0}, please check the file contents are valid.",
                    filePath);
                return;
            }

            // parse result from file into list of orders
            Dictionary<string, OrderItem> orderItemList = null;
            try
            {
                 orderItemList = JsonConvert.DeserializeObject<Dictionary<string, OrderItem>>(jsonContent);
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Deserialization failed, please check the contents of the following file are valid: {0}", filePath);
                return;
            }
            
            if (orderItemList == null)
            {
                Console.WriteLine("Failed to access data from the following file: {0}, please check the file contents are valid.",
                    filePath);
                return;
            }

            // assign orders to flights
            IList<Order> orderList = new List<Order>(); 
            foreach (var order in orderItemList)
            {
                string orderId = order.Key;
                OrderItem item = order.Value;

                // check next available flight with Dict
                uint nextAvailableFlight;
                if (destToNextAvailableFlight.TryGetValue(item.Destination, out nextAvailableFlight))
                {
                    // update capacity of the flight we're scheduling
                    var orderFlight = flightsList.FirstOrDefault(f => f.FlightNumber == nextAvailableFlight);
                    if (orderFlight != null)
                    {
                        orderFlight.FreeCapacity -= 1;

                        if (orderFlight.FreeCapacity == 0)
                        {
                            destToNextAvailableFlight.Remove(item.Destination);

                            // update destToNextAvailableFlight with next flight to this destination
                            var nextFlightToThisDestination = flightsList.FirstOrDefault((f => f.ArrivalAirport == item.Destination && f.FlightNumber > orderFlight.FlightNumber));
                            if (nextFlightToThisDestination != null)
                                destToNextAvailableFlight.Add(item.Destination, nextFlightToThisDestination.FlightNumber);
                        }

                        orderList.Add(new Order(orderId, orderFlight.FlightNumber));
                    }
                }
                else
                {
                    // no flight for this order's destination
                    orderList.Add(new Order(orderId, null));
                }
            }

            // print all orders with their scheduled flights (unless not scheduled)
            // enhancement: break out a printer class for printing Order's
            Console.Write(Environment.NewLine);
            Console.WriteLine("The flight itinerary is as follows: ");
            Console.Write(Environment.NewLine);

            foreach (var order in orderList)
            {
                if (order.FlightNumber == null)
                {
                    Console.WriteLine("order: {0}, flightNumber: not scheduled", order.OrderName);
                }
                else
                {
                    // enhancement: didn't use the Flight.ToString since its formatting is slightly different, could clean this up
                    var flight = flightsList.FirstOrDefault(f => f.FlightNumber == order.FlightNumber);
                    if (flight != null)
                        Console.WriteLine("order: {0}, flightNumber: {1}, departure: {2}, arrival: {3}, day: {4}", order.OrderName, flight.FlightNumber, flight.DepartureAirport, flight.ArrivalAirport, flight.DepartureDay);
                }
            }
        }
    }
}
