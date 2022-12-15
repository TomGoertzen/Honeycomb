using Grasshopper;
using Grasshopper.Kernel;
using Kaleidoscope.Properties;
using System;
using System.Drawing;

namespace Kaleidoscope
{
    public class KaleidoscopeInfo : GH_AssemblyInfo
    {
        public override string Name => "Kaleidoscope";

        //Return a 24x24 pixel bitmap to represent this GHA library. 
        public override Bitmap Icon => Resources.PluginIcon;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("e1916400-7369-4ae3-9b70-16ff35630dda");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}