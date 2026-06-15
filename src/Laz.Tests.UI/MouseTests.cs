// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Layout;
using Laz.Tests.UI.Infrastructure;
using Xunit;

namespace Laz.Tests.UI;

public class MouseTests : RobotTestBase
{
    #region Position Tests

    [Fact]
    public async Task ReturnsCorrectPosition()
    {
        var target = new Point(100, 200);

        Lazbot.Mouse.JumpTo(target);

        await DelayedAssertion.Eventually("Cursor should be at target position", () =>
        {
            var actual = Lazbot.Mouse.GetPosition();
            Assert.Equal(target.X, actual.X);
            Assert.Equal(target.Y, actual.Y);
        });
    }

    [Fact]
    public async Task CursorOverWindow()
    {

        var pointerEntered = false;
        await OnUIThread(() =>
        {
            TestWindow.PointerEntered += (_, _) => pointerEntered = true;
        });

        // Ensure cursor starts outside the window so PointerEntered can fire reliably on Windows.
        var outsideWindow = await OnUIThread(() => GetPointOutsideWindow(TestWindow));
        Lazbot.Mouse.JumpTo(outsideWindow);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var windowRect = await OnUIThread(() =>
        {
            var topLeft = TestWindow.Position;
            var scale = TestWindow.RenderScaling;
            var width = (int)Math.Round(TestWindow.Bounds.Width * scale);
            var height = (int)Math.Round(TestWindow.Bounds.Height * scale);
            return (TopLeft: new Point(topLeft.X, topLeft.Y),
                    BottomRight: new Point(topLeft.X + width, topLeft.Y + height));
        });

        var current = Lazbot.Mouse.GetPosition();
        var inside = current.X >= windowRect.TopLeft.X &&
                     current.X <= windowRect.BottomRight.X &&
                     current.Y >= windowRect.TopLeft.Y &&
                     current.Y <= windowRect.BottomRight.Y;

        if (inside)
        {
            int y = windowRect.BottomRight.Y + 50;
            Lazbot.Mouse.JumpTo(new Point(windowRect.BottomRight.X + 50, y));
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        var center = await OnUIThread(() => GetScreenCenter(TestWindow));
        Lazbot.Mouse.JumpTo(center);

        await DelayedAssertion.Eventually("PointerEntered event should fire", () => Assert.True(pointerEntered, "Cursor should have entered the window"));
    }

    [Fact]
    public async Task AreEquivalent()
    {
        var target = new Point(150, 250);

        Lazbot.Mouse.JumpTo(target);
        Point posAfterPoint = default;
        await DelayedAssertion.Eventually("Cursor should reach target via LazPoint overload", () =>
        {
            posAfterPoint = Lazbot.Mouse.GetPosition();
            Assert.Equal(target.X, posAfterPoint.X);
            Assert.Equal(target.Y, posAfterPoint.Y);
        });

        var outside = await OnUIThread(() => GetPointOutsideWindow(TestWindow));
        Lazbot.Mouse.JumpTo(outside);
        await DelayedAssertion.Eventually("Cursor should move outside the window", () =>
        {
            var pos = Lazbot.Mouse.GetPosition();
            Assert.Equal(outside.X, pos.X);
            Assert.Equal(outside.Y, pos.Y);
        });

        Lazbot.Mouse.JumpTo(new Point(target.X, target.Y));
        await DelayedAssertion.Eventually("Both JumpTo overloads should produce the same result", () =>
        {
            var posAfterXy = Lazbot.Mouse.GetPosition();
            Assert.Equal(posAfterPoint.X, posAfterXy.X);
            Assert.Equal(posAfterPoint.Y, posAfterXy.Y);
            Assert.Equal(target.X, posAfterXy.X);
            Assert.Equal(target.Y, posAfterXy.Y);
        });
    }

    #endregion

    #region Click Tests

    [Fact]
    public async Task TriggersButtonClick()
    {

        var clicked = false;
        Button button = null!;

        await OnUIThread(() =>
        {
            button = new Button
            {
                Content = "Click Me",
                Width = 150,
                Height = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            button.Click += (_, _) => clicked = true;
            TestWindow.Content = button;
        });

        await WaitForControlReady(button);
        await MoveToControl(button);

        Lazbot.Mouse.Click();

        await DelayedAssertion.Eventually("Button should register click", () => Assert.True(clicked, "Button click event should have fired"));
    }

    [Fact]
    public async Task IsPrimary()
    {

        var clicked = false;
        Button button = null!;

        await OnUIThread(() =>
        {
            button = new Button { Content = "Test", Width = 100, Height = 40 };
            button.Click += (_, _) => clicked = true;
            TestWindow.Content = button;
        });

        await WaitForControlReady(button);
        await MoveToControl(button);

        Lazbot.Mouse.Click();

        await DelayedAssertion.Eventually("Default click should trigger button", () => Assert.True(clicked));
    }

    [Fact]
    public async Task TriggersRightClick()
    {

        var rightClicked = false;
        Panel panel = null!;

        await OnUIThread(() =>
        {
            panel = new Panel { Background = Avalonia.Media.Brushes.LightGray };
            panel.PointerPressed += (_, e) =>
            {
                var props = e.GetCurrentPoint(panel).Properties;
                if (props.IsRightButtonPressed)
                    rightClicked = true;
            };
            TestWindow.Content = panel;
        });

        await WaitForControlReady(panel);
        await MoveToControl(panel);

        Lazbot.Mouse.Click(MouseButton.Secondary);

        await DelayedAssertion.Eventually("Right click should be detected", () => Assert.True(rightClicked, "Right click should have been detected"));
    }

    [Fact]
    public async Task TriggersMiddleClick()
    {

        var middleClicked = false;
        Panel panel = null!;

        await OnUIThread(() =>
        {
            panel = new Panel { Background = Avalonia.Media.Brushes.LightGray };
            panel.PointerPressed += (_, e) =>
            {
                var props = e.GetCurrentPoint(panel).Properties;
                if (props.IsMiddleButtonPressed)
                    middleClicked = true;
            };
            TestWindow.Content = panel;
        });

        await WaitForControlReady(panel);
        await MoveToControl(panel);

        Lazbot.Mouse.Click(MouseButton.Middle);

        await DelayedAssertion.Eventually("Middle click should be detected", () => Assert.True(middleClicked, "Middle click should have been detected"));
    }

    [Fact]
    public async Task TriggersDoubleClick()
    {

        var doubleClickCount = 0;
        Panel panel = null!;

        await OnUIThread(() =>
        {
            panel = new Panel { Background = Avalonia.Media.Brushes.LightGray };
            panel.DoubleTapped += (_, _) => doubleClickCount++;
            TestWindow.Content = panel;
        });

        await WaitForControlReady(panel);
        await MoveToControl(panel);

        Lazbot.Mouse.DoubleClick();

        await DelayedAssertion.Eventually("Double click should trigger DoubleTapped event", () => Assert.Equal(1, doubleClickCount));
    }

    #endregion

    #region Press/Release Tests

    [Fact]
    public async Task FiresBothEvents()
    {

        var pressedCount = 0;
        var releasedCount = 0;
        Panel panel = null!;

        await OnUIThread(() =>
        {
            panel = new Panel { Background = Avalonia.Media.Brushes.LightGray };
            panel.PointerPressed += (_, _) => pressedCount++;
            panel.PointerReleased += (_, _) => releasedCount++;
            TestWindow.Content = panel;
        });

        await WaitForControlReady(panel);
        await MoveToControl(panel);

        Lazbot.Mouse.Press();
        await DelayedAssertion.Eventually("Press event should fire", () => Assert.Equal(1, pressedCount));

        Lazbot.Mouse.Release();

        await DelayedAssertion.Eventually("Release event should fire", () => Assert.Equal(1, releasedCount));
    }

    [Fact]
    public async Task OnlyPressEventFires()
    {

        var pressedCount = 0;
        var releasedCount = 0;
        Panel panel = null!;

        await OnUIThread(() =>
        {
            panel = new Panel { Background = Avalonia.Media.Brushes.LightGray };
            panel.PointerPressed += (_, _) => pressedCount++;
            panel.PointerReleased += (_, _) => releasedCount++;
            TestWindow.Content = panel;
        });

        await WaitForControlReady(panel);
        await MoveToControl(panel);

        Lazbot.Mouse.Press();

        await DelayedAssertion.Eventually("Press event should fire", () => Assert.Equal(1, pressedCount));
        Assert.Equal(0, releasedCount);

        Lazbot.Mouse.Release();
    }

    [Fact]
    public async Task GeneratesPointerMovedWithButtonPressed()
    {

        var movedWithButtonPressed = false;
        Panel panel = null!;

        await OnUIThread(() =>
        {
            panel = new Panel { Background = Avalonia.Media.Brushes.LightGray };
            panel.PointerMoved += (_, e) =>
            {
                var props = e.GetCurrentPoint(panel).Properties;
                if (props.IsLeftButtonPressed)
                    movedWithButtonPressed = true;
            };
            TestWindow.Content = panel;
        });

        await WaitForControlReady(panel);

        var startPos = await OnUIThread(() => GetScreenCenter(panel));
        var endPos = new Point(startPos.X + 50, startPos.Y + 50);

        Lazbot.Mouse.JumpTo(startPos);
        Lazbot.Mouse.Delay(TimeSpan.FromMilliseconds(50));
        Lazbot.Mouse.Press();
        Lazbot.Mouse.Delay(TimeSpan.FromMilliseconds(50));
        Lazbot.Mouse.JumpTo(endPos);
        Lazbot.Mouse.Delay(TimeSpan.FromMilliseconds(50));
        Lazbot.Mouse.Release();

        await DelayedAssertion.Eventually("Drag should generate PointerMoved with button pressed", () => Assert.True(movedWithButtonPressed, "PointerMoved with left button pressed should have fired"));
    }

    #endregion

    #region Scroll Tests

    [Fact]
    public async Task GeneratesWheelEvent()
    {

        double? deltaY = null;
        Panel panel = null!;

        await OnUIThread(() =>
        {
            panel = new Panel { Background = Avalonia.Media.Brushes.LightGray };
            panel.PointerWheelChanged += (_, e) => deltaY = e.Delta.Y;
            TestWindow.Content = panel;
        });

        await WaitForControlReady(panel);
        await MoveToControl(panel);

        Lazbot.Mouse.Scroll(1);

        await DelayedAssertion.Eventually("Scroll should generate wheel event", () => Assert.NotNull(deltaY));
    }

    [Fact]
    public async Task GenerateOppositeDeltas()
    {

        var deltas = new List<double>();
        Panel panel = null!;

        await OnUIThread(() =>
        {
            panel = new Panel { Background = Avalonia.Media.Brushes.LightGray };
            panel.PointerWheelChanged += (_, e) => deltas.Add(e.Delta.Y);
            TestWindow.Content = panel;
        });

        await WaitForControlReady(panel);
        await MoveToControl(panel);

        Lazbot.Mouse.Scroll(1);
        await DelayedAssertion.Eventually("First scroll should generate wheel event", () => Assert.Single(deltas));

        Lazbot.Mouse.Scroll(-1);

        await DelayedAssertion.Eventually("Second scroll should generate opposite wheel event", () =>
        {
            Assert.Equal(2, deltas.Count);
            Assert.NotEqual(Math.Sign(deltas[0]), Math.Sign(deltas[1]));
        });
    }

    [Fact]
    public async Task GeneratesNoWheelEvent()
    {
        var wheelEventCount = 0;
        Panel panel = null!;

        await OnUIThread(() =>
        {
            panel = new Panel { Background = Avalonia.Media.Brushes.LightGray };
            panel.PointerWheelChanged += (_, _) => wheelEventCount++;
            TestWindow.Content = panel;
        });

        await WaitForControlReady(panel);
        await MoveToControl(panel);

        Lazbot.Mouse.Scroll(0);
        await Task.Delay(150, TestContext.Current.CancellationToken);

        Assert.Equal(0, wheelEventCount);
    }

    #endregion

    #region Fluent Interface Tests

    [Fact]
    public void AllMethodsReturnSameInstance()
    {
        var mouse = Lazbot.Mouse;

        mouse.JumpTo(new Point(100, 100));
        Assert.Same(mouse, mouse.JumpTo(new Point(100, 100)));
        Assert.Same(mouse, mouse.Click());
        Assert.Same(mouse, mouse.DoubleClick());
        Assert.Same(mouse, mouse.Press());
        Assert.Same(mouse, mouse.Release());
        Assert.Same(mouse, mouse.Scroll(1));
        Assert.Same(mouse, mouse.Delay(TimeSpan.Zero));
    }

    [Fact]
    public async Task ChainingWorks()
    {
        var startPos = new Point(100, 100);
        var endPos = new Point(150, 150);

        Lazbot.Mouse
            .JumpTo(startPos)
            .Delay(TimeSpan.FromMilliseconds(10))
            .Click()
            .JumpTo(endPos);

        await DelayedAssertion.Eventually("Cursor should be at end position after chained operations", () =>
        {
            var finalPos = Lazbot.Mouse.GetPosition();
            Assert.Equal(endPos.X, finalPos.X);
            Assert.Equal(endPos.Y, finalPos.Y);
        });
    }

    #endregion

    #region Context Menu Tests

    [Fact]
    public async Task OpensContextMenu()
    {
        var contextMenuOpened = false;
        Panel panel = null!;
        ContextMenu contextMenu = null!;

        await OnUIThread(() =>
        {
            contextMenu = new ContextMenu();
            contextMenu.Items.Add(new MenuItem { Header = "Test Item" });
            contextMenu.Opened += (_, _) => contextMenuOpened = true;

            panel = new Panel
            {
                Background = Avalonia.Media.Brushes.LightGray,
                ContextMenu = contextMenu
            };
            TestWindow.Content = panel;
        });

        await WaitForControlReady(panel);
        await MoveToControl(panel);

        Lazbot.Mouse.Click(MouseButton.Secondary);

        await DelayedAssertion.Eventually("Context menu should open on right-click", () => Assert.True(contextMenuOpened, "Context menu should have opened on right-click"));

        await OnUIThread(() => contextMenu.Close());
    }

    #endregion
}
