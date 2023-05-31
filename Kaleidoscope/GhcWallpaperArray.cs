using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Honeycomb.Properties;
using Rhino.Geometry;
using System;

namespace Honeycomb
{
    public class GhcWallpaperArray : GH_Component
    {
        /// Initializes a new instance of the GhcWallpaperArray class.
        public GhcWallpaperArray()
          : base("Wallpaper Array",
                 "WPArry",
                 "Use wallpaper group transformation data to array geometry across the grid",
                 "Honeycomb",
                 "Tiling")
        {
        }

        /// Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Input Geometry", "G", "Geometry to tile the grid", GH_ParamAccess.item);
            pManager.AddTransformParameter("Transformation Data", "T", "Transfromations to apply to geometry", GH_ParamAccess.tree);
        }

        /// Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Output Geometry", "G", "Resulting transformed geometry", GH_ParamAccess.tree);
        }

        /// This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ///

            Object geometry = null;
            DA.GetData(0, ref geometry);
            DA.GetDataTree(1, out GH_Structure<GH_Transform> transformations);

            ///

            DataTree<GeometryBase> outGeometry = new DataTree<GeometryBase>();
            for (int i = 0; i < transformations.Branches.Count; i++)
            {
                for (int j = 0; j < transformations.get_Branch(i).Count; j++)
                {
                    GeometryBase newItem = GH_Convert.ToGeometryBase(geometry).Duplicate();
                    int[] path = new int[] { i, j };
                    GH_Transform gh_t = (GH_Transform)transformations.get_Branch(i)[j];
                    newItem.Transform(gh_t.Value);
                    outGeometry.Add(newItem, new GH_Path(path));
                }
            }

            ///

            DA.SetDataTree(0, outGeometry);

            ///
        }

        /// Provides an Icon for the component.
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.PluginIcon;
            }
        }

        /// Gets the unique ID for this component. Do not change this ID after release.
        public override Guid ComponentGuid
        {
            get { return new Guid("79C27390-C4F4-4F60-80FF-B3146DB3870B"); }
        }
    }
}