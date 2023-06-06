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
    public class GhcMakeInterlockingBlockT : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcMakeInterlockingBlock class.
        /// </summary>
        public GhcMakeInterlockingBlockT()
          : base("Make Interlocking Block: Triangulate",
                 "InterTri",
                 "Use this component to triangluate between two sets of paired edges.",
                 "Honeycomb",
                 "Tiling")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Inital Set", "I", "Inital Set of paired curves.", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Amended Set", "A", "Amended Set of paired curves.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Offset", "O", "Offset between intial and amended domains.", GH_ParamAccess.item, 1.0);
            pManager.AddBooleanParameter("Cap", "C", "Enable or disable tops of geometry.", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Output Block", "O", "Resulting interlocking block.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double offset = double.NaN;
            Boolean capTops = true;
            DA.GetDataTree(0, out GH_Structure<GH_Curve> firstCurves);
            DA.GetDataTree(1, out GH_Structure<GH_Curve> secondCurves);
            DA.GetData(2, ref offset);
            DA.GetData(3, ref capTops);

            Transform offsetTransform = Transform.Translation(new Vector3d(0, 0, offset));

            GH_Structure<GH_Curve> flattenedCurves1 = firstCurves.Duplicate();
            GH_Structure<GH_Curve> flattenedCurves2 = secondCurves.Duplicate();
            flattenedCurves1.Flatten();
            flattenedCurves2.Flatten();
            List<Curve> curveList1 = new List<Curve>();
            List<Curve> curveList2 = new List<Curve>();
            foreach (GH_Curve ghCurve in flattenedCurves1) curveList1.Add(ghCurve.Value);
            foreach (GH_Curve ghCurve in flattenedCurves2) curveList2.Add(ghCurve.Value);
            Curve[] curveArray1 = curveList1.ToArray();
            Curve[] curveArray2 = curveList2.ToArray();
            Curve boundary1 = Curve.JoinCurves(curveArray1)[0];
            Curve boundary2 = Curve.JoinCurves(curveArray2)[0];
            Brep cap1 = Brep.CreatePlanarBreps(boundary1, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)[0];
            Brep cap2 = Brep.CreatePlanarBreps(boundary2, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)[0];
            cap2.Transform(offsetTransform);

            List<Polyline> polylines = new List<Polyline>();
            foreach (Curve curve in curveList2)
            {
                if (curve.TryGetPolyline(out Polyline polyline))
                {
                    polyline.Transform(offsetTransform);
                    polylines.Add(polyline);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Amended curves not polylines. Try using 'Make Interlocking Block Loft.'");
                    return;
                }
            }

            List<Brep> brepsToJoin = new List<Brep>();

            if (capTops)
            {
                brepsToJoin.Add(cap1);
                brepsToJoin.Add(cap2);
            }

            for (int i = 0; i < curveList1.Count; i++)
            {
                Curve curCurve = curveList1[i];
                if (curCurve.PointAtStart.DistanceTo(polylines[i][0]) > curCurve.PointAtEnd.DistanceTo(polylines[i][0])) curCurve.Reverse();

                if (polylines[i].Count == 3)
                {
                    Brep tri1 = Brep.CreateFromCornerPoints(curCurve.PointAtStart, polylines[i][0], polylines[i][1], RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    Brep tri2 = Brep.CreateFromCornerPoints(curCurve.PointAtStart, curCurve.PointAtEnd, polylines[i][1], RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    Brep tri3 = Brep.CreateFromCornerPoints(curCurve.PointAtEnd, polylines[i][1], polylines[i][2], RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

                    brepsToJoin.Add(tri1);
                    brepsToJoin.Add(tri2);
                    brepsToJoin.Add(tri3);
                }
                else if (polylines[i].Count == 2)
                {
                    Brep quad = Brep.CreateFromCornerPoints(curCurve.PointAtStart, polylines[i][0], polylines[i][1], curCurve.PointAtEnd, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

                    brepsToJoin.Add(quad);
                }
            }

            Brep[] joinedBreps = Brep.JoinBreps(brepsToJoin, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

            if (joinedBreps.Length > 1) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Error closing OutputBrep.");

            DA.SetDataList(0, joinedBreps.ToList());
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
                return Resources.MakeInterlockingBlock;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3861760F-B611-42C7-BED9-20AA45DAD6A4"); }
        }
    }
}