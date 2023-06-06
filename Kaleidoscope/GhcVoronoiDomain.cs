using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Geometry.Voronoi;
using Grasshopper.Kernel.Types;
using Honeycomb.Properties;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Honeycomb
{
    public class GhcVoronoiDomain : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcVoronoiDomain class.
        /// </summary>
        public GhcVoronoiDomain()
          : base("Get Voronoi Domain",
                 "VorDom",
                 "Use this component to generate a voronoi fundamental domain.",
                 "Honeycomb",
                 "Tiling")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTransformParameter("Transformation Data", "T", "Transformations based on wallpaper group", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Base Cell", "C", "Boundary of the Wallpaper's base cell", GH_ParamAccess.item);
            pManager.AddPointParameter("UV Coordinates", "UV", "Coordinates of the evaluated Point", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTransformParameter("Transform Data", "T", "Tree containing transform data to be applied to a geometry", GH_ParamAccess.tree);
            pManager.AddPointParameter("Eval Point", "P", "The evaluated point", GH_ParamAccess.item);
            pManager.AddCurveParameter("Fundamental Domain", "D", "Geometry representing the suggested fundamental domain boundary", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve inCurve = null;
            Point3d uv = new Point3d(0.5, 0.5, 0);
            DA.GetData("Base Cell", ref inCurve);
            DA.GetData(2, ref uv);
            DA.GetDataTree(0, out GH_Structure<GH_Transform> transformations);

            /*
            if (1 - uv.X < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance || uv.X < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Evaluation too near to 1.0 or 0, may encounter errors.");
            }

            if (1 - uv.Y < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance || uv.Y < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Evaluation too near to 1.0 or 0, may encounter errors.");
            }
            */

            if (Math.Abs(1 - uv.Y) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) uv.Y = 0.9999;
            if (Math.Abs(1 - uv.X) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) uv.X = 0.9999;
            if (Math.Abs(0 - uv.Y) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) uv.Y = 0.0001;
            if (Math.Abs(0 - uv.X) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) uv.X = 0.0001;
            if (Math.Abs(0.5 - uv.Y) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance &&
                Math.Abs(0.5 - uv.X) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
            {
                uv.X = 0.5001;
                uv.Y = 0.5001;
            }

            //GET ONE CELL Ts
            List<GH_Transform> oneCellTransform = (List<GH_Transform>)transformations.get_Branch(0);

            //CREATE POLYLINE AND SRF
            Boolean isPolyline = inCurve.TryGetPolyline(out Polyline boundary);
            if (!isPolyline)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Is not a Polyline");

            NurbsSurface cellSrf = null;
            if (boundary.Count == 5)
                cellSrf = NurbsSurface.CreateFromCorners(boundary[0], boundary[1], boundary[2], boundary[3]);
            else if (boundary.Count == 4)
                cellSrf = NurbsSurface.CreateFromCorners(boundary[0], boundary[1], boundary[2]);
            else
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong number of Polyline Points");

            //GET IMPORTANT STUFF FROM SRF
            cellSrf.SetDomain(0, new Interval(0.0, 1.0));
            cellSrf.SetDomain(1, new Interval(0.0, 1.0));

            cellSrf.Evaluate(uv.X, uv.Y, 3, out Point3d evalPt, out Vector3d[] derivatives);

            cellSrf.Evaluate(1.0, 0.0, 3, out Point3d pX, out Vector3d[] d3);
            cellSrf.Evaluate(0.0, 1.0, 3, out Point3d pY, out Vector3d[] d2);

            Vector3d vecX = new Vector3d(pX);
            Vector3d vecY = new Vector3d(pY);

            DA.SetData("Eval Point", evalPt);

            //SET BASE TRANSFORM FOR UNIFORMITY
            Transform t = SetProperTransfrom(evalPt, cellSrf, oneCellTransform);
            if (!t.TryGetInverse(out Transform invertedT))
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Problem recalculating transforms.");
            evalPt.Transform(invertedT);


            //ARRAY POINTS
            List<Point3d> pointArray = ArrayPoints(oneCellTransform, evalPt, vecX, vecY, out int indexOfBaseCell);

            //VORONOI DOMAIN
            PolylineCurve polylineCurve = GetVoronoiDomain(pointArray, t, indexOfBaseCell);
            DA.SetData("Fundamental Domain", polylineCurve);

            //NEW TRANSFORMS
            GH_Structure<GH_Transform> allNewTransforms = RecalculateTransforms(transformations, invertedT);
            DA.SetDataTree(0, allNewTransforms);
        }

        protected PolylineCurve GetVoronoiDomain(List<Point3d> pointArray, Transform t, int i)
        {
            Node2List nodes = new Node2List(pointArray);
            double x0 = double.NaN;
            double x1 = double.NaN;
            double y0 = double.NaN;
            double y1 = double.NaN;
            nodes.BoundingBox(2.0, false, ref x0, ref x1, ref y0, ref y1);
            Node2List node2List = new Node2List();
            node2List.Append(new Node2(x0, y0));
            node2List.Append(new Node2(x1, y0));
            node2List.Append(new Node2(x1, y1));
            node2List.Append(new Node2(x0, y1));

            List<Cell2> cell2List;
            cell2List = Grasshopper.Kernel.Geometry.Voronoi.Solver.Solve_BruteForce(nodes, (IEnumerable<Node2>)node2List.InternalList);
            Polyline polyline = cell2List[i].ToPolyline();
            PolylineCurve polylineCurve = new PolylineCurve((IEnumerable<Point3d>)polyline);
            polylineCurve.Transform(t);
            return polylineCurve;
        }

        protected GH_Structure<GH_Transform> RecalculateTransforms(GH_Structure<GH_Transform> transforms, Transform tToAdd)
        {
            GH_Structure<GH_Transform> newTransforms = new GH_Structure<GH_Transform>();
            int treeIndex = 0;
            for (int i = 0; i < transforms.Branches.Count; i++)
            {
                GH_Structure<GH_Transform> newBranchTransform = new GH_Structure<GH_Transform>();
                List<GH_Transform> branch = (List<GH_Transform>)transforms.get_Branch(i);
                for (int j = 0; j < branch.Count; j++)
                {
                    Transform t = branch[j].Value;
                    newBranchTransform.Append(new GH_Transform(t * tToAdd));
                }
                newTransforms.AppendRange(newBranchTransform, new GH_Path(treeIndex));
                treeIndex++;
            }
            return newTransforms;
        }

        protected List<Point3d> ArrayPoints(List<GH_Transform> transforms, Point3d basePt, Vector3d vecX, Vector3d vecY, out int index)
        {
            List<Point3d> ArrayPoints = new List<Point3d>();
            index = 0;
            for (int i = -1; i <= 1; i++)
            {
                Transform transl1 = Transform.Translation((vecX * i));
                for (int j = -1; j <= 1; j++)
                {
                    Transform transl2 = Transform.Translation(vecY * j);
                    for (int k = 0; k < transforms.Count; k++)
                    {
                        Transform transExist = transforms[k].Value;
                        Point3d transPoint = new Point3d(basePt);
                        transPoint.Transform(transl1 * transl2 * transExist);
                        if (transPoint.Equals(basePt)) index = ArrayPoints.Count;
                        ArrayPoints.Add(transPoint);
                    }
                }
            }
            return ArrayPoints;
        }
        protected Transform SetProperTransfrom(Point3d evalPt, Surface baseSrf, List<GH_Transform> transforms)
        {
            for (int i = 0; i < transforms.Count; i++)
            {
                Transform curT = transforms[i].Value;
                if (!curT.TryGetInverse(out Transform invertedT))
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Problem recalculating transforms.");
                Point3d invertedPt = new Point3d(evalPt);
                invertedPt.Transform(invertedT);

                Boolean pointsContained = true;
                for (int j = transforms.Count - 1; j >= 0; j--)
                {
                    Point3d testPt = new Point3d(invertedPt);
                    testPt.Transform(transforms[j].Value);

                    baseSrf.ClosestPoint(testPt, out double u, out double v);
                    Point3d srfPt = baseSrf.PointAt(u, v);

                    //if (0.0 > u || u > 1.0 || 0.0 > v || v > 1.0)
                    if (testPt.DistanceTo(srfPt) > 0.1)
                    {
                        pointsContained = false;
                        //break;
                    }
                }
                if (pointsContained)
                {
                    return curT;
                }
            }
            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No proper inverted transform.");
            return Transform.Unset;
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
                return Resources.GetVoronoiDomain;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("C64D0BC2-34D1-4AD1-B621-E934CB5F19AB"); }
        }
    }
}