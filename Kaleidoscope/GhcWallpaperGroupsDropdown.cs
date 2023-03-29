using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;

namespace Kaleidoscope
{
    public class GhcWallpaperGroupsDropdown : GH_ValueList
    {

        private readonly List<GH_ValueListItem> m_userItems;
        public List<GH_ValueListItem> ListItems => m_userItems;

        [Obsolete("This property has been replaced by ListItems")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public List<GH_ValueListItem> Values => m_userItems;

        public GH_ValueList()
            : base((IGH_InstanceDescription)new GH_InstanceDescription("Value List", "List", "Provides a list of preset values to choose from", "Params", "Input"))
        {
            m_listMode = GH_ValueListMode.DropDown;
            m_userItems = new List<GH_ValueListItem>();
            m_hidden = false;
            m_userItems.Add(new GH_ValueListItem("One", "1"));
            m_userItems.Add(new GH_ValueListItem("Two", "2"));
            m_userItems.Add(new GH_ValueListItem("Three", "2² - 1"));
            m_userItems.Add(new GH_ValueListItem("Four", "Sqrt(16)"));
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
            get { return new Guid("0582566E-8E38-4FD0-AC8E-38E4273BC9A5"); }
        }
    }
}