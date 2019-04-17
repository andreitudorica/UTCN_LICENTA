package net.osmand.router.model.connectedCars;

import net.osmand.data.LatLon;
import net.osmand.router.model.randomGenerators.ParallelRequest;

import java.util.ArrayList;
import java.util.List;

/**
 * Created by todericidan on 3/20/2018.
 */
public class RouteRequest extends ParallelRequest {

    private LatLon startPoint;
    private LatLon endPoint;
    private List<LatLon> intermediates;
    private long delay;


    public RouteRequest(long id, LatLon startPoint, LatLon endPoint, long delay) {
        super(id);
        this.startPoint = startPoint;
        this.endPoint = endPoint;
        this.intermediates = new ArrayList<LatLon>();
        this.delay = delay;

    }

    public LatLon getStartPoint() {
        return startPoint;
    }

    public void setStartPoint(LatLon startPoint) {
        this.startPoint = startPoint;
    }

    public LatLon getEndPoint() {
        return endPoint;
    }

    public void setEndPoint(LatLon endPoint) {
        this.endPoint = endPoint;
    }

    public List<LatLon> getIntermediates() {
        return intermediates;
    }

    public void setIntermediates(List<LatLon> intermediates) {
        this.intermediates = intermediates;
    }

    public long getDelay() {
        return delay;
    }

    public void setDelay(long delay) {
        this.delay = delay;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        RouteRequest request = (RouteRequest) o;

        if (delay != request.delay) return false;
        if (!startPoint.equals(request.startPoint)) return false;
        if (!endPoint.equals(request.endPoint)) return false;
        return intermediates.equals(request.intermediates);

    }

    @Override
    public int hashCode() {
        int result = startPoint.hashCode();
        result = 31 * result + endPoint.hashCode();
        result = 31 * result + intermediates.hashCode();
        result = 31 * result + (int) (delay ^ (delay >>> 32));
        return result;
    }

    @Override
    public String toString() {
        final StringBuilder sb = new StringBuilder("RouteRequest{");
        sb.append("startPoint=").append(startPoint);
        sb.append(", endPoint=").append(endPoint);
        sb.append(", intermediates=").append(intermediates);
        sb.append(", delay=").append(delay);
        sb.append('}');
        sb.append('\n');
        return sb.toString();
    }
}
