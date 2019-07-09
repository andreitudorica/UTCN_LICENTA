using Itinero;
using Itinero.IO.Osm;
using Itinero.Profiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrafficSimulator.Models;

namespace TrafficSimulator
{
    internal class Simulation
    {
        public static RequestSerializer requestSerializer;
        private ConfigurationModel configuration;
        public static RouterDb routerDb;
        public List<TrafficParticipant> trafficParticipants;
        private List<StatisticsEntry> Statistics;
        public static int threshold = 0;
        public DateTime simulationStart;
        public string path;
        public StringBuilder finalStatistics;
        private Thread routeSerializerThread, updateSerializerThread;
        public static string ErrorLogsFile;

        public Simulation(ConfigurationModel config)
        {
            var customCar = DynamicVehicle.Load(File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
            routerDb = new RouterDb();
            using (var stream = System.IO.File.OpenRead(CommonVariables.PathToCommonFolder + CommonVariables.PbfMapFileName))
            {
                routerDb.LoadOsmData(stream, customCar);
            }
            configuration = config;
            trafficParticipants = new List<TrafficParticipant>();
            Statistics = new List<StatisticsEntry>();
            simulationStart = DateTime.Now;
            finalStatistics = new StringBuilder();
            requestSerializer = new RequestSerializer(trafficParticipants);
            routeSerializerThread = new Thread(new ThreadStart(requestSerializer.ProcessRouteRequests));
            routeSerializerThread.Name = "route requests serializer";
            routeSerializerThread.Start();
            updateSerializerThread = new Thread(new ThreadStart(requestSerializer.ProcessUpdateRequests));
            updateSerializerThread.Name = "updates serializer";
            updateSerializerThread.Start();
        }
        
        public async Task<string> TestBasicFlow()
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    using (var httpClient = new HttpClient())
                    {
                        DateTime routeRequest = DateTime.Now;
                        using (var response = await httpClient.GetAsync("https://localhost:44351/api/values"))
                        {
                            string apiResponse = await response.Content.ReadAsStringAsync();
                            Console.WriteLine(apiResponse + "Total time:" + (DateTime.Now - routeRequest).ToString(@"dd\.hh\:mm\:ss\.ff"));
                        }
                    }
                }
                return "done";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public void ReleaseConsole(int choiceThreshold)
        {
            try
            {
                Console.CursorVisible = false;
                List<TrafficParticipant> failed = new List<TrafficParticipant>();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Simulation time: " + (DateTime.Now - simulationStart).ToString(@"dd\.hh\:mm\:ss"));
                sb.AppendLine("Traffic Participants: " + configuration.NumberOfCars + " Time multiplyer: " + configuration.timeMultiplyer);
                sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
                if (Statistics.Count > 0)
                {
                    sb.AppendLine("Current Statistics: ");
                    for (int i = 0; i < Statistics.Count; i++)
                    {
                        sb.AppendLine("Threshold: " + Statistics[i].threshold
                            + "% Total Time: " + Statistics[i].totalTime
                            + "\n Average Time: " + Statistics[i].averageTime
                            + "\n Average Time without waitings: " + (Statistics[i].averageTime - Statistics[i].averageUpdateWaitTime - Statistics[i].averageRouteRequestWaitTime)
                            + "\n Average features touched " + Statistics[i].averageTouchedFeatures
                            + "\n Average speed " + Statistics[i].averageSpeed
                            + "\n Average update wait Time: " + Statistics[i].averageUpdateWaitTime
                            + "\n Average route request wait Time: " + Statistics[i].averageRouteRequestWaitTime
                            + "\n____________________________________________");
                    }
                    sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
                }
                sb.AppendLine("Waiting route requests: " + requestSerializer.routeRequests.Count + " Waiting update requests: " + requestSerializer.updateRequests.Count);
                sb.AppendLine("Number of route requests: " + requestSerializer.numberOfRouteRequests + " Average wait: " + requestSerializer.averageRouteRequestWaitTime + "Max wait: " + requestSerializer.maxRouteRequestWaitTime + " failed: " + requestSerializer.numberOfFailedRouteRequests);
                sb.AppendLine("Number of update requests: " + requestSerializer.numberOfUpdateRequests + " Average wait: " + requestSerializer.averageUpdateRequestWaitTime + " failed: " + requestSerializer.numberOfFailedUpdateRequests);
                sb.AppendLine("The threshold was set for " + choiceThreshold + "%.");
                sb.Append("\nTraffic participants waiting to join: ");
                for (int i = 0; i < trafficParticipants.Count; i++)
                {
                    if (trafficParticipants[i].status == "waiting to join")
                    {
                        sb.Append(i + " ");
                    }

                    if (trafficParticipants[i].status == "failed")
                    {
                        failed.Add(trafficParticipants[i]);
                    }
                }
                sb.Append("\nTraffic participants in traffic: ");
                for (int i = 0; i < trafficParticipants.Count; i++)
                {
                    if (trafficParticipants[i].status == "in traffic")
                    {
                        sb.Append(i + " ");
                    }
                }

                sb.Append("\nTraffic participants waiting for route: ");
                for (int i = 0; i < trafficParticipants.Count; i++)
                {
                    if (trafficParticipants[i].status == "waiting for route")
                    {
                        sb.Append(i + " ");
                    }
                }

                sb.Append("\nTraffic participants waiting for update: ");
                for (int i = 0; i < trafficParticipants.Count; i++)
                {
                    if (trafficParticipants[i].status == "waiting for update")
                    {
                        sb.Append(i + " ");
                    }
                }

                sb.Append("\nTraffic participants processing: ");
                for (int i = 0; i < trafficParticipants.Count; i++)
                {
                    if (trafficParticipants[i].status == "other")
                    {
                        sb.Append(i + " ");
                    }
                }

                sb.Append("\nTraffic participants done: ");
                for (int i = 0; i < trafficParticipants.Count; i++)
                {
                    if (trafficParticipants[i].status == "done")
                    {
                        sb.Append(i + " ");
                    }
                }

                if (failed.Count > 0)
                {
                    sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
                    sb.AppendLine("Failed traffic participants");
                    foreach (var f in failed)
                    {
                        sb.AppendLine("Traffic Participant " + f.ID + " failed with message: " + f.errorMessage);
                    }
                }
                Console.Clear();
                Console.Write(sb.ToString());
            }
            catch (Exception e)
            {

                System.IO.File.AppendAllText(Simulation.ErrorLogsFile, e.Message + "\n");
            }
        }

