using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteFilesComparer
{
    class Program
    {
        static void Main(string[] args)
        {
            int n = 7;
            List<string> Routes = new List<string>();
            for (int i = 0; i < n; i++)
                Routes.Add(System.IO.File.ReadAllText(@"D:\Andrei\Scoala\LICENTA\TCCC\TrafficSimulator\TrafficSimulator\bin\Debug\TrafficParticipant" + i + "route.txt"));
            var duplicateIndexes = Routes
  .Select((t, i) => new { Index = i, Text = t })
  .GroupBy(g => g.Text)
  .Where(g => g.Count() > 1)
  .SelectMany(g => g, (g, x) => x.Index);
            foreach (var i in duplicateIndexes)
                Console.WriteLine(i);
        }
    }
}
