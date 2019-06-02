using System.Collections.Generic;

public partial struct Coordinate
{
    public double? Double;
    public List<double> Coords;

    public static implicit operator Coordinate(double Double) => new Coordinate { Double = Double };
    public static implicit operator Coordinate(List<double> DoubleArray) => new Coordinate { Coords = DoubleArray };
}

public class Geometry
{
    public string type { get; set; }
    public List<object> coordinates { get; set; }
}

public class Properties
{
    public string name { get; set; }
    public string oneway { get; set; }
    public string maxspeed { get; set; }
    public string highway { get; set; }
    public string profile { get; set; }
    public string distance { get; set; }
    public string time { get; set; }
    public string junction { get; set; }
    public string bridge { get; set; }
}

public class Feature
{
    public string type { get; set; }
    public string name { get; set; }
    public Geometry geometry { get; set; }
    public Properties properties { get; set; }
    public string Shape { get; set; }
}

public class RouteModel
{
    public string type { get; set; }
    public List<Feature> features { get; set; }
}