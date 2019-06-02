using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Mvc;
using Itinero;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Osm.Vehicles;
using Itinero.Profiles;

namespace RoutingAPI.Controllers
{
    public class RouterController : ApiController
    {
        //[System.Web.Http.HttpGet]
        //[Route("GetRoute/{profile}/{startLat}/{startLon}/{endLat}/{endLon}")]
        public HttpResponseMessage GetRoute(string profile,float startLat,float startLon,float endLat,float endLon)
        {
            var routerDb = new RouterDb();
            //load the current Map Data
            var customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
            using (var stream = System.IO.File.OpenRead(CommonVariables.PathToCommonFolder + CommonVariables.RouterDbFileName))
            {
                routerDb = RouterDb.Deserialize(stream);
            }
            
            // create router.
            var router = new Router(routerDb);
            //test link http://localhost:62917/api/router/GetRoute?profile=car&startLat=46.768293&startLon=23.629875&endLat=46.752623&endLon=23.577261
            // calculate route.
            var route = router.Calculate(customCar.Fastest(), new Coordinate(startLat,startLon), new Coordinate(endLat, endLon));
            //var route = router.Calculate(currentProfile, home, carina);
            //var routeGeoJson = Newtonsoft.Json.JsonConvert.SerializeObject(route);
            string routeJson = route.ToGeoJson();
            var instructions = route.GenerateInstructions(routerDb);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content= new StringContent(routeJson, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
