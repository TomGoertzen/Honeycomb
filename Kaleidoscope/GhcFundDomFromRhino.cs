using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Honeycomb.Properties;
using Rhino.Geometry;
using System;

namespace Honeycomb
{
    public class GhcFundDomFromRhino : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcFundDomFromRhino class.
        /// </summary>
        public GhcFundDomFromRhino()
          : base("Get Fundamental Domain From Rhino",
                 "DIYFundDom",
                 "Use this component to generate a fundamental domain via rhino drive curves.",
                 "Honeycomb",
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

            //PAIR RHINO CURVES
            GH_Structure<GH_Curve> outputPairs = new GH_Structure<GH_Curve>();
            int indexOfPair = -1;
            foreach (GH_Curve curCurve in inputCurves)
            {
                GH_Curve matchedCurve = generate_rhino_curve_pair(curCurve, inputCurvePairs, inputTransforms, ref indexOfPair);
                if (indexOfPair == -1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input curves are not coincident with domain points.");
                    return;
                }
                outputPairs.Append(curCurve, new GH_Path(indexOfPair));
                outputPairs.Append(matchedCurve, new GH_Path(indexOfPair));
            }

            //IN CASE OF NOT ALL PAIRS ARE ACCOUNTED FOR
            for (int i = 0; i < inputCurvePairs.Branches.Count; i++)
            {
                if (!outputPairs.PathExists(new GH_Path(i)))
                {
                    outputPairs.Append(inputCurvePairs.Branches[i][0], new GH_Path(i));
                    outputPairs.Append(inputCurvePairs.Branches[i][1], new GH_Path(i));
                }
            }

            DA.SetDataTree(0, outputPairs);
        }

        private GH_Curve generate_rhino_curve_pair(GH_Curve curCurve, GH_Structure<GH_Curve> inputCurvePairs,
                                               GH_Structure<GH_Transform> inputTransforms, ref int indexOfPair)
        {
            for (int i = 0; i < inputCurvePairs.Branches.Count; i++)
            {
                for (int j = 0; j < inputCurvePairs.Branches[i].Count; j++)
                {
                    Curve testCurve = inputCurvePairs.Branches[i][j].Value;
                    if (Utilities.endpoints_are_similar(curCurve.Value, testCurve))
                    {
                        Transform t = inputTransforms[i][0].Value;
                        Curve transformedCurve = curCurve.Value.DuplicateCurve();
                        transformedCurve.Transform(t);
                        if (is_valid_transfromed_curve(transformedCurve, inputCurvePairs))
                        {
                            indexOfPair = i;
                            return new GH_Curve(transformedCurve);
                        }
                        else
                        {
                            t.TryGetInverse(out Transform tI);
                            Curve transformedInverseCurve = curCurve.Value.DuplicateCurve();
                            transformedInverseCurve.Transform(tI);
                            if (is_valid_transfromed_curve(transformedInverseCurve, inputCurvePairs))
                            {
                                indexOfPair = i;
                                return new GH_Curve(transformedInverseCurve);
                            }
                        }
                    }
                }
            }
            return null;
        }

        private bool is_valid_transfromed_curve(Curve transformedCurve, GH_Structure<GH_Curve> inputCurvePairs)
        {
            for (int i = 0; i < inputCurvePairs.Branches.Count; i++)
            {
                for (int j = 0; j < inputCurvePairs.Branches[i].Count; j++)
                {
                    Curve testCurve = inputCurvePairs.Branches[i][j].Value;
                    if (Utilities.endpoints_are_similar(transformedCurve, testCurve))
                    {
                        return true;
                    }
                }
            }
            return false;
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