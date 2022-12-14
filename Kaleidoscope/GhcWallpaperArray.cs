using System;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Types.Transforms;
using Rhino.Geometry;

namespace Kaleidoscope
{
    public class GhcWallpaperArray : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcWallpaperArray class.
        /// </summary>
        public GhcWallpaperArray()
          : base("Wallpaper Array",
                 "WllpprArry",
                 "Use wallpaper group transformation data to array geometry across the grid",
                 "Kaleidoscope",
                 "Initialization")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Input Geometry", "g", "Geometry to tile the grid", GH_ParamAccess.item);
            pManager.AddTransformParameter("Transformation Data", "t", "Transfromations to apply to geometry", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Output Geometry", "og", "Resulting transformed geometry", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GeometryBase geometry = null;
            DA.GetData(0, ref geometry);
            DA.GetDataTree(1, out GH_Structure<GH_Transform> transformations);

            DataTree<GeometryBase> outGeometry = new DataTree<GeometryBase>();
            for (int i = 0; i < transformations.Branches.Count; i++)
            {
                for (int j = 0; j < transformations.get_Branch(i).Count; j++)
                {
                    GeometryBase newItem = geometry.Duplicate();
                    int[] path = new int[] { i, j };
                    transformations.get_Branch(i)[j].CastTo<Transform>(out Transform t);
                    newItem.Transform(t);
                    outGeometry.Add(newItem, new GH_Path(path));
                }
            }
            
            DA.SetDataTree(0,outGeometry);
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
            get { return new Guid("79C27390-C4F4-4F60-80FF-B3146DB3870B"); }
        }
    }
}