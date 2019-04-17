package net.osmand.router.model.randomGenerators;

import java.util.Date;

/**
 * Created by todericidan on 3/26/2018.
 */
public class ParallelRequest {

    private long requestID;
    private long creationTime;
    private long startProcessingTime;
    private long timeTookToCompute;
    private boolean hasFinishedProcessing;


    public ParallelRequest(long id){
        this.requestID = id;
        this.creationTime = System.currentTimeMillis();
        this.hasFinishedProcessing = false;
        this.timeTookToCompute = -1;
    }

    public void startProcessing(){
        startProcessingTime = System.currentTimeMillis();
    }

    public boolean hasFinishedProcessing() {
        return hasFinishedProcessing;
    }

    public void doneProcessing() {
        hasFinishedProcessing = true;
        timeTookToCompute = System.currentTimeMillis();
    }

    public long getTimeTookToCompute(){
        return timeTookToCompute;
    }

    public long getCreationTime() {
        return creationTime;
    }

    public long getStartProcessingTime(){
        return startProcessingTime;
    }

    public long getRequestID() {
        return requestID;
    }

    public long getTimeWaitedUntilProcessed(){
        return startProcessingTime - creationTime;
    }

    public String getCreationDate(){
        Date date = new Date(creationTime);
        return date.toString();
    }

    public String getFinishDate(){
        Date date = new Date(startProcessingTime+timeTookToCompute);
        return date.toString();
    }
}
