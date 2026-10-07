// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Laz;

/// <summary>
/// The role of an accessible element, normalized across platforms.
/// </summary>
/// <remarks>
/// The platform role is always available in <see cref="AccessibleElement.RawRole"/> and
/// <see cref="AccessibleElement.RawRoleId"/>. Roles that have no counterpart here map to
/// <see cref="Unknown"/>.
/// </remarks>
public enum AccessibleRole
{
    /// <summary>The platform role has no normalized counterpart.</summary>
    Unknown = 0,

    /// <summary>The root of the tree.</summary>
    Desktop,

    /// <summary>An application. Present on macOS and Linux, where windows are grouped by application.</summary>
    Application,

    /// <summary>A top-level or child window.</summary>
    Window,

    /// <summary>A dialog or alert.</summary>
    Dialog,

    /// <summary>A container without a more specific role, such as a scroll area.</summary>
    Pane,

    /// <summary>A group of related controls.</summary>
    Group,

    /// <summary>A push button.</summary>
    Button,

    /// <summary>A button that stays pressed until it is clicked again.</summary>
    ToggleButton,

    /// <summary>A check box.</summary>
    CheckBox,

    /// <summary>A radio button.</summary>
    RadioButton,

    /// <summary>A combo box or a pop-up button.</summary>
    ComboBox,

    /// <summary>An editable text field.</summary>
    Edit,

    /// <summary>A static text or label.</summary>
    Text,

    /// <summary>A hyperlink.</summary>
    Hyperlink,

    /// <summary>An image or icon.</summary>
    Image,

    /// <summary>A list.</summary>
    List,

    /// <summary>An item of a list.</summary>
    ListItem,

    /// <summary>A tree.</summary>
    Tree,

    /// <summary>An item of a tree.</summary>
    TreeItem,

    /// <summary>A table or data grid.</summary>
    Table,

    /// <summary>A row of a table.</summary>
    Row,

    /// <summary>A cell of a table.</summary>
    Cell,

    /// <summary>A header of a table or list.</summary>
    Header,

    /// <summary>An item of a header, such as a column header.</summary>
    HeaderItem,

    /// <summary>A tab control.</summary>
    Tab,

    /// <summary>A tab of a tab control.</summary>
    TabItem,

    /// <summary>A menu.</summary>
    Menu,

    /// <summary>A menu bar.</summary>
    MenuBar,

    /// <summary>An item of a menu or menu bar.</summary>
    MenuItem,

    /// <summary>A toolbar.</summary>
    ToolBar,

    /// <summary>A status bar.</summary>
    StatusBar,

    /// <summary>A scroll bar.</summary>
    ScrollBar,

    /// <summary>A slider.</summary>
    Slider,

    /// <summary>A spinner, also known as a stepper or spin button.</summary>
    Spinner,

    /// <summary>A progress bar.</summary>
    ProgressBar,

    /// <summary>A separator.</summary>
    Separator,

    /// <summary>A document, such as a web page.</summary>
    Document,

    /// <summary>The title bar of a window.</summary>
    TitleBar,

    /// <summary>A tooltip.</summary>
    ToolTip,

    /// <summary>A calendar.</summary>
    Calendar,

    /// <summary>A button with a drop-down part.</summary>
    SplitButton,

    /// <summary>The thumb of a scroll bar or slider.</summary>
    Thumb,

    /// <summary>A custom control.</summary>
    Custom,
}
