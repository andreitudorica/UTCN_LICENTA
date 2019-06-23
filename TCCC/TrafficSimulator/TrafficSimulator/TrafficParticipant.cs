using BAMCIS.GeoJSON;
using Itinero;
using Itinero.LocalGeo;
using Itinero.Profiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        public int ID { get; set; }
        private TimeSpan requestDelay { get; set; }//the momment in simulation this traffic participant requests a route
        private Itinero.LocalGeo.Coordinate startPos { get; set; } //the start location of this traffic participant
        private Itinero.LocalGeo.Coordinate endPos { get; set; } //the target location of this participant
        private RouteModel proposedRoute { get; set; } //the route proposed  for this traffic participant
        private RouteModel route { get; set; } //the route the traffic participant actually chose
        private RouteModel newRoute { get; set; } //the route the traffic participant actually chose
        private ConfigurationModel configuration { get; set; } //current simulation configuration
        public string behaviour { get; set; }
        public TimeSpan RouteDuration { get; set; }
        public TimeSpan RouteRequestTotalWaitTime { get; set; }
        public TimeSpan UpdateRequestTotalWaitTime { get; set; }
        public int RouteRequests { get; set; }
        public int UpdateRequests { get; set; }
        public bool isDone { get; set; }
        public bool hasStarted { get; set; }
        public int doneSteps { get; set; }
        public int steps { get; set; }
        public bool hasFailed { get; set; }
        public string errorMessage { get; set; }
        public DateTime simulationJoinTime { get; set; }
        private HttpClient httpClient { get; set; }
        private string apiResponse { get; set; }
        public int reroutingCount { get; set; }
        public double averageSpeed { get; set; }
        public int segmentsTouched { get; set; }


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
            hasStarted = false;
            hasFailed = false;
            errorMessage = "";
            httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(1, 0, 0, 0);
#if DEBUG  
            Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " was created and will request a route in " + TimeSpan.FromTicks(delay.Ticks * CommonVariables.timeMultiplyer));
