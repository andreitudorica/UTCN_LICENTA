using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LiveTrafficServer.Services
{
    public static class RoutesService
    {
        public static async Task<string> TryGetRoute(string profile, float startLat, float startLon, float endLat, float endLon)
        {
            string apiResponse;
            var routerDb = MapsService.routerDb;
            bool refreshedMap = false;
            if (MapsService.updatesCount >= Constants.UpdatesNecesaryForMapRefresh)
            {
                MapsService.UpdateMaps();
                refreshedMap = true;
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(Constants.RouteComputingServerURL + "api/routes/GetRoute?profile=" + profile + "&startLat=" + startLat + "&startLon=" + startLon + "&endLat=" + endLat + "&endLon=" + endLon + "&mapRefresh=" + refreshedMap);

                    if (!response.IsSuccessStatusCode)
                    {
                        MapsService.UpdateMaps();
                        response = await httpClient.GetAsync(Constants.RouteComputingServerURL + "api/routes/GetRoute?profile=" + profile + "&startLat=" + startLat + "&startLon=" + startLon + "&endLat=" + endLat + "&endLon=" + endLon + "&mapRefresh=" + refreshedMap);
                    }
                    apiResponse = await response.Content.ReadAsStringAsync();

                }
            }
            catch (Exception)
            {
                apiResponse = "failed";
            }

            return apiResponse;
        }
    }
}
