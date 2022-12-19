using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Geometry.SpatialTrees;
using Grasshopper.Kernel.Geometry.Voronoi;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Kaleidoscope
{
    public class GhcVoronoiDomain : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcVoronoiDomain class.
        /// </summary>
        public GhcVoronoiDomain()
          : base("VoronoiDomain",
                 "VorDom",
                 "Use this component to generate a voronoi fundamental domain.",
                 "Kaleidoscope",
                 "Tiling")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTransformParameter("Transformation Data", "T", "Transformations based on wallpaper group", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Base Cell", "c", "Boundary of the Wallpaper's base cell", GH_ParamAccess.item);
            pManager.AddPointParameter("UV Coordinates", "UV", "Coordinates of the evaluated Point", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Eval Point", "P", "The evaluated point", GH_ParamAccess.item);
            pManager.AddPointParameter("Trans Points", "P", "The evaluated point", GH_ParamAccess.list);
            pManager.AddCurveParameter("Fundamental Domain", "FD", "Geometry representing the suggested fundamental domain boundary", GH_ParamAccess.item);
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

            List<GH_Transform> oneCellTransform = (List<GH_Transform>)transformations.get_Branch(0);

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

            cellSrf.SetDomain(0, new Interval(0.0, 1.0));
            cellSrf.SetDomain(1, new Interval(0.0, 1.0));

            cellSrf.Evaluate(uv.X, uv.Y, 3, out Point3d evalPt, out Vector3d[] derivatives);
            
            cellSrf.Evaluate(1.0, 0.0, 3, out Point3d pX, out Vector3d[] d3);
            cellSrf.Evaluate(0.0, 1.0, 3, out Point3d pY, out Vector3d[] d2);

            Vector3d vecX = new Vector3d(pX);
            Vector3d vecY = new Vector3d(pY);

            DA.SetData("Eval Point", evalPt);

            //repeat in -1, 0, 1 dim x,y

            int indexOfEvalPt = int.MinValue;
            List<Point3d> pointTransform = new List<Point3d>();
            List<GH_Point> pt4Vor = new List<GH_Point>();
            Node2List nodes = new Node2List();
            for (int i = -1; i <= 1; i ++)
            {
                Transform transl1 = Transform.Translation((vecX * i));
                for (int j = -1; j <= 1; j ++)
                {
                    Transform transl2 = Transform.Translation(vecY * j);
                    for (int k = 0; k < oneCellTransform.Count; k++)
                    {
                        Transform transExist = oneCellTransform[k].Value;
                        Point3d transPoint = new Point3d(evalPt);
                        transPoint.Transform(transl1 * transl2 * transExist);
                        if (transPoint.Equals(evalPt)) indexOfEvalPt = pointTransform.Count;
                        pointTransform.Add(transPoint);
                        pt4Vor.Add(new GH_Point(transPoint));
                        nodes.Append(new Node2((double)transPoint.X, (double)transPoint.Y));
                    }
                }
            }

            DA.SetDataList(1, pointTransform);

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

            Polyline polyline = cell2List[indexOfEvalPt].ToPolyline();
            PolylineCurve polylineCurve = new PolylineCurve((IEnumerable<Point3d>)polyline);
                
            DA.SetData("Fundamental Domain", polylineCurve);
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
                return null;
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