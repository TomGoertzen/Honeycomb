﻿using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Honeycomb.Properties;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Honeycomb
{
    public class GhcGetFundDomainLite : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcGetFundDomainLite class.
        /// </summary>
        public GhcGetFundDomainLite()
          : base("Get Wallpaper Group (Lite)",
                 "GetWPGLt",
                 "Use this component to easily generate transformation data and a geometrical boundary for your tilings.",
                 "Honeycomb",
                 "Tiling")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddScriptVariableParameter("Wallpaper Group", "WPG", "One of the 17 wallpaper groups as a string", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTransformParameter("Transform Data", "T", "Tree containing transform data to be applied to a geometry", GH_ParamAccess.tree);
            pManager.AddPointParameter("Fixed Points", "P", "Points that are guaranteed to lie on a fundamental domain edge.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Base Cell", "C", "Geometry representing the cell boundary", GH_ParamAccess.item);
            pManager.AddCurveParameter("Fundamental Domain", "D", "Geometry representing the suggested fundamental domain boundary", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            string wallpaperGroup = string.Empty;
            DA.GetData("Wallpaper Group", ref wallpaperGroup);

            if (wallpaperGroup == "p2mm" ||
                wallpaperGroup == "p4mm" ||
                wallpaperGroup == "p3m1" ||
                wallpaperGroup == "p6mm")
            {

                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "This wallpaper group contains only mirror symmetries, certain functionalities will not work properly.");
            }

            double cellD1 = 10.0;
            double cellD2 = 10.0;
            double cellAngle = Math.PI / 2.0;
            Point3d origin = new Point3d(0.0, 0.0, 0.0);
            int cellsX = 5;
            int cellsY = 5;

            Vector3d vecX = Vector3d.XAxis;
            Vector3d vecY = Vector3d.XAxis;

            List<Transform> transformsWPG = GhcGetFundDomain.GetTransformsFromWPG(wallpaperGroup, origin, cellAngle, cellD1, cellD2, ref vecX, ref vecY);
            GH_Structure<GH_Transform> allTransforms = GhcGetFundDomain.CalculateAllTransforms(transformsWPG, vecX, vecY, cellsX, cellsY);

            DA.SetDataTree(0, allTransforms);
            PolylineCurve cellOutlines = GhcGetFundDomain.MakeCelloutlines(wallpaperGroup, origin, vecX, vecY);
            DA.SetData("Base Cell", cellOutlines);
            PolylineCurve fundDomain = GhcGetFundDomain.SuggestFundamentalDomain(wallpaperGroup, origin, vecX, vecY, out List<Point3d> gridPoints);
            DA.SetDataList(1, gridPoints);
            DA.SetData("Fundamental Domain", fundDomain);
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
                return Resources.GetFDIconLT;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("396C6031-F753-4CDB-9EF8-E3D459E3FA5E"); }
        }
    }
}