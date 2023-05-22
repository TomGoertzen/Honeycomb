using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Kaleidoscope.Properties;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kaleidoscope
{
    public class GhcPairDomainEdges2 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcPairDomainEdges class.
        /// </summary>
        public GhcPairDomainEdges2()
          : base("Pair Domain Edges2",
                 "Nickname",
                 "Description",
                 "Kaleidoscope",
                 "Tiling")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Grid Points", "G", "Grid points to divide domain.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Fundamental Domain", "D", "Polyline to divide by grid.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve Pairs", "O", "Pairs of matched edges.", GH_ParamAccess.tree);
            pManager.AddTransformParameter("Curve Pair Transforms", "T", "Transforms between each curve pair.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> gridPoints = new List<Point3d>();
            Curve fundamentalDomain = new PolylineCurve();
            DA.GetDataList(0, gridPoints);
            DA.GetData("Fundamental Domain", ref fundamentalDomain);

            List<double> shatterParams = new List<double>();
            if (gridPoints.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Grid Points");
                return;
            }
            foreach (Point3d point in gridPoints)
            {
                if (fundamentalDomain.ClosestPoint(point, out double parameter, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
                {
                    shatterParams.Add(parameter);
                }
            }

            Curve[] c = fundamentalDomain.DuplicateSegments();
            foreach (Curve curve in c)
            {
                Point3d point = curve.PointAtStart;
                if (fundamentalDomain.ClosestPoint(point, out double parameter, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
                {
                    shatterParams.Add(parameter);
                }
            }

            List<double> sortedParams = shatterParams.OrderBy(d => d).ToList();

            List<Line> segments = new List<Line>();
            for (int i = 0; i < sortedParams.Count; i++)
            {
                Point3d p1 = fundamentalDomain.PointAt(sortedParams[i]);
                int nextIndex = i + 1;
                if (nextIndex >= sortedParams.Count) nextIndex = 0;
                Point3d p2 = fundamentalDomain.PointAt(sortedParams[nextIndex]);
                Line l = new Line(p1, p2);
                if (l.Length > RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) segments.Add(l);
            }

            List<Line> sortedSegments = segments.OrderBy(l => l.Length).ToList();
            /*

            List<Point3d> closestPoints = new List<Point3d>();
            foreach (Line line in sortedSegments)
            {
                Point3d closestPoint = new Point3d();
                double bestDistance = double.NaN;
                foreach (Point3d point in gridPoints)
                {
                    Point3d testPoint = new Point3d();
                    double distance = double.NaN;
                    line.ClosestPoint(testPoint, true);
                    distance = testPoint.DistanceTo(point);
                    if (double.IsNaN(bestDistance) || distance < bestDistance)
                    {
                        bestDistance = distance;
                        closestPoint = point;
                    }
                }
                closestPoints.Add(closestPoint);
            }

            List<Line> sortedSegments = new List<Line>();
            while (closestPoints.Count > 0)
            {
                for (int i = 1; i < closestPoints.Count; i++)
                {
                    if (closestPoints[0] == closestPoints[i])
                    {
                        sortedSegments.Add(sortedLengths[0]);
                        sortedSegments.Add(sortedLengths[i]);
                        sortedLengths.RemoveAt(i);
                        sortedLengths.RemoveAt(0);
                        closestPoints.RemoveAt(i);
                        closestPoints.RemoveAt(0);
                        break;
                    }
                }
            }
            */
            GH_Structure<GH_Line> pairedSegments = new GH_Structure<GH_Line>();
            GH_Structure<GH_Transform> transforms = new GH_Structure<GH_Transform>();
            int treeIndex = 0;
            for (int i = 0; i < sortedSegments.Count; i++)
            {
                pairedSegments.Append(new GH_Line(sortedSegments[i]), new GH_Path(treeIndex));

                double distanceToNearestGridPt = double.NaN;
                foreach (Point3d gridPoint in gridPoints)
                {
                    Point3d testPoint = sortedSegments[i].ClosestPoint(gridPoint, true);
                    double testDistance = testPoint.DistanceTo(gridPoint);
                    if (double.IsNaN(distanceToNearestGridPt) || testPoint.DistanceTo(gridPoint) < distanceToNearestGridPt) distanceToNearestGridPt = testDistance;
                }

                if (distanceToNearestGridPt > RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) treeIndex++;
                else if (((i > 0) && (Math.Abs(sortedSegments[i - 1].Length - sortedSegments[i].Length) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
                        && !(pairedSegments.Branches[treeIndex].Count == 1))
                {
                    Point3d p1Origin = sortedSegments[i - 1].From;
                    Point3d p1X = sortedSegments[i - 1].To;
                    Point3d p1Y = sortedSegments[i - 1].From + new Point3d(0, 0, 1);
                    Point3d p2Origin = sortedSegments[i].To;
                    Point3d p2X = sortedSegments[i].From;
                    Point3d p2Y = sortedSegments[i].To + new Point3d(0, 0, 1);

                    Plane plane1 = new Plane(p1Origin, p1X, p1Y);
                    Plane plane2 = new Plane(p2Origin, p2X, p2Y);

                    GH_Transform t = new GH_Transform(Transform.PlaneToPlane(plane1, plane2));
                    transforms.Append(t, new GH_Path(treeIndex)); ;
                    treeIndex++;
                }
            }

            DA.SetDataTree(0, pairedSegments);
            DA.SetDataTree(1, transforms);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.PairDomainEdges;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("84C13ED6-4D63-4FF1-8D70-8510E820C772"); }
        }
    }
}