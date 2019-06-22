// The MIT License (MIT)

// Copyright (c) 2016 Ben Abelshausen

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Itinero;
using Itinero.Attributes;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Osm.Vehicles;
using Itinero.Profiles;
using System;
using System.IO;
using Attribute = Itinero.Attributes.Attribute;
using Vehicle = Itinero.Osm.Vehicles.Vehicle;

namespace Sample.Basic
{
    class Program
    {
        static void Main(string[] args)
        {
            // enable logging.
            OsmSharp.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Console.WriteLine($"[{o}] {level} - {message}");
            };
            Itinero.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Console.WriteLine($"[{o}] {level} - {message}");
            };
            
            var routerDb = new RouterDb();
            var customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
            using (var stream = System.IO.File.OpenRead(CommonVariables.PathToCommonFolder + CommonVariables.RouterDbFileName))
            {
                routerDb = RouterDb.Deserialize(stream);
            }

            // add custom profiles.
            var speed1 = routerDb.EdgeProfiles.Add(new AttributeCollection(
                new Attribute("highway", "residential"),
                new Attribute("custom-speed", "1")));
            var speed20 = routerDb.EdgeProfiles.Add(new AttributeCollection(
                new Attribute("highway", "residential"),
                new Attribute("custom-speed", "20")));
            var speed30 = routerDb.EdgeProfiles.Add(new AttributeCollection(
                new Attribute("highway", "residential"),
                new Attribute("custom-speed", "30")));
            var speed40 = routerDb.EdgeProfiles.Add(new AttributeCollection(
                new Attribute("highway", "residential"),
                new Attribute("custom-speed", "40")));
            var speed50 = routerDb.EdgeProfiles.Add(new AttributeCollection(
                new Attribute("highway", "residential"),
                new Attribute("custom-speed", "50")));

            // define locations, profile and router.
            var home = new Coordinate(46.768293f, 23.629875f);
             var carina = new Coordinate(46.752623f, 23.577261f);
            var router = new Router(routerDb);

            // calculate route before.
            var routeBefore = router.Calculate(customCar.Fastest(), home, carina);
            var routeBeforeGeoJson = routeBefore.ToGeoJson();
            File.WriteAllText("routeBeforeGeoJson.geojson", routeBeforeGeoJson);
            
            // resolve an edge.
            var edgeLocation = new Coordinate(46.7692801f, 23.6139063f);
            var resolved = router.Resolve(customCar.Fastest(), edgeLocation);

            // update the speed profile of this edge.
            var edgeData = routerDb.Network.GetEdge(resolved.EdgeId).Data;
            edgeData.Profile = (ushort)speed1;
            routerDb.Network.UpdateEdgeData(resolved.EdgeId, edgeData);

            
            // calculate route.
            var routeAfter = router.Calculate(customCar.Fastest(), home, carina);
            var routeAfterGeoJson = routeAfter.ToGeoJson();
            File.WriteAllText("routeAfterSlowGeoJson.geojson", routeAfterGeoJson);


            // update the speed profile of this edge.
            edgeData = routerDb.Network.GetEdge(resolved.EdgeId).Data;
            edgeData.Profile = (ushort)speed50;
            routerDb.Network.UpdateEdgeData(resolved.EdgeId, edgeData);


            // calculate route.
            routeAfter = router.Calculate(customCar.Fastest(), home, carina);
            routeAfterGeoJson = routeAfter.ToGeoJson();
            File.WriteAllText("routeAfterFastGeoJson.geojson", routeAfterGeoJson);

        }
        //// enable logging.
        //OsmSharp.Logging.Logger.LogAction = (o, level, message, parameters) =>
        //{
        //    Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
        //};
        //Itinero.Logging.Logger.LogAction = (o, level, message, parameters) =>
        //{
        //    Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
        //};

        ////Download.ToFile("http://files.itinero.tech/data/OSM/planet/europe/luxembourg-latest.osm.pbf", "luxembourg-latest.osm.pbf").Wait();

        //// load some routing data and create a router.


        //// create router.
        //var router = new Router(routerDb);
        //var e1 = routerDb.Network.GeometricGraph.Graph.GetEdge(1);
        //var d1 = routerDb.Network.GeometricGraph.Graph.GetEdge(1).Data;
        //Console.WriteLine(d1[0]);
        //var d2 = routerDb.Network.GeometricGraph.GetEdge(1).Data;
        //var d3 = routerDb.Network.GetEdge(1).Data;
        ////routerDb.Network.GeometricGraph.Graph.UpdateEdgeData(1, new uint[2] { 12,13 });
        //d1 = routerDb.Network.GeometricGraph.Graph.GetEdge(1).Data;
        //d2 = routerDb.Network.GeometricGraph.GetEdge(1).Data;
        ////Console.WriteLine(d1[0] + " " + d1[1]);
        //Console.WriteLine(d2[0]);
        //Console.WriteLine(d3.Distance);

        //var currentProfile = routerDb.GetSupportedProfile("car");

        //var v =routerDb.Network;
        //char command = 'c';
        //int i = 0;
        //while (command != 'q')
        //{
        

        // calculate a sequence.
        // this should be the result: http://geojson.io/#id=gist:xivk/760552b0abbcb37a3026273b165f63b8&map=16/49.5881/6.1115
        /* var locations = new []
         {
             new Coordinate(49.58562050646863f, 6.1020684242248535f), 
             new Coordinate(49.58645517402537f, 6.1063170433044430f), 
             new Coordinate(49.58976588133606f, 6.1078405380249020f),
             new Coordinate(49.59126814499573f, 6.1184406280517570f),
             new Coordinate(49.58816619787410f, 6.1208438873291010f)
         };
         route = router.Calculate(car, locations);
         routeGeoJson = route.ToGeoJson();
         File.WriteAllText("sequence1-undirected.geojson", routeGeoJson);

         // calculate a directed sequence with a turn penalty of 120 secs.
         // this should be the result: http://geojson.io/#id=gist:xivk/49f5d843c16adb68c740f8fc0b4d8583&map=16/49.5881/6.1115
         route = router.Calculate(car, locations, turnPenalty: 120, preferredDirections: null); 
         routeGeoJson = route.ToGeoJson();
         File.WriteAllText("sequence2-turn-penalty-120.geojson", routeGeoJson);

         // calculate a directed sequence without turn penalty but with a departure angle.
         // this should be the result: http://geojson.io/#id=gist:xivk/c93be9a18072a78ea931dbc5a772f34f&map=16/49.5881/6.1111
         var angles = new float?[]
         {
             -90, // leave west.
             null, // don't-care
             null, // don't-care
             null, // don't-care
             null // don't-care
         };
         route = router.Calculate(car, locations, preferredDirections: angles);
         routeGeoJson = route.ToGeoJson();
         File.WriteAllText("sequence3-preferred-directions.geojson", routeGeoJson);

         // calculate a direction with a turn penalty of 120 secs and more preferred departure/arrival angles.
         // this should be the result: http://geojson.io/#id=gist:xivk/660effe2cff422e183aed8efe1fc72c9&map=16/49.5881/6.1112
         angles = new float?[]
         {
             -90, // leave west.
             -90, // pass in western direction.
             null, // don't-care
             null, // don't-care
             -45 // arrive in north-west direction.
         };
         route = router.Calculate(car, locations, turnPenalty: 120, preferredDirections: angles);
         routeGeoJson = route.ToGeoJson();
         File.WriteAllText("sequence4-turn-penalty-120-preferred-directions.geojson", routeGeoJson);*/
    }

}
