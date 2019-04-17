package net.osmand.router.model;

import net.osmand.router.RouteSegmentResult;
import net.osmand.router.model.connectedCars.*;
import net.osmand.router.model.connectedCars.RequestProcessingUnit;
import net.osmand.router.model.connectedCars.RequestScheduler;
import net.osmand.router.model.connectedCars.RouteRequest;
import net.osmand.router.model.constants.RouterConstants;
import net.osmand.router.model.randomGenerators.CityRouteRequestsGenerator;
import net.osmand.router.model.randomGenerators.RequestFileHandler;
import net.osmand.router.model.traffic.TrafficDataSet;
import org.junit.Before;
import org.junit.Test;

import java.io.*;
import java.util.*;
import java.util.concurrent.TimeUnit;

/**
 * Created by todericidan on 4/18/2018.
 */
public class SeqVsParallelTests {


    private CityRouteRequestsGenerator generator;
    private RequestScheduler scheduler;
    private List<RouteRequest> requests;
    private RequestProcessingUnit unit;
    private long seqTime;
    private long parallelTime;
    private TrafficDataSet trafficSeq,trafficCCRA,trafficBRA;
    private ResultMerger ccraMerger, braMerger;

    private long seqNB = 0, parallelNB = 0;


    @Before
    public void setUp()  {
        trafficSeq = new TrafficDataSet();
        trafficCCRA = new TrafficDataSet();
        trafficBRA = new TrafficDataSet();

        braMerger = new ResultMerger();
        ccraMerger = new ResultMerger();


        unit = new RequestProcessingUnit(-1);
        unit.setMerger(ccraMerger);
        unit.setTraffic(trafficSeq);

        generator = new CityRouteRequestsGenerator();

        //PALALLEL PROCESSING
       // scheduler = new RequestScheduler();
       // Thread th = new Thread(scheduler);
       // th.start();


        //CHANGED
        //****************************************************
        //random request not in file but generated here
        //requests = generator.generateConcurrentRequests();

        //requests from file

        //
//        requests = new ArrayList<RouteRequest>();
//        RouteRequest request =  RequestFileHandler.getRequests(RouterConstants.REQUEST_FILE).get(0);
//        for(int i =0; i < 10;i++) {
//            //request.setDelay(i*10);
//            RouteRequest r = new RouteRequest(i,request.getStartPoint(),request.getEndPoint(),i*0);
//            requests.add(r);
//        }
//        System.out.println(requests);

        //partea de sus ia primul request de mai multe ori sa vezi daca densitatile sunt bune (mai multe masini din punctul a in punctul b)
        //partea de jos ia toate rutele
       requests = RequestFileHandler.getRequests(RouterConstants.REQUEST_FILE);
        int i =0;
        for(RouteRequest request : requests) {
            request.setDelay(i*0);
            i++;
        }
        System.out.println(requests);



        //****************************************************
    }


    public void braAlgorithmTest()throws IOException, InterruptedException{
       // trafficSeq.deleteTraffic();
       // trafficSeq.setAlgorithmUsed("BRA");

       // trafficParallel.deleteTraffic();
       // trafficSeq.setAlgorithmUsed("BRA");
       // trafficParallel.setAlgorithmUsed("BRA");

        trafficBRA.deleteTraffic();
        trafficBRA.setAlgorithmUsed("BRA");

        //PALALLEL
//        scheduler.setMerger(braMerger);
//        scheduler.setTraffic(trafficBRA);


        unit.setTraffic(trafficBRA);
        unit.setMerger(braMerger);

        seqNB = 0;
        parallelNB = 0;


        sequentialTest();

      //  System.out.println("Sequential BRA test "+seqNB+" min "+unit.getMin()+" max "+unit.getMax());

       // parallelTest("BRA");

       // System.out.println("Parallel BRA test "+parallelNB);
    }


