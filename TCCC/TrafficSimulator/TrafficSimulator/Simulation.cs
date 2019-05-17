using Itinero;
using Itinero.Graphs;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Osm.Vehicles;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TrafficSimulator.Models;

namespace TrafficSimulator
{
    internal class Simulation
    {
        private ConfigurationModel configuration;
        private RouterDb routerDb;
        public Simulation(ConfigurationModel config)
        {
            routerDb = new RouterDb();
            configuration = config;
            using (var stream = System.IO.File.OpenRead("D:\\Andrei\\Scoala\\LICENTA\\Maps\\Cluj-Napoca.pbf"))
            {
                routerDb.LoadOsmData(stream, Vehicle.Car);
            }
        }

        public void Start()
        {
            DateTime simulationStart = DateTime.Now;

            //run actual simulation
            while (DateTime.Now - simulationStart > configuration.SimulationLength)
            {

            }
        }

        public void FuckAround()
        {
            var router = new Router(routerDb);
            var currentProfile = routerDb.GetSupportedProfile("car");

            var weightHandler = router.GetDefaultWeightHandler(currentProfile); //vezi ce stie face asta
            
            var v = routerDb.Network;
            char command = 'c';
            int i = 0;
            while (command != 'q')
            {

                // calculate route.
                var home = new Coordinate(46.768293f, 23.629875f);
                var carina = new Coordinate(46.752623f, 23.577261f);
                var route = router.Calculate(currentProfile, home, carina);

                var routeGeoJson = route.ToGeoJson();
                Console.WriteLine(route.DirectionToNext(4));
                /*for (uint j = 0; j < routerDb.Network.GeometricGraph.Graph.VertexCount; j++)
                {
                    uint fromIndex = routerDb.Network.GeometricGraph.GetEdge(j).From;
                    Coordinate fromCoord = routerDb.Network.GeometricGraph.GetVertex(fromIndex);
                    uint toIndex = routerDb.Network.GeometricGraph.GetEdge(j).To;
                    Coordinate toCoord = routerDb.Network.GeometricGraph.GetVertex(toIndex);
                    if (fromCoord.Latitude == route.Shape[55].Latitude && fromCoord.Longitude == route.Shape[55].Longitude
                        && toCoord.Latitude == route.Shape[56].Latitude && toCoord.Longitude == route.Shape[56].Longitude)//nu gasesc pereche :(
                        Console.WriteLine("AI DE PULA MEA CE TARE!!!");
                }

                File.WriteAllText("route" + i + ".geojson", routeGeoJson);
                i++;

                var instructions = route.GenerateInstructions(routerDb);
                int a;
                a = 0;*/
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
                            Console.WriteLine(apiResponse + "Total time:" +(DateTime.Now - routeRequest).ToString(@"dd\.hh\:mm\:ss\.ff"));
                        }
                    }
                }
                return "done";
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
