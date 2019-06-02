using BAMCIS.GeoJSON;
using Itinero;
using Itinero.LocalGeo;
using Itinero.Profiles;
using Newtonsoft.Json;
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
        private Itinero.LocalGeo.Coordinate startPos { get; set; } //the start location of this traffic participant
        private Itinero.LocalGeo.Coordinate endPos { get; set; } //the target location of this participant
        private RouteModel route { get; set; } //the route computed for this traffic participant
        private ConfigurationModel configuration { get; set; } //current simulation configuration

        public TrafficParticipant(int id, TimeSpan delay, Itinero.LocalGeo.Coordinate start, Itinero.LocalGeo.Coordinate end, ConfigurationModel config)
        {
            ID = id;
            requestDelay = delay;
            startPos = start;
            endPos = end;
            configuration = config;
            route = new RouteModel();
            Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " was created and will request a route in " + delay.ToString());
        }

        public string HttpRequestBuilder(string profile, double slat, double slon, double elat, double elon)
        {
            string endPoint = "api/values/GetRoute";
            return configuration.LiveTrafficServerUri + endPoint + "?profile=" +
                profile + "&startLat=" +
                slat + "&startLon=" +
                slon + "&endLat=" +
                elat + "&endLon=" +
                elon;
        }

        public string HttpRequestBuilder(float plon, float plat, float clon, float clat)
        {
            string endPoint = "api/values/UpdateLocation";
            return configuration.LiveTrafficServerUri + endPoint
                + "?previousEdgeLon=" + plon
                + "&previousEdgeLat=" + plat
                + "&currentEdgeLon=" + clon
                + "&currentEdgeLat=" + clat;
        }

        public Itinero.LocalGeo.Coordinate ComputeMiddle(int e)
        {
            var start = ((Newtonsoft.Json.Linq.JArray)route.features[e].geometry.coordinates[0]).Select(item => (float)item).ToArray();
            var end = ((Newtonsoft.Json.Linq.JArray)route.features[e].geometry.coordinates[1]).Select(item => (float)item).ToArray();
            float lon = (start[0] + end[0]) / 2;
            float lat =  (start[1] + end[1]) / 2;
            return new Itinero.LocalGeo.Coordinate(lat, lon);
        }

        public TimeSpan ComputeTime(int i)
        {
            TimeSpan first = new TimeSpan(0,0,0,0,(int)(float.Parse(route.features[0].properties.time)*1000));
            if (i == 0)
                return first;
            TimeSpan prev = new TimeSpan(0,0, 0, 0, (int)(float.Parse(route.features[i - 1].properties.time) * 1000));
            TimeSpan curr = new TimeSpan(0,0, 0, 0, (int)(float.Parse(route.features[i].properties.time) * 1000));
            return (TimeSpan)(curr - prev);
        }

        public async void RunTrafficParticipant()
        {
            var customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
            Thread.Sleep(requestDelay);//delay before route request
            string routeRequestURI = HttpRequestBuilder("car", startPos.Latitude, startPos.Longitude, endPos.Latitude, endPos.Longitude);
            HttpClient httpClient = new HttpClient();
            bool receivedRoute = false;
            while (!receivedRoute)//make sure the participant receives a route (this won't fail as the coordinates chosen are known to have a route between them)
            {
                using (var response = await httpClient.GetAsync(routeRequestURI))
                {
                    Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " is requesting a route: " + routeRequestURI);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    try
                    {
                        route = JsonConvert.DeserializeObject<RouteModel>(apiResponse);
                        Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " received a route (TrafficParticipant)"+ID+"route.txt");
                        receivedRoute = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " received the following answer:" + e.ToString());
                    }
                }
            }
            //interpret the received route

            var currentMiddle = ComputeMiddle(0);
            var previousMiddle = ComputeMiddle(0);
            var updateURI = HttpRequestBuilder(0, 0, currentMiddle.Latitude, currentMiddle.Longitude);
            using (var response = await httpClient.GetAsync(updateURI))
            {
                Console.WriteLine("TrafficParticipant" + ID + "Updated his location with result: " + response.StatusCode);
            }

            for (int i = 1; i < route.features.Count; i++)
            {
                Thread.Sleep(ComputeTime(i));
                previousMiddle = currentMiddle;
                currentMiddle = ComputeMiddle(i);
                updateURI = HttpRequestBuilder(previousMiddle.Longitude,
                                               previousMiddle.Latitude,
                                               currentMiddle.Longitude,
                                               currentMiddle.Latitude);

                using (var response = await httpClient.GetAsync(updateURI))
                {
                    Console.WriteLine("TrafficParticipant" + ID + "Updated his location with result: " + response.StatusCode);
                }
            }
            Thread.Sleep(ComputeTime(route.features.Count - 1));
            updateURI = HttpRequestBuilder(currentMiddle.Longitude, currentMiddle.Latitude, 0, 0);
            using (var response = await httpClient.GetAsync(updateURI))
            {
                Console.WriteLine("TrafficParticipant" + ID + " FINISHED");
            }
        }
    }
}
