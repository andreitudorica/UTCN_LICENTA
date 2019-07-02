using Itinero;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RouteComputingServer.Services
{
    public static class MapsService
    {
        public static RouterDb routerDb;

        public static void LoadMaps()
        {
            routerDb = new RouterDb();
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
        }
    }
}