    public void ccraAlgorithmTest()throws IOException, InterruptedException{
        //trafficSeq.deleteTraffic();
       // trafficSeq.setAlgorithmUsed("CCRA");


      //  trafficParallel.deleteTraffic();
       // trafficSeq.setAlgorithmUsed("CCRA");
       // trafficParallel.setAlgorithmUsed("CCRA");

        trafficCCRA.deleteTraffic();
        trafficCCRA.setAlgorithmUsed("CCRA");

        //PALALLEL
//        scheduler.setMerger(ccraMerger);
//        scheduler.setTraffic(trafficCCRA);

        seqNB = 0;
        parallelNB = 0;

        unit.setMerger(ccraMerger);
        unit.setTraffic(trafficCCRA);


        sequentialTest();

       // System.out.println("Sequential CCRA test "+seqNB+" min "+unit.getMin()+" max "+unit.getMax());

        //parallelTest("CCRA");

       // System.out.println("Parallel CCRA test "+parallelNB);

    }

    public void parallelTest(String algorithm)throws IOException, InterruptedException{
        Date startTime= new Date(System.currentTimeMillis());
        System.out.println("PARALLEL START "+ startTime.toString());
        //System.out.println(requests);

        scheduler.putRequestsInWaitingList(requests);

        while(true){
            if(scheduler.areAllRequestsDone()){
                break;
            }
        }

        Date stopTime= new Date(System.currentTimeMillis());
        //System.out.println("PARALLEL STOP "+ stopTime.toString());

        parallelTime = stopTime.getTime() - startTime.getTime();
       System.out.println("PARALLEL TOOK "+ TimeUnit.MILLISECONDS.toSeconds(parallelTime) + " SECONDS");


        TrafficDataSet trafficParallel = null;

        if(algorithm.equals("BRA")){
            trafficParallel = trafficBRA;
        }else{
            trafficParallel = trafficCCRA;
        }


    }


    public void sequentialTest() throws IOException, InterruptedException {
        Date startTime= new Date(System.currentTimeMillis());

        for(RouteRequest request : requests){

            unit.computeRoute(request);
        }

        Date stopTime= new Date(System.currentTimeMillis());

        seqTime = stopTime.getTime() - startTime.getTime();
        System.out.println("SEQ TOOK "+TimeUnit.MILLISECONDS.toSeconds(seqTime)+" SECONDS");

    }

