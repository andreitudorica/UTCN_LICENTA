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
    internal class Simulation
    {
        private ConfigurationModel configuration;
        public static RouterDb routerDb;
        private List<TrafficParticipant> trafficParticipants;
        private List<TimeSpan> simulationTrafficInflictedDelays;
        public static int threshold = 0;
        public DateTime simulationStart;
        public void InitializeMaps()
        {
            var customCar = DynamicVehicle.Load(File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
            routerDb = new RouterDb();
            //load pbf file of the map
            using (var stream = System.IO.File.OpenRead(CommonVariables.PathToCommonFolder + CommonVariables.PbfMapFileName))
            {
                routerDb.LoadOsmData(stream, customCar);
            }

            //add the custom edge profiles to the routerDb (used for live traffic status on map)
            for (int i = 1; i <= 50; i++)
            {
                routerDb.EdgeProfiles.Add(new AttributeCollection(
                    new Itinero.Attributes.Attribute("highway", "residential"),
                    new Itinero.Attributes.Attribute("number-of-cars", "0"),
                    new Itinero.Attributes.Attribute("custom-speed", i + "")));
            }

            //write the routerDb to file so every project can use it
            using (var stream = System.IO.File.OpenWrite(CommonVariables.PathToCommonFolder + CommonVariables.RouterDbFileName))
            {
                routerDb.Serialize(stream);
            }
        }


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
            simulationTrafficInflictedDelays = new List<TimeSpan>();
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


        public async Task<string> RunOneRouteMultipleTimes()
        {
            var startPos = new Itinero.LocalGeo.Coordinate(46.7681917f, 23.6310351f);//hardcoded start and finish locations
            //var endPos = new Itinero.LocalGeo.Coordinate(46.7682747f, 23.6221141f);//mercur
            //var endPos = new Itinero.LocalGeo.Coordinate(46.7687294f, 23.6190373f);//interservisan
            var endPos = new Itinero.LocalGeo.Coordinate(46.767546f, 23.5999328f);//cipariu 
            //var endPos = new Itinero.LocalGeo.Coordinate(46.752623f, 23.577261f); //Golden tulip
            try
            {
                string path = CommonVariables.PathToResultsFolder + configuration.NumberOfCars + " participants 0 to 100 - home to cipariu - 5 s delay - TS "  +DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".txt";
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
                    while (tp.isDone == false)
                    {
#if ! DEBUG             
                        List<TrafficParticipant> failed = new List<TrafficParticipant>();
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Simulation time: " + (DateTime.Now - simulationStart).ToString(@"dd\.hh\:mm\:ss"));
                        sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
                        if (simulationTrafficInflictedDelays.Count > 0)
                        {
                            sb.AppendLine("Current Statistics: ");
                            for (int i = 0; i < simulationTrafficInflictedDelays.Count; i++)
                            {
                                sb.AppendLine("Threshold " + i * 10 + "%: Total Time " + simulationTrafficInflictedDelays[i]);
                            }
                            sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
                        }
                        sb.AppendLine("The threshold was set for " + choiceThreshold + "%.");
                        foreach (var trafficpart in trafficParticipants)
                        {
                            if (trafficpart.hasFailed)
                                failed.Add(trafficpart);
                            if (trafficpart.hasStarted == true && trafficpart.isDone == false)
                                sb.AppendLine("Traffic Participant "+trafficpart.ID + " - (" + trafficpart.doneSteps + " / " + trafficpart.steps + ") " + trafficpart.behaviour+ " route");
                        }
                        if(failed.Count>0)
                        {
                            sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
                            sb.AppendLine("Failed traffic participants");
                            foreach (var f in failed)
                                sb.AppendLine("Traffic Participant "+f.ID+" failed with message: "+f.errorMessage);
                        }
                        Console.Clear();
                        Console.Write(sb.ToString());
                        Thread.Sleep(1000);
#endif                  
                    }
                    #endregion

                    simulationTrafficInflictedDelays.Add(new TimeSpan());
                    foreach (var trafficp in trafficParticipants)
                    {
                        simulationTrafficInflictedDelays[simulationTrafficInflictedDelays.Count - 1] += trafficp.RouteDuration;

                    }
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine(choiceThreshold + "% - Total time: " + simulationTrafficInflictedDelays[simulationTrafficInflictedDelays.Count - 1] + " - Average time: " + (simulationTrafficInflictedDelays[simulationTrafficInflictedDelays.Count - 1].Milliseconds) / configuration.NumberOfCars);
                    }
                    //compute statistics
                    trafficParticipants.Clear();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
#if DEBUG
            for (int i = 0; i < 100; i++)
                foreach (var stid in simulationTrafficInflictedDelays)
                    Console.WriteLine(stid);
#endif
            return "done";
        }

        public async Task<string> RunMultipleRoutes()
        {
            List<Itinero.LocalGeo.Coordinate> starts =new List<Itinero.LocalGeo.Coordinate>();
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
                string path = CommonVariables.PathToResultsFolder + configuration.NumberOfCars + "participants 0 to 100 - multiple routes - 5 s delay.txt";
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
                    while (tp.isDone == false)
                    {
#if ! DEBUG             
                        List<TrafficParticipant> failed = new List<TrafficParticipant>();
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Simulation time: " + (DateTime.Now - simulationStart).ToString(@"dd\.hh\:mm\:ss"));
                        sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
                        if (simulationTrafficInflictedDelays.Count > 0)
                        {
                            sb.AppendLine("Current Statistics: ");
                            for (int i = 0; i < simulationTrafficInflictedDelays.Count; i++)
                            {
                                sb.AppendLine("Threshold " + i * 10 + "%: Total Time " + simulationTrafficInflictedDelays[i]);
                            }
                            sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
                        }
                        sb.AppendLine("The threshold was set for " + choiceThreshold + "%.");
                        foreach (var trafficpart in trafficParticipants)
                        {
                            if (trafficpart.hasFailed)
                                failed.Add(trafficpart);
                            if (trafficpart.hasStarted == true && trafficpart.isDone == false)
                                sb.AppendLine("Traffic Participant "+trafficpart.ID + " - (" + trafficpart.doneSteps + " / " + trafficpart.steps + ") " + trafficpart.behaviour+ " route");
                        }
                        if(failed.Count>0)
                        {
                            sb.AppendLine("----------------------------------------------------------------------------------------------------------------------");
                            sb.AppendLine("Failed traffic participants");
                            foreach (var f in failed)
                                sb.AppendLine("Traffic Participant "+f.ID+" failed with message: "+f.errorMessage);
                        }
                        Console.Clear();
                        Console.Write(sb.ToString());
                        Thread.Sleep(1000);
#endif                  
                    }
                    #endregion
                    simulationTrafficInflictedDelays.Add(new TimeSpan());
                    foreach (var trafficp in trafficParticipants)
                    {
                        simulationTrafficInflictedDelays[simulationTrafficInflictedDelays.Count - 1] += trafficp.RouteDuration;

                    }
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine(choiceThreshold + "% - Total time: " + simulationTrafficInflictedDelays[simulationTrafficInflictedDelays.Count - 1] + " - Average time: " + (simulationTrafficInflictedDelays[simulationTrafficInflictedDelays.Count - 1].Milliseconds) / configuration.NumberOfCars);
                    }
                    //compute statistics
                    trafficParticipants.Clear();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
            for (int i = 0; i < 100; i++)
                foreach (var stid in simulationTrafficInflictedDelays)
                    Console.WriteLine(stid);

            return "done";
        }
    }
}
