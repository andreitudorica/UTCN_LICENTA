package net.osmand.router.model.randomGenerators;

/**
 * Created by todericidan on 4/6/2018.
 */
public class InterestZone {

    private long id;
    private String name;

    private double latMin;
    private double latMax;
    private double lonMin;
    private double lonMax;


    public InterestZone(long id, String name, double latMin, double latMax, double lonMin, double lonMax) {
        this.id = id;
        this.name = name;
        this.latMin = latMin;
        this.latMax = latMax;
        this.lonMin = lonMin;
        this.lonMax = lonMax;
    }


    public long getId() {
        return id;
    }

    public void setId(long id) {
        this.id = id;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
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

    @Override
    public String toString() {
        final StringBuilder sb = new StringBuilder("InterestZone{");
        sb.append("id=").append(id);
        sb.append(", name='").append(name).append('\'');
        sb.append(", latMin=").append(latMin);
        sb.append(", latMax=").append(latMax);
        sb.append(", lonMin=").append(lonMin);
        sb.append(", lonMax=").append(lonMax);
        sb.append('}').append('\n');
        return sb.toString();
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        InterestZone that = (InterestZone) o;

        if (id != that.id) return false;
        if (Double.compare(that.latMin, latMin) != 0) return false;
        if (Double.compare(that.latMax, latMax) != 0) return false;
        if (Double.compare(that.lonMin, lonMin) != 0) return false;
        if (Double.compare(that.lonMax, lonMax) != 0) return false;
        return name.equals(that.name);

    }

    @Override
    public int hashCode() {
        int result;
        long temp;
        result = (int) (id ^ (id >>> 32));
        result = 31 * result + name.hashCode();
        temp = Double.doubleToLongBits(latMin);
        result = 31 * result + (int) (temp ^ (temp >>> 32));
        temp = Double.doubleToLongBits(latMax);
        result = 31 * result + (int) (temp ^ (temp >>> 32));
        temp = Double.doubleToLongBits(lonMin);
        result = 31 * result + (int) (temp ^ (temp >>> 32));
        temp = Double.doubleToLongBits(lonMax);
        result = 31 * result + (int) (temp ^ (temp >>> 32));
        return result;
    }
}
