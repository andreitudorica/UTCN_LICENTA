using Itinero;
using Itinero.Attributes;
using Itinero.IO.Osm;
using Itinero.Profiles;
using System;
using System.Threading;

namespace LiveTrafficServer.Services
{
    public static class MapsService
    {
        public static RouterDb routerDb;
        public static Route route;
        public static uint profilesStart;
        public static int updatesCount;
        public static DynamicVehicle customCar;
        public static Router router;
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

        public static string InitializeMaps()
        {
            try
            {
                customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
                var routerDb = new RouterDb();
                //load pbf file of the map
                using (var stream = System.IO.File.OpenRead(CommonVariables.PathToCommonFolder + CommonVariables.PbfMapFileName))
                {
                    routerDb.LoadOsmData(stream, customCar);
                }
                profilesStart = routerDb.EdgeProfiles.Add(new AttributeCollection(

                new Itinero.Attributes.Attribute("highway", "residential"),
                new Itinero.Attributes.Attribute("custom-speed", "0"),
                new Itinero.Attributes.Attribute("car-count", "0")));
                //add the custom edge profiles to the routerDb (used for live traffic status on map)
                for (int c = 0; c < 100; c++)
                {
                    for (int cs = 1; cs <= 50; cs++)
                    {
                        routerDb.EdgeProfiles.Add(new AttributeCollection(
                            new Itinero.Attributes.Attribute("highway", "residential"),
                            new Itinero.Attributes.Attribute("custom-speed", cs + ""),
                            new Itinero.Attributes.Attribute("car-count", c + "")));
                    }
                }

                //write the routerDb to file so every project can use it
                MapsService.routerDb = routerDb;
                router = new Router(routerDb);
                router.ProfileFactorAndSpeedCache.CalculateFor(customCar.Fastest());
                UpdateMaps();
                MapsService.updatesCount = Constants.UpdatesNecesaryForMapRefresh + 1;
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return "done";
        }

        public static void UpdateMaps()
        {
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
                    Thread.Sleep(100);
                }
            }
            updatesCount = 0;
        }
    }
}
