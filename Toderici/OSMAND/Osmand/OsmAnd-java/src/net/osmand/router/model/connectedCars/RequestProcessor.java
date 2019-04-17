package net.osmand.router.model.connectedCars;


import net.osmand.router.model.constants.RouterConstants;
import net.osmand.router.model.traffic.TrafficDataSet;

import java.io.BufferedWriter;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.LinkedList;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Created by todericidan on 3/25/2018.
 */
public class RequestProcessor implements Runnable {

    private BlockingQueue<RouteRequest> blockingQueue;
    private LinkedList<RouteRequest> processedRequests = new LinkedList<RouteRequest>();
    private RequestProcessingUnit processingUnit;

    /*There is a need to limit the number of request
    that can be hold in line to be processed by a processor.
    If the threshold is exceeded another processor is created
    */
    private AtomicInteger waitingNumberOfRequests;



    /*
    avgWaitingTime = willStartProcessingTime - creationTime (how much time it takes until the route calculation begins)
    avgServiceTime average time it takes for one route to be computed
     */
    private float avgWaitingTime, avgServiceTime;

    private long creationTime, terminationTime;
    private int id;
    private RouteRequest requestInProcess;
    private TrafficDataSet dataSet;
    private ResultMerger merger;


    public RequestProcessor(int id) {
        this.id = id;
        this.creationTime = System.currentTimeMillis();
        this.waitingNumberOfRequests = new AtomicInteger(0);
        this.blockingQueue = new LinkedBlockingQueue<RouteRequest>();
        this.processingUnit = new RequestProcessingUnit(id);
    }

    public void setMerger(ResultMerger merger){
        this.merger = merger;
        this.processingUnit.setMerger(merger);
    }

    public void setTraffic(TrafficDataSet traffic) {
        this.dataSet = traffic;
        this.processingUnit.setTraffic(traffic);
    }

    @Override
    public void run() {


        while (true) {

            RouteRequest request;
            try {
                request = blockingQueue.take();
                request.startProcessing();
                requestInProcess = request;

               // ROUTE CALCULATION

                processingUnit.computeRoute(request);

                processedRequests.add(request);
                waitingNumberOfRequests.addAndGet(-1);

                computeAvgWaitingTime();
                computeAvgServiceTime();

            }
            catch (InterruptedException e) {
                e.printStackTrace();
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
    }

    public void setId(int id)
    {
        this.id = id;
    }

    public int getId()
    {
        return id;
    }

    public long getCreationTime()
    {
        return creationTime;
    }

    public long getTerminationTime()
    {
        return terminationTime;
    }


    public float getAvgWaitingTime()
    {
        return avgWaitingTime;
    }

    public float getAvgServiceTime()
    {
        return avgServiceTime;
    }



    private void computeAvgServiceTime()
    {
        if(processedRequests.size()>0)
        {
            long sum =0;
            for(RouteRequest request: processedRequests)
            {
                if(request.hasFinishedProcessing()) {
                    sum = sum + (request.getTimeTookToCompute() - request.getCreationTime());
                }
            }

            avgServiceTime = (float)sum / processedRequests.size();
           // System.out.println("AVG_SERVICE_TIME "+avgServiceTime+" SIZE "+ processedRequests.size());
        }
    }

    private void computeAvgWaitingTime()
    {
        if(processedRequests.size()>0)
        {
            long sum =0;
            for(RouteRequest request: processedRequests) {
                sum = sum +(request.getStartProcessingTime()-request.getCreationTime());
            }

            avgWaitingTime =(float) sum / processedRequests.size();

        }
    }

    public boolean isEmpty() {

        return (blockingQueue.isEmpty()&& processingUnit.isWorkDone());
    }


    public RouteRequest getRequestInProcess() {
        return requestInProcess;
    }


    public String generateStringOfProcessedRequests()
    {
        StringBuilder stringBuilder = new StringBuilder("");
        stringBuilder.append("SERVER "+id+":"+'\n');

        for(RouteRequest request: processedRequests) {
            stringBuilder.append(request.getRequestID() +"- waited "+request.getTimeWaitedUntilProcessed()+" -"+" DONE!"+'\n');
        }
        stringBuilder.append("AVG_WAITING_TIME "+getAvgWaitingTime()/1000+'\n');
        stringBuilder.append("AVG_SERVICE_TIME "+getAvgServiceTime());

        return stringBuilder.toString();
    }



    public boolean addRequest(RouteRequest request) {
        if(!hasReachedThreshold()) {
            blockingQueue.add(request);
            waitingNumberOfRequests.addAndGet(1);
            return true;
        }
        return false;
    }

    public RouteRequest[] getRequests(){

        RouteRequest[] requests= new RouteRequest[blockingQueue.size()];
        blockingQueue.toArray(requests);

        return requests;
    }

    public int getNbOfWaitingRequestes()
    {
        return waitingNumberOfRequests.get();
    }


    public boolean hasReachedThreshold() {
        int aux = waitingNumberOfRequests.intValue();
        if(aux >= RouterConstants.MAX_NB_OF_REQS_PROCESSING) {
            return true;
        }
        return false;
    }



}
