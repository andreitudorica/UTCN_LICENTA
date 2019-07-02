using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Itinero;
using Itinero.Attributes;
using Itinero.IO.Osm;
using Itinero.Profiles;
using LiveTrafficServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LiveTrafficServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrafficController : ControllerBase
    {
        // GET: api/Traffic
        [HttpGet]
        public string Get()
        {
            return "Live Traffic Server is Running. Waiting for commands.";
        }

        [HttpGet("InitializeMaps")]
        public string InitializeMaps()
        {
            return MapsService.InitializeMaps();
        }

        [HttpGet("GetRoute")]
        public async Task<string> GetRoute(string profile, float startLat, float startLon, float endLat, float endLon)
        {
            string apiResponse = await RoutesService.TryGetRoute(profile,startLat,startLon,endLat,endLon);
            return apiResponse;
        }
        
        [HttpGet("UpdateLocation")]
        public IActionResult UpdateLocation(float previousEdgeLon, float previousEdgeLat, float currentEdgeLon, float currentEdgeLat)
        {
            try
            {
                    EdgeWeightsService.HandleChange(previousEdgeLon, previousEdgeLat, currentEdgeLon, currentEdgeLat);
                return Ok("succesful");
            }
            catch (Exception e)
            {

                return BadRequest(e.ToString());
            }
        }
    }
}
