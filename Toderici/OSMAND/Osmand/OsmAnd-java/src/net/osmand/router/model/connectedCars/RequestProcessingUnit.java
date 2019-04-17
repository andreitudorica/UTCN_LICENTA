package net.osmand.router.model.connectedCars;

import net.osmand.binary.BinaryInspector;
import net.osmand.binary.BinaryMapIndexReader;
import net.osmand.data.LatLon;
import net.osmand.router.*;
import net.osmand.router.model.constants.RouterConstants;
import net.osmand.router.model.traffic.IntervalTree;
import net.osmand.router.model.traffic.TrafficDataSet;
import org.omg.PortableServer.RequestProcessingPolicy;

import java.io.*;
import java.util.*;
import java.util.concurrent.TimeUnit;

/**
 * Created by todericidan on 3/26/2018.
 */
public class RequestProcessingUnit {

    private RoutePlannerFrontEnd fe;
    private RoutingContext ctx;
    private int idOfProcessor;
    private TrafficDataSet dataSet;
    private boolean isWorkDone;
    private long min = 10000;
    private long max = 0 ;
    private ResultMerger merger;
    private long requestIndex=0;

    public RequestProcessingUnit(int id) {
        idOfProcessor = id;
        try {
            setUp();
        } catch (IOException e) {
            e.printStackTrace();
        }

        isWorkDone = true;
    }

    public void setMerger(ResultMerger merger){
        this.merger = merger;
    }

    public void setTraffic(TrafficDataSet traffic){
        this.dataSet = traffic;
        requestIndex = 0;
    }


    public void setUp() throws IOException {
        String fileName = "D:\\Andrei\\Scoala\\Licenta\\config\\romania.obf";

        File fl = new File(fileName);

        RandomAccessFile raf = new RandomAccessFile(fl, "r");

        this.fe = new RoutePlannerFrontEnd(false);
        RoutingConfiguration.Builder builder = RoutingConfiguration.getDefault();
        Map<String, String> params = new LinkedHashMap<String, String>();
        params.put("car", "true");
        RoutingConfiguration config = builder.build("car", RoutingConfiguration.DEFAULT_MEMORY_LIMIT * 3, params);
        BinaryMapIndexReader[] binaryMapIndexReaders = {new BinaryMapIndexReader(raf, fl)};
        this.ctx = this.fe.buildRoutingContext(config, null, binaryMapIndexReaders,
                RoutePlannerFrontEnd.RouteCalculationMode.NORMAL);
        this.ctx.leftSideNavigation = false;
        RouteResultPreparation.PRINT_TO_CONSOLE_ROUTE_INFORMATION_TO_TEST = true;

    }



    public void computeRoute(RouteRequest request) throws IOException, InterruptedException {
        isWorkDone = false;

        Date startTime= new Date(System.currentTimeMillis());//how much it took

        List<RouteSegmentResult> routeSegments = fe.searchRoute(ctx, request.getStartPoint(), request.getEndPoint(), request.getIntermediates(), dataSet, request.getDelay());
        printResultsInFile(ctx,request.getRequestID(), request.getDelay(), startTime, request.getStartPoint(), request.getEndPoint(), routeSegments);


        isWorkDone = true;
    }


