using Itinero;
using Itinero.Attributes;
using Itinero.Graphs;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Osm.Vehicles;
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
    internal class StatisticsEntry
    {

        public TimeSpan totalTime;
        public TimeSpan averageTime;
        public double averageTouchedFeatures;
        public double averageTouchedSegments;
        public double averageSpeed;
        public TimeSpan totalUpdateWaitTime;
        public TimeSpan totalRouteRequestWaitTime;
        public TimeSpan averageUpdateWaitTime;
        public TimeSpan averageRouteRequestWaitTime;
        public TimeSpan averageTimeWithoutWaiting;
    }

    internal class Simulation
    {
        private ConfigurationModel configuration;
        public static RouterDb routerDb;
        private List<TrafficParticipant> trafficParticipants;
        private List<StatisticsEntry> Statistics;
        public static int threshold = 0;
        public DateTime simulationStart;
        public string path;

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
        }

        public void TryNewStuff()
        {
            var router = new Router(routerDb);
            var currentProfile = routerDb.GetSupportedProfile("car");

            var weightHandler = router.GetDefaultWeightHandler(currentProfile); //vezi ce stie face asta

            var v = routerDb.Network;
            char command = 'c';
            while (command != 'q')
            {

                // calculate route.
                var home = new Itinero.LocalGeo.Coordinate(46.768293f, 23.629875f);
                var carina = new Itinero.LocalGeo.Coordinate(46.752623f, 23.577261f);
                var route = router.Calculate(currentProfile, home, carina);

                var routeGeoJson = route.ToGeoJson();
                Console.WriteLine(route.DirectionToNext(4));

                command = Console.ReadKey().KeyChar;
            }
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
            List<TrafficParticipant> failed = new List<TrafficParticipant>();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Simulation time: " + (DateTime.Now - simulationStart).ToString(@"dd\.hh\:mm\:ss"));
            sb.AppendLine("Traffic Participants: " + configuration.NumberOfCars + " Time multiplyer: " + CommonVariables.timeMultiplyer);
            sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
            if (Statistics.Count > 0)
            {
                sb.AppendLine("Current Statistics: ");
                for (int i = 0; i < Statistics.Count; i++)
                {
                    sb.AppendLine("Threshold: " + i * 10
                        + "% Total Time: " + Statistics[i].totalTime
                        + "\n Average Time: " + Statistics[i].averageTime
                        + "\n Average Time without waitings: " + (Statistics[i].averageTime - Statistics[i].averageUpdateWaitTime - Statistics[i].averageRouteRequestWaitTime)
                        + "\n Average features touched " + Statistics[i].averageTouchedFeatures
                        + "\n Average segments touched " + Statistics[i].averageTouchedSegments
                        + "\n Average speed " + Statistics[i].averageSpeed
                        + "\n Average update wait Time: " + Statistics[i].averageUpdateWaitTime
                        + "\n Average route request wait Time: " + Statistics[i].averageUpdateWaitTime
                        + "____________________________________________");
                }
                sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
            }
            sb.AppendLine("The threshold was set for " + choiceThreshold + "%.");
            foreach (var trafficpart in trafficParticipants)
            {
                if (trafficpart.hasFailed)
                    failed.Add(trafficpart);
                if (trafficpart.hasStarted == true && trafficpart.isDone == false && trafficpart.hasFailed == false)
                    sb.AppendLine("Traffic Participant " + trafficpart.ID + " - (" + trafficpart.doneSteps + " / " + trafficpart.steps + ") " + trafficpart.behaviour + " route" + " - reroutings " + trafficpart.reroutingCount);
            }
            if (failed.Count > 0)
            {
                sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
                sb.AppendLine("Failed traffic participants");
                foreach (var f in failed)
                    sb.AppendLine("Traffic Participant " + f.ID + " failed with message: " + f.errorMessage);
            }
            Console.Clear();
            Console.Write(sb.ToString());
        }

        public void ManageStatistics(int choiceThreshold)
        {
            Statistics.Add(new StatisticsEntry());
            foreach (var trafficp in trafficParticipants)
            {
                if (trafficp.hasFailed == false)
                {
                    Statistics[Statistics.Count - 1].totalTime += trafficp.RouteDuration;
                    Statistics[Statistics.Count - 1].averageTimeWithoutWaiting += trafficp.RouteDuration - trafficp.RouteRequestTotalWaitTime - trafficp.UpdateRequestTotalWaitTime;
                    Statistics[Statistics.Count - 1].averageTouchedFeatures += trafficp.steps;
                    Statistics[Statistics.Count - 1].averageTouchedSegments += trafficp.segmentsTouched;
                    Statistics[Statistics.Count - 1].averageSpeed += trafficp.averageSpeed;
                    Statistics[Statistics.Count - 1].totalRouteRequestWaitTime += trafficp.RouteRequestTotalWaitTime;
                    Statistics[Statistics.Count - 1].totalUpdateWaitTime += trafficp.UpdateRequestTotalWaitTime;
                    Statistics[Statistics.Count - 1].averageRouteRequestWaitTime += TimeSpan.FromTicks(trafficp.RouteRequestTotalWaitTime.Ticks / trafficp.RouteRequests);
                    Statistics[Statistics.Count - 1].averageUpdateWaitTime += TimeSpan.FromTicks(trafficp.UpdateRequestTotalWaitTime.Ticks / trafficp.UpdateRequests);
                }
            }
            Statistics[Statistics.Count - 1].averageTime = TimeSpan.FromTicks(Statistics[Statistics.Count - 1].totalTime.Ticks / configuration.NumberOfCars);
            Statistics[Statistics.Count - 1].averageTouchedFeatures /= configuration.NumberOfCars;
            Statistics[Statistics.Count - 1].averageTouchedSegments /= configuration.NumberOfCars;
            Statistics[Statistics.Count - 1].averageRouteRequestWaitTime = TimeSpan.FromTicks(Statistics[Statistics.Count - 1].averageRouteRequestWaitTime.Ticks / configuration.NumberOfCars);
            Statistics[Statistics.Count - 1].averageUpdateWaitTime = TimeSpan.FromTicks(Statistics[Statistics.Count - 1].averageUpdateWaitTime.Ticks / configuration.NumberOfCars);
            Statistics[Statistics.Count - 1].averageSpeed /= configuration.NumberOfCars;

            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine("Threshold " + choiceThreshold
                            + "%\n Total Time: " + Statistics[choiceThreshold / 10].totalTime
                            + "\n Average Time: " + Statistics[choiceThreshold / 10].averageTime
                            + "\n Average Time without waitings: " + Statistics[choiceThreshold / 10].averageTimeWithoutWaiting
                            + "\n Average features touched " + Statistics[choiceThreshold / 10].averageTouchedFeatures
                            + " features\n Average segments touched " + Statistics[choiceThreshold / 10].averageTouchedSegments
                            + "segments\n Average speed " + Statistics[choiceThreshold / 10].averageSpeed
                            + "km/h\n Average update wait Time " + Statistics[choiceThreshold / 10].averageUpdateWaitTime
                            + "\n Average route request wait Time " + Statistics[choiceThreshold / 10].averageUpdateWaitTime
                            );
            }
            //compute statistics
            trafficParticipants.Clear();
        }

        public async Task<string> RunOneRouteMultipleTimes()
        {
            var startPos = new Itinero.LocalGeo.Coordinate(46.7681917f, 23.6310351f);//hardcoded start and finish locations
            //var endPos = new Itinero.LocalGeo.Coordinate(46.7682747f, 23.6221141f);//mercur
            //var endPos = new Itinero.LocalGeo.Coordinate(46.7687294f, 23.6190373f);//interservisan
            var endPos = new Itinero.LocalGeo.Coordinate(46.767546f, 23.5999328f);//cipariu 
            //var endPos = new Itinero.LocalGeo.Coordinate(46.752623f, 23.577261f); //Golden tulip
            try
            {
                path = CommonVariables.PathToResultsFolder + configuration.NumberOfCars + " participants 0 to 100 - home to cipariu - 5 s delay - TS " + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".txt";
                if (!File.Exists(path))
                {
                    // Create a file to write to.
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine("Simulation Date: " + DateTime.Now);
                    }
                }
                for (int choiceThreshold = 0; choiceThreshold <= 100; choiceThreshold += 10)// variate threshold if people chosing the sugested route over people chosng the fastest route
                {
                    Console.WriteLine("The threshold was set for " + choiceThreshold + "%.");

                    HttpClient httpClient = new HttpClient();
                    using (var response = await httpClient.GetAsync(configuration.LiveTrafficServerUri + "api/values/InitializeMaps"))
                    {
#if DEBUG
                        Console.WriteLine("The maps have been Initialized in the Live Traffic Server.");
#endif
                    }


                    Thread thrd;
                    //run first car (separately so I can join last thread at the end of this function)
                    Random rnd = new Random();
                    var choice = (rnd.Next(99) < choiceThreshold) ? "proposed" : "greedy";
                    TrafficParticipant tp = new TrafficParticipant(0, (new TimeSpan(0, 0, 0)), startPos, endPos, configuration, choice);
                    trafficParticipants.Add(tp);
                    thrd = new Thread(new ThreadStart(tp.RunTrafficParticipant));
                    thrd.Start();

                    for (int i = 1; i < configuration.NumberOfCars; i++)//generate the cars
                    {
                        choice = (rnd.Next(99) < choiceThreshold) ? "proposed" : "greedy";
                        tp = new TrafficParticipant(i, (new TimeSpan(0, 0, +configuration.RequestDelay.Seconds * i)), startPos, endPos, configuration, choice);
                        trafficParticipants.Add(tp);
                        thrd = new Thread(new ThreadStart(tp.RunTrafficParticipant));//run each car on an independent thread
                        thrd.Start();
                    }

                    #region CONSOLE
                    while (true)
                    {
                        bool allDone = true;
                        trafficParticipants.ForEach(t => { if (!(t.isDone || t.hasFailed)) allDone = false; });
                        if (allDone) break;
#if ! DEBUG             
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
                return null;
            }
            return "done";
        }

        public async Task<string> RunMultipleRoutes()
        {
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
            try
            {
                path = CommonVariables.PathToResultsFolder + configuration.NumberOfCars + "participants 0 to 100 - multiple routes - 5 s delay.txt";
                if (!File.Exists(path))
                {
                    // Create a file to write to.
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine("Simulation Date: " + DateTime.Now);
                    }
                }
                for (int choiceThreshold = 0; choiceThreshold <= 100; choiceThreshold += 10)// variate threshold if people chosing the sugested route over people chosng the fastest route
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
                    var startPos = starts[rnd.Next(starts.Count)];
                    var endPos = ends[rnd.Next(ends.Count)];
                    TrafficParticipant tp = new TrafficParticipant(0, (new TimeSpan(0, 0, 0)), startPos, endPos, configuration, choice);
                    trafficParticipants.Add(tp);
                    thrd = new Thread(new ThreadStart(tp.RunTrafficParticipant));
                    thrd.Start();
                    for (int i = 1; i < configuration.NumberOfCars; i++)//generate the cars
                    {
                        choice = (rnd.Next(99) < choiceThreshold) ? "proposed" : "greedy";
                        startPos = starts[rnd.Next(starts.Count)];
                        endPos = ends[rnd.Next(ends.Count)];
                        tp = new TrafficParticipant(i, (new TimeSpan(0, 0, +configuration.RequestDelay.Seconds * i)), startPos, endPos, configuration, choice);
                        trafficParticipants.Add(tp);
                        thrd = new Thread(new ThreadStart(tp.RunTrafficParticipant));//run each car on an independent thread
                        thrd.Start();
                    }
                    #region CONSOLE
                    while (true)
                    {
                        bool allDone = true;
                        trafficParticipants.ForEach(t => { if (!(t.isDone || t.hasFailed)) allDone = false; });
                        if (allDone) break;
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
                return null;
            }
            return "done";
        }
    }
}
