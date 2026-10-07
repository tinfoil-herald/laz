// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Laz.Tests.UI.Infrastructure;
using Xunit;

namespace Laz.Tests.UI;

[Trait("Category", "Accessibility")]
public class AccessibilityTests : RobotTestBase
{
    private const string OkButtonId = "okButton";
    private const string InputId = "input";
    private const string CheckBoxId = "agree";

    private static readonly FindOptions Search = new() { MaxDepth = 20, Timeout = TimeSpan.FromSeconds(10) };

    private Button _okButton = null!;
    private TextBox _input = null!;
    private bool _clicked;

    private Accessibility Accessibility => Lazbot.Accessibility;

    /// <summary>
    /// Fills the test window with known controls and returns its accessible element.
    /// </summary>
    private async Task<AccessibleElement> SetUpWindow()
    {
        Assert.SkipUnless(Accessibility.IsAvailable(), "The accessibility service is not available.");

        await OnUIThread(() =>
        {
            _okButton = new Button { Content = "OK", Width = 120, Height = 40 };
            AutomationProperties.SetAutomationId(_okButton, OkButtonId);
            _okButton.Click += (_, _) => _clicked = true;

            _input = new TextBox { Text = "hello", Width = 200 };
            AutomationProperties.SetAutomationId(_input, InputId);

            var checkBox = new CheckBox { Content = "I agree", IsChecked = true };
            AutomationProperties.SetAutomationId(checkBox, CheckBoxId);

            TestWindow.Content = new StackPanel
            {
                Spacing = 10,
                Margin = new Avalonia.Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { _okButton, _input, checkBox },
            };
        });
        await WaitForControlReady(_okButton);

        var title = await OnUIThread(() => TestWindow.Title);
        return Accessibility.WaitFor(
            e => e.Role == AccessibleRole.Window && e.Name == title && e.ProcessId == Environment.ProcessId,
            TimeSpan.FromSeconds(10),
            options: new FindOptions { MaxDepth = 2 });
    }

    private AccessibleElement FindById(AccessibleElement window, string automationId)
    {
        var element = Accessibility.Find(e => e.AutomationId == automationId, window, Search);
        Assert.NotNull(element);
        return element;
    }

    private async Task<global::Laz.Rectangle> GetWindowRect()
    {
        return await OnUIThread(() =>
        {
            var scale = TestWindow.RenderScaling;
            return new global::Laz.Rectangle(
                TestWindow.Position.X,
                TestWindow.Position.Y,
                (int)Math.Round(TestWindow.Bounds.Width * scale),
                (int)Math.Round(TestWindow.Bounds.Height * scale));
        });
    }

    [Fact]
    public async Task FindsTestWindow()
    {
        using var window = await SetUpWindow();

        Assert.Equal(AccessibleRole.Window, window.Role);
        Assert.Equal(Environment.ProcessId, window.ProcessId);
        Assert.True(window.IsEnabled);
    }

    [Fact]
    public async Task GetWindowsIncludesTestWindow()
    {
        using var window = await SetUpWindow();

        var windows = Accessibility.GetWindows();
        try
        {
            Assert.Contains(windows, w => w.IsSameAs(window));
        }
        finally
        {
            foreach (var w in windows)
                w.Dispose();
        }
    }

    [Fact]
    public async Task FindsButtonByRoleAndName()
    {
        using var window = await SetUpWindow();

        using var button = Accessibility.Find(AccessibleRole.Button, "OK", window, Search);

        Assert.NotNull(button);
        Assert.Equal(OkButtonId, button.AutomationId);
        Assert.True(button.IsEnabled);
        Assert.Equal(Environment.ProcessId, button.ProcessId);
    }

    [Fact]
    public async Task ButtonBoundsMatchControl()
    {
        using var window = await SetUpWindow();
        using var button = FindById(window, OkButtonId);

        var windowRect = await GetWindowRect();
        var expectedCenter = await OnUIThread(() => GetScreenCenter(_okButton));

        Assert.Equal(BoundsKind.Screen, button.BoundsKind);
        Assert.True(windowRect.IntersectsWith(button.Bounds), $"Button {button.Bounds} is outside of window {windowRect}");
        Assert.InRange(button.Bounds.Center.X, expectedCenter.X - 2, expectedCenter.X + 2);
        Assert.InRange(button.Bounds.Center.Y, expectedCenter.Y - 2, expectedCenter.Y + 2);
    }

