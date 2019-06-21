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
                    new Itinero.Attributes.Attribute("maxspeed", "RO:urban"),
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
                for (int choiceThreshold = 0; choiceThreshold <= 100; choiceThreshold += 10)// variate threshold if people chosing the sugested route over people chosng the fastest route
                {
                    Console.WriteLine("The threshold was set for "+choiceThreshold+"%.");
                    
                    HttpClient httpClient = new HttpClient();
                    using (var response = await httpClient.GetAsync(configuration.LiveTrafficServerUri+ "api/values/InitializeMaps"))
                    {
                        Console.WriteLine("The maps have been Initialized in the Live Traffic Server.");
                    }


                        Thread thrd;
                    //run first car (separately so I can join last thread at the end of this function)
                    Random rnd = new Random();
                    var choice = (rnd.Next(99)<choiceThreshold) ? "proposed" : "greedy";
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
                    while (tp.isDone == false) ;
                    simulationTrafficInflictedDelays.Add(new TimeSpan());
                    foreach(var trafficp in trafficParticipants)
                    {
                        simulationTrafficInflictedDelays[simulationTrafficInflictedDelays.Count - 1] += trafficp.RouteDuration;
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
            foreach (var stid in simulationTrafficInflictedDelays)
                Console.WriteLine(stid);

            return "done";
        }

        public async Task<string> RunRandomRoutes()
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
    }
}
