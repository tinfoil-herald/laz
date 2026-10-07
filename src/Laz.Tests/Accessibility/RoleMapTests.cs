// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Xunit;

namespace Laz.Tests;

public class RoleMapTests
{
    [Theory]
    [InlineData(50000, AccessibleRole.Button)]
    [InlineData(50002, AccessibleRole.CheckBox)]
    [InlineData(50003, AccessibleRole.ComboBox)]
    [InlineData(50004, AccessibleRole.Edit)]
    [InlineData(50005, AccessibleRole.Hyperlink)]
    [InlineData(50007, AccessibleRole.ListItem)]
    [InlineData(50008, AccessibleRole.List)]
    [InlineData(50011, AccessibleRole.MenuItem)]
    [InlineData(50013, AccessibleRole.RadioButton)]
    [InlineData(50020, AccessibleRole.Text)]
    [InlineData(50024, AccessibleRole.TreeItem)]
    [InlineData(50028, AccessibleRole.Table)]
    [InlineData(50029, AccessibleRole.Row)]
    [InlineData(50032, AccessibleRole.Window)]
    [InlineData(50033, AccessibleRole.Pane)]
    [InlineData(50038, AccessibleRole.Separator)]
    [InlineData(12345, AccessibleRole.Unknown)]
    public void MapsUiaControlTypes(int controlType, AccessibleRole expected)
    {
        var role = RoleMap.Map(A11yPlatform.Windows, controlType, "raw", null, "SomeClass", AccessibleStates.None);

        Assert.Equal(expected, role);
    }

    [Fact]
    public void MapsUiaDesktopPaneToDesktop()
    {
        var role = RoleMap.Map(A11yPlatform.Windows, 50033, "pane", null, "#32769", AccessibleStates.None);

        Assert.Equal(AccessibleRole.Desktop, role);
    }

    [Fact]
    public void MapsModalUiaWindowToDialog()
    {
        var role = RoleMap.Map(A11yPlatform.Windows, 50032, "window", null, null, AccessibleStates.Modal);

        Assert.Equal(AccessibleRole.Dialog, role);
    }

    [Theory]
    [InlineData("AXButton", null, AccessibleRole.Button)]
    [InlineData("AXWindow", "AXStandardWindow", AccessibleRole.Window)]
    [InlineData("AXWindow", "AXDialog", AccessibleRole.Dialog)]
    [InlineData("AXCheckBox", null, AccessibleRole.CheckBox)]
    [InlineData("AXCheckBox", "AXToggle", AccessibleRole.ToggleButton)]
    [InlineData("AXCheckBox", "AXSwitch", AccessibleRole.ToggleButton)]
    [InlineData("AXRadioButton", "AXTabButton", AccessibleRole.TabItem)]
    [InlineData("AXTextField", "AXSearchField", AccessibleRole.Edit)]
    [InlineData("AXTextField", null, AccessibleRole.Edit)]
    [InlineData("AXStaticText", null, AccessibleRole.Text)]
    [InlineData("AXRow", null, AccessibleRole.Row)]
    [InlineData("AXRow", "AXOutlineRow", AccessibleRole.TreeItem)]
    [InlineData("AXApplication", null, AccessibleRole.Application)]
    [InlineData("AXPopUpButton", null, AccessibleRole.ComboBox)]
    [InlineData("AXWebArea", null, AccessibleRole.Document)]
    [InlineData("AXSomethingNew", null, AccessibleRole.Unknown)]
    [InlineData(null, null, AccessibleRole.Unknown)]
    public void MapsAxRoles(string? role, string? subrole, AccessibleRole expected)
    {
        var mapped = RoleMap.Map(A11yPlatform.MacOS, -1, role, subrole, null, AccessibleStates.None);

        Assert.Equal(expected, mapped);
    }

    [Theory]
    [InlineData(2, AccessibleRole.Dialog)]        // ALERT
    [InlineData(7, AccessibleRole.CheckBox)]      // CHECK_BOX
    [InlineData(14, AccessibleRole.Desktop)]      // DESKTOP_FRAME
    [InlineData(16, AccessibleRole.Dialog)]       // DIALOG
    [InlineData(23, AccessibleRole.Window)]       // FRAME
    [InlineData(29, AccessibleRole.Text)]         // LABEL
    [InlineData(43, AccessibleRole.Button)]       // BUTTON
    [InlineData(62, AccessibleRole.ToggleButton)] // TOGGLE_BUTTON
    [InlineData(75, AccessibleRole.Application)]  // APPLICATION
    [InlineData(79, AccessibleRole.Edit)]         // ENTRY
    [InlineData(88, AccessibleRole.Hyperlink)]    // LINK
    [InlineData(91, AccessibleRole.TreeItem)]     // TREE_ITEM
    [InlineData(67, AccessibleRole.Unknown)]      // UNKNOWN
    [InlineData(9999, AccessibleRole.Unknown)]
    public void MapsAtspiRoles(int role, AccessibleRole expected)
    {
        var mapped = RoleMap.Map(A11yPlatform.Linux, role, "raw", null, null, AccessibleStates.None);

        Assert.Equal(expected, mapped);
    }
}
