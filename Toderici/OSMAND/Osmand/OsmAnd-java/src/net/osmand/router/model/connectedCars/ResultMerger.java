package net.osmand.router.model.connectedCars;

import net.osmand.binary.BinaryInspector;
import net.osmand.router.RouteSegmentResult;
import net.osmand.router.model.constants.RouterConstants;
import net.osmand.router.model.traffic.TrafficDataSet;

import java.util.*;

/**
 * Created by todericidan on 6/15/2018.
 */
public class ResultMerger {

    private List<RouteMetrics> results = Arrays.asList(new RouteMetrics[RouterConstants.SIM_NB_OF_REQUESTS]);

    private Set<Long> segmentIds = new HashSet<>();


    public ResultMerger() {
        for(int i = 0; i< RouterConstants.SIM_NB_OF_REQUESTS; i++){
           results.set(i, new RouteMetrics(0, new ArrayList<RouteSegmentResult>(),0,0,0.0f,0.0f,0,0.0f));
        }
    }

    public Set<Long> getSegmentIds(){
        for(RouteMetrics result: results){
            for(RouteSegmentResult segment: result.getSegmentList()){
                segmentIds.add(segment.getObject().getId());
            }
        }
        return segmentIds;
    }

    public long getNumberOfSegments(){
        for(RouteMetrics result: results){
            for(RouteSegmentResult segment: result.getSegmentList()){
                segmentIds.add(segment.getObject().getId());
            }
        }

        return segmentIds.size();
    }

    public void setResultAtIndex(int i, RouteMetrics result){
        results.set(i, result);
    }

    public RouteMetrics getResultAtIndex(int i){
        return results.get(i);
    }







}
