// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Laz;

/// <summary>
/// The accessibility backend that produced an element.
/// </summary>
internal enum A11yPlatform
{
    Windows,
    MacOS,
    Linux,
}

/// <summary>
/// Maps platform roles to <see cref="AccessibleRole"/>.
/// </summary>
internal static class RoleMap
{
    internal static A11yPlatform CurrentPlatform { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? A11yPlatform.Windows
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? A11yPlatform.MacOS
        : A11yPlatform.Linux;

    internal static AccessibleRole Map(
        A11yPlatform platform,
        int rawRoleId,
        string? rawRole,
        string? rawSubrole,
        string? className,
        AccessibleStates states)
    {
        return platform switch
        {
            A11yPlatform.Windows => FromUia(rawRoleId, className, states),
            A11yPlatform.MacOS => FromAx(rawRole, rawSubrole),
            A11yPlatform.Linux => FromAtspi(rawRoleId),
            _ => AccessibleRole.Unknown,
        };
    }

    #region Windows: UI Automation control type IDs (UIAutomationClient.h)

    private const int UiaPane = 50033;
    private const int UiaWindow = 50032;

    // The class name of the desktop window.
    private const string DesktopClassName = "#32769";

    private static readonly Dictionary<int, AccessibleRole> s_uia = new()
    {
        [50000] = AccessibleRole.Button,
        [50001] = AccessibleRole.Calendar,
        [50002] = AccessibleRole.CheckBox,
        [50003] = AccessibleRole.ComboBox,
        [50004] = AccessibleRole.Edit,
        [50005] = AccessibleRole.Hyperlink,
        [50006] = AccessibleRole.Image,
        [50007] = AccessibleRole.ListItem,
        [50008] = AccessibleRole.List,
        [50009] = AccessibleRole.Menu,
        [50010] = AccessibleRole.MenuBar,
        [50011] = AccessibleRole.MenuItem,
        [50012] = AccessibleRole.ProgressBar,
        [50013] = AccessibleRole.RadioButton,
        [50014] = AccessibleRole.ScrollBar,
        [50015] = AccessibleRole.Slider,
        [50016] = AccessibleRole.Spinner,
        [50017] = AccessibleRole.StatusBar,
        [50018] = AccessibleRole.Tab,
        [50019] = AccessibleRole.TabItem,
        [50020] = AccessibleRole.Text,
        [50021] = AccessibleRole.ToolBar,
        [50022] = AccessibleRole.ToolTip,
        [50023] = AccessibleRole.Tree,
        [50024] = AccessibleRole.TreeItem,
        [50025] = AccessibleRole.Custom,
        [50026] = AccessibleRole.Group,
        [50027] = AccessibleRole.Thumb,
        [50028] = AccessibleRole.Table,     // DataGrid
        [50029] = AccessibleRole.Row,       // DataItem
        [50030] = AccessibleRole.Document,
        [50031] = AccessibleRole.SplitButton,
        [50032] = AccessibleRole.Window,
        [50033] = AccessibleRole.Pane,
        [50034] = AccessibleRole.Header,
        [50035] = AccessibleRole.HeaderItem,
        [50036] = AccessibleRole.Table,
        [50037] = AccessibleRole.TitleBar,
        [50038] = AccessibleRole.Separator,
        [50039] = AccessibleRole.Pane,      // SemanticZoom
        [50040] = AccessibleRole.ToolBar,   // AppBar
    };

    private static AccessibleRole FromUia(int controlTypeId, string? className, AccessibleStates states)
    {
        if (controlTypeId == UiaPane && className == DesktopClassName)
            return AccessibleRole.Desktop;
        if (controlTypeId == UiaWindow && states.HasFlag(AccessibleStates.Modal))
            return AccessibleRole.Dialog;
        return s_uia.TryGetValue(controlTypeId, out var role) ? role : AccessibleRole.Unknown;
    }

    #endregion

    #region macOS: AXRole and AXSubrole strings

    private static readonly Dictionary<string, AccessibleRole> s_axRoles = new(StringComparer.Ordinal)
    {
        ["AXSystemWide"] = AccessibleRole.Desktop,
        ["AXApplication"] = AccessibleRole.Application,
        ["AXWindow"] = AccessibleRole.Window,
        ["AXSheet"] = AccessibleRole.Dialog,
        ["AXDrawer"] = AccessibleRole.Pane,
        ["AXGroup"] = AccessibleRole.Group,
        ["AXRadioGroup"] = AccessibleRole.Group,
        ["AXScrollArea"] = AccessibleRole.Pane,
        ["AXSplitGroup"] = AccessibleRole.Pane,
        ["AXLayoutArea"] = AccessibleRole.Pane,
        ["AXButton"] = AccessibleRole.Button,
        ["AXMenuButton"] = AccessibleRole.SplitButton,
        ["AXDisclosureTriangle"] = AccessibleRole.Button,
        ["AXCheckBox"] = AccessibleRole.CheckBox,
        ["AXRadioButton"] = AccessibleRole.RadioButton,
        ["AXComboBox"] = AccessibleRole.ComboBox,
        ["AXPopUpButton"] = AccessibleRole.ComboBox,
        ["AXTextField"] = AccessibleRole.Edit,
        ["AXTextArea"] = AccessibleRole.Edit,
        ["AXSecureTextField"] = AccessibleRole.Edit,
        ["AXStaticText"] = AccessibleRole.Text,
        ["AXLink"] = AccessibleRole.Hyperlink,
        ["AXImage"] = AccessibleRole.Image,
        ["AXList"] = AccessibleRole.List,
        ["AXOutline"] = AccessibleRole.Tree,
        ["AXBrowser"] = AccessibleRole.Tree,
        ["AXTable"] = AccessibleRole.Table,
        ["AXGrid"] = AccessibleRole.Table,
        ["AXRow"] = AccessibleRole.Row,
        ["AXCell"] = AccessibleRole.Cell,
        ["AXColumn"] = AccessibleRole.Group,
        ["AXTabGroup"] = AccessibleRole.Tab,
        ["AXMenu"] = AccessibleRole.Menu,
        ["AXMenuBar"] = AccessibleRole.MenuBar,
        ["AXMenuItem"] = AccessibleRole.MenuItem,
        ["AXMenuBarItem"] = AccessibleRole.MenuItem,
        ["AXToolbar"] = AccessibleRole.ToolBar,
        ["AXScrollBar"] = AccessibleRole.ScrollBar,
        ["AXSlider"] = AccessibleRole.Slider,
        ["AXIncrementor"] = AccessibleRole.Spinner,
        ["AXStepper"] = AccessibleRole.Spinner,
        ["AXProgressIndicator"] = AccessibleRole.ProgressBar,
        ["AXBusyIndicator"] = AccessibleRole.ProgressBar,
        ["AXLevelIndicator"] = AccessibleRole.ProgressBar,
        ["AXSplitter"] = AccessibleRole.Separator,
        ["AXWebArea"] = AccessibleRole.Document,
        ["AXHelpTag"] = AccessibleRole.ToolTip,
        ["AXDateField"] = AccessibleRole.Calendar,
        ["AXValueIndicator"] = AccessibleRole.Thumb,
    };

    private static readonly Dictionary<string, AccessibleRole> s_axSubroles = new(StringComparer.Ordinal)
    {
        ["AXDialog"] = AccessibleRole.Dialog,
        ["AXSystemDialog"] = AccessibleRole.Dialog,
        ["AXFloatingWindow"] = AccessibleRole.Window,
        ["AXToggle"] = AccessibleRole.ToggleButton,
        ["AXSwitch"] = AccessibleRole.ToggleButton,
        ["AXTabButton"] = AccessibleRole.TabItem,
        ["AXOutlineRow"] = AccessibleRole.TreeItem,
        ["AXSortButton"] = AccessibleRole.HeaderItem,
        ["AXSearchField"] = AccessibleRole.Edit,
        ["AXSecureTextField"] = AccessibleRole.Edit,
        ["AXContentList"] = AccessibleRole.List,
        ["AXDefinitionList"] = AccessibleRole.List,
    };

    private static AccessibleRole FromAx(string? role, string? subrole)
    {
        if (subrole != null && s_axSubroles.TryGetValue(subrole, out var bySubrole))
            return bySubrole;
        if (role == null)
            return AccessibleRole.Unknown;
        if (role == "AXRow" && subrole == null)
            return AccessibleRole.Row;
        return s_axRoles.TryGetValue(role, out var byRole) ? byRole : AccessibleRole.Unknown;
    }

    #endregion

    #region Linux: AtspiRole values (atspi-constants.h)

    private static readonly Dictionary<int, AccessibleRole> s_atspi = new()
    {
        [2] = AccessibleRole.Dialog,        // ALERT
        [5] = AccessibleRole.Calendar,      // CALENDAR
        [7] = AccessibleRole.CheckBox,      // CHECK_BOX
        [8] = AccessibleRole.MenuItem,      // CHECK_MENU_ITEM
        [10] = AccessibleRole.HeaderItem,   // COLUMN_HEADER
        [11] = AccessibleRole.ComboBox,     // COMBO_BOX
        [14] = AccessibleRole.Desktop,      // DESKTOP_FRAME
        [16] = AccessibleRole.Dialog,       // DIALOG
        [19] = AccessibleRole.Dialog,       // FILE_CHOOSER
        [20] = AccessibleRole.Pane,         // FILLER
        [23] = AccessibleRole.Window,       // FRAME
        [26] = AccessibleRole.Image,        // ICON
        [27] = AccessibleRole.Image,        // IMAGE
        [28] = AccessibleRole.Window,       // INTERNAL_FRAME
        [29] = AccessibleRole.Text,         // LABEL
        [31] = AccessibleRole.List,         // LIST
        [32] = AccessibleRole.ListItem,     // LIST_ITEM
        [33] = AccessibleRole.Menu,         // MENU
        [34] = AccessibleRole.MenuBar,      // MENU_BAR
        [35] = AccessibleRole.MenuItem,     // MENU_ITEM
        [37] = AccessibleRole.TabItem,      // PAGE_TAB
        [38] = AccessibleRole.Tab,          // PAGE_TAB_LIST
        [39] = AccessibleRole.Pane,         // PANEL
        [40] = AccessibleRole.Edit,         // PASSWORD_TEXT
        [41] = AccessibleRole.Menu,         // POPUP_MENU
        [42] = AccessibleRole.ProgressBar,  // PROGRESS_BAR
        [43] = AccessibleRole.Button,       // BUTTON (PUSH_BUTTON)
        [44] = AccessibleRole.RadioButton,  // RADIO_BUTTON
        [45] = AccessibleRole.MenuItem,     // RADIO_MENU_ITEM
        [46] = AccessibleRole.Pane,         // ROOT_PANE
        [47] = AccessibleRole.HeaderItem,   // ROW_HEADER
        [48] = AccessibleRole.ScrollBar,    // SCROLL_BAR
        [49] = AccessibleRole.Pane,         // SCROLL_PANE
        [50] = AccessibleRole.Separator,    // SEPARATOR
        [51] = AccessibleRole.Slider,       // SLIDER
        [52] = AccessibleRole.Spinner,      // SPIN_BUTTON
        [53] = AccessibleRole.Pane,         // SPLIT_PANE
        [54] = AccessibleRole.StatusBar,    // STATUS_BAR
        [55] = AccessibleRole.Table,        // TABLE
        [56] = AccessibleRole.Cell,         // TABLE_CELL
        [57] = AccessibleRole.HeaderItem,   // TABLE_COLUMN_HEADER
        [58] = AccessibleRole.HeaderItem,   // TABLE_ROW_HEADER
        [59] = AccessibleRole.MenuItem,     // TEAROFF_MENU_ITEM
        [60] = AccessibleRole.Document,     // TERMINAL
        [61] = AccessibleRole.Edit,         // TEXT
        [62] = AccessibleRole.ToggleButton, // TOGGLE_BUTTON
        [63] = AccessibleRole.ToolBar,      // TOOL_BAR
        [64] = AccessibleRole.ToolTip,      // TOOL_TIP
        [65] = AccessibleRole.Tree,         // TREE
        [66] = AccessibleRole.Tree,         // TREE_TABLE
        [69] = AccessibleRole.Window,       // WINDOW
        [71] = AccessibleRole.Header,       // HEADER
        [75] = AccessibleRole.Application,  // APPLICATION
        [79] = AccessibleRole.Edit,         // ENTRY
        [82] = AccessibleRole.Document,     // DOCUMENT_FRAME
        [85] = AccessibleRole.Group,        // SECTION
        [88] = AccessibleRole.Hyperlink,    // LINK
        [90] = AccessibleRole.Row,          // TABLE_ROW
        [91] = AccessibleRole.TreeItem,     // TREE_ITEM
        [92] = AccessibleRole.Document,     // DOCUMENT_SPREADSHEET
        [93] = AccessibleRole.Document,     // DOCUMENT_PRESENTATION
        [94] = AccessibleRole.Document,     // DOCUMENT_TEXT
        [95] = AccessibleRole.Document,     // DOCUMENT_WEB
        [96] = AccessibleRole.Document,     // DOCUMENT_EMAIL
        [98] = AccessibleRole.List,         // LIST_BOX
        [99] = AccessibleRole.Group,        // GROUPING
        [104] = AccessibleRole.TitleBar,    // TITLE_BAR
        [116] = AccessibleRole.Text,        // STATIC
    };

    private static AccessibleRole FromAtspi(int role)
    {
        return s_atspi.TryGetValue(role, out var mapped) ? mapped : AccessibleRole.Unknown;
    }

    #endregion
}