        public void endSimulation()
        {

            Console.Clear();
            Console.WriteLine("SIMULATION OVER");
            Console.WriteLine(finalStatistics.ToString());

            System.IO.File.WriteAllText(path, "Simulation Length" + (DateTime.Now - simulationStart) + "\n" + configuration.printVersion() + "\n" + finalStatistics.ToString());
            routeSerializerThread.Abort();
            updateSerializerThread.Abort();
        }

        public void ManageStatistics(int choiceThreshold)
        {
            try
            {
                Statistics.Add(new StatisticsEntry());
                var currentStats = Statistics.Count - 1;
                foreach (var trafficp in trafficParticipants)
                {
                    if (trafficp.status == "done")
                    {
                        Statistics[currentStats].threshold = choiceThreshold;
                        Statistics[currentStats].totalTime += trafficp.routeDuration;
                        Statistics[currentStats].averageTimeWithoutWaiting += trafficp.timeInSimulation - trafficp.routeRequestTotalWaitTime - trafficp.updateRequestTotalWaitTime;
                        Statistics[currentStats].averageTouchedFeatures += trafficp.steps;
                        Statistics[currentStats].averageTouchedSegments += trafficp.segmentsTouched;
                        Statistics[currentStats].averageSpeed += trafficp.averageSpeed;
                        Statistics[currentStats].totalRouteRequestWaitTime += trafficp.routeRequestTotalWaitTime;
                        Statistics[currentStats].totalUpdateWaitTime += trafficp.updateRequestTotalWaitTime;
                        Statistics[currentStats].averageRouteRequestWaitTime += TimeSpan.FromTicks(trafficp.routeRequestTotalWaitTime.Ticks / trafficp.RouteRequests);
                        Statistics[currentStats].averageUpdateWaitTime += TimeSpan.FromTicks(trafficp.updateRequestTotalWaitTime.Ticks / trafficp.UpdateRequests);
                    }
                }
                Statistics[currentStats].averageTime = TimeSpan.FromTicks(Statistics[currentStats].totalTime.Ticks / configuration.NumberOfCars);
                Statistics[currentStats].averageTouchedFeatures /= configuration.NumberOfCars;
                Statistics[currentStats].averageTouchedSegments /= configuration.NumberOfCars;
                Statistics[currentStats].averageRouteRequestWaitTime = TimeSpan.FromTicks(Statistics[currentStats].averageRouteRequestWaitTime.Ticks / configuration.NumberOfCars);
                Statistics[currentStats].averageUpdateWaitTime = TimeSpan.FromTicks(Statistics[currentStats].averageUpdateWaitTime.Ticks / configuration.NumberOfCars);
                Statistics[currentStats].averageSpeed /= configuration.NumberOfCars;


                finalStatistics.AppendLine("Threshold " + choiceThreshold
                            + "%\n Total Time: " + Statistics[currentStats].totalTime
                            + "\n Average Route Time: " + Statistics[currentStats].averageTime
                            + "\n Average Time in simulation without waitings: " + Statistics[currentStats].averageTimeWithoutWaiting
                            + "\n Average features touched " + Statistics[currentStats].averageTouchedFeatures
                            + " features\n Average segments touched " + Statistics[currentStats].averageTouchedSegments
                            + "segments\n Average speed " + Statistics[currentStats].averageSpeed
                            + "km/h\n Average update wait Time " + Statistics[currentStats].averageUpdateWaitTime
                            + "\n Average route request wait Time " + Statistics[currentStats].averageRouteRequestWaitTime
                            );

                //compute statistics
                trafficParticipants.Clear();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                System.IO.File.AppendAllText(Simulation.ErrorLogsFile, e.Message + "\n");
                throw;
            }
        }

