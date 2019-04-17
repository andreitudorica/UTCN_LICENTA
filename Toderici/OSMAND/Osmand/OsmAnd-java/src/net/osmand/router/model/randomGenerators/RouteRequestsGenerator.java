package net.osmand.router.model.randomGenerators;

import net.osmand.data.LatLon;
import net.osmand.router.model.constants.RouterConstants;
import net.osmand.router.model.connectedCars.RouteRequest;
import net.osmand.util.MapUtils;

import java.security.SecureRandom;
import java.util.List;

/**
 * Created by todericidan on 4/6/2018.
 */
public abstract class RouteRequestsGenerator {

    private int numberOfSecondsBetweenRequests;
    //expressed in minutes
    private int simulationTimeSpan;
    private int concurrentRequestNumber;
    //which request is next
    private int requestID;

    private SecureRandom randomGenerator = new SecureRandom() ;


    public RouteRequestsGenerator() {
        setRequestID(0);
        setConcurrentRequestNumber(RouterConstants.SIM_NB_OF_REQUESTS);
        setNumberOfSecondsBetweenRequests(RouterConstants.SIM_BREAK_INTERVAL);
        setSimulationTimeSpan(RouterConstants.SIM_INTERVAL_IN_MINUTES);
    }

    public abstract List<RouteRequest> generateConcurrentRequests();

    public int getNumberOfSecondsBetweenRequests() {
        return numberOfSecondsBetweenRequests;
    }

    public void setNumberOfSecondsBetweenRequests(int numberOfSecondsBetweenRequests) {
        this.numberOfSecondsBetweenRequests = numberOfSecondsBetweenRequests;
    }

    public int getSimulationTimeSpan() {
        return simulationTimeSpan;
    }

    public void setSimulationTimeSpan(int simulationTimeSpan) {
        this.simulationTimeSpan = simulationTimeSpan;
    }

    public int getConcurrentRequestNumber() {
        return concurrentRequestNumber;
    }

    public void setConcurrentRequestNumber(int concurrentRequestNumber) {
        this.concurrentRequestNumber = concurrentRequestNumber;
    }

    public int getRequestID() {
        return requestID;
    }

    public void setRequestID(int requestID) {
        this.requestID = requestID;
    }


    //interval [lowestValue, highestValue)
    public int generateRandomIntValue(int lowestValue, int highestValue){
        return randomGenerator.nextInt(highestValue - lowestValue) + lowestValue;
    }

    public double generateRandomDoubleValue(double lowestValue, double highestValue){
        return randomGenerator.nextDouble() * (highestValue - lowestValue) + lowestValue;
    }


    //low and high expressed in km
    public boolean checkRequestDistance(double lowestValue, double highestValue, LatLon start, LatLon stop){
        double distance = MapUtils.getDistance(start.getLatitude(),start.getLongitude(),stop.getLatitude(),stop.getLongitude());


        if((distance > lowestValue * 1000) && (distance < highestValue * 1000) ) {
            return true;
        }
        return  false;
    }

}
