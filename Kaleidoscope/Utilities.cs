using Rhino;
using Rhino.Geometry;
using System;

namespace Honeycomb
{
    internal class Utilities
    {
        public static Boolean endpoints_are_similar(Curve curCurve, Curve testCurve)
        {
            Point3d curStart = curCurve.PointAtStart;
            Point3d curEnd = curCurve.PointAtEnd;
            Point3d testStart = testCurve.PointAtStart;
            Point3d testEnd = testCurve.PointAtEnd;
            return (curStart.DistanceTo(testStart) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance &&
                    curEnd.DistanceTo(testEnd) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) ||
                   (curStart.DistanceTo(testEnd) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance &&
                    curEnd.DistanceTo(testStart) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
        }
    }
}
