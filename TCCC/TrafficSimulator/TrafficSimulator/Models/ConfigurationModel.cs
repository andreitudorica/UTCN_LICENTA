using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficSimulator.Models
{
    public class ConfigurationModel
    {
        public enum SimulationTypeEnum { OneRouteMultipleTimes = 1, RandomRoutes = 2 }
        public TimeSpan SimulationLength { get; set; }
        public int NumberOfCars { get; set; }
        public TimeSpan RequestDelay { get; set; }
        public Uri LiveTrafficServerUri { get; set; }
        public SimulationTypeEnum SimulationType { get; set; }
    }
}
