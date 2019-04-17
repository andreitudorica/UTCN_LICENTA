package net.osmand.router.model.constants;

/**
 * Created by todericidan on 5/20/2018.
 */
public final class RouterConstants {

    //default values for traffic data set
    public static final int ROUTE_MAX_TIME_SPAN_IN_MINUTES = 90;
   // multiply by this factor to obtain the desired unit of time
    //now  we convert from minutes to half a second
    public static final int MULTIPLYING_FACTOR = 120;

    //default values for request simulator
    public static final int SIM_NB_OF_REQUESTS = 10;
    public static final int SIM_BREAK_INTERVAL = 1;
    public static final int SIM_INTERVAL_IN_MINUTES = 5;

    // files locations
    public static final String REQUEST_FILE ="D:\\Andrei\\Scoala\\Licenta\\results\\LOGS\\Requests.csv";
    public static final String ZONES_FILE ="D:\\Andrei\\Scoala\\Licenta\\config\\Zones.csv";
    public static final String RESPONSE_TIME_FILE = "D:\\Andrei\\Scoala\\Licenta\\results\\LOGS\\ResponseTime.csv";
    public static final String ETAS_FILE = "D:\\Andrei\\Scoala\\Licenta\\results\\LOGS\\ETAs.csv";
    public static final String SPEEDS_FILE = "D:\\Andrei\\Scoala\\Licenta\\results\\LOGS\\Speeds.csv";
    public static final String DENSITIES_FILE = "D:\\Andrei\\Scoala\\Licenta\\results\\LOGS\\Densities.csv";
    public static final String ROUTE_NB_OF_SEGS_FILE = "D:\\Andrei\\Scoala\\Licenta\\results\\LOGS\\NbOfSegments.csv";
    public static final String INFO_FILE = "D:\\Andrei\\Scoala\\Licenta\\results\\LOGS\\INFO.txt";

    //fine tuning parameter
    public static final float TRAFFIC_JAM_DENSITY = 0.5f;

    //test distance parameters in kilometers
    public static final double MIN_DISTANCE = 0.0;
    public static final double MAX_DISTANCE = 15.0;

    //equivalent to maximum number of routes that can be in case to be processed
    public static final int MAX_NB_OF_REQS_PROCESSING  = 1;

    //max number of processing units to be started on this device
    public static final int MAX_NB_OF_PROCESSING_UNITS = 4;

    //vehicle statistics
    public static final int VEHICLE_LENGTH = 7;

    public static final float TRAFFIC_JAM_SPEED = 2.2f;//14 m/s  is 50 km/h

    //segment load in percentages out of total space
    public static final int LEVEL_1_DENSITY = 30;
    public static final int LEVEL_2_DENSITY = 40;
    public static final int LEVEL_3_DENSITY = 50;
    public static final int LEVEL_4_DENSITY = 60;
    public static final int LEVEL_5_DENSITY = 70;
    public static final int LEVEL_6_DENSITY = 80;

    //percentages of speed keep out of original speed in case of different levels of traffic
    public static final float LITTLE_1_SPEED = 0.8f;
    public static final float LITTLE_2_SPEED = 0.6f;
    public static final float LITTLE_3_SPEED = 0.5f;
    public static final float LITTLE_4_SPEED= 0.4f;
    public static final float LITTLE_5_SPEED = 0.2f;
    public static final float LITTLE_6_SPEED = 0.1f;   //in case the density is over 80%


    public static final float CONVERT_FACTOR = 3.6f; // to convert from m/s to km/m




    private RouterConstants(){
        //this prevents even the native class from
        //calling this constructor as well :
        throw new AssertionError();
    }
}
