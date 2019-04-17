package net.osmand.router.model.randomGenerators;

import net.osmand.data.LatLon;
import net.osmand.router.model.connectedCars.RouteRequest;
import net.osmand.router.model.constants.RouterConstants;

import java.io.*;
import java.util.ArrayList;
import java.util.List;

/**
 * Created by todericidan on 6/16/2018.
 */
public class RequestFileHandler {

    public static List<RouteRequest> getRequests(String file){
        List<RouteRequest> requests = new ArrayList<RouteRequest>();
        BufferedReader br = null;
        String line = "";
        String cvsSplitBy = ",";
        try {
            br = new BufferedReader(new FileReader(file));
            while ((line = br.readLine()) != null) {
                // use comma as separator
                String[] values = line.split(cvsSplitBy);
                LatLon start = new LatLon(Double.parseDouble(values[1]),Double.parseDouble(values[2]));
                LatLon stop = new LatLon(Double.parseDouble(values[3]),Double.parseDouble(values[4]));
                RouteRequest request= new RouteRequest(Long.parseLong(values[0]),start,stop, 0 ) ;
                requests.add(request);
            }
        } catch (FileNotFoundException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        } finally {
            if (br != null) {
                try {
                    br.close();
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
        }

        return requests;
    }


    public static List<InterestZone> getInterestZone(String file){
        List<InterestZone> zones = new ArrayList<>();

        BufferedReader br = null;
        String line = "";
        String cvsSplitBy = ",";


        try {

            br = new BufferedReader(new FileReader(file));
            while ((line = br.readLine()) != null) {

                // use comma as separator
                String[] values = line.split(cvsSplitBy);

                InterestZone zone = new InterestZone(Long.parseLong(values[0]),values[1],
                        Double.parseDouble(values[2]),Double.parseDouble(values[3]),
                        Double.parseDouble(values[4]),Double.parseDouble(values[5]));

                zones.add(zone);
            }
        } catch (FileNotFoundException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        } finally {
            if (br != null) {
                try {
                    br.close();
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
        }

        return zones;

    }


    public static void writeRequestInFile(String file, List<RouteRequest> requests){

        //System.out.println(requests);

        FileWriter fileWriter = null;
        try {
            fileWriter = new FileWriter(file);
        } catch (IOException e) {
            e.printStackTrace();
        }

        PrintWriter printWriter = new PrintWriter(fileWriter);

        for(RouteRequest request: requests){
            StringBuilder sb = new StringBuilder();
            sb.append(request.getRequestID());
            sb.append(',');
            sb.append(request.getStartPoint().getLatitude());
            sb.append(',');
            sb.append(request.getStartPoint().getLongitude());
            sb.append(',');
            sb.append(request.getEndPoint().getLatitude());
            sb.append(',');
            sb.append(request.getEndPoint().getLongitude());
            sb.append('\n');

            printWriter.write(sb.toString());
        }

        printWriter.close();

    }

    public static void writeZonesInFile(String file, List<InterestZone> zones){
        FileWriter fileWriter = null;
        try {
            fileWriter = new FileWriter(file);
        } catch (IOException e) {
            e.printStackTrace();
        }

        PrintWriter printWriter = new PrintWriter(fileWriter);

        for(InterestZone zone: zones){
            StringBuilder sb = new StringBuilder();
            sb.append(zone.getId());
            sb.append(',');
            sb.append(zone.getName());
            sb.append(',');
            sb.append(zone.getLatMin());
            sb.append(',');
            sb.append(zone.getLatMax());
            sb.append(',');
            sb.append(zone.getLonMin());
            sb.append(',');
            sb.append(zone.getLonMax());
            sb.append('\n');

            printWriter.write(sb.toString());
        }

        printWriter.close();
    }


    public static void main(String[] args) {
        System.out.println(RequestFileHandler.getInterestZone(RouterConstants.ZONES_FILE));
    }

}
