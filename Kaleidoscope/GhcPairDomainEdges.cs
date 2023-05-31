using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Honeycomb.Properties;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Honeycomb
{
    public class GhcPairDomainEdges : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcPairDomainEdges class.
        /// </summary>
        public GhcPairDomainEdges()
          : base("Pair Domain Edges",
                 "PrEdges",
                 "Use this component to pair edges in preperation for warp fundamental domain.",
                 "Honeycomb",
                 "Tiling")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTransformParameter("Transform Data", "T", "Transforms between each curve pair.", GH_ParamAccess.tree);
            pManager.AddPointParameter("Grid Points", "G", "Grid points to divide domain.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Base Cell", "C", "Base Cell of the wallpaper group", GH_ParamAccess.item);
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

            Curve fundamentalDomain = new PolylineCurve();
            Curve baseCell = new PolylineCurve();
            List<Point3d> gridPoints = new List<Point3d>();
            DA.GetDataTree(0, out GH_Structure<GH_Transform> inTransforms);
            DA.GetDataList(1, gridPoints);
            DA.GetData(2, ref baseCell);
            DA.GetData(3, ref fundamentalDomain);

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

            Polyline baseCellPoly = baseCell.ToPolyline(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance,
                                                        RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, int.MinValue, int.MaxValue).ToPolyline();
            Curve[] curveArray = baseCell.DuplicateSegments();
            List<Point3d> endPointsBaseCell = new List<Point3d>();
            foreach (Curve curve in curveArray)
            {
                endPointsBaseCell.Add(curve.PointAtStart);
            }
            Vector3d translateX = new Vector3d(endPointsBaseCell[1] - endPointsBaseCell[0]);
            Vector3d translateY = new Vector3d(endPointsBaseCell[2] - endPointsBaseCell[1]);

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

            var transformsOneCell = inTransforms.Branches[0];

            var transformedSegments = new GH_Structure<GH_Curve>();

            int treeIndex = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < transformsOneCell.Count; k++)
                    {
                        GH_Transform t = transformsOneCell[k];
                        foreach (Line line in segments)
                        {
                            Curve curve = line.ToNurbsCurve().DuplicateCurve();
                            curve.Transform(t.Value);
                            curve.Transform(Transform.Translation(translateX * i));
                            curve.Transform(Transform.Translation(translateY * j));
                            transformedSegments.Append(new GH_Curve(curve), new GH_Path(treeIndex, k));
                        }
                    }
                    treeIndex++;
                }
            }

            var centerSegements = transformedSegments.get_Branch(new GH_Path(4, 0));
            Dictionary<int, int> usedIndicies = new Dictionary<int, int>();

            for (int i = 0; i < centerSegements.Count; i++)
            {
                if (usedIndicies.ContainsKey(i)) continue;
                Curve curCenterSegment = ((GH_Curve)centerSegements[i]).Value;
                for (int j = 0; j < transformedSegments.Branches.Count; j++)
                {
                    for (int k = i + 1; k < transformedSegments.Branches[0].Count; k++)
                    {
                        if (usedIndicies.ContainsKey(k)) continue;
                        Curve curTestSegment = transformedSegments[j][k].Value;
                        if ((curCenterSegment.PointAtStart.DistanceTo(curTestSegment.PointAtStart) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance &&
                            curCenterSegment.PointAtEnd.DistanceTo(curTestSegment.PointAtEnd) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) ||
                            (curCenterSegment.PointAtEnd.DistanceTo(curTestSegment.PointAtStart) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance &&
                            curCenterSegment.PointAtStart.DistanceTo(curTestSegment.PointAtEnd) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
                        {
                            usedIndicies[i] = k;
                            usedIndicies[k] = int.MinValue;
                        }
                    }
                }
            }


            var pairedSegments = new GH_Structure<GH_Curve>();
            treeIndex = 0;
            foreach (int key in usedIndicies.Keys)
            {
                if (usedIndicies[key] == int.MinValue) continue;
                pairedSegments.Append(new GH_Curve(segments[key].ToNurbsCurve()), new GH_Path(treeIndex));
                pairedSegments.Append(new GH_Curve(segments[usedIndicies[key]].ToNurbsCurve()), new GH_Path(treeIndex));
                treeIndex++;
            }

            var transformsBetweenPairs = new GH_Structure<GH_Transform>();
            for (int i = 0; i < pairedSegments.Branches.Count; i++)
            {
                Point3d start1 = pairedSegments[i][0].Value.PointAtStart;
                Point3d end1 = pairedSegments[i][0].Value.PointAtEnd;
                Point3d start2 = pairedSegments[i][1].Value.PointAtStart;
                Point3d end2 = pairedSegments[i][1].Value.PointAtEnd;

                if (start1.DistanceTo(start2) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    Vector3d vec1 = new Vector3d(end1 - start1);
                    Vector3d vec2 = new Vector3d(end2 - start2);
                    Transform t = new Transform(Transform.Rotation(vec1, vec2, start1));
                    transformsBetweenPairs.Append(new GH_Transform(t), new GH_Path(i));
                }
                else if (end1.DistanceTo(start2) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    Vector3d vec1 = new Vector3d(start1 - end1);
                    Vector3d vec2 = new Vector3d(end2 - start2);
                    Transform t = new Transform(Transform.Rotation(vec1, vec2, end1));
                    transformsBetweenPairs.Append(new GH_Transform(t), new GH_Path(i));
                }
                else if (end1.DistanceTo(end2) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    Vector3d vec1 = new Vector3d(start1 - end1);
                    Vector3d vec2 = new Vector3d(start2 - end2);
                    Transform t = new Transform(Transform.Rotation(vec1, vec2, end1));
                    transformsBetweenPairs.Append(new GH_Transform(t), new GH_Path(i));
                }
                else if (start1.DistanceTo(end2) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    Vector3d vec1 = new Vector3d(end1 - start1);
                    Vector3d vec2 = new Vector3d(start2 - end2);
                    Transform t = new Transform(Transform.Rotation(vec1, vec2, start1));
                    transformsBetweenPairs.Append(new GH_Transform(t), new GH_Path(i));
                }
                else if (Math.Abs(start1.DistanceTo(start2) - end1.DistanceTo(end2)) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    Vector3d vec = new Line(start1, end2).Direction;
                    Transform t = new Transform(Transform.Translation(vec));
                    transformsBetweenPairs.Append(new GH_Transform(t), new GH_Path(i));
                }
                else if (Math.Abs(end1.DistanceTo(start2) - start1.DistanceTo(end2)) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    Vector3d vec = new Line(start1, start2).Direction;
                    Transform t = new Transform(Transform.Translation(vec));
                    transformsBetweenPairs.Append(new GH_Transform(t), new GH_Path(i));
                }
                else
                {
                    Vector3d vec = new Line(start1, start2).Direction;
                    Vector3d line1 = new Line(start1, end1).Direction;
                    Vector3d line2 = new Line(start2, end2).Direction;
                    double angle = Vector3d.VectorAngle(line1, line2);

                    Transform translate = new Transform(Transform.Translation(vec));
                    Transform mirror = new Transform(Transform.Mirror(new Plane(start1, end1, (start1 + new Vector3d(0.0, 0.0, 1.0)))));

                    Transform rotate = new Transform(Transform.Rotation(angle, start1));
                    Transform t = translate * rotate * mirror;
                    end1.Transform(t);

                    if (end2.DistanceTo(end1) > RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    {
                        rotate = new Transform(Transform.Rotation(-angle, start1));
                        t = translate * rotate * mirror;
                        transformsBetweenPairs.Append(new GH_Transform(t), new GH_Path(i));

                    }
                    else
                    {
                        transformsBetweenPairs.Append(new GH_Transform(t), new GH_Path(i));
                    }
                }
            }

            for (int i = 0; i < segments.Count; i++)
            {
                if (usedIndicies.ContainsKey(i)) continue;
                pairedSegments.Append(new GH_Curve(segments[i].ToNurbsCurve()), new GH_Path(treeIndex));
            }

            DA.SetDataTree(0, pairedSegments);
            DA.SetDataTree(1, transformsBetweenPairs);
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
            get { return new Guid("AC369232-4388-4DEF-973E-61993C8B27B2"); }
        }
    }
}