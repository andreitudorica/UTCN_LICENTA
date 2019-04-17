package net.osmand.router.model.traffic;

import net.osmand.binary.BinaryInspector;
import net.osmand.router.RouteSegmentResult;
import net.osmand.router.model.constants.RouterConstants;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Created by todericidan on 4/25/2018.
 */
public class TrafficDataSet {

    private Map<Long, IntervalTree> traffic;

    private int intervalThreshold;

    private String algorithmUsed;

    public TrafficDataSet() {
        traffic = new HashMap<Long, IntervalTree>();
        intervalThreshold = RouterConstants.ROUTE_MAX_TIME_SPAN_IN_MINUTES * RouterConstants.MULTIPLYING_FACTOR;
        //System.out.println();
    }


     public void addCarToTree(Long segmentId,long segmentLength, float segmentSpeed, long left, long right){

        if(traffic.containsKey(segmentId)){
            traffic.get(segmentId).updateLength(segmentLength);
            traffic.get(segmentId).update(1,1,intervalThreshold,left,right);
        }else{
            IntervalTree tree = new IntervalTree(segmentId,segmentLength,intervalThreshold*4 -2,segmentSpeed);
            tree.update(1,1,intervalThreshold,left,right);
            traffic.put(segmentId,tree);
        }
    }

    public String getAlgorithmUsed() {
        return algorithmUsed;
    }

    public void setAlgorithmUsed(String algorithmUsed) {
        this.algorithmUsed = algorithmUsed;
    }

    public IntervalTree getTree(Long segmentId){
        return traffic.get(segmentId);
    }

    public long queryTree(Long segmentId, long left, long right){
        return traffic.get(segmentId).query(1,1,intervalThreshold,left,right);
    }

    public Map<Long, IntervalTree> getTraffic() {
        return traffic;
    }

    public void deleteTraffic(){
        traffic = new HashMap<Long, IntervalTree>();
    }

    public float querySpeed(Long segmentId, long left, long right){
        if(traffic.containsKey(segmentId)) {
            if(algorithmUsed.equals("CCRA")) {
                return traffic.get(segmentId).ccraGetSpeedOnInterval(intervalThreshold, left, right);
            }else{
                return traffic.get(segmentId).braGetSpeedOnInterval(intervalThreshold, left, right);
            }
           // return traffic.get(segmentId).braGetSpeedOnInterval(intervalThreshold, left, right);
        }else{
            return 0.0f;
        }
    }


    public float queryBRASpeed(Long segmentId, long left, long right){
        if(traffic.containsKey(segmentId)) {
            return traffic.get(segmentId).braGetSpeedOnInterval(intervalThreshold, left, right);
        }else{
            return 0.0f;
        }
    }


    public long getLength(Long segmentId){
        if(traffic.containsKey(segmentId)) {
            return traffic.get(segmentId).getLength();
        }else{
            return 0;
        }
    }

    public float queryDensity(Long segmentId, long left, long right){
        if(traffic.containsKey(segmentId)) {
            return traffic.get(segmentId).getDensityOnInterval(intervalThreshold, left, right);
        }else{
            return 0.0f;
        }
    }

    public long queryTraffic(Long segmentId, long left, long right) {
        if(traffic.containsKey(segmentId)) {
            return traffic.get(segmentId).query(1,1,intervalThreshold,left,right);
        }else{
            return 0;
        }
    }

    //    public void deleteAllOtherSegments(List<RouteSegmentResult> segments) {
//
//        for(RouteSegmentResult seg : segments) {
//            boolean keep = false;
//            for (IntervalTree tree : traffic.values()) {
//                if( tree.getId() == (seg.getObject().getId()>> (BinaryInspector.SHIFT_ID)) ){
//                    keep = true;
//                    break;
//                }
//            }
//            if(keep ==false){
//                traffic.remove(seg.getObject().getId()>> (BinaryInspector.SHIFT_ID));
//            }
//        }
//    }

    public static void main(String[] args) {
        TrafficDataSet dataSet1 = new TrafficDataSet();
        dataSet1.setAlgorithmUsed("CCRA");
        TrafficDataSet dataSet2 = new TrafficDataSet();
        dataSet2.setAlgorithmUsed("BRA");

        if(dataSet1.getAlgorithmUsed().equals("CA")){
            System.out.println(dataSet1.getAlgorithmUsed());
        }

        dataSet1.addCarToTree(Long.valueOf(1),200,43.2f,2,5);
        dataSet1.addCarToTree(Long.valueOf(1),200,50.0f,6,8);
        //dataSet1.deleteTraffic();


        dataSet2.addCarToTree(Long.valueOf(1),10,50.0f,4,7);
        dataSet2.addCarToTree(Long.valueOf(1),10,50.0f,2,5);

        System.out.println(dataSet2.queryTraffic(Long.valueOf(1),3,8) + " "+ dataSet2.queryBRASpeed(Long.valueOf(1),3,8)+" "+dataSet2.queryDensity(Long.valueOf(1),3,8));

    }



}