#endif
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

        public async Task getRoute(Itinero.LocalGeo.Coordinate startPos, Itinero.LocalGeo.Coordinate endPos, bool init)
        {
            bool receivedRoute = false;
            string routeRequestURI;

            if (behaviour == "greedy")
                routeRequestURI = HttpRequestBuilder("shortest", startPos.Latitude, startPos.Longitude, endPos.Latitude, endPos.Longitude);
            else
                routeRequestURI = HttpRequestBuilder("best", startPos.Latitude, startPos.Longitude, endPos.Latitude, endPos.Longitude);


            while (!receivedRoute)//make sure the participant receives a route (this won't fail as the coordinates chosen are known to have a route between them)
            {
                using (var response = await httpClient.GetAsync(routeRequestURI))
                {
#if DEBUG
                    if (init)
                        Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant " + ID + " is requesting a route. His behaviour is " + behaviour);
#endif
                    DateTime time = DateTime.Now;
                    apiResponse = await response.Content.ReadAsStringAsync();
                    RouteRequests++;
                    RouteRequestTotalWaitTime += (DateTime.Now - time);
                    try
                    {
                        newRoute = JsonConvert.DeserializeObject<RouteModel>(apiResponse);
                        if (init)
                        {
                            string path = CommonVariables.PathToResultsFolder + "Routes\\" + "TrafficParticipant" + ID ;
                            if (!Directory.Exists(path))
                                Directory.CreateDirectory(path);
#if DEBUG
                            Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant " + ID + " received a route (TrafficParticipant)" + ID + "route.txt");
#endif
                            System.IO.File.WriteAllText(path + "\\route.txt", apiResponse);
                        }
                        if (init)
                            route = newRoute;
                        RouteDuration = new TimeSpan(0, 0, 0, 0, (int)(float.Parse(route.features[route.features.Count - 1].properties.time) * 1000));
                        receivedRoute = true;
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " received the following answer:" + e.ToString());
#endif
                    }
                }
            }
        }

        public async void RunTrafficParticipant()
        {
            try
            {

                #region initialize traffic participant
                string updateURI;
                var customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
                Thread.Sleep(TimeSpan.FromTicks(requestDelay.Ticks * CommonVariables.timeMultiplyer));//delay before route request
                simulationJoinTime = DateTime.Now;
                hasStarted = true;

                #endregion
                #region request route initial route
                await getRoute(startPos, endPos, true);
                #endregion

                #region  interpret the received route
                //in order to get the ID of the edge we are trying to change (add a car) we need a snapping point as close to it as possible, so I chose the middle
                Itinero.LocalGeo.Coordinate currentMiddle = ComputeMiddle(0, 0);//get the first edge of the path
                Itinero.LocalGeo.Coordinate previousMiddle;

                #region FOLLOW ROUTE

                for (int feature = 0; feature < route.features.Count-2; feature++)
                {
                    doneSteps = feature;
                    steps = route.features.Count - 2;
                    try
                    {
                        averageSpeed += Double.Parse(route.features[feature].properties.maxspeed);
                    }
                    catch (Exception)
                    {
                        
                    }

                    TimeSpan computedTimeOfFeature = new TimeSpan(0, 0, 0, 0);
                    double computedDistanceOfFeature = 0;
                    for (int segment = 0; segment < route.features[feature].geometry.coordinates.Count - 1; segment++)
                    {
                        TimeSpan timeOfSegment = ComputeTimeOfSegment(feature, segment);
                        computedTimeOfFeature += timeOfSegment;
                        computedDistanceOfFeature += ComputeDistanceOfSegment(feature, segment);

                        previousMiddle = currentMiddle;
                        currentMiddle = ComputeMiddle(feature, segment);

                        if (feature == 0 && segment == 0)
                            updateURI = HttpRequestBuilder(0, 0, currentMiddle.Latitude, currentMiddle.Longitude);//let the live traffic server know that you started the route 
                        else if (feature == route.features.Count - 3 && segment == route.features[feature].geometry.coordinates.Count - 2)
                            updateURI = HttpRequestBuilder(currentMiddle.Latitude, currentMiddle.Longitude, 0, 0);
                        else
                            updateURI = HttpRequestBuilder(previousMiddle.Latitude, previousMiddle.Longitude, currentMiddle.Latitude, currentMiddle.Longitude);

                        DateTime time = DateTime.Now;
                        await httpClient.GetAsync(updateURI);
                        UpdateRequests++;
                        UpdateRequestTotalWaitTime += (DateTime.Now - time);
#if DEBUG           
                        Console.WriteLine("TP" + ID + " Updated his segment (" + ComputeStartOfSegment(feature, segment).ToString() + "->" + ComputeEndOfSegment(feature, segment).ToString() + "). Next update in " + TimeSpan.FromTicks(timeOfSegment.Ticks * CommonVariables.timeMultiplyer) + " seconds (" + feature + "/" + route.features.Count + ")");
#endif
                        try
                        {
                            averageSpeed += ComputeDistanceOfSegment(feature, segment) /timeOfSegment.TotalSeconds*3.6;
                            segmentsTouched++;
                            Thread.Sleep(TimeSpan.FromTicks(timeOfSegment.Ticks * CommonVariables.timeMultiplyer));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("thread sleep failed");
                            Console.WriteLine(e.Message);
                        }
                    }

                    if (feature < route.features.Count - 3)
                    {
                        int oldLength = route.features.Count ;
                        var pos = ComputeMiddle(feature, route.features[feature].geometry.coordinates.Count - 2);
                        try
                        {
                            await getRoute(new Itinero.LocalGeo.Coordinate(pos.Latitude, pos.Longitude), endPos, false);

                            if (newRoute.features != null)
                            {
                                newRoute.features.RemoveAt(0);
                                newRoute.features.ForEach(f =>
                                {
                                    f.properties.distance = (Double.Parse(f.properties.distance) + Double.Parse(route.features[feature].properties.distance)).ToString();
                                    f.properties.time = (Double.Parse(f.properties.time) + Double.Parse(route.features[feature].properties.time)).ToString();
                                });


                                route.features.RemoveRange(feature + 1, route.features.Count - feature - 1);
                                route.features.AddRange(newRoute.features);
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                        if (route.features.Count != oldLength)
                        {
#if DEBUG
                            Console.WriteLine($"TP{ID} Rerouted!!");
#endif
                            reroutingCount++;
                            System.IO.File.WriteAllText(CommonVariables.PathToResultsFolder + "Routes\\" + "TrafficParticipant" + ID + "\\reroute"+reroutingCount+".txt", apiResponse);
                        }
                    }

                }
                #endregion
#if DEBUG
                Console.WriteLine("TrafficParticipant" + ID + " FINISHED route after " + TimeSpan.FromTicks((DateTime.Now - simulationJoinTime).Ticks / CommonVariables.timeMultiplyer));
#endif
                RouteDuration = TimeSpan.FromTicks((DateTime.Now - simulationJoinTime).Ticks / CommonVariables.timeMultiplyer);
                averageSpeed /= segmentsTouched;
                isDone = true;
#endregion
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.Message);
#endif
                hasFailed = true;
                errorMessage = e.Message;
            }
            //update statistics
        }
    }
}