    @Test
    public void mainTest()throws IOException, InterruptedException{

        braAlgorithmTest();
        ccraAlgorithmTest();

        //writeMetricsForOneAlgorithm();

        writeMetrics();

    }

//    private void writeMetricsForOneAlgorithm() {
//        String responseTimeFile = "C:\\Users\\Admin\\Desktop\\LOGS\\ResponseTimeCCRA.csv";
//        String etasFile = "C:\\Users\\Admin\\Desktop\\LOGS\\ETAsCCRA.csv";
//        String speedsFile = "C:\\Users\\Admin\\Desktop\\LOGS\\SpeedsCCRA.csv";
//        String densitiesFile = "C:\\Users\\Admin\\Desktop\\LOGS\\DensitiesCCRA.csv";
//        String trafficFile = "C:\\Users\\Admin\\Desktop\\LOGS\\TrafficCCRA.csv";
//        String infoFile = "C:\\Users\\Admin\\Desktop\\LOGS\\INFO_CCRA.txt";
//
//        PrintWriter responseTimeWriter = null;
//        PrintWriter etasWriter = null;
//        PrintWriter speedsWriter = null;
//        PrintWriter densitiesWriter = null;
//        PrintWriter trafficWriter = null;
//        PrintWriter infoWriter = null;
//
//
//        try {
//            responseTimeWriter = new PrintWriter(new File(responseTimeFile));
//            etasWriter = new PrintWriter(new File(etasFile));
//            speedsWriter = new PrintWriter(new File(speedsFile));
//            densitiesWriter = new PrintWriter(new File(densitiesFile));
//            trafficWriter = new PrintWriter(new File(trafficFile));
//            infoWriter= new PrintWriter(new File(infoFile));
//
//        } catch (FileNotFoundException e) {
//            e.printStackTrace();
//        }
//
//        densitiesWriter.write("Segment_ID, Density"+'\n');
//        trafficWriter.write("Segment_ID, Length, Traffic"+'\n');
//
//        long etaSum = 0;
//
//        float avgSpeedSum = 0.0f;
//
//        long trafficSum = 0;
//
//        float densitySum = 0.0f;
//
//        float minDensity = 2.0f;
//        float maxDensity = 0.0f;
//
//
//        long nbOfIDs=0;
//
//        Set<String> ids;
//
//        for (int i = 0; i < RouterConstants.SIM_NB_OF_REQUESTS ; i++) {
//
//            RouteMetrics metric = ccraMerger.getResultAtIndex(i);
//
//            //Response time
//            StringBuilder sb = new StringBuilder();
//            sb.append(metric.getComputationTime());
//            sb.append('\n');
//
//            responseTimeWriter.write(sb.toString());
//
//            //ETAs
//            sb = new StringBuilder();
//            sb.append(metric.getEta());
//            sb.append('\n');
//
//            etasWriter.write(sb.toString());
//
//            //Speeds
//            sb = new StringBuilder();
//            sb.append(metric.getAverageSpeed());
//            sb.append('\n');
//
//            speedsWriter.write(sb.toString());
//
//
////            //Densities
////            ids = braMerger.getSegmentIds();
////            for(Long id: ids) {
////                float braDensity = trafficBRA.queryDensity(id,1,
////                        RouterConstants.ROUTE_MAX_TIME_SPAN_IN_MINUTES * RouterConstants.MULTIPLYING_FACTOR) ;
////
////                if (braDensity > braMaxDensity) {
////                    braMaxDensity = braDensity;
////                }
////                if (braDensity < braMinDensity) {
////                    braMinDensity = braDensity;
////                }
////            }
////
////            ids = ccraMerger.getSegmentIds();
////            for(Long id: ids) {
////                float ccraDensity = trafficCCRA.queryDensity(id,1,
////                        RouterConstants.ROUTE_MAX_TIME_SPAN_IN_MINUTES * RouterConstants.MULTIPLYING_FACTOR) ;
////
////                if (ccraDensity > ccraMaxDensity) {
////                    ccraMaxDensity = ccraDensity;
////                }
////                if (ccraDensity < ccraMinDensity) {
////                    ccraMinDensity = ccraDensity;
////                }
////            }
////
////
////            for(Long id: ccraMerger.getSegmentIds()){
////                ids.add(id);
////            }
////
////            for(Long id: ids){
////                float braDensity = trafficBRA.queryDensity(id,1,
////                                 RouterConstants.ROUTE_MAX_TIME_SPAN_IN_MINUTES * RouterConstants.MULTIPLYING_FACTOR) ;
////                float ccraDensity = trafficCCRA.queryDensity(id,1,
////                        RouterConstants.ROUTE_MAX_TIME_SPAN_IN_MINUTES * RouterConstants.MULTIPLYING_FACTOR) ;
////
////
////                sb = new StringBuilder();
////                sb.append(id);
////                sb.append(',');
////                sb.append(braDensity);
////                sb.append(',');
////                sb.append(ccraDensity);
////                sb.append('\n');
////
////                densitiesWriter.write(sb.toString());
////
////                braDensitySum += braDensity;
////                ccraDensitySum += ccraDensity;
////
////            }
//
//            //Traffic
//            ids = ccraMerger.getSegmentIds();
//
//            for(Long id: ids){
//                long traffic = trafficCCRA.queryTraffic(id,1,
//                        RouterConstants.ROUTE_MAX_TIME_SPAN_IN_MINUTES * RouterConstants.MULTIPLYING_FACTOR) ;
//
//                long length = trafficCCRA.getLength(id);
//
//                if(length==0){
//                    length = trafficBRA.getLength(id);
//                }
//
//                sb = new StringBuilder();
//                sb.append(id);
//                sb.append(',');
//                sb.append(length);
//                sb.append(',');
//                sb.append(traffic);
//                sb.append('\n');
//
//                trafficWriter.write(sb.toString());
//                float density;
//
//                if(length !=0) {
//                    density = (((float)RouterConstants.VEHICLE_LENGTH * traffic) /(float) length);
//
//                }else{
//                    density = 0.0f;
//                }
//
//                if(density>1.0f){
//                    density = 1.0f;
//                }
//
//                if (density > maxDensity) {
//                    maxDensity = density;
//                }
//                if (density < minDensity) {
//                    minDensity = density;
//                }
//
//
//                sb = new StringBuilder();
//                sb.append(id);
//                sb.append(',');
//                sb.append(density);
//                sb.append('\n');
//
//                densitiesWriter.write(sb.toString());
//
//
//                densitySum += density;
//
//                trafficSum += traffic;
//
//                nbOfIDs++;
//            }
//
//            //Numbers
//            etaSum += metric.getEta();
//
//            avgSpeedSum += metric.getAverageSpeed();
//
//        }
//
//        avgSpeedSum = avgSpeedSum/(float)RouterConstants.SIM_NB_OF_REQUESTS;
//
//        trafficSum = (long)(trafficSum/(float)nbOfIDs);
//
//
//        float avgDensity = ((float) densitySum/(float)nbOfIDs);
//
//
//        infoWriter.println("Sum ETAs for CCRA "+etaSum);
//        infoWriter.println("Avg of speeds for CCRA "+avgSpeedSum);
//        infoWriter.println("Nb of segments touched by CCRA "+ ccraMerger.getNumberOfSegments());
//        infoWriter.println("Avg of traffic for CCRA "+ trafficSum);
//        infoWriter.println("sum of densities for CCRA "+ densitySum);
//        infoWriter.println("Nb of segments "+ nbOfIDs);
//        infoWriter.println("Avg of densities for CCRA "+ avgDensity);
//        infoWriter.println("Min of density for CCRA "+ minDensity);
//        infoWriter.println("Max of density for CCRA "+ maxDensity);
//
//        responseTimeWriter.close();
//        etasWriter.close();
//        speedsWriter.close();
//        densitiesWriter.close();
//        trafficWriter.close();
//        infoWriter.close();
//
//        System.out.println(nbOfIDs);
//
//    }


