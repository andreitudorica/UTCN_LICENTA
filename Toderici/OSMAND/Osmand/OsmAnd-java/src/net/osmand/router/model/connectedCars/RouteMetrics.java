package net.osmand.router.model.connectedCars;

import net.osmand.router.RouteSegmentResult;

import java.util.HashSet;
import java.util.List;
import java.util.Set;

/**
 * Created by todericidan on 6/15/2018.
 */
public class RouteMetrics {

    private long requestID;
    private List<RouteSegmentResult> segmentList;
    private long computationTime;//milliseconds
    private long eta;
    private float averageSpeed;
    private float averageDensity;
    private long totalNbOfCars;
    private float lengthOfRoute;
    //avg density din dataset

    public RouteMetrics(long requestID, List<RouteSegmentResult> segmentList, long computationTime, long eta, float averageSpeed, float avgDensity, long totalNbOfCars, float lengthOfRoute) {
        this.requestID = requestID;
        this.segmentList = segmentList;
        this.computationTime = computationTime;
        this.eta = eta;
        this.averageSpeed = averageSpeed;
        this.averageDensity = avgDensity;
        this.totalNbOfCars = totalNbOfCars;
        this.lengthOfRoute = lengthOfRoute;
    }

    public long getRequestID() {
        return requestID;
    }

    public void setRequestID(long requestID) {
        this.requestID = requestID;
    }

    public List<RouteSegmentResult> getSegmentList() {
        return segmentList;
    }

    public void setSegmentList(List<RouteSegmentResult> segmentList) {
        this.segmentList = segmentList;
    }

    public long getComputationTime() {
        return computationTime;
    }

    public void setComputationTime(long computationTime) {
        this.computationTime = computationTime;
    }

    public long getEta() {
        return eta;
    }

    public void setEta(long eta) {
        this.eta = eta;
    }

    public float getAverageSpeed() {
        return averageSpeed;
    }

    public void setAverageSpeed(float averageSpeed) {
        this.averageSpeed = averageSpeed;
    }

    public float getAverageDensity() {
        return averageDensity;
    }

    public void setAverageDensity(float averageDensity) {
        this.averageDensity = averageDensity;
    }

    public long getTotalNbOfCars() {
        return totalNbOfCars;
    }

    public void setTotalNbOfCars(long totalNbOfCars) {
        this.totalNbOfCars = totalNbOfCars;
    }

    public float getLengthOfRoute() {
        return lengthOfRoute;
    }

    public void setLengthOfRoute(float lengthOfRoute) {
        this.lengthOfRoute = lengthOfRoute;
    }

    @Override
    public String toString() {
        final StringBuilder sb = new StringBuilder("RouteMetrics{");
        sb.append("requestID=").append(requestID);
        sb.append(", segmentList=").append(segmentList);
        sb.append(", computationTime=").append(computationTime);
        sb.append(", eta=").append(eta);
        sb.append(", averageSpeed=").append(averageSpeed);
        sb.append(", averageDensity=").append(averageDensity);
        sb.append(", totalNbOfCars=").append(totalNbOfCars);
        sb.append(", lengthOfRoute=").append(lengthOfRoute);
        sb.append('}');
        return sb.toString();
    }
}

