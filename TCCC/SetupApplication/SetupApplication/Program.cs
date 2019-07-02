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
namespace SetupApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            var customCar = DynamicVehicle.Load(File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
            var routerDb = new RouterDb();
            //load pbf file of the map
            using (var stream = System.IO.File.OpenRead(CommonVariables.PathToCommonFolder + CommonVariables.PbfMapFileName))
            {
                routerDb.LoadOsmData(stream, customCar);
            }

            routerDb.EdgeProfiles.Add(new AttributeCollection(
            new Itinero.Attributes.Attribute("highway", "residential"),
                new Itinero.Attributes.Attribute("custom-speed", "0"),
                new Itinero.Attributes.Attribute("car-count", "0")));
            //add the custom edge profiles to the routerDb (used for live traffic status on map)
            for (int c = 0; c < 100; c++)
                for (int cs = 1; cs <= 50; cs++)
                {
                    routerDb.EdgeProfiles.Add(new AttributeCollection(
                        new Itinero.Attributes.Attribute("highway", "residential"),
                        new Itinero.Attributes.Attribute("custom-speed", cs + ""),
                        new Itinero.Attributes.Attribute("car-count", c + "")));
                }

            //write the routerDb to file so every project can use it
            using (var stream = System.IO.File.OpenWrite(CommonVariables.PathToCommonFolder + CommonVariables.RouterDbFileName))
            {
                routerDb.Serialize(stream);
            }
        }
    }
}
