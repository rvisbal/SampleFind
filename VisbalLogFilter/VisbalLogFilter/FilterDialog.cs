using System;
using System.Windows.Forms;

public class FilterDialog : Form
{
    public FilterCondition FilterCondition { get; private set; }

    public FilterDialog()
    {
        this.Size = new System.Drawing.Size(400, 200);
        this.Text = "Add Filter";

        ComboBox filterTypeCombo = new ComboBox
        {
            Location = new System.Drawing.Point(10, 10),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        filterTypeCombo.Items.AddRange(new string[] { "CONTAINS", "EQUALS" });
        filterTypeCombo.SelectedIndex = 0;

        TextBox filterValueBox = new TextBox
        {
            Location = new System.Drawing.Point(10, 40),
            Width = 200
        };

        Button okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new System.Drawing.Point(10, 70)
        };
        okButton.Click += (s, e) =>
        {
            FilterCondition = new FilterCondition
            {
                Type = (string)filterTypeCombo.SelectedItem,
                Value = filterValueBox.Text
            };
        };

        Button cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new System.Drawing.Point(100, 70)
        };

        this.Controls.AddRange(new Control[] { filterTypeCombo, filterValueBox, okButton, cancelButton });
    }
} 