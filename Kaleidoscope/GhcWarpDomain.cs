using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Honeycomb.Properties;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Honeycomb
{
    public class GhcWarpDomain : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcWarpDomain class.
        /// </summary>
        public GhcWarpDomain()
          : base("Warp Domain",
                 "WrpDom",
                 "Use this component to change the fundamental domain via input points.",
                 "Honeycomb",
                 "Tiling")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point List", "P", "Points to amend the fundamental domain with.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Paired Input Curves", "I", "Use 'Pair Domain Edges' to produce this input.", GH_ParamAccess.tree);
            pManager.AddTransformParameter("Pair Transformations", "T", "Use 'Pair Domain Edges' to produce this input.", GH_ParamAccess.tree);
            //pManager.AddBooleanParameter("Flip Pair List", "F", "Reorder pairs of input curves", GH_ParamAccess.list);
            //pManager.AddBooleanParameter("Interpolate", "S", "If true, construct interpolated curves. Else, construct polyline.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Paired Output Curves", "O", "Amended fundamental domain.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> inPoints = new List<Point3d>();
            DA.GetDataList(0, inPoints);
            DA.GetDataTree(1, out GH_Structure<GH_Curve> inputCurves);
            DA.GetDataTree(2, out GH_Structure<GH_Transform> inputTransforms);

            int numReq = inputTransforms.Branches.Count;
            int numGiven = inPoints.Count;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Warping this domain requires a minimum of " + numReq + " input points.");

            if (numGiven < numReq)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Not enough input points.");
                return;
            }

            GH_Structure<GH_Curve> curves = new GH_Structure<GH_Curve>();
            for (int i = 0; i < inputCurves.Branches.Count; i++)
            {
                if (inputTransforms.PathExists(new GH_Path(i)))
                {
                    Point3d s = inputCurves.Branches[i][0].Value.PointAtStart;
                    Point3d e = inputCurves.Branches[i][0].Value.PointAtEnd;
                    List<Point3d> ptsToCurve = new List<Point3d> { s, inPoints[i], e };
                    Curve newCurve = new Polyline(ptsToCurve).ToPolylineCurve();
                    Curve transCurve = (Curve)newCurve.Duplicate();
                    Transform t = ((List<GH_Transform>)(inputTransforms.get_Branch(new GH_Path(i))))[0].Value;
                    transCurve.Transform(t);
                    curves.Append(new GH_Curve(newCurve), new GH_Path(i));
                    curves.Append(new GH_Curve(transCurve), new GH_Path(i));
                }

                else
                {
                    foreach (GH_Curve curve in inputCurves.Branches[i])
                    {
                        curves.Append(curve, new GH_Path(i));
                    }
                }
            }

            DA.SetDataTree(0, curves);
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
                return Resources.WarpDomain;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("94E5C9B2-B57C-4AEE-87E0-639EC272F295"); }
        }
    }
}