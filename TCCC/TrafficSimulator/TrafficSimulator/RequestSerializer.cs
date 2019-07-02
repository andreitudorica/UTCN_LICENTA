using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TrafficSimulator.Models;

namespace TrafficSimulator
{
    public class RequestSerializer
    {
        public List<TrafficParticipant> trafficParticipants;
        public List<RequestModel> routeRequests;
        public List<RequestModel> updateRequests;
        private HttpClient routeHttpClient { get; set; }
        private HttpClient updateHttpClient { get; set; }
        public TimeSpan averageRouteRequestWaitTime { get; set; }
        public TimeSpan maxRouteRequestWaitTime { get; set; }
        public TimeSpan averageUpdateRequestWaitTime { get; set; }
        public int numberOfRouteRequests { get; set; }
        public int numberOfUpdateRequests { get; set; }
        public int numberOfFailedRouteRequests { get; set; }
        public int numberOfFailedUpdateRequests { get; set; }

        public RequestSerializer(List<TrafficParticipant> participants)
        {
            trafficParticipants = participants;
            routeRequests = new List<RequestModel>();
            updateRequests = new List<RequestModel>();
            routeHttpClient = new HttpClient();
            routeHttpClient.Timeout = new TimeSpan(1, 0, 0, 0);
            updateHttpClient = new HttpClient();
            updateHttpClient.Timeout = new TimeSpan(1, 0, 0, 0);
        }

        public void AddRouteRequest(RequestModel rrm)
        {
            routeRequests.Add(rrm);
        }

        public void AddUpdateRequest(RequestModel rrm)
        {
            updateRequests.Add(rrm);
        }

        public async void ProcessRouteRequests()
        {

            while (true)
            {
                string route;
                RouteModel newRoute;
                int i;
                try
                {
                    if (routeRequests.Count > 0)
                    {
                        for (i = 0; i < routeRequests.Count; i++)
                        {
                            bool goodRouteReceived = false;
                            while (!goodRouteReceived)
                            {
                                var time = DateTime.Now;
                                using (var response = await routeHttpClient.GetAsync(routeRequests[i].request))
                                {
                                    try
                                    {
                                        route = await response.Content.ReadAsStringAsync();
                                        newRoute = JsonConvert.DeserializeObject<RouteModel>(route);
                                        if (newRoute.features != null)
                                        {
                                            numberOfRouteRequests++;
                                            if (maxRouteRequestWaitTime < DateTime.Now - time)
                                                maxRouteRequestWaitTime = DateTime.Now - time;
                                            averageRouteRequestWaitTime = TimeSpan.FromTicks((averageRouteRequestWaitTime.Ticks * (numberOfRouteRequests - 1) + (DateTime.Now - time).Ticks) / numberOfRouteRequests);
                                            goodRouteReceived = true;
                                            foreach (TrafficParticipant tp in trafficParticipants)
                                                if (tp.ID == routeRequests[i].trafficParticipantID)
                                                {
                                                    tp.newRoute = newRoute;

                                                    tp.status = "other";
                                                    tp.routeRequestSerializerResponse = route;
                                                    break;
                                                }
                                            routeRequests.RemoveAt(i);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        numberOfFailedRouteRequests++;
                                    }
                                }
                            }
                            i--;
                        }
                    }
                }
                catch (Exception e)
                {

                    System.IO.File.AppendAllText(Simulation.ErrorLogsFile, e.Message + "\n");
                }
            }
        }

        public async void ProcessUpdateRequests()
        {
            while (true)
            {
                HttpResponseMessage response;
                int i, j;
                try
                {
                    if (updateRequests.Count > 0)
                    {
                        for (i = 0; i < updateRequests.Count; i++)
                        {
                            bool goodResponse = false;
                            while (!goodResponse)
                            {
                                var time = DateTime.Now;
                                using (response = await updateHttpClient.GetAsync(updateRequests[i].request))
                                {
                                    if (response.IsSuccessStatusCode)
                                    {
                                        numberOfUpdateRequests++;
                                        averageUpdateRequestWaitTime = TimeSpan.FromTicks((averageUpdateRequestWaitTime.Ticks * (numberOfUpdateRequests - 1) + (DateTime.Now - time).Ticks) / numberOfUpdateRequests);
                                        for (j = 0; j < trafficParticipants.Count; j++)
                                        {
                                            if (trafficParticipants[j].ID == updateRequests[i].trafficParticipantID)
                                            {
                                                trafficParticipants[j].status = "other";
                                                trafficParticipants[j].updated = true;
                                                goodResponse = true;
                                                updateRequests.RemoveAt(i);
                                                break;
                                            }
                                        }
                                        i--;
                                    }
                                    else
                                        numberOfFailedUpdateRequests++;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {

                    System.IO.File.AppendAllText(Simulation.ErrorLogsFile, e.Message + "\n");
                }
            }

        }
    }
}
