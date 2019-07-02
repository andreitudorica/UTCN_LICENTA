using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficSimulator.Models
{
    public class ConfigurationModel
    {
        public enum SimulationTypeEnum { OneRouteMultipleTimes = 1, MultipleRoutes = 2 }
        public TimeSpan SimulationLength { get; set; }
        public int NumberOfCars { get; set; }
        public TimeSpan RequestDelay { get; set; }
        public Uri LiveTrafficServerUri { get; set; }
        public SimulationTypeEnum SimulationType { get; set; }
        public int timeMultiplyer = 1; // how many times slower does the simulation move
        public int startTH = 100;
        public int endTH = 100;
        public int delayBetweenRouteRequest = 10000;

        public string printVersion()
        {
            return "Number of cars: " + NumberOfCars + "\nRequest delay: " + RequestDelay + "\nSimulation type" + SimulationType + "\nTime multiplyer: " + timeMultiplyer + "delay Between Route Request" + delayBetweenRouteRequest + " millisec \nStart threshold: " + startTH + "\nEnd threshold: " + endTH + "\n_______________________________________________________";
        }
    }
}
