package net.osmand.router.model;

import net.osmand.router.model.connectedCars.RequestScheduler;
import net.osmand.router.model.connectedCars.ResultMerger;
import net.osmand.router.model.traffic.TrafficDataSet;
import org.junit.*;

import java.util.Date;
import java.util.List;
import java.util.concurrent.TimeUnit;
import net.osmand.router.model.connectedCars.RouteRequest;

import net.osmand.router.model.randomGenerators.CityRouteRequestsGenerator;

/**
 * Created by todericidan on 3/22/2018.
 */

public class RouterRequesterTest {
    private CityRouteRequestsGenerator generator;
    private RequestScheduler scheduler;


    @Before
    public void setUp()  {
        generator = new CityRouteRequestsGenerator();
        scheduler = new RequestScheduler();
        scheduler.setTraffic(new TrafficDataSet());
        scheduler.setMerger(new ResultMerger());
        Thread th = new Thread(scheduler);
        th.start();
    }

    @Test
    public void testRouteRequester() {

        long startTime = System.nanoTime();
        while(true) {
            List<RouteRequest> requestsList = generator.generateConcurrentRequests();

            scheduler.putRequestsInWaitingList(requestsList);

            try {
                TimeUnit.SECONDS.sleep(2);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }

            long endTime = System.nanoTime();
            long totalTime = endTime - startTime;

            if (TimeUnit.SECONDS.convert(totalTime, TimeUnit.NANOSECONDS) > 10) {
                break;
           }
        }

        while(true){
            if(scheduler.areAllRequestsDone()){
                break;
            }
        }

        Date date = new Date(System.currentTimeMillis());
        System.out.println("DONE SIM AT "+ date.toString());

    }



    @After
    public void before() {

    }

}
