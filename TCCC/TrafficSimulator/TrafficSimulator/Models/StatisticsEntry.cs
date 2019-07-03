using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficSimulator.Models
{
    public class StatisticsEntry
    {
        public TimeSpan totalTime;
        public TimeSpan averageTime;
        public double averageTouchedFeatures;
        public double averageTouchedSegments;
        public double averageSpeed;
        public TimeSpan totalUpdateWaitTime;
        public TimeSpan totalRouteRequestWaitTime;
        public TimeSpan averageUpdateWaitTime;
        public TimeSpan averageRouteRequestWaitTime;
        public TimeSpan averageTimeWithoutWaiting;
        public int threshold;
    }
}
