using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Itinero;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Osm.Vehicles;

namespace RoutingAPI.Controllers
{
    public class RouterController : ApiController
    {
        RouterDb routerDb;
        
        [System.Web.Http.HttpGet]
        public string Init()
        {
            routerDb = new RouterDb();

            using (var stream = System.IO.File.OpenRead("D:\\Andrei\\Scoala\\LICENTA\\Maps\\Cluj-Napoca.pbf"))
            {
                routerDb.LoadOsmData(stream, Vehicle.Car);
            }
            return "done";
        }
        
        //[System.Web.Http.HttpGet]
        //[Route("GetRoute/{profile}/{startX}/{startY}/{endX}/{endY}")]
        public string GetRoute(string profile,float startX,float startY,float endX,float endY)
        {
            routerDb = new RouterDb();

            /*using (var stream = System.IO.File.OpenRead("D:\\Andrei\\Scoala\\LICENTA\\Maps\\Cluj-Napoca.routerdb"))
            {
                routerDb = RouterDb.Deserialize(stream);
            }*/

            using (var stream = System.IO.File.OpenRead("D:\\Andrei\\Scoala\\LICENTA\\Maps\\Cluj-Napoca.pbf"))
            {
                routerDb.LoadOsmData(stream, Vehicle.Car);
            }

            // get the profile from the routerdb.
            // this is best-practice in Itinero, to prevent mis-matches.
            var currentProfile = routerDb.GetSupportedProfile(profile);
            
            // create router.
            var router = new Router(routerDb);
            //test link http://localhost:62917/api/router/GetRoute?profile=car&startX=46.768293&startY=23.629875&endX=46.752623&endY=23.577261
            // calculate route.
            var home = new Coordinate(46.768293f, 23.629875f);
            var carina = new Coordinate(46.752623f, 23.577261f);
            var route = router.Calculate(currentProfile, new Coordinate(startX,startY), new Coordinate(endX, endY));
            //var route = router.Calculate(currentProfile, home, carina);
            var routeGeoJson = route.ToGeoJson();
            var instructions = route.GenerateInstructions(routerDb);
            return routeGeoJson;
        }
    }
}
