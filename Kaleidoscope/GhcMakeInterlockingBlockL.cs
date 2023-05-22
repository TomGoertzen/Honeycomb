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
    public class GhcMakeInterlockingBlockL : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcMakeInterlockingBlockL class.
        /// </summary>
        public GhcMakeInterlockingBlockL()
          : base("Make Interlocking Block: Loft",
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
            pManager.AddCurveParameter("Inital Set", "I", "Inital Set of paired curves.", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Amended Set", "A", "Amended Set of paired curves.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Offset", "O", "Offset between intial and amended domains.", GH_ParamAccess.item, 1.0);
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
            DA.GetDataTree(0, out GH_Structure<GH_Curve> firstCurves);
            DA.GetDataTree(1, out GH_Structure<GH_Curve> secondCurves);
            DA.GetData(2, ref offset);

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

            List<Brep> brepsToJoin = new List<Brep> { cap1, cap2 };

            foreach (Curve curve1 in curveList1)
            {
                foreach (Curve curve2 in curveList2)
                {
                    if ((curve1.PointAtEnd.DistanceTo(curve2.PointAtEnd) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance &&
                        curve1.PointAtStart.DistanceTo(curve2.PointAtStart) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
                    {
                        curve2.Transform(offsetTransform);
                        List<Curve> c = new List<Curve> { curve1, curve2 };
                        Brep[] loft = Brep.CreateFromLoft(c, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
                        brepsToJoin.AddRange(loft);
                    }
                    else if (curve1.PointAtEnd.DistanceTo(curve2.PointAtStart) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance &&
                             curve1.PointAtStart.DistanceTo(curve2.PointAtEnd) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    {
                        curve2.Transform(offsetTransform);
                        curve1.Reverse();
                        List<Curve> c = new List<Curve> { curve1, curve2 };
                        Brep[] loft = Brep.CreateFromLoft(c, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
                        brepsToJoin.AddRange(loft);
                    }
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
                return Resources.MakeLoftedBlock;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1E3F4BDA-F5A5-4668-93E8-DCCAF90C6060"); }
        }
    }
}