    public void writeMetrics(){


        PrintWriter responseTimeWriter = null;
        PrintWriter etasWriter = null;
        PrintWriter speedsWriter = null;
        PrintWriter densitiesWriter = null;
        PrintWriter routeNbOfSegsWriter  = null;
        PrintWriter infoWriter = null;


        try {
            responseTimeWriter = new PrintWriter(new File(RouterConstants.RESPONSE_TIME_FILE));
            etasWriter = new PrintWriter(new File(RouterConstants.ETAS_FILE));
            speedsWriter = new PrintWriter(new File(RouterConstants.SPEEDS_FILE));
            densitiesWriter = new PrintWriter(new File(RouterConstants.DENSITIES_FILE));
            routeNbOfSegsWriter = new PrintWriter(new File(RouterConstants.ROUTE_NB_OF_SEGS_FILE));
            infoWriter= new PrintWriter(new File(RouterConstants.INFO_FILE));

        } catch (FileNotFoundException e) {
            e.printStackTrace();
        }

        responseTimeWriter.write("BRA, CCRA"+'\n');
        etasWriter.write("BRA, CCRA"+'\n');
        speedsWriter.write("BRA, CCRA"+'\n');
        densitiesWriter.write("BRA_Density, CCRA_Density, BRA_NBofCars, CCRA_NbofCars, BRA_Length, CCRA_Length"+'\n');

        long braETAsSum = 0;
        long ccraETAsSum = 0;

        float braAvgSpeedSum = 0.0f;
        float ccraAvgSpeedSum = 0.0f;

        long braTrafficSum = 0;
        long ccraTrafficSum = 0;

        float braDensitySum = 0.0f;
        float ccraDensitySum = 0.0f;


        Set<Long> braSegmentIds = new HashSet<Long>();
        Set<Long> ccraSegmentIds = new HashSet<Long>();
        Set<Long> segmentIds = new HashSet<Long>();

        StringBuilder sb;

        for (int i = 0; i < RouterConstants.SIM_NB_OF_REQUESTS ; i++) {

            RouteMetrics braMetric = braMerger.getResultAtIndex(i);
            RouteMetrics ccraMetric = ccraMerger.getResultAtIndex(i);

            //Response time
            sb = new StringBuilder();
            sb.append(braMetric.getComputationTime());
            sb.append(',');
            sb.append(ccraMetric.getComputationTime());
            sb.append('\n');

            responseTimeWriter.write(sb.toString());

            //ETAs
            sb = new StringBuilder();
            sb.append(braMetric.getEta());
            sb.append(',');
            sb.append(ccraMetric.getEta());
            sb.append('\n');

            etasWriter.write(sb.toString());

            //Speeds
            sb = new StringBuilder();
            sb.append(braMetric.getAverageSpeed());
            sb.append(',');
            sb.append(ccraMetric.getAverageSpeed());
            sb.append('\n');

            speedsWriter.write(sb.toString());

            //Nb of Segs
            sb = new StringBuilder();
            sb.append(braMetric.getSegmentList().size());
            sb.append(',');
            sb.append(ccraMetric.getSegmentList().size());
            sb.append('\n');

            routeNbOfSegsWriter.write(sb.toString());

            //Density
            sb = new StringBuilder();
            sb.append(braMetric.getAverageDensity());
            sb.append(',');
            sb.append(ccraMetric.getAverageDensity());
            sb.append(',');
            sb.append(braMetric.getTotalNbOfCars());
            sb.append(',');
            sb.append(ccraMetric.getTotalNbOfCars());
            sb.append(',');
            sb.append(braMetric.getLengthOfRoute());
            sb.append(',');
            sb.append(ccraMetric.getLengthOfRoute());
            sb.append('\n');

            densitiesWriter.write(sb.toString());

            //fetch Ids bra
           for(RouteSegmentResult segment: braMetric.getSegmentList()){
               Long id = segment.getObject().getId();

               braSegmentIds.add(id);
               segmentIds.add(id);
           }

            for(RouteSegmentResult segment: ccraMetric.getSegmentList()){
                Long id = segment.getObject().getId();

                ccraSegmentIds.add(id);
                segmentIds.add(id);
            }

            //Numbers
            braETAsSum += braMetric.getEta();
            ccraETAsSum += ccraMetric.getEta();

            braAvgSpeedSum += braMetric.getAverageSpeed();
            ccraAvgSpeedSum += ccraMetric.getAverageSpeed();

            braDensitySum += braMetric.getAverageDensity();
            ccraDensitySum += ccraMetric.getAverageDensity();
        }



        braAvgSpeedSum = braAvgSpeedSum/(float)RouterConstants.SIM_NB_OF_REQUESTS;
        ccraAvgSpeedSum = ccraAvgSpeedSum/(float)RouterConstants.SIM_NB_OF_REQUESTS;

        float braTrafficAvg = (long)(braTrafficSum/(float)braSegmentIds.size());
        float ccraTrafficAvg = (long)(ccraTrafficSum/(float)ccraSegmentIds.size());



        float braAvgDensity = (braDensitySum/(float)RouterConstants.SIM_NB_OF_REQUESTS);
        float ccraAvgDensity = (ccraDensitySum/(float)RouterConstants.SIM_NB_OF_REQUESTS);


        infoWriter.println("Sum ETAs for BRA "+braETAsSum);
        infoWriter.println("Sum ETAs for CCRA "+ccraETAsSum);
        infoWriter.println("Avg of speeds for BRA "+braAvgSpeedSum);
        infoWriter.println("Avg of speeds for CCRA "+ccraAvgSpeedSum);
        infoWriter.println("Nb of segments touched by BRA "+ braSegmentIds.size());
        infoWriter.println("Nb of segments touched by CCRA "+ ccraSegmentIds.size());
        infoWriter.println("Avg of densities for BRA "+ braAvgDensity);
        infoWriter.println("Avg of densities for CCRA "+ ccraAvgDensity);

        responseTimeWriter.close();
        etasWriter.close();
        speedsWriter.close();
        densitiesWriter.close();
        infoWriter.close();
        routeNbOfSegsWriter.close();

       // System.out.println(segmentIds.size());

    }




}
