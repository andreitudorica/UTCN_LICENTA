package net.osmand.router.model.connectedCars;

import net.osmand.router.model.constants.RouterConstants;
import net.osmand.router.model.traffic.TrafficDataSet;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Created by todericidan on 3/25/2018.
 */
public class RequestScheduler implements Runnable {

    //this queue contains all the received request that we have to dispatch
    private BlockingQueue<RouteRequest> requestsList;
    //list of all processing units we have
    private ArrayList<RequestProcessor> processorsList;
    private ResultMerger merger;


    private AtomicInteger idServer;

    //only the ones that are started
    private AtomicInteger nbOfProcessorsAvailable;


    private TrafficDataSet traffic;


    public RequestScheduler() {
        init();
    }


    public void setTraffic(TrafficDataSet traffic){
        this.traffic = traffic;
        for(RequestProcessor processor: processorsList){
            processor.setTraffic(traffic);
        }
    }

    public void setMerger(ResultMerger merger){
        this.merger = merger;
        for(RequestProcessor processor: processorsList){
            processor.setMerger(merger);
        }
    }




    public void init(){

        idServer = new AtomicInteger(0);

        requestsList = new LinkedBlockingQueue<RouteRequest>();

        processorsList = new ArrayList<RequestProcessor>();


        RequestProcessor processor = new RequestProcessor(idServer.intValue());
        //idServer.getAndAdd(idServer.intValue()+1);
        processorsList.add(processor);
        nbOfProcessorsAvailable = new AtomicInteger(1);

        // start first processor
        Thread thread = new Thread(processor);
        thread.start();
    }




    @Override
    public void run() {
        while (true) {
            int size = processorsList.size();
            if (size != 0) {
                // System.out.println(size);
                for (int i = 0; i < size; i++) {
                    if (size == 0) {
                        break;
                    }
                    int processorID = getAServerToDispatchOn();

                    if (processorID != -1) {
                        RouteRequest request = null;

                        try {
                            request = requestsList.take();
                        } catch (InterruptedException e) {
                            e.printStackTrace();
                        }


                        RequestProcessor processor = processorsList.get(processorID);
                        processor.addRequest(request);

                    }
                }
            }
        }
    }

    private int getAServerToDispatchOn() {

        boolean canBeDispatchedOn = false;

        for (RequestProcessor processor : processorsList) {
            if (!processor.hasReachedThreshold()) {

                canBeDispatchedOn = true;
               // System.out.println("Can be sent to server " + processor.getId());
                return processor.getId();
            }
            else {
               // System.out.println("Server " + processor.getId() + " is full!");
            }
        }

        if (!canBeDispatchedOn && nbOfProcessorsAvailable.get() <= RouterConstants.MAX_NB_OF_PROCESSING_UNITS) {
           // System.out.println("A new server must be created!");
            this.idServer.addAndGet(1);
            nbOfProcessorsAvailable.addAndGet(1);

            RequestProcessor processor = new RequestProcessor(idServer.get());
            processor.setMerger(merger);
            processor.setTraffic(traffic);

            Thread thread1 = new Thread(processor);
            thread1.start();

            processorsList.add(processor);
            //System.out.println("Can be sent to server " + server.getId());

            // System.out.println("Server " + s.getId() + " was to");

            return processor.getId();
        }
        else {
           // System.out.println("Cannot find any server that has space left!");

            try {
                Thread.sleep(1000);
            } catch (InterruptedException e) {
                // TODO Auto-generated catch block
                e.printStackTrace();
            }
        }
        return -1;
    }


    public boolean areAllRequestsDone() {
        if(requestsList.isEmpty()) {
            for (RequestProcessor processor : processorsList) {
//                System.out.println("Server"+s.getId());
//                for(RouteRequest request: processor.getRequests()) {
//                    System.out.println("REQUEST "+request.getRequestID());
//                }
                if(!processor.isEmpty()) {
                   // System.out.println("Server "+processor.getId()+" NOT EMPTY!");
                    return false;

                } else {
                    //System.out.println("Server "+processor.getId()+" is empty!");
                }
            }
        } else {
            return false;
        }
        return true;
    }

    public void putRequestsInWaitingList(List<RouteRequest> requestsList){
        for(RouteRequest request:requestsList){
            putRequestInWaitingList(request);
        }
    }

    public void putRequestInWaitingList(RouteRequest request) {
        requestsList.add(request);
    }

    public RouteRequest[] getTasksYetToBeSent(){

        RouteRequest[] requests = new RouteRequest[requestsList.size()];
        requestsList.toArray(requests);

        return requests;
    }

    public ArrayList<RequestProcessor> getProcessors() {
        return this.processorsList;
    }
}
