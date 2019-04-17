package net.osmand.router.model.randomGenerators;

import net.osmand.data.LatLon;
import net.osmand.router.model.connectedCars.RouteRequest;
import net.osmand.router.model.constants.RouterConstants;
import net.osmand.util.MapUtils;

import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.ArrayList;
import java.util.List;

/**
 * Created by todericidan on 4/6/2018.
 */
public class ZonesRouteRequestGenerator extends RouteRequestsGenerator {


    private List<InterestZone> zones = new ArrayList<InterestZone>();

    public ZonesRouteRequestGenerator() {
        super();
    }

    public ZonesRouteRequestGenerator(List<InterestZone> zones) {
        super();
        this.zones = zones;
    }

    public List<InterestZone> getZones() {
        return zones;
    }

    public void setZones(List<InterestZone> zones) {
        this.zones = zones;
    }

    public void addZones(InterestZone zone){
        zones.add(zone);
    }



    @Override
    public List<RouteRequest> generateConcurrentRequests() {
        List<RouteRequest> requests = new ArrayList<RouteRequest>();

        int requestGenerated = 0;

        while(requestGenerated < RouterConstants.SIM_NB_OF_REQUESTS) {

            int startZoneIndex = generateRandomIntValue(0, zones.size()) ;

            int endZoneIndex = generateRandomIntValue(0, zones.size()) ;

            //System.out.println("ZONE START: "+zones.get(startZoneIndex).getName()+" ZONE END: "+ zones.get(endZoneIndex).getName());

            double startLat = generateRandomDoubleValue(zones.get(startZoneIndex).getLatMin(),zones.get(startZoneIndex).getLatMax());
            double startLon = generateRandomDoubleValue(zones.get(startZoneIndex).getLonMin(),zones.get(startZoneIndex).getLonMax());


            double stopLat = generateRandomDoubleValue(zones.get(endZoneIndex).getLatMin(),zones.get(endZoneIndex).getLatMax());
            double stopLon = generateRandomDoubleValue(zones.get(endZoneIndex).getLonMin(),zones.get(endZoneIndex).getLonMax());


            LatLon startPoint = new LatLon(startLat,startLon);
            LatLon stopPoint = new LatLon(stopLat,stopLon);

            if(checkRequestDistance(RouterConstants.MIN_DISTANCE, RouterConstants.MAX_DISTANCE, startPoint, stopPoint)  && (startZoneIndex!=endZoneIndex)){

                RouteRequest r = new RouteRequest(getRequestID(), startPoint, stopPoint, 0);
                setRequestID(getRequestID() + 1);

                requests.add(r);

                requestGenerated++;
            }

        }

        return requests;
    }


    public static void main(String[] args) throws InterruptedException {
        ZonesRouteRequestGenerator generator = new ZonesRouteRequestGenerator(RequestFileHandler.getInterestZone(RouterConstants.ZONES_FILE));

        RequestFileHandler.writeRequestInFile(RouterConstants.REQUEST_FILE,generator.generateConcurrentRequests());

    }


}
