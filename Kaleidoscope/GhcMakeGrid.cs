using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Kaleidoscope.Properties;
using Rhino.Display;
using Rhino.Geometry;

namespace Kaleidoscope
{
    public class GhcMakeGrid : GH_Component
    {
        /// Initializes a new instance of the GhcMakeGrid class.
        public GhcMakeGrid()
          : base("MakeGrid",
                 "MkGrd",
                 "Use this component to generate transformation data for your tilings",
                 "Kaleidoscope",
                 "Initialization")
        {
        }

        /// Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddScriptVariableParameter("Wallpaper Group", "WPG", "One of the 17 wallpaper groups as a string", GH_ParamAccess.item);

            pManager.AddNumberParameter("Cell Dimension 1", "cD1", "Dimension 1 of the base cell", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Cell Dimension 2", "cD2", "Dimension 2 of the base cell (when applicable)", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Cell Angle", "cA", "Angle of the base cell (when applicable)", GH_ParamAccess.item, 90.0);

            pManager.AddPointParameter("Grid Origin", "o", "Position of the grid in rhino-space", GH_ParamAccess.item, new Point3d(0.0, 0.0, 0.0));
            pManager.AddIntegerParameter("X Cell Repetitions", "numX", "Number of cells in the X Direction", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Y Cell Repetitions", "numY", "Number of cells in the Y Direction", GH_ParamAccess.item, 5);

            pManager.AddBooleanParameter("Show Grid Points", "sGrid", "Preview the grid geometry", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Show Base Cell", "sCell", "Preview the base cell geometry", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Show Fundamental Domain", "sFD", "Preview the fundamental domain geometry", GH_ParamAccess.item, true);
        }

        /// Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTransformParameter("Transform Data", "transf", "Tree containing transform data to be applied to a geometry", GH_ParamAccess.tree);
            pManager.AddPointParameter("Grid Points", "grid", "Geometry representing the cells of the grid", GH_ParamAccess.list);
            pManager.AddCurveParameter("Base Cell", "cell", "Geometry representing the cell boundary", GH_ParamAccess.list);
            pManager.AddCurveParameter("Fundamental Domain", "fd", "Geometry representing the suggested fundamental domain boundary", GH_ParamAccess.list);
        }

        /// This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ///

            string wallpaperGroup = string.Empty;
            DA.GetData("Wallpaper Group", ref wallpaperGroup);
            double cellDimension1 = double.NaN;
            double cellDimension2 = double.NaN;
            double cellAngle = double.NaN;
            DA.GetData("Cell Dimension 1", ref cellDimension1);
            DA.GetData("Cell Dimension 2", ref cellDimension2);
            DA.GetData("Cell Angle", ref cellAngle);
            Point3d origin = new Point3d(0.0, 0.0, 0.0);
            int cellsX = int.MinValue;
            int cellsY = int.MinValue;
            DA.GetData("Grid Origin", ref origin);
            DA.GetData("X Cell Repetitions", ref cellsX);
            DA.GetData("Y Cell Repetitions", ref cellsY);
            bool showGrid = true;
            bool showBaseCell = true;
            bool showFundDomain = true;
            DA.GetData("Show Grid Points", ref showGrid);
            DA.GetData("Show Base Cell", ref showBaseCell);
            DA.GetData("Show Fundamental Domain", ref showFundDomain);

            ///

            Vector3d vecX = new Vector3d(Vector3d.XAxis) * cellDimension1;
            Vector3d vecY = Vector3d.XAxis * cellDimension2;
            vecY.Rotate(Math.PI * cellAngle / 180.0, Vector3d.ZAxis);

            Point3d midpoint = origin + (vecY / 2) + (vecX / 2);

            //Main Script
            List<Transform> transformsWPG = GetTransformsFromWPG(wallpaperGroup, midpoint);
            GH_Structure<GH_Transform> allTransforms = CalculateAllTransforms(transformsWPG, vecX, vecY, cellsX, cellsY);

            ///

            DA.SetDataTree(0, allTransforms);
            if (showBaseCell)
            {
                List<Curve> cellOutlines = MakeCelloutlines(origin, vecX, vecY);
                DA.SetDataList("Base Cell", cellOutlines);
            }
            if (showGrid)
            {
                GH_Structure<GH_Point> gridPoints = MakeGridPoints(vecX, vecY, cellsX, cellsY);
                DA.SetDataList("Grid Points", gridPoints);
            }
            if (showFundDomain)
            {
                DA.SetDataList("Fundamental Domain", null);
            }

