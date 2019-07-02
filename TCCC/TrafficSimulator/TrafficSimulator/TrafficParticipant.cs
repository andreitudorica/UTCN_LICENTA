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
    public class TrafficParticipant
    {
        public int ID { get; set; }
        public DateTime simulationJoinTime { get; set; }
        private TimeSpan requestDelay { get; set; }//the momment in simulation this traffic participant requests a route
        private Itinero.LocalGeo.Coordinate startPos { get; set; } //the start location of this traffic participant
        private Itinero.LocalGeo.Coordinate endPos { get; set; } //the target location of this participant
        private RouteModel proposedRoute { get; set; } //the route proposed  for this traffic participant
        public RouteModel route { get; set; } //the route the traffic participant actually chose
        public RouteModel newRoute { get; set; } //the route the traffic participant actually chose
        private ConfigurationModel simulationConfig { get; set; } //current simulation configuration
        public string behaviour { get; set; }
        public TimeSpan routeDuration { get; set; }
        public TimeSpan routeRequestTotalWaitTime { get; set; }
        public TimeSpan updateRequestTotalWaitTime { get; set; }
        public TimeSpan timeInSimulation { get; set; }
        public int RouteRequests { get; set; }
        public int UpdateRequests { get; set; }
        public int doneSteps { get; set; }
        public int steps { get; set; }
        public bool updated { get; set; }
        public string status { get; set; }
        public string errorMessage { get; set; }
        public string routeRequestSerializerResponse { get; set; }
        public int reroutingCount { get; set; }
        public double averageSpeed { get; set; }
        public int segmentsTouched { get; set; }
        private DateTime lastRerouteRequest { get; set; }

        public TrafficParticipant(int id, TimeSpan delay, Itinero.LocalGeo.Coordinate start, Itinero.LocalGeo.Coordinate end, ConfigurationModel config, string b)
        {
            ID = id;
            requestDelay = delay;
            startPos = start;
            endPos = end;
            simulationConfig = config;
            behaviour = b;
            route = new RouteModel();
            status = "created";
            errorMessage = "";
#if DEBUG  
            Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " was created and will request a route in " + TimeSpan.FromTicks(delay.Ticks * simulationConfig.timeMultiplyer));
#endif
        }

        public string HttpRequestBuilder(string profile, double slat, double slon, double elat, double elon)
        {
            string endPoint = "api/traffic/GetRoute";
            return simulationConfig.LiveTrafficServerUri + endPoint + "?profile=" +
                profile + "&startLat=" +
                slat + "&startLon=" +
                slon + "&endLat=" +
                elat + "&endLon=" +
                elon;
        }

        public string HttpRequestBuilder(float plon, float plat, float clon, float clat)
        {
            string endPoint = "api/traffic/UpdateLocation";
            return simulationConfig.LiveTrafficServerUri + endPoint
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
            string routeRequestURI;

            if (behaviour == "greedy")
                routeRequestURI = HttpRequestBuilder("shortest", startPos.Latitude, startPos.Longitude, endPos.Latitude, endPos.Longitude);
            else
                routeRequestURI = HttpRequestBuilder("best", startPos.Latitude, startPos.Longitude, endPos.Latitude, endPos.Longitude);


            DateTime time = DateTime.Now;
            status = "waiting for route";
            Simulation.requestSerializer.AddRouteRequest(new RequestModel() { trafficParticipantID = ID, request = routeRequestURI });
            routeRequestSerializerResponse = null;
#if DEBUG
            if (init)
                 Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant " + ID + " is requesting a route.");
#endif
            await Task.Run(()=>{ while (routeRequestSerializerResponse == null) Thread.Sleep(10); });
            status = "other";
            RouteRequests++;
            routeRequestTotalWaitTime += (DateTime.Now - time);

            try
            {
               
                if (init)
                {
                    string path = CommonVariables.PathToResultsFolder + "Routes\\" + "TrafficParticipant" + ID;
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
#if DEBUG
                            Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant " + ID + " received a route (TrafficParticipant)" + ID + "route.txt");
#endif
                    System.IO.File.WriteAllText(path + "\\route.txt", routeRequestSerializerResponse);
                }
                if (init)
                    route = newRoute;
            }
            catch (Exception e)
            {
#if DEBUG
                        Console.WriteLine(DateTime.Now.ToString() + ": TrafficParticipant" + ID + " received the following answer:" + e.ToString());
#endif
                System.IO.File.AppendAllText(Simulation.ErrorLogsFile,DateTime.Now.ToString() + ": TrafficParticipant" + ID + " received the following answer:" + e.ToString()+"\n");
            }
        }

        public async void RunTrafficParticipant()
        {
            try
            {

                #region initialize traffic participant
                string updateURI;
                var customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
                status = "waiting to join";
                Thread.Sleep(TimeSpan.FromTicks(requestDelay.Ticks * simulationConfig.timeMultiplyer));//delay before route request
                //Thread.Sleep(new TimeSpan(0,0,0,ID));
                simulationJoinTime = DateTime.Now;
                status = "in traffic";

                #endregion
                #region request initial route
                await getRoute(startPos, endPos, true);
                lastRerouteRequest = DateTime.Now;
                #endregion

                #region  interpret the received route
                //in order to get the ID of the edge we are trying to change (add a car) we need a snapping point as close to it as possible, so I chose the middle
                Itinero.LocalGeo.Coordinate currentMiddle = ComputeMiddle(0, 0);//get the first edge of the path
                Itinero.LocalGeo.Coordinate previousMiddle;

                #region FOLLOW ROUTE

                for (int feature = 0; feature < route.features.Count - 2; feature++)
                {
                    doneSteps = feature;
                    steps = route.features.Count - 2;
                    //averageSpeed += Double.Parse(route.features[feature].properties.maxspeed);

                    previousMiddle = currentMiddle;
                    currentMiddle = ComputeMiddle(feature, 0);

                    if (feature == 0)
                        updateURI = HttpRequestBuilder(0, 0, currentMiddle.Latitude, currentMiddle.Longitude);//let the live traffic server know that you started the route 
                    else if (feature == route.features.Count - 3)
                        updateURI = HttpRequestBuilder(currentMiddle.Latitude, currentMiddle.Longitude, 0, 0);
                    else
                        updateURI = HttpRequestBuilder(previousMiddle.Latitude, previousMiddle.Longitude, currentMiddle.Latitude, currentMiddle.Longitude);

                    DateTime time = DateTime.Now;

                    updated = false;
                    status = "waiting for update";
                    Simulation.requestSerializer.AddUpdateRequest(new RequestModel() { trafficParticipantID=ID,request=updateURI});
                    while(!updated)
                        Thread.Sleep(10);
                    UpdateRequests++;
                    updateRequestTotalWaitTime += (DateTime.Now - time);
#if DEBUG
                        Console.WriteLine("TP" + ID + " Updated. Next update in " + TimeSpan.FromTicks(ComputeTimeOfFeature(feature).Ticks * simulationConfig.timeMultiplyer)
                                                    + " seconds (" + feature + "/" + (route.features.Count-3) + ")" + 
                                                    " distance from start: "+ Double.Parse(route.features[feature].properties.distance)+ " time from start: "+ new TimeSpan(0, 0, 0, 0, (int)(float.Parse(route.features[feature].properties.time) * 1000)));
#endif
                    try
                    {
                        averageSpeed += ComputeDistanceOfFeature(feature) / ComputeTimeOfFeature(feature).TotalSeconds * 3.6;
                        segmentsTouched++;
                        status = "in traffic";
                        Thread.Sleep(TimeSpan.FromTicks(ComputeTimeOfFeature(feature).Ticks * simulationConfig.timeMultiplyer));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("thread sleep failed");
                        Console.WriteLine(e.Message);
                        System.IO.File.AppendAllText(Simulation.ErrorLogsFile, e.Message + "\n");
                    }

                    #region follow route segment by segments
                    //                for (int feature = 0; feature < route.features.Count-2; feature++)
                    //                {
                    //                    doneSteps = feature;
                    //                    steps = route.features.Count - 2;
                    //                        averageSpeed += Double.Parse(route.features[feature].properties.maxspeed);

                    //                    for (int segment = 0; segment < route.features[feature].geometry.coordinates.Count - 1; segment++)
                    //                    {
                    //                        TimeSpan timeOfSegment = ComputeTimeOfSegment(feature, segment);

                    //                        previousMiddle = currentMiddle;
                    //                        currentMiddle = ComputeMiddle(feature, segment);

                    //                        if (feature == 0 && segment == 0)
                    //                            updateURI = HttpRequestBuilder(0, 0, currentMiddle.Latitude, currentMiddle.Longitude);//let the live traffic server know that you started the route 
                    //                        else if (feature == route.features.Count - 3 && segment == route.features[feature].geometry.coordinates.Count - 2)
                    //                            updateURI = HttpRequestBuilder(currentMiddle.Latitude, currentMiddle.Longitude, 0, 0);
                    //                        else
                    //                            updateURI = HttpRequestBuilder(previousMiddle.Latitude, previousMiddle.Longitude, currentMiddle.Latitude, currentMiddle.Longitude);

                    //                        DateTime time = DateTime.Now;
                    //                        await httpClient.GetAsync(updateURI);
                    //                        UpdateRequests++;
                    //                        UpdateRequestTotalWaitTime += (DateTime.Now - time);
                    //#if DEBUG           
                    //                        Console.WriteLine("TP" + ID + " Updated his segment (" + ComputeStartOfSegment(feature, segment).ToString() + "->" + ComputeEndOfSegment(feature, segment).ToString() + "). Next update in " + TimeSpan.FromTicks(timeOfSegment.Ticks * CommonVariables.timeMultiplyer) + " seconds (" + feature + "/" + route.features.Count + ")");
                    //#endif
                    //                        try
                    //                        {
                    //                            averageSpeed += ComputeDistanceOfSegment(feature, segment) /timeOfSegment.TotalSeconds*3.6;
                    //                            segmentsTouched++;
                    //                            totalSleepTime += TimeSpan.FromTicks(timeOfSegment.Ticks * CommonVariables.timeMultiplyer);
                    //                            Thread.Sleep(TimeSpan.FromTicks(timeOfSegment.Ticks * CommonVariables.timeMultiplyer));
                    //                        }
                    //                        catch (Exception e)
                    //                        {
                    //                            Console.WriteLine("thread sleep failed");
                    //                            Console.WriteLine(e.Message);
                    //                        }
                    //                    }
                    #endregion
                    if (feature > 0 && feature < route.features.Count - 3 && (DateTime.Now-lastRerouteRequest).TotalMilliseconds>simulationConfig.delayBetweenRouteRequest)
                    {
                        int oldLength = route.features.Count;
                        int newLength = 0;
                        var pos = ComputeMiddle(feature, route.features[feature].geometry.coordinates.Count - 2);
                        try
                        {
                            await getRoute(new Itinero.LocalGeo.Coordinate(pos.Latitude, pos.Longitude), endPos, false);

                            if (newRoute.features != null)
                            {
                                lastRerouteRequest = DateTime.Now;
                                for (int fe = 1; fe < newRoute.features.Count; fe++)
                                {
                                    var f = newRoute.features[fe];
                                    f.properties.distance = (Double.Parse(f.properties.distance) - Double.Parse(newRoute.features[0].properties.distance) + Double.Parse(route.features[feature].properties.distance)).ToString();
                                    f.properties.time = (Double.Parse(f.properties.time) - Double.Parse(newRoute.features[0].properties.time) + Double.Parse(route.features[feature].properties.time)).ToString();
                                }
                                newRoute.features.RemoveAt(0);

                                newLength = newRoute.features.Count + 1 + feature;
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            System.IO.File.AppendAllText(Simulation.ErrorLogsFile, e.Message + "\n");
                        }
                        if (Math.Abs(newLength - oldLength) >= 2)
                        {

                            route.features.RemoveRange(feature + 1, route.features.Count - feature - 1);
                            route.features.AddRange(newRoute.features);
#if DEBUG
                            Console.WriteLine($"TP{ID} Rerouted!!");
#endif
                            reroutingCount++;
                            System.IO.File.WriteAllText(CommonVariables.PathToResultsFolder + "Routes\\" + "TrafficParticipant" + ID + "\\reroute" + reroutingCount + ".txt", routeRequestSerializerResponse);
                        }
                    }

                }
                #endregion
#if DEBUG
                Console.WriteLine("TrafficParticipant" + ID + " FINISHED route after " + TimeSpan.FromTicks((DateTime.Now - simulationJoinTime).Ticks / simulationConfig.timeMultiplyer));
#endif
                routeDuration = TimeSpan.FromTicks((new TimeSpan(0, 0, 0, 0, (int)(float.Parse(route.features[route.features.Count - 1].properties.time) * 1000))).Ticks);
                timeInSimulation = TimeSpan.FromTicks((DateTime.Now - simulationJoinTime).Ticks);
                averageSpeed /= segmentsTouched;
                status = "done";

                System.IO.File.WriteAllText(CommonVariables.PathToResultsFolder + "Routes\\" + "TrafficParticipant" + ID + "statistics.txt",
                                            "time in simulation: " + timeInSimulation +
                                            "\n behaviour: " + behaviour +
                                            "\n routeDuration: " + routeDuration +
                                            "\n averageSpeed: " + averageSpeed +
                                            "\n route request wait time " + routeRequestTotalWaitTime + " / " + RouteRequests + " requests\n" +
                                            "\n update wait time " + updateRequestTotalWaitTime + " / " + UpdateRequests + " uppdates/n");
                #endregion
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.Message);
#endif
                status = "failed";
                errorMessage = e.Message;
                System.IO.File.AppendAllText(Simulation.ErrorLogsFile, e.Message + "\n");
            }
            //update statistics
        }
    }
}