    [Fact]
    public async Task ClickingBoundsCenterClicksButton()
    {
        using var window = await SetUpWindow();
        using var button = FindById(window, OkButtonId);

        Lazbot.Mouse.JumpTo(button.Bounds.Center);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Lazbot.Mouse.Click();

        await DelayedAssertion.Eventually("Button found by accessibility should be clicked", () => Assert.True(_clicked));
    }

    [Fact]
    [SkipOn(Platform.LinuxWayland, "Wayland has no global hit-testing")]
    public async Task ElementFromPointReturnsButton()
    {
        using var window = await SetUpWindow();
        using var button = FindById(window, OkButtonId);

        using var hit = Accessibility.ElementFromPoint(button.Bounds.Center);

        Assert.NotNull(hit);
        // Some toolkits return the text inside the button rather than the button itself.
        var isButton = hit.IsSameAs(button) || hit.Parent?.IsSameAs(button) == true;
        Assert.True(isButton, $"Expected the button, got {hit}");
    }

    [Fact]
    public async Task ReadsTextBoxValue()
    {
        using var window = await SetUpWindow();
        using var input = FindById(window, InputId);

        Assert.Equal(AccessibleRole.Edit, input.Role);
        Assert.Equal("hello", input.Value);
    }

    [Fact]
    public async Task FocusedElementIsTextBox()
    {
        using var window = await SetUpWindow();
        await OnUIThread(() => _input.Focus());

        await DelayedAssertion.Eventually("The text box should have focus", () =>
        {
            using var focused = Accessibility.GetFocusedElement();
            Assert.NotNull(focused);
            Assert.Equal(InputId, focused.AutomationId);
            Assert.True(focused.IsFocused);
        });
    }

    [Fact]
    public async Task ReadsCheckBoxState()
    {
        using var window = await SetUpWindow();
        using var checkBox = FindById(window, CheckBoxId);

        Assert.Equal(AccessibleRole.CheckBox, checkBox.Role);
        Assert.True(checkBox.States.HasFlag(AccessibleStates.Checked));
    }

    [Fact]
    public async Task RefreshReadsNewValue()
    {
        using var window = await SetUpWindow();
        using var input = FindById(window, InputId);

        await OnUIThread(() => _input.Text = "changed");

        await DelayedAssertion.Eventually("Refresh should pick up the new value", () =>
        {
            input.Refresh();
            Assert.Equal("changed", input.Value);
        });
    }

    [Fact]
    public async Task ParentAndChildrenAreConsistent()
    {
        using var window = await SetUpWindow();

        Assert.NotEmpty(window.Children);
        foreach (var child in window.Children)
        {
            Assert.NotNull(child.Parent);
            Assert.True(child.Parent.IsSameAs(window), $"Parent of {child} should be the window");
        }
    }

    [Fact]
    public async Task DesktopHasNoParent()
    {
        using var window = await SetUpWindow();
        using var desktop = Accessibility.GetDesktop();

        Assert.Equal(AccessibleRole.Desktop, desktop.Role);
        Assert.Null(desktop.Parent);
    }

    [Fact]
    public async Task DisposedElementThrows()
    {
        var window = await SetUpWindow();
        window.Dispose();

        Assert.Throws<ObjectDisposedException>(() => window.Children);
        Assert.Throws<ObjectDisposedException>(() => window.Refresh());
    }

    [Fact]
    public async Task ClosedWindowIsNotAvailable()
    {
        using var window = await SetUpWindow();
        using var button = FindById(window, OkButtonId);

        await OnUIThread(() => TestWindow.Close());

        await DelayedAssertion.Eventually("The element of a closed window should be gone", () =>
            Assert.Throws<ElementNotAvailableException>(() => button.Refresh()));
    }
}
