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
            for(int i=1;i<=50;i++)
            {
                routerDb.EdgeProfiles.Add(new AttributeCollection(
                new Itinero.Attributes.Attribute("highway", "residential"),
                new Itinero.Attributes.Attribute("custom-speed", i+"")));
            }
        }

        public static void SetWeight(RouterDb routerDb, uint edgeId, int i)
        {
            // update the speed profile of this edge.
            var edgeData = routerDb.Network.GetEdge(edgeId).Data;
            var asdf =  routerDb.EdgeProfiles.Get(edgeData.Profile); 
            edgeData.Profile = (ushort)(87+i);
            asdf = routerDb.EdgeProfiles.Get(edgeData.Profile);
            routerDb.Network.UpdateEdgeData(edgeId, edgeData);
        }
    }
}
