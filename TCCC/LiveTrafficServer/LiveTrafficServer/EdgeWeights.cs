using Itinero;
using Itinero.Attributes;
using Itinero.LocalGeo;
using Itinero.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveTrafficServer
{
    public static class EdgeWeights
    {
        public static void AddWeightsToRouterDb(RouterDb routerDb)
        {
            for (int i = 1; i <= 50; i++)
            {
                routerDb.EdgeProfiles.Add(new AttributeCollection(
                new Itinero.Attributes.Attribute("highway", "residential"),
                new Itinero.Attributes.Attribute("custom-speed", i + "")));
            }
        }



        public static void SetWeight(RouterDb routerDb, uint edgeId, int i)
        {
            try
            {
                // update the speed profile of this edge.
                var edgeData = routerDb.Network.GetEdge(edgeId).Data;
                var asdf = routerDb.EdgeProfiles.Get(edgeData.Profile);
                edgeData.Profile = (ushort)(87 + i);
                asdf = routerDb.EdgeProfiles.Get(edgeData.Profile);
                routerDb.Network.UpdateEdgeData(edgeId, edgeData);
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
        }

        public static void DecreaseWeight(RouterDb routerDb, uint edgeId)
        {
            try
            {
                // update the speed profile of this edge.
                var edgeData = routerDb.Network.GetEdge(edgeId).Data;
                var newEdgeData = edgeData.Profile;
                if (newEdgeData < 87 + 50)
                    newEdgeData++;
                edgeData.Profile = (ushort)(newEdgeData);
                routerDb.Network.UpdateEdgeData(edgeId, edgeData);
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
        }

        public static void IncreaseWeight(RouterDb routerDb, uint edgeId)
        {
            try
            {
                // update the speed profile of this edge.
                var edgeData = routerDb.Network.GetEdge(edgeId).Data;
                var newEdgeData = edgeData.Profile;
                if (newEdgeData > 87 + 1)
                    newEdgeData--;
                edgeData.Profile = (ushort)(newEdgeData);
                routerDb.Network.UpdateEdgeData(edgeId, edgeData);
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
        }

        public static void HandleChange(float previousEdgeLon, float previousEdgeLat, float currentEdgeLon, float currentEdgeLat)
        {

            try
            {
                var customCar = DynamicVehicle.Load(System.IO.File.ReadAllText(CommonVariables.PathToCommonFolder + CommonVariables.CustomCarProfileFileName));
                RouterDb routerDb = Startup.routerDb;
                var router = new Router(routerDb);

                if (previousEdgeLon != 0)
                {
                    var previousEdgeLocation = new Coordinate(previousEdgeLon, previousEdgeLat);
                    var resolvedPrevious = router.Resolve(customCar.Fastest(), previousEdgeLocation);
                    uint previousEdgeId = resolvedPrevious.EdgeId;
                    DecreaseWeight(routerDb, (uint)previousEdgeId);
                }
                if (currentEdgeLon != 0)
                {
                    var currentEdgeLocation = new Coordinate(currentEdgeLon, currentEdgeLat);
                    var resolvedCurrent = router.Resolve(customCar.Fastest(), currentEdgeLocation);
                    uint currentEdgeId = resolvedCurrent.EdgeId;
                    IncreaseWeight(routerDb, (uint)currentEdgeId);
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
        }
    }
}
