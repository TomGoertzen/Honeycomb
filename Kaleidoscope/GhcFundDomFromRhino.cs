using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Kaleidoscope.Properties;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Kaleidoscope
{
    public class GhcFundDomFromRhino : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcFundDomFromRhino class.
        /// </summary>
        public GhcFundDomFromRhino()
          : base("Make Fundamental Domain From Rhino",
                 "DIYFundDom",
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
            pManager.AddCurveParameter("Rhino Input Curves", "I", "Curves to transform", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Curve Pairs", "P", "Use 'Pair Domain Edges' to produce this input.", GH_ParamAccess.tree);
            pManager.AddTransformParameter("Curve Pair Transforms", "T", "Use 'Pair Domain Edges' to produce this input.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Output Curves", "O", "DIY output curves.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree(0, out GH_Structure<GH_Curve> inputCurves);
            DA.GetDataTree(1, out GH_Structure<GH_Curve> inputCurvePairs);
            DA.GetDataTree(2, out GH_Structure<GH_Transform> inputTransforms);

            inputCurvePairs.Flatten();

            List<Point3d> gridPoints = new List<Point3d>();
            foreach (GH_Curve curve in inputCurvePairs)
            {
                gridPoints.Add(curve.Value.PointAtStart);
                gridPoints.Add(curve.Value.PointAtEnd);
            }

            GH_Structure<GH_Curve> outputCurves = new GH_Structure<GH_Curve>();
            int treeIndex = 0;

            foreach (GH_Curve ghCurve in inputCurves)
            {
                Curve curve = ghCurve.Value;
                bool matchFound = false;
                if (!IsValidCurve(gridPoints, curve))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input curves not coincident with grid points.");
                    return;
                }
                foreach (GH_Transform t in inputTransforms)
                {
                    Curve testCurve = curve.DuplicateCurve();
                    testCurve.Transform(t.Value);
                    if (IsValidCurve(gridPoints, testCurve))
                    {
                        matchFound = true;
                        outputCurves.Append(ghCurve, new GH_Path(treeIndex));
                        outputCurves.Append(new GH_Curve(testCurve), new GH_Path(treeIndex));
                        treeIndex++;
                        break;
                    }
                    if (t.Value.TryGetInverse(out Transform tI))
                    {
                        Curve testCurveInverse = curve.DuplicateCurve();
                        testCurveInverse.Transform(tI);
                        if (IsValidCurve(gridPoints, testCurveInverse))
                        {
                            matchFound = true;
                            outputCurves.Append(ghCurve, new GH_Path(treeIndex));
                            outputCurves.Append(new GH_Curve(testCurveInverse), new GH_Path(treeIndex));
                            treeIndex++;
                            break;
                        }
                    }
                }
                if (!matchFound) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Something went wrong.");
            }


            DA.SetDataTree(0, outputCurves);
        }

        protected bool IsValidCurve(List<Point3d> gridPoints, Curve curve)
        {
            Point3d startPt = curve.PointAtStart;
            Point3d endPt = curve.PointAtEnd;
            bool endIsValid = false;
            bool startIsValid = false;
            foreach (Point3d point in gridPoints)
            {
                if (startPt.DistanceTo(point) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) startIsValid = true;
                if (endPt.DistanceTo(point) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) endIsValid = true;
            }
            if (startIsValid && endIsValid) return true;
            else return false;
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
                return Resources.FromRhino;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("51F45288-C2CF-4557-B819-BA9D4BD71694"); }
        }
    }
}