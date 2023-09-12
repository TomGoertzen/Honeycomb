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

            List<Line> segments = get_lines_of_domain(gridPoints, baseCell, fundamentalDomain,
                                                      out Vector3d translateX, out Vector3d translateY);

            //REPEAT CELL IN 3X3 IN ORDER TO MATCH CURVES; EXACTLY 1 WILL OVERLAY EACH OF THE BASE CELL
            var baseCellTransforms = inTransforms.Branches[0];
            var allTransforms = new List<Transform>();
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    for (int k = 0; k < baseCellTransforms.Count; k++)
                    {
                        Transform t = baseCellTransforms[k].Value;
                        Transform full_transform = Transform.Translation(translateX * i) *
                                                   Transform.Translation(translateY * j) * t;
                        allTransforms.Add(full_transform);
                    }
                }
            }

            GH_Structure<GH_Curve> curvePairs = new GH_Structure<GH_Curve>();
            GH_Structure<GH_Transform> curvePairsTransforms = new GH_Structure<GH_Transform>();
            int treeIndex = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                Curve curCurve = segments[i].ToNurbsCurve();
                for (int j = i + 1; j < segments.Count; j++)
                {
                    Curve testCurve = segments[j].ToNurbsCurve();
                    if (try_find_transform_between_curves(curCurve, testCurve, allTransforms, out Transform t))
                    {
                        curvePairs.Append(new GH_Curve(curCurve), new GH_Path(treeIndex));
                        curvePairs.Append(new GH_Curve(testCurve), new GH_Path(treeIndex));
                        curvePairsTransforms.Append(new GH_Transform(t), new GH_Path(treeIndex));
                        treeIndex++;
                    }
                }
            }

            //What if a curve has no pair? Add it to its own branch!
            List<GH_Curve> curvePairsFlat = curvePairs.ToList();
            foreach (Line line in segments)
            {
                Curve curSeg = line.ToNurbsCurve();
                bool foundPair = false;
                for (int j = 0; j < curvePairsFlat.Count; j++)
                {
                    Curve testCurve = curvePairsFlat[j].Value;
                    if (Utilities.endpoints_are_similar(curSeg, testCurve))
                    {
                        foundPair = true;
                    }
                }
                if (!foundPair)
                {
                    curvePairs.Append(new GH_Curve(curSeg), new GH_Path(treeIndex));
                    treeIndex++;
                }
            }

            DA.SetDataTree(0, curvePairs);
            DA.SetDataTree(1, curvePairsTransforms);
        }

        private Boolean try_find_transform_between_curves(Curve curCurve, Curve testCurve, List<Transform> tList, out Transform t)
        {
            foreach (Transform testT in tList)
            {
                Curve transfromedTestCurve = curCurve.DuplicateCurve();
                transfromedTestCurve.Transform(testT);
                if (Utilities.endpoints_are_similar(transfromedTestCurve, testCurve))
                {
                    t = testT;
                    return true;
                }
            }
            t = Transform.Unset;
            return false;
        }


        private List<Line> get_lines_of_domain(List<Point3d> gridPoints, Curve baseCell, Curve fundamentalDomain,
                                               out Vector3d translateX, out Vector3d translateY)
        {
            //GET SEGMENTS OF FUNDAMENTAL DOMAIN
            List<double> shatterParams = new List<double>();
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
            translateX = new Vector3d(endPointsBaseCell[1] - endPointsBaseCell[0]);
            translateY = new Vector3d(endPointsBaseCell[2] - endPointsBaseCell[1]);

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
            return segments;
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