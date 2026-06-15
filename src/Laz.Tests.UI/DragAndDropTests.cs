// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Laz.Extensions.Mouse;
using Laz.Tests.UI.Infrastructure;
using Xunit;

namespace Laz.Tests.UI;

public class DragAndDropTests : RobotTestBase
{
    // Mirrors the VisualDemo drag-and-drop scene:
    //   A draggable Border on a Canvas, and a drop zone Border.
    //   Pointer capture (e.Pointer.Capture) ensures the draggable follows the
    //   cursor while the button is held, even when the pointer leaves the element.
    private sealed record DragDropScene(Canvas Canvas, Border Draggable, Border DropZone);

    private static DragDropScene CreateScene(Window window)
    {
        var canvas = new Canvas { Background = Brushes.WhiteSmoke };

        // Draggable item - left side
        var draggable = new Border
        {
            Width = 80,
            Height = 60,
            Background = Brushes.Coral,
            CornerRadius = new CornerRadius(8),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        Canvas.SetLeft(draggable, 40);
        Canvas.SetTop(draggable, 120);

        // Drop zone - right side
        var dropZone = new Border
        {
            Width = 120,
            Height = 80,
            Background = Brushes.AliceBlue,
            BorderBrush = Brushes.DarkSlateBlue,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8)
        };
        Canvas.SetLeft(dropZone, 240);
        Canvas.SetTop(dropZone, 110);

        canvas.Children.Add(dropZone);
        canvas.Children.Add(draggable);
        window.Content = canvas;

        // Drag state - identical logic to VisualDemo's OnDraggable* handlers
        var isDragging = false;
        Avalonia.Point dragOffset = default;

        draggable.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(draggable).Properties.IsLeftButtonPressed) return;
            isDragging = true;
            dragOffset = e.GetPosition(draggable);
            e.Pointer.Capture(draggable);
            e.Handled = true;
        };

        draggable.PointerMoved += (_, e) =>
        {
            if (!isDragging) return;
            var pos = e.GetPosition(canvas);
            Canvas.SetLeft(draggable, pos.X - dragOffset.X);
            Canvas.SetTop(draggable, pos.Y - dragOffset.Y);
            e.Handled = true;
        };

        draggable.PointerReleased += (_, e) =>
        {
            if (!isDragging) return;
            isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        };

        return new DragDropScene(canvas, draggable, dropZone);
    }

    // Mirrors VisualDemo's IsOverDropZone - checks whether the draggable's center
    // falls within the drop zone rectangle.
    private static bool IsOverDropZone(Border draggable, Border dropZone)
    {
        var itemCenterX = Canvas.GetLeft(draggable) + draggable.Width / 2;
        var itemCenterY = Canvas.GetTop(draggable) + draggable.Height / 2;
        var dropLeft = Canvas.GetLeft(dropZone);
        var dropTop = Canvas.GetTop(dropZone);
        return itemCenterX >= dropLeft && itemCenterX <= dropLeft + dropZone.Width &&
               itemCenterY >= dropTop && itemCenterY <= dropTop + dropZone.Height;
    }

    private async Task BringWindowToFront()
    {
        await OnUIThread(() =>
        {
            TestWindow.Topmost = true;
            TestWindow.Activate();
        });
        await Task.Delay(200);
        await OnUIThread(() => TestWindow.Topmost = false);
    }

    [Fact]
    public async Task DropsItemOntoTarget()
    {
        DragDropScene scene = null!;
        await OnUIThread(() => scene = CreateScene(TestWindow));
        await WaitForControlReady(scene.Canvas);
        await BringWindowToFront();

        var from = await OnUIThread(() => GetScreenCenter(scene.Draggable));
        var to = await OnUIThread(() => GetScreenCenter(scene.DropZone));

        await Task.Run(() => Lazbot.Mouse.DragAndDrop(from, to, TimeSpan.FromMilliseconds(500), dragPreamble: true));

        await DelayedAssertion.Eventually("Draggable center should be within the drop zone after releasing over it", () =>
        {
            var over = Dispatcher.UIThread.Invoke(() => IsOverDropZone(scene.Draggable, scene.DropZone));
            Assert.True(over, "Draggable was not released over the drop zone");
        });
    }

    [Fact]
    public async Task MissedDragDoesNotTriggerDrop()
    {
        DragDropScene scene = null!;
        await OnUIThread(() => scene = CreateScene(TestWindow));
        await WaitForControlReady(scene.Canvas);
        await BringWindowToFront();

        var from = await OnUIThread(() => GetScreenCenter(scene.Draggable));
        var to = new Point(from.X, from.Y - 80);

        await Task.Run(() => Lazbot.Mouse.DragAndDrop(from, to, TimeSpan.FromMilliseconds(500)));

        await DelayedAssertion.Eventually("Draggable should not be over the drop zone after an off-target drag", () =>
        {
            var over = Dispatcher.UIThread.Invoke(() => IsOverDropZone(scene.Draggable, scene.DropZone));
            Assert.False(over, "Draggable should not be over the drop zone when released elsewhere");
        });
    }
}
