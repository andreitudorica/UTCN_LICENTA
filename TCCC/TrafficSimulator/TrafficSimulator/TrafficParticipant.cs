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
        private RouteModel proposedRoute { get; set; } //the route proposed  for this traffic participant
        private RouteModel route { get; set; } //the route the traffic participant actually chose
        private ConfigurationModel configuration { get; set; } //current simulation configuration
        public string behaviour { get; set; }
        public TimeSpan RouteDuration { get; set; }
        public TimeSpan TrafficInflictedDelay { get; set; }
        public TimeSpan RoutingInflictedDelay { get; set; }
        public bool isDone { get; set; }




        public TrafficParticipant(int id, TimeSpan delay, Itinero.LocalGeo.Coordinate start, Itinero.LocalGeo.Coordinate end, ConfigurationModel config, string b)
        {
            ID = id;
            requestDelay = delay;
            startPos = start;
            endPos = end;
            configuration = config;
            behaviour = b;
            route = new RouteModel();
            isDone = false;
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

        public Itinero.LocalGeo.Coordinate ComputeStartOfSegment(int f, int s)//f = feature , s = segment
        {
            try
            {
                var start = ((Newtonsoft.Json.Linq.JArray)route.features[f].geometry.coordinates[s]).Select(item => (float)item).ToArray();
                return new Itinero.LocalGeo.Coordinate(start[0], start[1]);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public Itinero.LocalGeo.Coordinate ComputeEndOfSegment(int f, int s)//f = feature , s = segment
        {
            try
            {
                var end = ((Newtonsoft.Json.Linq.JArray)route.features[f].geometry.coordinates[s + 1]).Select(item => (float)item).ToArray();
                return new Itinero.LocalGeo.Coordinate(end[0], end[1]);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public Itinero.LocalGeo.Coordinate ComputeMiddle(int f, int s)//f = feature , s = segment
        {
            try
            {
                var start = ComputeStartOfSegment(f, s);
                var end = ComputeEndOfSegment(f, s);
                float lon = (start.Longitude + end.Longitude) / 2;
                float lat = (start.Latitude + end.Latitude) / 2;
                return new Itinero.LocalGeo.Coordinate(lon, lat);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public double ComputeDistanceOfSegment(int f, int s)
        {

            //var customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
            //RouterDb routerDb = Simulation.routerDb;
            //var router = new Router(Simulation.routerDb);
            //var middle = ComputeMiddle(f, s);
            //var resolvedPrevious = router.Resolve(customCar.Fastest(), middle);
            //uint edgeId = resolvedPrevious.EdgeId;
            //var edge = routerDb.Network.GetEdge(edgeId);
            //var edgeData = edge.Data.Distance;


            var start = ComputeStartOfSegment(f, s);
            var end = ComputeEndOfSegment(f, s);
            var result = Itinero.LocalGeo.Coordinate.DistanceEstimateInMeter(start, end);
            return result;
        }

        public double ComputeDistanceOfFeature(int f)
        {
            if (f == 0)

                return Double.Parse(route.features[f].properties.distance);
            return Double.Parse(route.features[f].properties.distance) - Double.Parse(route.features[f - 1].properties.distance);
        }

        public TimeSpan ComputeTimeOfFeature(int f)
        {
            TimeSpan first = new TimeSpan(0, 0, 0, 0, (int)(float.Parse(route.features[0].properties.time) * 1000));
            if (f == 0)
                return first;
            TimeSpan prev = new TimeSpan(0, 0, 0, 0, (int)(float.Parse(route.features[f - 1].properties.time) * 1000));
            TimeSpan curr = new TimeSpan(0, 0, 0, 0, (int)(float.Parse(route.features[f].properties.time) * 1000));
            return (TimeSpan)(curr - prev);
        }

        public TimeSpan ComputeTimeOfSegment(int f, int s)
        {
            double featureDistance = ComputeDistanceOfFeature(f);
            double segmentDistance = ComputeDistanceOfSegment(f, s);
            TimeSpan timeOfFeature = ComputeTimeOfFeature(f);
            return new TimeSpan(0, 0, 0, 0, (int)(timeOfFeature.TotalMilliseconds * (segmentDistance / featureDistance)));
        }
        public async void RunTrafficParticipant()
        {
            #region initialize traffic participant
            var customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
            Thread.Sleep(requestDelay);//delay before route request
            string routeRequestURI;
            #endregion
            #region decide behabiour and request route
            if (behaviour == "greedy")
                routeRequestURI = HttpRequestBuilder("shortest", startPos.Latitude, startPos.Longitude, endPos.Latitude, endPos.Longitude);
            else
                routeRequestURI = HttpRequestBuilder("best", startPos.Latitude, startPos.Longitude, endPos.Latitude, endPos.Longitude);
            HttpClient httpClient = new HttpClient();
            bool receivedRoute = false;
            string apiResponse;
            while (!receivedRoute)//make sure the participant receives a route (this won't fail as the coordinates chosen are known to have a route between them)
            {
                using (var response = await httpClient.GetAsync(routeRequestURI))
                {
                    Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant " + ID + " is requesting a route. His behaviour is " + behaviour);
                    apiResponse = await response.Content.ReadAsStringAsync();
                    try
                    {
                        route = JsonConvert.DeserializeObject<RouteModel>(apiResponse);
                        Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant " + ID + " received a route (TrafficParticipant)" + ID + "route.txt");

                        System.IO.File.WriteAllText("TrafficParticipant" + ID + "route.txt", apiResponse);

                        RouteDuration = new TimeSpan(0, 0, 0, 0, (int)(float.Parse(route.features[route.features.Count - 1].properties.time) * 1000));
                        receivedRoute = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " received the following answer:" + e.ToString());
                    }
                }
            }
            #endregion

            #region  interpret the received route
            //in order to get the ID of the edge we are trying to change (add a car) we need a snapping point as close to it as possible, so I chose the middle
            var currentMiddle = ComputeMiddle(0, 0);//get the first edge of the path
            var previousMiddle = currentMiddle;
            #region START ROUTE

            var updateURI = HttpRequestBuilder(0, 0, currentMiddle.Latitude, currentMiddle.Longitude);//let the live traffic server know that you started the route 
            using (var response = await httpClient.GetAsync(updateURI))
            {
                Console.WriteLine("TP" + ID + " Started his route with result: " + response.ReasonPhrase + " First update in " + ComputeTimeOfSegment(0, 0) + " seconds");
            }
            Thread.Sleep(ComputeTimeOfSegment(0, 0));

            #endregion
            #region FOLLOW ROUTE
            for (int i = 1; i < route.features.Count - 2; i++)
            {
                TimeSpan computedTimeOfFeature = new TimeSpan(0, 0, 0, 0);
                double computedDistanceOfFeature = 0;
                for (int j = 0; j < route.features[i].geometry.coordinates.Count - 1; j++)
                {
                    TimeSpan timeOfSegment = ComputeTimeOfSegment(i, j);
                    computedTimeOfFeature += timeOfSegment;
                    computedDistanceOfFeature += ComputeDistanceOfSegment(i, j);
                    previousMiddle = currentMiddle;
                    currentMiddle = ComputeMiddle(i, j);
                    updateURI = HttpRequestBuilder(previousMiddle.Latitude, previousMiddle.Longitude, currentMiddle.Latitude, currentMiddle.Longitude);

                    using (var response = await httpClient.GetAsync(updateURI))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            Console.WriteLine("TP" + ID + " Updated his segment (" + ComputeStartOfSegment(i, j).ToString() + "->" + ComputeEndOfSegment(i, j).ToString() + ") with result: " + response.ReasonPhrase + " Next update in " + timeOfSegment + " seconds (" + i + "/" + route.features.Count + ")");
                        else
                            Console.WriteLine("TP" + ID + " Updated his location with result: " + response.ReasonPhrase + " Message " + response.Content.ToString());
                    }
                    Thread.Sleep(timeOfSegment);
                }
                //Console.WriteLine("Computed Time of feature: " + computedTimeOfFeature + "/" + computedDistanceOfFeature + "\nActual Time of feature:   " + ComputeTimeOfFeature(i) + "/" + ComputeDistanceOfFeature(i));
            }
            #endregion
            #region FINISH ROUTE
            updateURI = HttpRequestBuilder(currentMiddle.Latitude, currentMiddle.Longitude, 0, 0);
            using (var response = await httpClient.GetAsync(updateURI))
            {
                Console.WriteLine("TrafficParticipant" + ID + " FINISHED route after " + (new TimeSpan(0, 0, 0, 0, (int)(float.Parse(route.features[route.features.Count - 1].properties.time) * 1000))));
            }
            isDone = true;
            #endregion 
            #endregion 
            //update statistics
        }
    }
}
