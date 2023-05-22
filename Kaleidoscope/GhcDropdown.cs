using GH_IO.Serialization;
using Grasshopper.Kernel;
using Kaleidoscope.Properties;
using System;
using System.Windows.Forms;

namespace Kaleidoscope
{
    public enum PanelType
    {
        p1,
        p2,
        pm,
        pg,
        cm,
        p2mm,
        p2mg,
        p2gg,
        c2mm,
        p4,
        p4mm,
        p4gm,
        p3,
        p3m1,
        p31m,
        p6,
        p6mm,
    }
    public class GhcDropdown : GH_Component
    {
        private PanelType panelType = PanelType.p6;
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GhcDropdown()
          : base("Wallpaper Group Selector",
                 "WPG Select",
                 "...",
                 "Kaleidoscope",
                 "Tiling")
        {
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("PanelType", (int)panelType);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            int aIndex = -1;
            if (reader.TryGetInt32("PanelType", ref aIndex))
                panelType = (PanelType)aIndex;

            return base.Read(reader);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            foreach (PanelType panelType in Enum.GetValues(typeof(PanelType)))
                GH_Component.Menu_AppendItem(menu, panelType.ToString(), Menu_PanelTypeChanged, true, panelType == this.panelType).Tag = panelType;

            base.AppendAdditionalComponentMenuItems(menu);
        }

        private void Menu_PanelTypeChanged(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is PanelType)
            {
                //Do something with panelType
                this.panelType = (PanelType)item.Tag;
                ExpireSolution(true);
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Wallpaper Group", "WPG", "Code for the output wallpaper group.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Params.Output[0].NickName = panelType.ToString();
            Params.Output[0].Name = "Wallpaper Group: " + panelType.ToString();
            DA.SetData(0, panelType);
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
                return Resources.Dropdown;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("46AB189A-B6DC-47A9-BDF8-65446B90CB0E"); }
        }
    }
}