package net.osmand.router.model.traffic;


import net.osmand.binary.BinaryInspector;
import net.osmand.map.WorldRegion;
import net.osmand.router.model.constants.RouterConstants;

import java.util.concurrent.atomic.AtomicLongArray;

/**
 * Created by todericidan on 5/5/2018.
 */
public class IntervalTree {

    private AtomicLongArray atomicValues;
    private AtomicLongArray atomicCommonValues;
    private Long id;
    private long length;
    private float initialSpeed;


    public IntervalTree(Long id, long length, int nbOfIntervals, float initialSpeed){
        this.id = id;
        this.length = length;
        this.atomicCommonValues = new AtomicLongArray(nbOfIntervals);
        this.atomicValues = new AtomicLongArray(nbOfIntervals);
        this.initialSpeed = initialSpeed;
    }

    public Long getId() {
        return id;
    }

    public long getLength() {
        return length;
    }

    public void updateLength(long length){
        if(this.length< length){
            this.length = length;
        }
       // this.length += length;
    }

    //left and right will be received in units of measurement which for us in 1/2 of a second
    public long query(int node, long left, long right, long queryLeft, long queryRight){

        if(queryLeft <= left && queryRight >= right){
            return atomicValues.get(node);
        }
        if( queryLeft > right || queryRight < left){
            return 0;
        }

        long middle = (left + right)/2;

        long value = query(2*node, left, middle, queryLeft, queryRight) +
                     query(2*node + 1, middle + 1, right, queryLeft, queryRight);

        if( queryLeft <= middle && queryRight > middle) {
            value -= atomicCommonValues.get(node);
        }

        return  value;
    }

    public void update(int node, long left, long right, long updateLeft, long updateRight) {

        if(updateLeft >right  || updateRight < left){
            return;
        }

        atomicValues.addAndGet(node,1);

        if(left < right){
            long middle = ( left + right ) /2;

            update(2 * node, left, middle, updateLeft, updateRight);
            update(2 * node + 1, middle + 1, right, updateLeft, updateRight);

            if(updateLeft <= middle && updateRight > middle){
                atomicCommonValues.addAndGet(node,1);
            }
        }
    }

    public boolean isAvailableOnInterval(int size, long left, long right){

        float speed = braGetSpeedOnInterval(size, left, right);

        if(speed < RouterConstants.TRAFFIC_JAM_SPEED){
            return false;
        }


        return true;
    }


    public float getDensityOnInterval(int size, long left, long right){

        long nbOfCars = query(1,
                1, size,
                left, right);

        float segmentDensity = 0.0f;

        if(length != 0) {
            segmentDensity = ((float) nbOfCars * RouterConstants.VEHICLE_LENGTH) / (float) length;
        }else{
            return 1.0f;
        }

        if(segmentDensity>1.0f){
            return 1.0f;
        }
        return segmentDensity;
    }

    public float braGetSpeedOnInterval(int size,long left, long right){

        float segmentDensity = getDensityOnInterval(size, left,right);

        if(segmentDensity < 0.4){
            return initialSpeed;
        }
        if(segmentDensity < 0.5){
            return initialSpeed * 0.6f;
        }
        if(segmentDensity < 0.6){
            return  initialSpeed * 0.4f;
        }
        if(segmentDensity < 0.7){
            return  initialSpeed * 0.3f;
        }

        return initialSpeed * 0.1f;
    }

    public float ccraGetSpeedOnInterval(int size,long left, long right){

        float segmentDensity = getDensityOnInterval(size, left,right);

        if(segmentDensity > RouterConstants.TRAFFIC_JAM_DENSITY){
            //System.out.println("SEG "+id+" density "+segmentDensity);
            return  initialSpeed * 0.1f;
        }
        return initialSpeed;
    }


    public float getInitialSpeed() {
        return initialSpeed;
    }

    public static void main(String[] args) {
        IntervalTree tree = new IntervalTree(Long.valueOf(1),70,50,50.0f);

        tree.update(1, 1, 20, 2, 3);
        tree.update(1, 1, 20, 4, 5);
        tree.update(1, 1, 20, 6, 8);
        tree.update(1, 1, 20, 4, 5);
        tree.update(1, 1, 20, 4, 5);
        tree.update(1, 1, 20, 4, 5);
        tree.update(1, 1, 20, 6, 8);
        tree.update(1, 1, 20, 6, 8);
        tree.update(1, 1, 20, 6, 8);
        tree.update(1, 1, 20, 8, 12);
        tree.update(1, 1, 20, 3, 9);
        tree.update(1, 1, 20, 2, 3);
        tree.update(1, 1, 20, 6, 7);
        tree.update(1, 1, 20, 4, 5);
        tree.update(1, 1, 20, 2, 3);
        tree.update(1, 1, 20, 6, 7);
        tree.update(1, 1, 20, 4, 5);
//        tree.update(1, 1, 20, 2, 3);
//        tree.update(1, 1, 20, 4, 5);
//        tree.update(1, 1, 20, 6, 8);
//        tree.update(1, 1, 20, 8, 12);
//        tree.update(1, 1, 20, 3, 9);

        System.out.println(tree.getLength()+" | "+RouterConstants.VEHICLE_LENGTH*tree.query(1,1,20, 2, 7));
        System.out.println(tree.getDensityOnInterval(20,2,7));

        System.out.println(tree.braGetSpeedOnInterval(20,2,7) +" | "+ tree.ccraGetSpeedOnInterval(20,2,7));

    }

    
}
