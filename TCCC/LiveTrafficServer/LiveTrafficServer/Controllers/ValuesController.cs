using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Itinero;
using Itinero.Attributes;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Osm.Vehicles;
using Itinero.Profiles;
using Microsoft.AspNetCore.Mvc;

namespace LiveTrafficServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpGet("InitializeMaps")]
        public string InitializeMaps()
        {
            try
            {
                var customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
                var routerDb = new RouterDb();
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
                Startup.routerDb = routerDb;
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return "done";
        }
        // GET api/values
        [HttpGet("GetRoute")]//"{profile}/{startLat}/{startLon}/{endLat}/{endLon}")]
        public async Task<string> Get(string profile, float startLat, float startLon, float endLat, float endLon)
        {
            string apiResponse;
            var routerDb = Startup.routerDb;

            while (true)
            {
                try
                {
                    using (var stream = System.IO.File.OpenWrite(CommonVariables.PathToCommonFolder + CommonVariables.RouterDbFileName))
                    {
                        routerDb.Serialize(stream);
                    }
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Thread.Sleep(50);
                }
            }

            using (var httpClient = new HttpClient())
                {
                    //using (var response = await httpClient.GetAsync("http://localhost:62917/api/router/GetRoute?profile=car&startLat=46.768293&startLon=23.629875&endLat=46.752623&endLon=23.577261"))
                    //http://localhost:62917/api/router/GetRoute?profile=shortest&startLat=46.7681922912598&startLon=23.6310348510742&endLat=46.7675476074219&endLon=23.5999336242676
                    using (var response = await httpClient.GetAsync("http://localhost:62917/api/router/GetRoute?profile=" + profile + "&startLat=" + startLat + "&startLon=" + startLon + "&endLat=" + endLat + "&endLon=" + endLon))
                    {
                        apiResponse = await response.Content.ReadAsStringAsync();
                    }
                }
    
            return apiResponse;
        }

        // GET api/values
        [HttpGet("UpdateLocation")]
        public IActionResult Get(float previousEdgeLon, float previousEdgeLat, float currentEdgeLon, float currentEdgeLat)
        {
            try
            {
                var time = DateTime.Now;
                string result = "";

                //result += "reading RouteDB: " + (DateTime.Now - time).ToString(@"dd\.hh\:mm\:ss") + " ";
                lock(Startup.routerDb)
                EdgeWeights.HandleChange( previousEdgeLon,  previousEdgeLat,  currentEdgeLon,  currentEdgeLat);
                //file concurency to be handled 
                
                //routerDb.AddContracted(routerDb.GetSupportedProfile("car"));
                
                //result += " writing RouterDB: " + (DateTime.Now - time).ToString(@"dd\.hh\:mm\:ss\.ff") + " ";
                //result += " finished computing route: " + (DateTime.Now - time).ToString(@"dd\.hh\:mm\:ss\.ff") + " ";
                return Ok("succesful");
            }
            catch (Exception e)
            {

                return BadRequest(e.ToString());
            }
        }

        public string Get()
        {
            return "Server is Running. Waiting for commands.";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
