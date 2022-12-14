using System;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Kaleidoscope
{
    public class GhcMakeGrid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcMakeGrid class.
        /// </summary>
        public GhcMakeGrid()
          : base("MakeGrid", 
                 "MkGrd",
                 "Use this component to generate transformation data for your tilings",
                 "Kaleidoscope",
                 "Initialization")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddScriptVariableParameter("Wallpaper Group", "WPG", "One of the 17 wallpaper groups as a string", GH_ParamAccess.item);

            pManager.AddNumberParameter("Cell Dimension 1", "cD1", "Dimension 1 of the base cell", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Cell Dimension 2", "cD2", "Dimension 2 of the base cell (when applicable)", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Cell Angle", "cA", "Angle of the base cell (when applicable)", GH_ParamAccess.item, 90.0);

            pManager.AddPointParameter("Grid Origin", "o", "Position of the grid in rhino-space", GH_ParamAccess.item, new Point3d(0.0,0.0,0.0));
            pManager.AddIntegerParameter("X Cell Repetitions", "numX", "Number of cells in the X Direction", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Y Cell Repetitions", "numY", "Number of cells in the Y Direction", GH_ParamAccess.item, 5);

            pManager.AddBooleanParameter("Show Grid Points", "sGrid", "Preview the grid geometry", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Show Base Cell", "sCell", "Preview the base cell geometry", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Show Fundamental Domain", "sFD", "Preview the fundamental domain geometry", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTransformParameter("Transform Data", "transf", "Tree containing transform data to be applied to a geometry", GH_ParamAccess.tree);
            pManager.AddPointParameter("Grid Points", "grid", "Geometry representing the cells of the grid", GH_ParamAccess.list);
            pManager.AddCurveParameter("Base Cell", "cell", "Geometry representing the cell boundary", GH_ParamAccess.list);
            pManager.AddCurveParameter("Fundamental Domain", "fd", "Geometry representing the suggested fundamental domain boundary", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string wallpaperGroup = string.Empty;
            DA.GetData("Wallpaper Group", ref wallpaperGroup);

            double cellDimension1 = double.NaN;
            double cellDimension2 = double.NaN;
            double cellAngle = double.NaN;
            DA.GetData("Cell Dimension 1", ref cellDimension1);
            DA.GetData("Cell Dimension 2", ref cellDimension2);
            DA.GetData("Cell Angle", ref cellAngle);

            Point3d origin = new Rhino.Geometry.Point3d(0.0, 0.0, 0.0);
            int cellRepX = int.MinValue;
            int cellRepY = int.MinValue;
            DA.GetData("Grid Origin", ref origin);
            DA.GetData("X Cell Repetitions", ref cellRepX);
            DA.GetData("Y Cell Repetitions", ref cellRepY);

            bool showGrid = true;
            bool showBaseCell = true;
            bool showFundDomain = true;
            DA.GetData("Show Grid Points", ref showGrid);
            DA.GetData("Show Base Cell", ref showBaseCell);
            DA.GetData("Show Fundamental Domain", ref showFundDomain);


            //Actual Code//


            ///VISUALIZATION OUTPUTS INITIALIZATION
            List<Point3d> gridPoints = new List<Point3d>();
            List<Curve> cellOutlines = new List<Curve>();

            ///TRANSORM INITIALIZATION
            GH_Structure<GH_Transform> translateTransforms = new GH_Structure<GH_Transform>();

            ///GRID VECTORS
            Vector3d richtungX = new Vector3d(Vector3d.XAxis) * cellDimension1;
            Vector3d richtungY = Vector3d.XAxis * cellDimension2;
            richtungY.Rotate(Math.PI * cellAngle / 180.0, Vector3d.ZAxis);

            ///CELL MIDPOINT
            Point3d midpoint = origin + (richtungY / 2) + (richtungX / 2);

            ///SPECIFIC CELL TRANSFROMS PER WALLPAPER GROUPS -- to helper function
            List<Transform> baseCellTransform = new List<Transform>();
            baseCellTransform.Add(Transform.Identity);
            if (wallpaperGroup == "p2")
            {
                Transform p2Rotation = Transform.Rotation(Math.PI * 180 / 180.0, Vector3d.ZAxis, midpoint);
                baseCellTransform.Add(p2Rotation);
            }

            ///CREATE THE TRANSFORMATION TREE -- to helper function
            int treeIndex = 0;
            for (int i = 0 / 2; i < cellRepX; i++)
            {
                for (int j = 0 / 2; j < cellRepY; j++)
                {
                    GH_Structure<GH_Transform> newCellTransform = new GH_Structure<GH_Transform>();
                    Transform t = Transform.Translation((richtungX * i) + (richtungY * j));
                    for (int transformIndex = 0; transformIndex < baseCellTransform.Count; transformIndex++)
                    {
                        newCellTransform.Append(new GH_Transform(t * baseCellTransform[transformIndex]));
                    }
                    translateTransforms.AppendRange(newCellTransform, new GH_Path(treeIndex));

                    ///GRID POINTS
                    Point3d point = new Point3d(richtungX * i + richtungY * j);
                    gridPoints.Add(point);

                    treeIndex++;
                }
            }

            ///MAKE BASE CELL -- to helper function
            LineCurve xBottom = new LineCurve(origin, origin + richtungX);
            LineCurve yBottom = new LineCurve(origin, origin + richtungY);
            LineCurve xTop = new LineCurve(origin + richtungY, origin + richtungX + richtungY);
            LineCurve yTop = new LineCurve(origin + richtungX, origin + richtungX + richtungY);
            cellOutlines.Add(xBottom);
            cellOutlines.Add(yBottom);
            cellOutlines.Add(xTop);
            cellOutlines.Add(yTop);

            ///OUTPUTS
            DA.SetDataTree(0, translateTransforms);
            if (showBaseCell) DA.SetDataList("Base Cell", cellOutlines); //organize to speed up code
            if (showGrid) DA.SetDataList("Grid Points",gridPoints); //organize to speed up code
            if (showFundDomain) DA.SetDataList("Fundamental Domain", null); //organize to speed up code
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
            get { return new Guid("AA866C08-4037-4CF2-8461-74E161510615"); }
        }
    }
}