    private void printResultsInFile(RoutingContext ctx,long requestId, long delay, Date startTime, LatLon start, LatLon end, List<RouteSegmentResult> result) {

        float completeDistance = 0.0f;
        float checkpointTime = 0.0f;

        long maxNbOfCars  = 0;
        float maxDensity = 0.0f;
        long nbOfCars = 0;

        //uncomment when verifications done
//        for(RouteSegmentResult r : result) {
//            checkpointTime += r.getRoutingTime();
//            completeDistance += r.getDistance();
//        }

        Date stopTime = new Date(System.currentTimeMillis());
        long responseTime;

        responseTime = stopTime.getTime() - startTime.getTime();

        if( responseTime < min){
            min = responseTime;
         }

        if( responseTime > max){
            max = responseTime;
        }

        //int numberOfSegment = 1 ;
        String previousSegmentName = "";
        FileWriter fw = null;
        BufferedWriter bw = null;
        PrintWriter out = null;

      //  FileWriter fw2 = null;
       // BufferedWriter bw2 = null;
      //  PrintWriter out2 = null;
        try {
            fw = new FileWriter("D:\\Andrei\\Scoala\\Licenta\\results\\LOGS\\PROC_"+dataSet.getAlgorithmUsed()+"_"+idOfProcessor+".txt", true);
            bw = new BufferedWriter(fw);
            out = new PrintWriter(bw);

          //  fw2 = new FileWriter("C:\\Users\\Admin\\Desktop\\LOGS\\TEST_"+dataSet.getAlgorithmUsed()+"_"+idOfProcessor+".txt", true);
          //  bw2 = new BufferedWriter(fw2);
          //  out2 = new PrintWriter(bw2);

            out.println("Route: ");

          //  out2.println("Route "+requestId+" :");

            long previousID =0;


           // System.out.println("FILELOG: ");

            for(RouteSegmentResult segment: result) {


                long lowIntervalValue =(long) (Math.ceil(checkpointTime)*2);

                long highIntervalValue = (long) (Math.ceil(checkpointTime + segment.getRoutingTime())*2);


               // long segTraffic = dataSet.queryTraffic(segment.getObject().getId(),lowIntervalValue, highIntervalValue);

//                if(segTraffic == 0){
//
//                    dataSet.addCarToTree(segment.getObject().getId(),(long)segment.getDistance(),segment.getSegmentSpeed(),(long) checkpointTime*2, (long) (checkpointTime + segment.getRoutingTime()) * 2);
//                }
//                segTraffic = dataSet.queryTraffic(segment.getObject().getId(),(long) checkpointTime*2, (long) (checkpointTime + segment.getRoutingTime()) * 2);

                String name ;
                if (segment.getObject().getName() != null && !segment.getObject().getName().isEmpty()) {
                    name = segment.getObject().getName();
                    previousSegmentName = name;
                }else{
                    name = previousSegmentName;
                }

                //Density compute
                long traffic = 0;
                float distance = 0.0f;


               // out2.println("INTERVAL: "+lowIntervalValue+" - "+highIntervalValue);

                float time = 0.0f;

                long cars = 0;

                for(RouteSegmentResult densitySegment: result) {

                    if(previousID != densitySegment.getObject().getId()){
                      //  if(lowIntervalValue<=((long) Math.ceil(time)*2) && lowIntervalValue>=((long) Math.ceil(time+ densitySegment.getRoutingTime())*2)) {
                        traffic += dataSet.queryTraffic(densitySegment.getObject().getId(), lowIntervalValue+1, highIntervalValue-1);
                        cars = dataSet.queryTraffic(densitySegment.getObject().getId(), lowIntervalValue+1, highIntervalValue-1);
                      //  }
                    }



                    distance += densitySegment.getDistance();

//                    out2.println("ID: "+densitySegment.getObject().getId()+" (" +(long) (Math.ceil(time)*2)+ " ; "+ (long) (Math.ceil(time + densitySegment.getRoutingTime())*2) +")"+
//                            " CARS: "+cars
//                            +" LNG: "+densitySegment.getDistance()
//                            +" DENSITY: "+cars * RouterConstants.VEHICLE_LENGTH / densitySegment.getDistance());

                    previousID = densitySegment.getObject().getId();
                    time += densitySegment.getRoutingTime();
                }

                //out2.println();

                if(maxDensity < (traffic* RouterConstants.VEHICLE_LENGTH / distance)){
                    if(distance != 0) {
                        maxDensity = traffic * RouterConstants.VEHICLE_LENGTH / distance;
                        maxNbOfCars = traffic;
                    }
                }

               // updateTraffic(segment,checkpointTime);
               // System.out.println("id: " + String.valueOf(segment.getObject().getId())+"  "+dataSet.queryDensity(segment.getObject().getId(),1,(long)checkpointTime));

//                float segDensity = dataSet.queryDensity(segment.getObject().getId(),
//                        (long) checkpointTime*2,
//                        (long) (checkpointTime + segment.getRoutingTime())*2);

                //(float) dataSet.queryTraffic(segment.getObject().getId(),(long) checkpointTime*2,
                // (long) (checkpointTime + segment.getRoutingTime()) * 2) / segment.getDistance();


//                if(segDensity == 0.0f){
//                    System.out.println(segment.getDistance()+" "+segDensity+" "+
//                            dataSet.queryTraffic(segment.getObject().getId(),(long) checkpointTime*2, (long) (checkpointTime + segment.getRoutingTime()) * 2)
//                    );
//                }

                //float segDensity = segTraffic*RouterConstants.VEHICLE_LENGTH/segment.getDistance();
                if(maxDensity > 1.0f){
                    maxDensity = 1.0f;
                }
//
//                maxDensity += segDensity;

                out.print("id:" + String.valueOf(segment.getObject().getId()));
                out.print(" name:" + name);
                out.print("| distance:" + segment.getDistance());
                out.print(" routeTime:" + segment.getRoutingTime());
                out.print(" speed(m/s):" + String.valueOf(segment.getDistance()/segment.getRoutingTime()));
                out.print(" density:" + maxDensity);
               // out.print(" traffic:" + maxNbOfCars);
                out.println("");

                //numberOfSegment++;
               // nbOfCars += dataSet.queryTraffic(segment.getObject().getId(),lowIntervalValue, highIntervalValue) ;


               // long segTraffic = traffic.get
                //CHECK PURPOSES
               //System.out.println("ID: "+segment.getObject().getId()+" | ("+lowIntervalValue+" ; "+highIntervalValue+")"+" TRAFFIC "+segTraffic);

                checkpointTime +=(long) segment.getRoutingTime() ;
                completeDistance += segment.getDistance();

            }

            out.println("Routing TIME "+checkpointTime);

            out.println("");

            out.close();

          //  out2.close();
          //  bw2.close();
          //  fw2.close();

        } catch (IOException e) {
        }
        finally {
            if(out != null)
                out.close();
            try {
                if(bw != null)
                    bw.close();
            } catch (IOException e) {
            }
            try {
                if(fw != null)
                    fw.close();
            } catch (IOException e) {
            }
        }


        maxDensity = maxDensity/result.size();

        RouteMetrics routeMetrics = new RouteMetrics(requestIndex, result,responseTime,
                (long) checkpointTime, (completeDistance/checkpointTime), maxDensity, maxNbOfCars, completeDistance);


        merger.setResultAtIndex((int)requestIndex,routeMetrics);
        requestIndex++;



    }


    public TrafficDataSet getDataSet() {
        return dataSet;
    }


    public boolean isWorkDone(){
        return isWorkDone;
    }


    public long getMin() {
        return min;
    }

    public long getMax() {
        return max;
    }

    public static void main(String[] args) {
        TrafficDataSet dataSet = new TrafficDataSet();
        dataSet.setAlgorithmUsed("");
        ResultMerger merger =new ResultMerger();
        try {
            RequestProcessingUnit unit = new RequestProcessingUnit(1);
            unit.setMerger(merger);
            unit.setTraffic(dataSet);
            unit.computeRoute(new RouteRequest(1,new LatLon(46.768773,23.618254),new LatLon(46.77213,23.5576253),0));
        } catch (IOException e) {
            e.printStackTrace();
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
        for(IntervalTree tree:dataSet.getTraffic().values()){
            System.out.println("ID "+tree.getId()+" traffic "+ tree.query(1,1,5*600,1,5*600));

        }

    }


}
