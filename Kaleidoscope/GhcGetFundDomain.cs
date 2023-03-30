using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Kaleidoscope.Properties;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Kaleidoscope
{
    public class GhcGetFundDomain : GH_Component
    {
        /// Initializes a new instance of the GhcMakeGrid class.
        public GhcGetFundDomain()
          : base("GetFundamentalDomain",
                 "FundD",
                 "Use this component to generate transformation data and a gemetrical boundary for your tilings",
                 "Kaleidoscope",
                 "Tiling")
        {
        }
        private bool _useDegrees = false;

        /// Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddScriptVariableParameter("Wallpaper Group", "WPG", "One of the 17 wallpaper groups as a string", GH_ParamAccess.item);

            pManager.AddNumberParameter("Cell Dimension 1", "C1", "Dimension 1 of the base cell", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Cell Dimension 2", "C2", "Dimension 2 of the base cell (when applicable)", GH_ParamAccess.item, 10.0);
            pManager.AddAngleParameter("Cell Angle", "CA", "Angle of the base cell (when applicable)", GH_ParamAccess.item, Math.PI / 2.0);

            pManager.AddPointParameter("Grid Origin", "O", "Position of the grid in rhino-space", GH_ParamAccess.item, new Point3d(0.0, 0.0, 0.0));
            pManager.AddIntegerParameter("X Cell Repetitions", "NumX", "Number of cells in the X Direction", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Y Cell Repetitions", "NumY", "Number of cells in the Y Direction", GH_ParamAccess.item, 5);
        }

        /// Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTransformParameter("Transform Data", "T", "Tree containing transform data to be applied to a geometry", GH_ParamAccess.tree);
            pManager.AddPointParameter("Grid Points", "G", "Geometry representing the cells of the grid", GH_ParamAccess.list);
            pManager.AddCurveParameter("Base Cell", "BC", "Geometry representing the cell boundary", GH_ParamAccess.item);
            pManager.AddCurveParameter("Fundamental Domain", "FD", "Geometry representing the suggested fundamental domain boundary", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            _useDegrees = false;
            Param_Number angleParameter = Params.Input[3] as Param_Number;
            if (angleParameter != null)
                _useDegrees = angleParameter.UseDegrees;
        }
        /// This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ///

            string wallpaperGroup = string.Empty;
            double cellD1 = double.NaN;
            double cellD2 = double.NaN;
            double cellAngle = double.NaN;
            Point3d origin = new Point3d(0.0, 0.0, 0.0);
            int cellsX = int.MinValue;
            int cellsY = int.MinValue;
            DA.GetData("Wallpaper Group", ref wallpaperGroup);
            DA.GetData("Cell Dimension 1", ref cellD1);
            DA.GetData("Cell Dimension 2", ref cellD2);
            if (!DA.GetData("Cell Angle", ref cellAngle)) return;
            if (_useDegrees) cellAngle = RhinoMath.ToRadians(cellAngle);
            DA.GetData("Grid Origin", ref origin);
            DA.GetData("X Cell Repetitions", ref cellsX);
            DA.GetData("Y Cell Repetitions", ref cellsY);

            ///

            //put this in the groups
            Vector3d vecX = Vector3d.XAxis;
            Vector3d vecY = Vector3d.XAxis;

            List<Transform> transformsWPG = GetTransformsFromWPG(wallpaperGroup, origin, cellAngle, cellD1, cellD2, ref vecX, ref vecY);
            GH_Structure<GH_Transform> allTransforms = CalculateAllTransforms(transformsWPG, vecX, vecY, cellsX, cellsY);

            ///

            DA.SetDataTree(0, allTransforms);
            PolylineCurve cellOutlines = MakeCelloutlines(wallpaperGroup, origin, vecX, vecY);
            //GH_Structure<GH_Point> gridPoints = MakeGridPoints(vecX, vecY, cellsX, cellsY);
            PolylineCurve fundDomain = SuggestFundamentalDomain(wallpaperGroup, origin, vecX, vecY, out List<Point3d> gridPoints);
            DA.SetData("Base Cell", cellOutlines);
            DA.SetDataList("Grid Points", gridPoints);
            DA.SetData("Fundamental Domain", fundDomain);

            ///
        }

        public static List<Transform> GetTransformsFromWPG(string wallpaperGroup, Point3d origin, double cellAngle,
                                                     double cellD1, double cellD2, ref Vector3d vecX, ref Vector3d vecY)
        {
            List<Transform> baseCellTransform = new List<Transform>();
            vecX *= cellD1;
            baseCellTransform.Add(Transform.Identity);
            if (wallpaperGroup == "p1")
            {
                vecY *= cellD2;
                vecY.Rotate(cellAngle, Vector3d.ZAxis);
            }
            else if (wallpaperGroup == "p2")
            {
                vecY *= cellD2;
                vecY.Rotate(cellAngle, Vector3d.ZAxis);
                Point3d rotationCenter = origin + (vecY / 2) + (vecX / 2);
                Transform rotation1 = Transform.Rotation(Math.PI, Vector3d.ZAxis, rotationCenter);
                baseCellTransform.Add(rotation1);
            }
            else if (wallpaperGroup == "pm")
            {
                vecY *= cellD2;
                vecY.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                Point3d mirrorPt = origin + (vecX / 2);
                Transform pmMirror = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecY));
                baseCellTransform.Add(pmMirror);
            }
            else if (wallpaperGroup == "pg")
            {
                vecY *= cellD2;
                vecY.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                Point3d mirrorPt = origin;
                Transform pgMirror = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecY));
                Transform pgTransl = Transform.Translation((vecY / 2.0) + vecX);
                baseCellTransform.Add(pgTransl * pgMirror);
            }
            else if (wallpaperGroup == "cm")
            {
                vecY *= cellD2;
                vecY.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                Point3d mirrorPt = origin + vecY;
                Transform pgMirrorY = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecY));
                Transform pgTransl1 = Transform.Translation(vecX);
                Transform pgTransl2 = Transform.Translation((vecX / 2.0) + (vecY / 2.0));
                baseCellTransform.Add(pgTransl1 * pgMirrorY);
                baseCellTransform.Add(pgTransl2 * pgMirrorY);
                baseCellTransform.Add(pgTransl2);
            }
            else if (wallpaperGroup == "p2mm" || wallpaperGroup == "pmm")
            {
                vecY *= cellD2;
                vecY.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                Point3d mirrorPt = origin + (vecY / 2) + (vecX / 2);
                Transform pgMirrorX = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecX));
                Transform pgMirrorY = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecY));
                baseCellTransform.Add(pgMirrorX);
                baseCellTransform.Add(pgMirrorY);
                baseCellTransform.Add(pgMirrorY * pgMirrorX);
            }
            else if (wallpaperGroup == "p2mg" || wallpaperGroup == "pmg")
            {
                vecY *= cellD2;
                vecY.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                Point3d mirrorPt = origin + vecY;
                Transform pgMirrorX = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecX));
                Transform pgMirrorY = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecY));
                Transform pgTransl1 = Transform.Translation((vecX / 2.0) - vecY);
                Transform pgTransl2 = Transform.Translation(vecX / 2.0);
                Transform pgTransl3 = Transform.Translation(vecX - vecY);
                baseCellTransform.Add(pgTransl1 * pgMirrorX);
                baseCellTransform.Add(pgTransl2 * pgMirrorY);
                baseCellTransform.Add(pgTransl3 * pgMirrorY * pgMirrorX);
            }
            else if (wallpaperGroup == "p2gg" || wallpaperGroup == "pgg")
            {
                vecY *= cellD2;
                vecY.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                Point3d mirrorPt = origin + vecY;
                Transform transl1 = Transform.Translation((vecX / 2.0) - (3.0 * vecY / 2.0));
                Transform transl2 = Transform.Translation((vecX / 2.0) + (vecY / 2.0));
                Transform transl3 = Transform.Translation(-vecY + vecX);
                Transform pgMirrorY = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecY));
                Transform pgMirrorX = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecX));
                baseCellTransform.Add(transl1 * pgMirrorX);
                baseCellTransform.Add(transl2 * pgMirrorY);
                baseCellTransform.Add(transl3 * pgMirrorY * pgMirrorX);
            }
            else if (wallpaperGroup == "c2mm" || wallpaperGroup == "cmm")
            {
                vecY *= cellD2;
                vecY.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                Point3d mirrorPt = origin + vecY;
                Transform pgMirrorX = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecX));
                Transform pgMirrorY = Transform.Mirror(new Plane(mirrorPt, Vector3d.ZAxis, vecY));
                Transform pgTransl1 = Transform.Translation(-vecY);
                Transform pgTransl2 = Transform.Translation(vecX);
                Transform pgTransl3 = Transform.Translation((vecX / 2.0) - (vecY / 2.0));
                Transform pgTransl4 = Transform.Translation(vecY);
                baseCellTransform.Add(pgTransl1 * pgMirrorX);
                baseCellTransform.Add(pgTransl2 * pgMirrorY);
                baseCellTransform.Add(pgTransl1 * pgTransl2 * pgMirrorX * pgMirrorY);
                baseCellTransform.Add(pgTransl3 * pgTransl1 * pgMirrorX);
                baseCellTransform.Add(pgTransl3 * pgTransl4 * pgMirrorY);
                baseCellTransform.Add(pgTransl3 * pgTransl1 * pgMirrorX * pgMirrorY);
                baseCellTransform.Add(pgTransl3 * pgTransl4);
            }
            else if (wallpaperGroup == "p3")
            {
                vecY *= cellD1;
                vecY.Rotate((2.0 * Math.PI) / 3.0, Vector3d.ZAxis);
                Point3d rotationCenter = (origin + vecY + (vecY + vecX)) / 3.0;
                Transform p3Rotation1 = Transform.Rotation(Math.PI * (2.0 / 3.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation2 = Transform.Rotation(Math.PI * (4.0 / 3.0), Vector3d.ZAxis, rotationCenter);
                baseCellTransform.Add(p3Rotation1);
                baseCellTransform.Add(p3Rotation2);
            }
            else if (wallpaperGroup == "p31m")
            {
                vecY *= cellD1;
                vecY.Rotate((2.0 * Math.PI) / 3.0, Vector3d.ZAxis);
                Point3d rotationCenter = (origin + vecY + (vecY + vecX)) / 3.0;
                Transform pMirrorY = Transform.Mirror(new Plane(rotationCenter, Vector3d.ZAxis, Vector3d.YAxis));
                Transform p3Rotation1 = Transform.Rotation(Math.PI * (2.0 / 3.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation2 = Transform.Rotation(Math.PI * (4.0 / 3.0), Vector3d.ZAxis, rotationCenter);
                baseCellTransform.Add(p3Rotation1);
                baseCellTransform.Add(p3Rotation2);
                baseCellTransform.Add(pMirrorY);
                baseCellTransform.Add(pMirrorY * p3Rotation1);
                baseCellTransform.Add(pMirrorY * p3Rotation2);
            }
            else if (wallpaperGroup == "p3m1")
            {
                vecY *= cellD1;
                vecY.Rotate((2.0 * Math.PI) / 3.0, Vector3d.ZAxis);
                Point3d rotationCenter = (origin + vecY + (vecY + vecX)) / 3.0;
                Transform pgMirrorXY = Transform.Mirror(new Plane(origin, Vector3d.ZAxis, vecX + vecY));
                Transform p3Rotation1 = Transform.Rotation(Math.PI * (2.0 / 3.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation2 = Transform.Rotation(Math.PI * (4.0 / 3.0), Vector3d.ZAxis, rotationCenter);
                Transform pgTranslX = Transform.Translation(vecX);
                Transform pgTranslY = Transform.Translation(-vecY);
                baseCellTransform.Add(p3Rotation1);
                baseCellTransform.Add(p3Rotation2);
                baseCellTransform.Add(pgMirrorXY);
                baseCellTransform.Add(pgTranslY * p3Rotation1 * pgMirrorXY);
                baseCellTransform.Add(pgTranslX * p3Rotation2 * pgMirrorXY);
            }
            else if (wallpaperGroup == "p4")
            {
                vecY *= cellD1;
                vecY.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                Point3d rotationCenter = origin + (vecY / 2) + (vecX / 2);
                Transform p3Rotation1 = Transform.Rotation((1.0 * Math.PI) / 2.0, Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation2 = Transform.Rotation((2.0 * Math.PI) / 2.0, Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation3 = Transform.Rotation((3.0 * Math.PI) / 2.0, Vector3d.ZAxis, rotationCenter);
                baseCellTransform.Add(p3Rotation1);
                baseCellTransform.Add(p3Rotation2);
                baseCellTransform.Add(p3Rotation3);
            }
            else if (wallpaperGroup == "p4mm" || wallpaperGroup == "p4m")
            {
                vecY *= cellD1;
                vecY.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                Point3d rotationCenter = origin + (vecY / 2) + (vecX / 2);
                Transform pgMirrorY = Transform.Mirror(new Plane(rotationCenter, Vector3d.ZAxis, vecY));
                Transform p3Rotation1 = Transform.Rotation((1.0 * Math.PI) / 2.0, Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation2 = Transform.Rotation((2.0 * Math.PI) / 2.0, Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation3 = Transform.Rotation((3.0 * Math.PI) / 2.0, Vector3d.ZAxis, rotationCenter);
                baseCellTransform.Add(p3Rotation1);
                baseCellTransform.Add(p3Rotation2);
                baseCellTransform.Add(p3Rotation3);
                baseCellTransform.Add(p3Rotation1 * pgMirrorY);
                baseCellTransform.Add(p3Rotation2 * pgMirrorY);
                baseCellTransform.Add(p3Rotation3 * pgMirrorY);
                baseCellTransform.Add(pgMirrorY);
            }
            else if (wallpaperGroup == "p4gm" || wallpaperGroup == "p4g")
            {
                vecY *= cellD1;
                vecY.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                Point3d rotationCenter = origin + (vecY / 2) + (vecX / 2);
                Transform pgMirrorY = Transform.Mirror(new Plane(rotationCenter, Vector3d.ZAxis, vecY));
                Transform p3Rotation1 = Transform.Rotation((1.0 * Math.PI) / 2.0, Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation2 = Transform.Rotation((2.0 * Math.PI) / 2.0, Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation3 = Transform.Rotation((3.0 * Math.PI) / 2.0, Vector3d.ZAxis, rotationCenter);
                Transform pgTransl1 = Transform.Translation(-(vecX / 2.0) - (vecY / 2.0));
                Transform pgTransl2 = Transform.Translation((vecX / 2.0) - (vecY / 2.0));
                Transform pgTransl3 = Transform.Translation((vecX / 2.0) + (vecY / 2.0));
                Transform pgTransl4 = Transform.Translation(-(vecX / 2.0) + (vecY / 2.0));
                baseCellTransform.Add(p3Rotation1);
                baseCellTransform.Add(p3Rotation2);
                baseCellTransform.Add(p3Rotation3);
                baseCellTransform.Add(pgTransl1 * p3Rotation1 * pgMirrorY);
                baseCellTransform.Add(pgTransl2 * p3Rotation2 * pgMirrorY);
                baseCellTransform.Add(pgTransl3 * p3Rotation3 * pgMirrorY);
                baseCellTransform.Add(pgTransl4 * pgMirrorY);
            }
            else if (wallpaperGroup == "p6")
            {
                vecY *= cellD1;
                vecY.Rotate((2.0 * Math.PI) / 3.0, Vector3d.ZAxis);
                Point3d rotationCenter = origin;
                Transform p3Rotation1 = Transform.Rotation(Math.PI * (2.0 / 6.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation2 = Transform.Rotation(Math.PI * (4.0 / 6.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation3 = Transform.Rotation(Math.PI * (6.0 / 6.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation4 = Transform.Rotation(Math.PI * (8.0 / 6.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation5 = Transform.Rotation(Math.PI * (10.0 / 6.0), Vector3d.ZAxis, rotationCenter);
                Transform translX = Transform.Translation(vecX);
                Transform translY = Transform.Translation(vecY);
                baseCellTransform.Add(translX * p3Rotation1);
                baseCellTransform.Add(translX * translY * p3Rotation2);
                baseCellTransform.Add(translX * translY * p3Rotation3);
                baseCellTransform.Add(translY * p3Rotation4);
                baseCellTransform.Add(p3Rotation5);
            }
            else if (wallpaperGroup == "p6mm" || wallpaperGroup == "p6m")
            {
                vecY *= cellD1;
                vecY.Rotate((2.0 * Math.PI) / 3.0, Vector3d.ZAxis);
                Point3d rotationCenter = origin;
                Transform pgMirrorX = Transform.Mirror(new Plane(rotationCenter, Vector3d.ZAxis, Vector3d.YAxis));
                Transform p3Rotation1 = Transform.Rotation(Math.PI * (2.0 / 6.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation2 = Transform.Rotation(Math.PI * (4.0 / 6.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation3 = Transform.Rotation(Math.PI * (6.0 / 6.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation4 = Transform.Rotation(Math.PI * (8.0 / 6.0), Vector3d.ZAxis, rotationCenter);
                Transform p3Rotation5 = Transform.Rotation(Math.PI * (10.0 / 6.0), Vector3d.ZAxis, rotationCenter);
                Transform translX = Transform.Translation(vecX);
                Transform translY = Transform.Translation(vecY);
                baseCellTransform.Add(translX * p3Rotation1);
                baseCellTransform.Add(translX * translY * p3Rotation2);
                baseCellTransform.Add(translX * translY * p3Rotation3);
                baseCellTransform.Add(translY * p3Rotation4);
                baseCellTransform.Add(p3Rotation5);
                baseCellTransform.Add(pgMirrorX);
                baseCellTransform.Add(translX * p3Rotation1 * pgMirrorX);
                baseCellTransform.Add(translX * translY * p3Rotation2 * pgMirrorX);
                baseCellTransform.Add(translX * translY * p3Rotation3 * pgMirrorX);
                baseCellTransform.Add(translY * p3Rotation4 * pgMirrorX);
                baseCellTransform.Add(p3Rotation5 * pgMirrorX);
            }
            else
            {
                //throw error
            }
            return baseCellTransform;
        }
        public static GH_Structure<GH_Transform> CalculateAllTransforms(List<Transform> baseCellTransform,
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

        public static PolylineCurve MakeCelloutlines(string WPG, Point3d origin, Vector3d vecX, Vector3d vecY)
        {
            List<Point3d> cellBounds = new List<Point3d>();
            cellBounds.Add(origin);
            if (WPG != "p3" && WPG != "p31m")
                cellBounds.Add(new Point3d(vecX));
            cellBounds.Add(new Point3d(vecX + vecY));
            cellBounds.Add(new Point3d(vecY));
            cellBounds.Add(origin);
            return new PolylineCurve(cellBounds);
        }

        public static GH_Structure<GH_Point> MakeGridPoints(Vector3d vecX, Vector3d vecY, double cellsX, double cellsY)
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

        public static PolylineCurve SuggestFundamentalDomain(string wallpaperGroup, Point3d origin, Vector3d vecX, Vector3d vecY, out List<Point3d> gridPointsOut)
        {
            List<Point3d> fundDomain = new List<Point3d>();
            fundDomain.Add(origin);

            List<Point3d> gridPoints = new List<Point3d>();
            gridPoints.Add(origin);
            gridPoints.Add(new Point3d(vecX));
            gridPoints.Add(new Point3d(vecY));
            gridPoints.Add(new Point3d(vecX + vecY));

            if (wallpaperGroup == "p1")
            {
                fundDomain.Add(new Point3d(vecX));
                fundDomain.Add(new Point3d(vecX + vecY));
                fundDomain.Add(new Point3d(vecY));

                gridPoints.Clear();
            }
            else if (wallpaperGroup == "p2")
            {
                fundDomain.Add(new Point3d(vecX + vecY));
                fundDomain.Add(new Point3d(vecY));

                gridPoints.Add(new Point3d(vecX / 2.0));
                gridPoints.Add(new Point3d(vecY / 2.0));
                gridPoints.Add(new Point3d(vecX + (vecY / 2.0)));
                gridPoints.Add(new Point3d((vecX / 2.0) + vecY));
                gridPoints.Add(new Point3d((vecX / 2.0) + (vecY / 2.0)));
            }
            else if (wallpaperGroup == "pm")
            {
                fundDomain.Add(new Point3d(vecX / 2.0));
                fundDomain.Add(new Point3d(vecY + (vecX / 2.0)));
                fundDomain.Add(new Point3d(vecY));

                gridPoints.Clear();
            }
            else if (wallpaperGroup == "pg")
            {
                fundDomain.Add(new Point3d(vecX));
                fundDomain.Add(new Point3d(vecX + (vecY / 2.0)));
                fundDomain.Add(new Point3d(vecY / 2.0));

                gridPoints.Clear();
            }
            else if (wallpaperGroup == "cm")
            {
                fundDomain.Add(new Point3d(vecX / 2.0));
                fundDomain.Add(new Point3d((vecX / 2.0) + (vecY / 2.0)));
                fundDomain.Add(new Point3d(vecY / 2.0));

                gridPoints.Clear();
            }
            else if (wallpaperGroup == "p2mm" || wallpaperGroup == "pmm")
            {
                fundDomain.Add(new Point3d(vecX / 2.0));
                fundDomain.Add(new Point3d((vecX / 2.0) + (vecY / 2.0)));
                fundDomain.Add(new Point3d(vecY / 2.0));

                gridPoints.Clear();
            }
            else if (wallpaperGroup == "p2mg" || wallpaperGroup == "pmg")
            {
                fundDomain.Add(new Point3d(vecX / 4.0));
                fundDomain.Add(new Point3d((vecX / 4.0) + vecY));
                fundDomain.Add(new Point3d(vecY));

                gridPoints.Add(new Point3d(vecX / 2.0));
                gridPoints.Add(new Point3d(vecY / 2.0));
                gridPoints.Add(new Point3d(vecX / 2.0) + vecY);
                gridPoints.Add(new Point3d(vecY / 2.0) + vecX);
                gridPoints.Add(new Point3d((vecX + vecY) / 2.0));
            }
            else if (wallpaperGroup == "p2gg" || wallpaperGroup == "pgg")
            {
                fundDomain.Add(new Point3d(vecX / 2.0));
                fundDomain.Add(new Point3d((vecX / 2.0) + (vecY / 2.0)));
                fundDomain.Add(new Point3d(vecY / 2.0));

                gridPoints.Add(new Point3d(vecX / 2.0));
                gridPoints.Add(new Point3d(vecY / 2.0));
                gridPoints.Add(new Point3d(vecX / 2.0) + vecY);
                gridPoints.Add(new Point3d(vecY / 2.0) + vecX);
                gridPoints.Add(new Point3d((vecX + vecY) / 2.0));
            }
            else if (wallpaperGroup == "c2mm" || wallpaperGroup == "cmm")
            {
                fundDomain.Add(new Point3d(vecX / 2.0));
                fundDomain.Add(new Point3d(vecY / 2.0));

                gridPoints.Clear();
                gridPoints.Add(new Point3d((vecY / 4.0) + (vecX / 4.0)));
                gridPoints.Add(new Point3d((3.0 * (vecY / 4.0)) + (vecX / 4.0)));
                gridPoints.Add(new Point3d((3.0 * (vecX / 4.0)) + (vecY / 4.0)));
                gridPoints.Add(new Point3d((3.0 * (vecY / 4.0)) + (3.0 * (vecX / 4.0))));
            }
            else if (wallpaperGroup == "p4")
            {
                fundDomain.Add(new Point3d(vecX / 2.0));
                fundDomain.Add(new Point3d((vecX / 2.0) + (vecY / 2.0)));
                fundDomain.Add(new Point3d(vecY / 2.0));

                gridPoints.Add(new Point3d(vecX / 2.0));
                gridPoints.Add(new Point3d(vecY / 2.0));
                gridPoints.Add(new Point3d(vecX / 2.0) + vecY);
                gridPoints.Add(new Point3d(vecY / 2.0) + vecX);
                gridPoints.Add(new Point3d((vecX + vecY) / 2.0));
            }
            else if (wallpaperGroup == "p4mm" || wallpaperGroup == "p4m")
            {
                fundDomain.Add(new Point3d(vecX / 2.0));
                fundDomain.Add(new Point3d((vecX / 2.0) + (vecY / 2.0)));

                gridPoints.Clear();
            }
            else if (wallpaperGroup == "p4gm" || wallpaperGroup == "p4g")
            {
                fundDomain.Add(new Point3d(vecX / 2.0));
                fundDomain.Add(new Point3d(vecY / 2.0));

                gridPoints.Add(new Point3d((vecX + vecY) / 2.0));
            }
            else if (wallpaperGroup == "p3")
            {
                fundDomain.Add(new Point3d(origin + vecY + (vecY + vecX)) / 3.0);
                fundDomain.Add(new Point3d(vecX + vecY));
                fundDomain.Add(new Point3d(origin + vecX + (vecY + vecX)) / 3.0);

                gridPoints.Clear();
                gridPoints.Add(origin);
                gridPoints.Add(new Point3d(origin + vecY));
                gridPoints.Add(new Point3d(origin + vecX + vecY));
                gridPoints.Add(new Point3d(origin + vecX + (vecY + vecX)) / 3.0);
                gridPoints.Add(new Point3d(origin + vecY + (vecY + vecX)) / 3.0);
                gridPoints.Add(new Point3d(origin + ((vecY - vecX)) / 3.0));
                gridPoints.Add(new Point3d(origin + (vecX + vecY) + ((vecY - vecX)) / 3.0));
            }
            else if (wallpaperGroup == "p31m")
            {
                fundDomain.Add(new Point3d(origin + vecY + (vecY + vecX)) / 3.0);
                fundDomain.Add(new Point3d(origin + vecX + (vecY + vecX)) / 3.0);

                gridPoints.Clear();
            }
            else if (wallpaperGroup == "p3m1")
            {
                fundDomain.Add(new Point3d(origin + vecY + (vecY + vecX)) / 3.0);
                fundDomain.Add(new Point3d(vecX + vecY));

                gridPoints.Clear();
                gridPoints.Add(new Point3d(origin + vecY + (vecY + vecX)) / 3.0);
                gridPoints.Add(new Point3d(origin + vecX + (vecX + vecY)) / 3.0);
            }
            else if (wallpaperGroup == "p6")
            {
                fundDomain.Add(new Point3d(vecY / 2.0));
                fundDomain.Add(new Point3d(origin + vecY + (vecY + vecX)) / 3.0);
                fundDomain.Add(new Point3d((vecX + vecY) / 2.0));

                gridPoints.Add(new Point3d(origin + (vecY / 2.0)));
                gridPoints.Add(new Point3d(origin + (vecX / 2.0)));
                gridPoints.Add(new Point3d(origin + vecX + (vecY / 2.0)));
                gridPoints.Add(new Point3d(origin + vecY + (vecX / 2.0)));
                gridPoints.Add(new Point3d(origin + (vecY / 2.0) + (vecX / 2.0)));
                gridPoints.Add(new Point3d(origin + vecX + (vecY + vecX)) / 3.0);
                gridPoints.Add(new Point3d(origin + vecY + (vecY + vecX)) / 3.0);
            }
            else if (wallpaperGroup == "p6mm" || wallpaperGroup == "p6m")
            {
                fundDomain.Add(new Point3d(vecY / 2.0));
                fundDomain.Add(new Point3d(origin + vecY + (vecY + vecX)) / 3.0);

                gridPoints.Clear();
            }
            else
            {
                //throw error
            }
            fundDomain.Add(origin);
            gridPointsOut = gridPoints;
            return new PolylineCurve(fundDomain);
        }

        /// Provides an Icon for the component.
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.GetFDIcon;
            }
        }

        /// Gets the unique ID for this component. Do not change this ID after release.
        public override Guid ComponentGuid
        {
            get { return new Guid("AA866C08-4037-4CF2-8461-74E161510615"); }
        }
    }
}