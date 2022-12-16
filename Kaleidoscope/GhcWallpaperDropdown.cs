using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;

namespace Kaleidoscope
{
    /// <exclude />
    public class GhcWallPaperDropdown : GH_ValueList
    {
        private GH_ValueListMode m_listMode;
        private readonly List<GH_ValueListItem> m_userItems;
        private bool m_hidden;

        public GhcWallPaperDropdown()
        {
            this.m_listMode = GH_ValueListMode.DropDown;
            this.m_userItems = new List<GH_ValueListItem>();
            this.m_hidden = false;
            this.m_userItems.Add(new GH_ValueListItem("p5", "1"));
            this.m_userItems.Add(new GH_ValueListItem("Two", "2"));
            this.m_userItems.Add(new GH_ValueListItem("Three", "2\u00B2 - 1"));
            this.m_userItems.Add(new GH_ValueListItem("Four", "Sqrt(16)"));
        }

        public override void CreateAttributes() => this.m_attributes = (IGH_Attributes)new GH_ValueListAttributes(this);

        //public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid
        {
            get { return new Guid("6CB7FC37-E93B-4C35-B99C-1A97AC065E18"); }
        }

        protected override System.Drawing.Bitmap Icon => null;
    }
}
/*
            public string DisplayName => !string.IsNullOrWhiteSpace(this.NickName) ? (!this.NickName.Equals("List", StringComparison.OrdinalIgnoreCase) ? this.NickName : (string)null) : (string)null;

            public GH_ValueListMode ListMode
            {
                get => this.m_listMode;
                set
                {
                    this.m_listMode = value;
                    if (this.m_attributes == null)
                        return;
                    this.m_attributes.ExpireLayout();
                }
            }

            public List<GH_ValueListItem> ListItems => this.m_userItems;

            [Obsolete("This property has been replaced by ListItems")]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public List<GH_ValueListItem> Values => this.m_userItems;

            /// <summary>
            /// Gets the selected value list items. If the ListMode only supports a single selected item
            /// then only the first selected item will be included.
            /// </summary>
            public List<GH_ValueListItem> SelectedItems
            {
                get
                {
                    List<GH_ValueListItem> ghValueListItemList = new List<GH_ValueListItem>();
                    List<GH_ValueListItem> selectedItems;
                    if (this.m_userItems.Count == 0)
                    {
                        selectedItems = ghValueListItemList;
                    }
                    else
                    {
                        if (this.ListMode == GH_ValueListMode.CheckList)
                        {
                            try
                            {
                                foreach (GH_ValueListItem userItem in this.m_userItems)
                                {
                                    if (userItem.Selected)
                                        ghValueListItemList.Add(userItem);
                                }
                            }
                            finally
                            {
                                List<GH_ValueListItem>.Enumerator enumerator;
                                enumerator.Dispose();
                            }
                        }
                        else
                        {
                            try
                            {
                                foreach (GH_ValueListItem userItem in this.m_userItems)
                                {
                                    if (userItem.Selected)
                                    {
                                        ghValueListItemList.Add(userItem);
                                        selectedItems = ghValueListItemList;
                                        goto label_15;
                                    }
                                }
                            }
                            finally
                            {
                                List<GH_ValueListItem>.Enumerator enumerator;
                                enumerator.Dispose();
                            }
                            this.m_userItems[0].Selected = true;
                            ghValueListItemList.Add(this.m_userItems[0]);
                        }
                        selectedItems = ghValueListItemList;
                    }
                label_15:
                    return selectedItems;
                }
            }

            /// <summary>
            /// Gets the first selected item in this list, or the first item if there are no selected items.
            /// </summary>
            public GH_ValueListItem FirstSelectedItem
            {
                get
                {
                    GH_ValueListItem firstSelectedItem;
                    if (this.m_userItems.Count == 0)
                    {
                        firstSelectedItem = (GH_ValueListItem)null;
                    }
                    else
                    {
                        try
                        {
                            foreach (GH_ValueListItem userItem in this.m_userItems)
                            {
                                if (userItem.Selected)
                                {
                                    firstSelectedItem = userItem;
                                    goto label_8;
                                }
                            }
                        }
                        finally
                        {
                            List<GH_ValueListItem>.Enumerator enumerator;
                            enumerator.Dispose();
                        }
                        firstSelectedItem = this.m_userItems[0];
                    }
                label_8:
                    return firstSelectedItem;
                }
            }

            /// <summary>Safe method of toggling the item at the specified index.</summary>
            /// <param name="index">Index of item to toggle.</param>
            public void ToggleItem(int index)
            {
                if (index < 0 || index >= this.m_userItems.Count)
                    return;
                this.RecordUndoEvent("Toggle: " + this.m_userItems[index].Name);
                this.m_userItems[index].Selected = !this.m_userItems[index].Selected;
                this.ExpireSolution(true);
            }

            /// <summary>Safe method to select a single item.</summary>
            /// <param name="index">Index of item to select.</param>
            public void SelectItem(int index)
            {
                if (index < 0 || index >= this.m_userItems.Count)
                    return;
                bool flag = false;
                int num1 = this.m_userItems.Count - 1;
                for (int index1 = 0; index1 <= num1; ++index1)
                {
                    if (index1 == index)
                    {
                        if (!this.m_userItems[index1].Selected)
                        {
                            flag = true;
                            break;
                        }
                    }
                    else if (this.m_userItems[index1].Selected)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                    return;
                this.RecordUndoEvent("Select: " + this.m_userItems[index].Name);
                int num2 = this.m_userItems.Count - 1;
                for (int index2 = 0; index2 <= num2; ++index2)
                    this.m_userItems[index2].Selected = index2 == index;
                this.ExpireSolution(true);
            }

            /// <summary>
            /// Safe method of selecting the next item.
            /// The select next logic depends on the ListMode, in that only Cycle and Sequence support NextItem()
            /// </summary>
            public void NextItem()
            {
                if (this.ListMode == GH_ValueListMode.CheckList || this.ListMode == GH_ValueListMode.DropDown || this.m_userItems.Count < 2)
                    return;
                int num1 = 0;
                int num2 = this.m_userItems.Count - 1;
                for (int index = 0; index <= num2; ++index)
                {
                    if (this.m_userItems[index].Selected)
                    {
                        num1 = index;
                        break;
                    }
                }
                int num3 = this.m_userItems.Count - 1;
                for (int index = 0; index <= num3; ++index)
                    this.m_userItems[index].Selected = false;
                int index1 = num1 + 1;
                if (index1 == this.m_userItems.Count)
                {
                    switch (this.ListMode)
                    {
                        case GH_ValueListMode.Sequence:
                            index1 = this.m_userItems.Count - 1;
                            break;
                        case GH_ValueListMode.Cycle:
                            index1 = 0;
                            break;
                    }
                }
                this.m_userItems[index1].Selected = true;
                this.ExpireSolution(true);
            }

            /// <summary>
            /// Safe method of selecting the previous item.
            /// The select prev logic depends on the ListMode, in that only Cycle and Sequence support PrevItem()
            /// </summary>
            public void PrevItem()
            {
                if (this.ListMode == GH_ValueListMode.CheckList || this.ListMode == GH_ValueListMode.DropDown || this.m_userItems.Count < 2)
                    return;
                int num1 = 0;
                int num2 = this.m_userItems.Count - 1;
                for (int index = 0; index <= num2; ++index)
                {
                    if (this.m_userItems[index].Selected)
                    {
                        num1 = index;
                        break;
                    }
                }
                int num3 = this.m_userItems.Count - 1;
                for (int index = 0; index <= num3; ++index)
                    this.m_userItems[index].Selected = false;
                int index1 = num1 - 1;
                if (index1 == -1)
                {
                    switch (this.ListMode)
                    {
                        case GH_ValueListMode.Sequence:
                            index1 = 0;
                            break;
                        case GH_ValueListMode.Cycle:
                            index1 = this.m_userItems.Count - 1;
                            break;
                    }
                }
                this.m_userItems[index1].Selected = true;
                this.ExpireSolution(true);
            }

            /// <summary>Display the list name/value editor.</summary>
            /// <param name="owner">Owner window.</param>
            public void ShowListEditor(IWin32Window owner)
            {
                List<bool> boolList = new List<bool>();
                try
                {
                    foreach (GH_ValueListItem userItem in this.m_userItems)
                        boolList.Add(userItem.Selected);
                }
                finally
                {
                    List<GH_ValueListItem>.Enumerator enumerator;
                    enumerator.Dispose();
                }
                GH_ValueListEditor F = new GH_ValueListEditor();
                F.AssignValues((IList<GH_ValueListItem>)this.m_userItems);
                GH_WindowsFormUtil.CenterFormOnCursor((Form)F, true);
                if (F.ShowDialog(owner) != DialogResult.OK || !F.Changed)
                    return;
                this.OnPingDocument()?.UndoUtil.RecordGenericObjectEvent("Value List Change", (IGH_DocumentObject)this);
                this.m_userItems.Clear();
                this.m_userItems.AddRange((IEnumerable<GH_ValueListItem>)F.RetrieveValues());
                int num = Math.Min(boolList.Count, this.m_userItems.Count) - 1;
                for (int index = 0; index <= num; ++index)
                    this.m_userItems[index].Selected = boolList[index];
                this.ExpireSolution(true);
            }

            public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
            {
                ToolStripMenuItem toolStripMenuItem = GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Edit…", new EventHandler(this.Menu_EditorClicked));
                toolStripMenuItem.Font = GH_FontServer.NewFont(toolStripMenuItem.Font, FontStyle.Bold);
                menu.Items.Remove((ToolStripItem)toolStripMenuItem);
                menu.Items.Insert(1, (ToolStripItem)toolStripMenuItem);
                GH_DocumentObject.Menu_AppendSeparator((ToolStrip)menu);
                GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Check List", new EventHandler(this.Menu_CheckListClicked), true, this.ListMode == GH_ValueListMode.CheckList);
                GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Dropdown List", new EventHandler(this.Menu_DropdownClicked), true, this.ListMode == GH_ValueListMode.DropDown);
                GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Value Sequence", new EventHandler(this.Menu_SequenceClicked), true, this.ListMode == GH_ValueListMode.Sequence);
                GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Value Cycle", new EventHandler(this.Menu_CycleClicked), true, this.ListMode == GH_ValueListMode.Cycle);
            }

            private void Menu_EditorClicked(object sender, EventArgs e) => this.ShowListEditor((IWin32Window)Instances.DocumentEditor);

            private void Menu_CheckListClicked(object sender, EventArgs e)
            {
                if (this.ListMode == GH_ValueListMode.CheckList)
                    return;
                this.RecordUndoEvent("Check List");
                this.ListMode = GH_ValueListMode.CheckList;
                this.ExpireSolution(true);
            }

            private void Menu_DropdownClicked(object sender, EventArgs e)
            {
                if (this.ListMode == GH_ValueListMode.DropDown)
                    return;
                this.RecordUndoEvent("Dropdown List");
                this.ListMode = GH_ValueListMode.DropDown;
                this.ExpireSolution(true);
            }

            private void Menu_SequenceClicked(object sender, EventArgs e)
            {
                if (this.ListMode == GH_ValueListMode.Sequence)
                    return;
                this.RecordUndoEvent("Value Sequence");
                this.ListMode = GH_ValueListMode.Sequence;
                this.ExpireSolution(true);
            }

            private void Menu_CycleClicked(object sender, EventArgs e)
            {
                if (this.ListMode == GH_ValueListMode.Cycle)
                    return;
                this.RecordUndoEvent("Value Cycle");
                this.ListMode = GH_ValueListMode.Cycle;
                this.ExpireSolution(true);
            }

            protected override IGH_Goo InstantiateT() => (IGH_Goo)new GH_ObjectWrapper();

            protected override void CollectVolatileData_Custom()
            {
                this.m_data.Clear();
                try
                {
                    foreach (GH_ValueListItem selectedItem in this.SelectedItems)
                        this.m_data.Append(selectedItem.Value, new GH_Path(0));
                }
                finally
                {
                    List<GH_ValueListItem>.Enumerator enumerator;
                    enumerator.Dispose();
                }
            }

            public bool Hidden
            {
                get => this.m_hidden;
                set => this.m_hidden = value;
            }

            public bool IsPreviewCapable => true;

            public BoundingBox ClippingBox => this.Preview_ComputeClippingBox();

            public void DrawViewportMeshes(IGH_PreviewArgs args) => this.Preview_DrawMeshes(args);

            public void DrawViewportWires(IGH_PreviewArgs args) => this.Preview_DrawWires(args);

            public bool IsBakeCapable => !this.m_data.IsEmpty;

            public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids) => this.BakeGeometry(doc, (ObjectAttributes)null, obj_ids);

            public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
            {
                if (att == null)
                    att = doc.CreateDefaultAttributes();
                try
                {
                    foreach (object obj in this.m_data)
                    {
                        object objectValue = RuntimeHelpers.GetObjectValue(obj);
                        Guid obj_guid;
                        if (objectValue != null && objectValue is IGH_BakeAwareData && ((IGH_BakeAwareData)objectValue).BakeGeometry(doc, att, out obj_guid))
                            obj_ids.Add(obj_guid);
                    }
                }
                finally
                {
                    IEnumerator enumerator;
                    if (enumerator is IDisposable)
                        (enumerator as IDisposable).Dispose();
                }
            }

            public void LoadState(string state)
            {
                try
                {
                    foreach (GH_ValueListItem userItem in this.m_userItems)
                        userItem.Selected = false;
                }
                finally
                {
                    List<GH_ValueListItem>.Enumerator enumerator;
                    enumerator.Dispose();
                }
                int result;
                if (int.TryParse(state, out result))
                {
                    if (result >= 0 && result < this.m_userItems.Count)
                        this.m_userItems[result].Selected = true;
                }
                else
                {
                    int num = Math.Min(state.Length, this.m_userItems.Count) - 1;
                    for (int index = 0; index <= num; ++index)
                        this.m_userItems[index].Selected = state[index].Equals('Y');
                }
                this.ExpireSolution(false);
            }

            public string SaveState()
            {
                StringBuilder stringBuilder = new StringBuilder(this.m_userItems.Count);
                try
                {
                    foreach (GH_ValueListItem userItem in this.m_userItems)
                    {
                        if (userItem.Selected)
                            stringBuilder.Append('Y');
                        else
                            stringBuilder.Append('N');
                    }
                }
                finally
                {
                    List<GH_ValueListItem>.Enumerator enumerator;
                    enumerator.Dispose();
                }
                return stringBuilder.ToString();
            }

            public override bool Write(GH_IWriter writer)
            {
                writer.SetInt32("ListMode", (int)this.ListMode);
                writer.SetInt32("ListCount", this.m_userItems.Count);
                int num = this.m_userItems.Count - 1;
                for (int index = 0; index <= num; ++index)
                {
                    GH_IWriter chunk = writer.CreateChunk("ListItem", index);
                    chunk.SetString("Name", this.m_userItems[index].Name);
                    chunk.SetString("Expression", this.m_userItems[index].Expression);
                    chunk.SetBoolean("Selected", this.m_userItems[index].Selected);
                }
                return base.Write(writer);
            }

            public override bool Read(GH_IReader reader)
            {
                int num1 = 1;
                reader.TryGetInt32("UIMode", ref num1);
                reader.TryGetInt32("ListMode", ref num1);
                this.ListMode = (GH_ValueListMode)num1;
                int int32_1 = reader.GetInt32("ListCount");
                int num2 = 0;
                reader.TryGetInt32("CacheCount", ref num2);
                this.m_userItems.Clear();
                int num3 = int32_1 - 1;
                for (int index = 0; index <= num3; ++index)
                {
                    GH_IReader chunk = reader.FindChunk("ListItem", index);
                    if (chunk == null)
                    {
                        reader.AddMessage("Missing chunk for List Value: " + index.ToString(), GH_Message_Type.error);
                    }
                    else
                    {
                        string name = chunk.GetString("Name");
                        string expression = chunk.GetString("Expression");
                        bool flag = false;
                        chunk.TryGetBoolean("Selected", ref flag);
                        this.m_userItems.Add(new GH_ValueListItem(name, expression)
                        {
                            Selected = flag
                        });
                    }
                }
                if (reader.ItemExists("ListIndex"))
                {
                    int int32_2 = reader.GetInt32("ListIndex");
                    if (int32_2 >= 0 && int32_2 < this.m_userItems.Count)
                        this.m_userItems[int32_2].Selected = true;
                }
                return base.Read(reader);
            }
        }
    }
*/