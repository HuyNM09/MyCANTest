using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Peak.Can.Basic;
// chưa sử dụng
public class ListTestCasesExplorer : ListView
{
    // Sự kiện khi user click vào group
    public event Action<string> OnGroupClicked;

        public ListTestCasesExplorer()
    {
        this.View = View.Details;
        this.FullRowSelect = true;
        this.GridLines = true;
        this.MultiSelect = false;
        this.HeaderStyle = ColumnHeaderStyle.None;
        this.Columns.Add("Group Name", 250);
        this.Size = new Size(300, 200);
    }

    /// <summary>
    /// Load danh sách tên group vào ListView
    /// </summary>
    public void LoadGroups(List<string> groupNames)
    {
        this.Items.Clear();

        foreach (var name in groupNames)
        {
            ListViewItem item = new ListViewItem(name);
            item.Tag = name;
            this.Items.Add(item);
        }

        this.ItemSelectionChanged += (s, e) =>
        {
            if (e.IsSelected && e.Item.Tag is string groupName)
            {
                OnGroupClicked?.Invoke(groupName);
            }
        };

    }
}
