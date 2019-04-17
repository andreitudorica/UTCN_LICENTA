package net.osmand.router.model.randomGenerators;

import net.osmand.data.LatLon;
import net.osmand.router.model.connectedCars.RequestProcessingUnit;
import net.osmand.router.model.connectedCars.RouteRequest;
import net.osmand.router.model.constants.RouterConstants;
import net.osmand.util.MapUtils;

import java.io.*;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.TimeUnit;

/**
 * Created by todericidan on 3/20/2018.
 */
public class CityRouteRequestsGenerator extends RouteRequestsGenerator {

    //default is Cluj-Napoca bounding box
    private static double DEFAULT_LAT_MIN = 46.720606;
    private static double DEFAULT_LAT_MAX = 46.815038;
    private static double DEFAULT_LON_MIN = 23.508614;
    private static double DEFAULT_LON_MAX = 23.745887;


    //City's bounding box parameters
    private double latMin;
    private double latMax;
    private double lonMin;
    private double lonMax;

    public CityRouteRequestsGenerator(double latMin, double latMax, double lonMin, double lonMax) {
        super();
        this.latMin = latMin;
        this.latMax = latMax;
        this.lonMin = lonMin;
        this.lonMax = lonMax;
    }

    public CityRouteRequestsGenerator() {
        super();
        setLatMax(DEFAULT_LAT_MAX);
        setLatMin(DEFAULT_LAT_MIN);
        setLonMax(DEFAULT_LON_MAX);
        setLonMin(DEFAULT_LON_MIN);
    }



    @Override
    public List<RouteRequest> generateConcurrentRequests(){

        List<RouteRequest> requests = new ArrayList<RouteRequest>();

        int requestGenerated = 0;

        while(requestGenerated < RouterConstants.SIM_NB_OF_REQUESTS) {

            double startLat = generateRandomDoubleValue(getLatMin(),getLatMax());
            double startLon = generateRandomDoubleValue(getLonMin(),getLonMax());

            double stopLat = generateRandomDoubleValue(getLatMin(),getLatMax());
            double stopLon = generateRandomDoubleValue(getLonMin(),getLonMax());

            LatLon startPoint = new LatLon(startLat,startLon);
            LatLon stopPoint = new LatLon(stopLat,stopLon);

            if(checkRequestDistance(0, RouterConstants.MAX_DISTANCE, startPoint, stopPoint ) ){
                RouteRequest r = new RouteRequest(getRequestID(),startPoint,stopPoint,0);
                setRequestID(getRequestID()+1);

                requests.add(r);

                requestGenerated++;
            }


        }

        return requests;
    }


    public void setBoundingBox(LatLon bottomCorner, LatLon topCorner){
        setTopCornerOfTheBoundingBox(topCorner);
        setBottomCornerOfTheBoundingBox(bottomCorner);
    }

    public void setTopCornerOfTheBoundingBox(LatLon latLon){
        setLatMax(latLon.getLatitude());
        setLonMax(latLon.getLongitude());
    }

    public void setBottomCornerOfTheBoundingBox(LatLon latLon){
        setLatMin(latLon.getLatitude());
        setLonMin(latLon.getLongitude());
    }

    public double getLatMin() {
        return latMin;
    }

    public void setLatMin(double latMin) {
        this.latMin = latMin;
    }

    public double getLatMax() {
        return latMax;
    }

    public void setLatMax(double latMax) {
        this.latMax = latMax;
    }

    public double getLonMin() {
        return lonMin;
    }

    public void setLonMin(double lonMin) {
        this.lonMin = lonMin;
    }

    public double getLonMax() {
        return lonMax;
    }

    public void setLonMax(double lonMax) {
        this.lonMax = lonMax;
    }



    public static void main(String[] args) throws InterruptedException {
        CityRouteRequestsGenerator generator = new CityRouteRequestsGenerator();

        List<RouteRequest> requests = generator.generateConcurrentRequests();

        RequestFileHandler.writeRequestInFile(RouterConstants.REQUEST_FILE,requests);


    }


}
