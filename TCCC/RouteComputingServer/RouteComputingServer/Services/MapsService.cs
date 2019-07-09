using Itinero;
using Itinero.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RouteComputingServer.Services
{
    public static class MapsService
    {
        public static RouterDb routerDb;
        public static Router router;
        public static DynamicVehicle customCar;

        public static void LoadMaps()
        {
            customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
            routerDb = new RouterDb();
            while (true)
            {
                try
                {
                    using (var stream = System.IO.File.OpenRead(CommonVariables.PathToCommonFolder + CommonVariables.RouterDbFileName))
                    {
                        routerDb = RouterDb.Deserialize(stream);
                        router = new Router(routerDb);
                    }
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
