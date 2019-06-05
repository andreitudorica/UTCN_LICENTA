using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Itinero;
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

        // GET api/values
        [HttpGet("GetRoute")]//"{profile}/{startLat}/{startLon}/{endLat}/{endLon}")]
        public async Task<string> Get(string profile, float startLat, float startLon, float endLat, float endLon)
        {
            string apiResponse;
            using (var httpClient = new HttpClient())
            {
                //using (var response = await httpClient.GetAsync("http://localhost:62917/api/router/GetRoute?profile=car&startLat=46.768293&startLon=23.629875&endLat=46.752623&endLon=23.577261"))
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
                var routerDb = new RouterDb();
                var time = DateTime.Now;
                string result = "";
                var customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
                while (true)
                {
                    try
                    {
                        using (var stream = System.IO.File.OpenRead(CommonVariables.PathToCommonFolder + CommonVariables.RouterDbFileName))
                        {
                            routerDb = RouterDb.Deserialize(stream);
                        }
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            var router = new Router(routerDb);

                //result += "reading RouteDB: " + (DateTime.Now - time).ToString(@"dd\.hh\:mm\:ss") + " ";

                //file concurency to be handled 
                if (previousEdgeLon != 0)
                {
                    var previousEdgeLocation = new Coordinate(previousEdgeLon, previousEdgeLat);
                    var resolvedPrevious = router.Resolve(customCar.Fastest(), previousEdgeLocation);
                    uint previousEdgeId = resolvedPrevious.EdgeId;
                    EdgeWeights.SetWeight(routerDb, (uint)previousEdgeId, 50);
                }
                if (currentEdgeLon != 0)
                {
                    var currentEdgeLocation = new Coordinate(currentEdgeLon, currentEdgeLat);
                    var resolvedCurrent = router.Resolve(customCar.Fastest(), currentEdgeLocation);
                    uint currentEdgeId = resolvedCurrent.EdgeId;
                    EdgeWeights.SetWeight(routerDb, (uint)currentEdgeId, 1);
                }
                //routerDb.AddContracted(routerDb.GetSupportedProfile("car"));
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
                        
                        Console.WriteLine(e.ToString());
                    }
                }
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
