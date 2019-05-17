using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using TrafficSimulator.Models;

namespace TrafficSimulator
{
    internal class Program
    {
        private static Simulation simulation = null;
        private static ConfigurationModel configuration = null;

        private static ConfigurationModel GenerateDefaultConfig()
        {
            var config = new ConfigurationModel()
            {
                LiveTrafficServerUri = new Uri("https://localhost:44351"),
                NumberOfCars = 100,
                RequestDelay = new TimeSpan(0, 0, 6),
                SimulationLength = new TimeSpan(0, 2, 0),
                SimulationType = ConfigurationModel.SimulationTypeEnum.OneRouteMultipleTimes
            };
            using (StreamWriter file = File.CreateText(@"SimulationParameters.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, config);
            }
            return config;
        }

        private static ConfigurationModel GetSimulationParameters()
        {
            return JsonConvert.DeserializeObject<ConfigurationModel>(File.ReadAllText(@"SimulationParameters.json"));
        }

        private static void Main(string[] args)
        {
            //GenerateDefaultConfig();
            //MainAsync().Wait();
            configuration = GetSimulationParameters();
            simulation = new Simulation(configuration);
            simulation.FuckAround();
        }

        static async Task MainAsync()
        {
            try
            {
                await simulation.TestBasicFlow();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
