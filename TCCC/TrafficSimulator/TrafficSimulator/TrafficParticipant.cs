using Itinero;
using Itinero.LocalGeo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrafficSimulator.Models;

namespace TrafficSimulator
{
    class TrafficParticipant
    {
        private int ID { get; set; }
        private TimeSpan requestDelay { get; set; }//the momment in simulation this traffic participant requests a route
        private Coordinate startPos { get; set; } //the start location of this traffic participant
        private Coordinate endPos { get; set; } //the target location of this participant
        private Route route { get; set; } //the route computed for this traffic participant
        private ConfigurationModel configuration { get; set; } //current simulation configuration
        public TrafficParticipant(int id, TimeSpan delay, Coordinate start, Coordinate end, ConfigurationModel config)
        {
            ID = id;
            requestDelay = delay;
            startPos = start;
            endPos = end;
            configuration = config;
            Console.WriteLine(DateTime.Now.ToString()+": TrafficParticipant" + ID + " was created and will request a route in "+delay.ToString());
        }

        public async void RunTrafficParticipant()
        {
            Thread.Sleep(requestDelay);//delay before route request
            string profile = "car";
            string endPoint = "api/values/GetRoute";
            string requestURI = configuration.LiveTrafficServerUri + endPoint + "?profile=" +
                profile + "&startX=" +
                startPos.Latitude + "&startY=" +
                startPos.Longitude + "&endX=" +
                endPos.Latitude + "&endY=" +
                endPos.Longitude;
            HttpClient httpClient = new HttpClient();
            Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " is requesting a route: " + requestURI);
            using (var response = await  httpClient.GetAsync(requestURI))
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " received the following answer:" + apiResponse);
            }
        }
    }
}