            ///
        }

        private List<Transform> GetTransformsFromWPG(string wallpaperGroup, Point3d midpoint)
        {
            List<Transform> baseCellTransform = new List<Transform>();
            baseCellTransform.Add(Transform.Identity);
            if (wallpaperGroup == "p1") { }
            else if (wallpaperGroup == "p2")
            {
                Transform p2Rotation = Transform.Rotation(Math.PI * 180 / 180.0, Vector3d.ZAxis, midpoint);
                baseCellTransform.Add(p2Rotation);
            }
            else if (wallpaperGroup == "pm") 
            { }
            else if (wallpaperGroup == "pg") 
            { }
            else if (wallpaperGroup == "cm") 
            { }
            else if (wallpaperGroup == "pmm") 
            { }
            else if (wallpaperGroup == "pmg") 
            { }
            else if (wallpaperGroup == "pgg") 
            { }
            else if (wallpaperGroup == "cmm") 
            { }
            else if (wallpaperGroup == "p4") 
            { }
            else if (wallpaperGroup == "p4m") 
            { }
            else if (wallpaperGroup == "p4g") 
            { }
            else if (wallpaperGroup == "p3") 
            { }
            else if (wallpaperGroup == "p31m") 
            { }
            else if (wallpaperGroup == "p3m1") 
            { }
            else if (wallpaperGroup == "p6") 
            { }
            else if (wallpaperGroup == "p6m") 
            { }
            return baseCellTransform;
        }
        private GH_Structure<GH_Transform> CalculateAllTransforms(List<Transform> baseCellTransform, 
                                                                  Vector3d vecX, Vector3d vecY, 
                                                                  double cellsX, double cellsY)
        {
            GH_Structure<GH_Transform> translateTransforms = new GH_Structure<GH_Transform>();
            int treeIndex = 0;
            for (int i = 0; i < cellsX; i++)
            {
                for (int j = 0; j < cellsY; j++)
                {
                    GH_Structure<GH_Transform> newCellTransform = new GH_Structure<GH_Transform>();
                    Transform t = Transform.Translation((vecX * i) + (vecY * j));
                    for (int transformIndex = 0; transformIndex < baseCellTransform.Count; transformIndex++)
                    {
                        newCellTransform.Append(new GH_Transform(t * baseCellTransform[transformIndex]));
                    }
                    translateTransforms.AppendRange(newCellTransform, new GH_Path(treeIndex));
                    treeIndex++;
                }
            }
            return translateTransforms;
        }

        private List<Curve> MakeCelloutlines(Point3d origin, Vector3d vecX, Vector3d vecY)
        {
            List<Curve> cellOutlines = new List<Curve>();
            LineCurve xBottom = new LineCurve(origin, origin + vecX);
            LineCurve yBottom = new LineCurve(origin, origin + vecY);
            LineCurve xTop = new LineCurve(origin + vecY, origin + vecX + vecY);
            LineCurve yTop = new LineCurve(origin + vecX, origin + vecX + vecY);
            cellOutlines.Add(xBottom);
            cellOutlines.Add(yBottom);
            cellOutlines.Add(xTop);
            cellOutlines.Add(yTop);
            return cellOutlines;
        }

        private GH_Structure<GH_Point> MakeGridPoints(Vector3d vecX, Vector3d vecY, double cellsX, double cellsY)
        {
            GH_Structure<GH_Point> gridPoints = new GH_Structure<GH_Point>();
            for (int i = 0; i <= cellsX; i++)
            {
                for (int j = 0; j <= cellsY; j++)
                {
                    int[] path = new int[] { i, j };
                    GH_Point newPt = new GH_Point(new Point3d(vecX * i + vecY * j));
                    gridPoints.Append(newPt, new GH_Path(path));
                }
            }
            return gridPoints;
        }

        /// Provides an Icon for the component.
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.PluginIcon;
            }
        }

        /// Gets the unique ID for this component. Do not change this ID after release.
        public override Guid ComponentGuid
        {
            get { return new Guid("AA866C08-4037-4CF2-8461-74E161510615"); }
        }
    }
}