        public async Task<string> RunOneRouteMultipleTimes()
        {
            var startPos = new Itinero.LocalGeo.Coordinate(46.7681917f, 23.6310351f); string startName = "home";//hardcoded start and finish locations
            //var endPos = new Itinero.LocalGeo.Coordinate(46.7682747f, 23.6221141f);string endName = "mercur";//mercur
            //var endPos = new Itinero.LocalGeo.Coordinate(46.7687294f, 23.6190373f);string endName = "interservisan";//interservisan
            var endPos = new Itinero.LocalGeo.Coordinate(46.767546f, 23.5999328f); string endName = "cipariu";//cipariu 
            //var endPos = new Itinero.LocalGeo.Coordinate(46.752623f, 23.577261f);string endName = "Golden tulip"; //Golden tulip
            try
            {
                path = CommonVariables.PathToResultsFolder + configuration.NumberOfCars + " participants - Th " + configuration.startTH + " to " + configuration.endTH + " - " + startName + " to " + endName + " - TS " + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".txt";
                finalStatistics.AppendLine("Simulation Date: " + DateTime.Now);
                ErrorLogsFile = CommonVariables.PathToResultsFolder + "ErrorLogs\\Error Log " + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".txt";
                System.IO.File.WriteAllText(ErrorLogsFile, "Simulation Time: " + DateTime.Now + "\n" + configuration.printVersion() + "\n");

                for (int choiceThreshold = configuration.startTH; choiceThreshold <= configuration.endTH; choiceThreshold += 10)// variate threshold if people chosing the sugested route over people chosng the fastest route
                {

                    Console.WriteLine("The threshold was set for " + choiceThreshold + "%.");

                    HttpClient httpClient = new HttpClient();
                    using (var response = await httpClient.GetAsync(configuration.LiveTrafficServerUri + "api/values/InitializeMaps"))
                    {
                        Console.WriteLine("The maps have been Initialized in the Live Traffic Server.");
                    }


                    Thread thrd;
                    //run first car (separately so I can join last thread at the end of this function)
                    Random rnd = new Random();
                    var choice = (rnd.Next(99) < choiceThreshold) ? "proposed" : "greedy";
                    TrafficParticipant tp = new TrafficParticipant(0, (new TimeSpan(0, 0, 0)), startPos, endPos, configuration, choice);
                    trafficParticipants.Add(tp);
                    thrd = new Thread(new ThreadStart(tp.RunTrafficParticipant));
                    thrd.Name = "TP0";
                    thrd.Start();

                    for (int i = 1; i < configuration.NumberOfCars; i++)//generate the cars
                    {
                        choice = (rnd.Next(99) < choiceThreshold) ? "proposed" : "greedy";
                        tp = new TrafficParticipant(i, (new TimeSpan(0, 0, +configuration.RequestDelay.Seconds * i)), startPos, endPos, configuration, choice);
                        trafficParticipants.Add(tp);
                        thrd = new Thread(new ThreadStart(tp.RunTrafficParticipant));//run each car on an independent thread
                        thrd.Name = "TP" + i;
                        thrd.Start();
                    }

                    #region CONSOLE
                    while (true)
                    {
                        bool allDone = true;
                        trafficParticipants.ForEach(t => { if (!(t.status == "done" || t.status == "failed")) { allDone = false; } });
                        if (allDone)
                        {
                            break;
                        }
                        //#if ! DEBUG             
                        ReleaseConsole(choiceThreshold);
                        Thread.Sleep(50);
                        //#endif                  
                    }
                    #endregion
                    ManageStatistics(choiceThreshold);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                System.IO.File.AppendAllText(Simulation.ErrorLogsFile, e.Message + "\n");
                return null;
            }
            endSimulation();
            return "done";
        }

        public async Task<string> RunMultipleRoutes()
        {
            #region locations
            List<Itinero.LocalGeo.Coordinate> starts = new List<Itinero.LocalGeo.Coordinate>();
            List<Itinero.LocalGeo.Coordinate> ends = new List<Itinero.LocalGeo.Coordinate>();
            starts.Add(new Itinero.LocalGeo.Coordinate(46.7681917f, 23.6310351f));//home
            starts.Add(new Itinero.LocalGeo.Coordinate(46.7824045f, 23.6397901f));//IRA
            ends.Add(new Itinero.LocalGeo.Coordinate(46.7611929f, 23.5647638f));//Big Belly 
            ends.Add(new Itinero.LocalGeo.Coordinate(46.7707019f, 23.5660589f));//Piata 14 Iulie 
                                                                                //var startPos = new Itinero.LocalGeo.Coordinate(46.7681917f, 23.6310351f);//hardcoded start and finish locations
                                                                                //var endPos = new Itinero.LocalGeo.Coordinate(46.7682747f, 23.6221141f);//mercur
                                                                                //var endPos = new Itinero.LocalGeo.Coordinate(46.7687294f, 23.6190373f);//interservisan
                                                                                //var endPos = new Itinero.LocalGeo.Coordinate(46.767546f, 23.5999328f);//cipariu 
                                                                                //var endPos = new Itinero.LocalGeo.Coordinate(46.7611929f, 23.5647638f);//Big Belly 
                                                                                //var endPos = new Itinero.LocalGeo.Coordinate(46.7824045f, 23.6397901f);//IRA 
                                                                                //var endPos = new Itinero.LocalGeo.Coordinate(46.7707019f, 23.5660589f);//Piata 14 Iulie 
                                                                                //var endPos = new Itinero.LocalGeo.Coordinate(46.752623f, 23.577261f); //Golden tulip
            #endregion

            try
            {
                path = CommonVariables.PathToResultsFolder + configuration.NumberOfCars + "participants  - Th " + configuration.startTH + " to " + configuration.endTH + " - multiple routes - TS " + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".txt";
                finalStatistics.AppendLine("Simulation Date: " + DateTime.Now);
                ErrorLogsFile = CommonVariables.PathToResultsFolder + "ErrorLogs\\Error Log " + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".txt";
                System.IO.File.WriteAllText(ErrorLogsFile, "Simulation Time: " + DateTime.Now + "\n" + configuration.printVersion() + "\n");


                for (int choiceThreshold = configuration.startTH; choiceThreshold <= configuration.endTH; choiceThreshold += 10)// variate threshold if people chosing the sugested route over people chosng the fastest route
                {
                    Console.WriteLine("The threshold was set for " + choiceThreshold + "%.");

                    HttpClient httpClient = new HttpClient();
                    using (var response = await httpClient.GetAsync(configuration.LiveTrafficServerUri + "api/traffic/InitializeMaps"))
                    {
                        Console.WriteLine("The maps have been Initialized in the Live Traffic Server.");
                    }


                    Thread thrd;
                    //run first car (separately so I can join last thread at the end of this function)
                    Random rnd = new Random();
                    var choice = (rnd.Next(99) < choiceThreshold) ? "proposed" : "greedy";
                    var startPos = starts[rnd.Next(starts.Count)];
                    var endPos = ends[rnd.Next(ends.Count)];
                    TrafficParticipant tp = new TrafficParticipant(0, (new TimeSpan(0, 0, 0)), startPos, endPos, configuration, choice);
                    trafficParticipants.Add(tp);
                    thrd = new Thread(new ThreadStart(tp.RunTrafficParticipant));
                    thrd.Name = "TP0";
                    thrd.Start();
                    for (int i = 1; i < configuration.NumberOfCars; i++)//generate the cars
                    {
                        choice = (rnd.Next(99) < choiceThreshold) ? "proposed" : "greedy";
                        startPos = starts[rnd.Next(starts.Count)];
                        endPos = ends[rnd.Next(ends.Count)];
                        tp = new TrafficParticipant(i, (new TimeSpan(0, 0, +configuration.RequestDelay.Seconds * i)), startPos, endPos, configuration, choice);
                        trafficParticipants.Add(tp);
                        thrd = new Thread(new ThreadStart(tp.RunTrafficParticipant));//run each car on an independent thread
                        thrd.Name = "TP" + i;
                        thrd.Start();
                    }
                    #region CONSOLE
                    while (true)
                    {
                        bool allDone = true;
                        trafficParticipants.ForEach(t => { if (!(t.status == "done" || t.status == "failed")) { allDone = false; } });
                        if (allDone)
                        {
                            break;
                        }
#if !DEBUG
                        ReleaseConsole(choiceThreshold);
                        Thread.Sleep(1000);
#endif
                    }
                    #endregion

                    ManageStatistics(choiceThreshold);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                System.IO.File.AppendAllText(Simulation.ErrorLogsFile, e.Message + "\n");
                return null;
            }
            endSimulation();
            return "done";
        }
    }
}
