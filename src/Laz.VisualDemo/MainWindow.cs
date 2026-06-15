// Copyright (c) 2026 Vladyslav Lubenskyi
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Laz.Extensions.Keyboard;
using Laz.Extensions.Mouse;

namespace Laz.VisualDemo;

public class MainWindow : Window
{
    private readonly Canvas _canvas;
    private readonly Canvas _screenshotCanvas;
    private readonly TextBox _inputField;
    private readonly Button _startButton;
    private readonly Lazbot _lazbot = new();

    // Track if we're currently drawing (mouse pressed)
    private bool _isDrawing;
    private Avalonia.Point _lastDrawPoint;

    // Drag-and-drop demo state
    private Border _draggableItem = null!;
    private Border _dropZone = null!;
    private Canvas _dragDropCanvas = null!;
    private TextBlock _dropStatusText = null!;
    private bool _isDragging;
    private Avalonia.Point _dragOffset;
    private const double DraggableInitialLeft = 50;
    private const double DraggableInitialTop = 45;

    public MainWindow()
    {
        Title = "Laz Visual Demo";
        Width = 800;
        Height = 900;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        // Create the main layout
        var mainPanel = new DockPanel
        {
            LastChildFill = true
        };

        // Create bottom panel with input field and start button
        var bottomPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 20,
            Margin = new Thickness(10)
        };
        DockPanel.SetDock(bottomPanel, Dock.Bottom);

        _inputField = new TextBox
        {
            Name = "InputField",
            Width = 300,
            Height = 35,
            Watermark = "Text will be typed here...",
            FontSize = 14
        };

        _startButton = new Button
        {
            Content = "Start",
            Width = 100,
            Height = 35,
            FontSize = 14
        };
        _startButton.Click += OnStartButtonClick;

        bottomPanel.Children.Add(_inputField);
        bottomPanel.Children.Add(_startButton);

        // Create the screenshot canvas with a border (docked at bottom, above input panel)
        var screenshotBorder = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(2),
            Margin = new Thickness(10, 5, 10, 0),
            Background = Brushes.LightGray,
            Height = 250
        };
        DockPanel.SetDock(screenshotBorder, Dock.Bottom);

        _screenshotCanvas = new Canvas
        {
            Name = "ScreenshotCanvas",
            Background = Brushes.LightGray
        };

        screenshotBorder.Child = _screenshotCanvas;

        // Create the drawing canvas with a border (fills remaining space)
        var canvasBorder = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(2),
            Margin = new Thickness(10, 10, 10, 0),
            Background = Brushes.White
        };

        _canvas = new Canvas
        {
            Name = "DrawingCanvas",
            Background = Brushes.White
        };

        // Attach mouse event handlers to canvas
        _canvas.PointerPressed += OnCanvasPointerPressed;
        _canvas.PointerReleased += OnCanvasPointerReleased;
        _canvas.PointerMoved += OnCanvasPointerMoved;

        canvasBorder.Child = _canvas;

        var dragDropSection = CreateDragDropSection();
        DockPanel.SetDock(dragDropSection, Dock.Bottom);

        mainPanel.Children.Add(bottomPanel);
        mainPanel.Children.Add(screenshotBorder);
        mainPanel.Children.Add(dragDropSection);
        mainPanel.Children.Add(canvasBorder);

        Content = mainPanel;
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(_canvas);
        var props = e.GetCurrentPoint(_canvas).Properties;

        if (props.IsLeftButtonPressed)
        {
            DrawClickCircle(point.X, point.Y);
            _isDrawing = true;
            _lastDrawPoint = point;
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDrawing = false;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDrawing) return;

        var point = e.GetPosition(_canvas);
        var props = e.GetCurrentPoint(_canvas).Properties;

        if (props.IsLeftButtonPressed)
        {
            DrawLine(_lastDrawPoint.X, _lastDrawPoint.Y, point.X, point.Y);
            _lastDrawPoint = point;
        }
        else
        {
            _isDrawing = false;
        }
    }

    private void DrawClickCircle(double x, double y)
    {
        var circle = new Ellipse
        {
            Width = 30, // 15px radius = 30px diameter
            Height = 30,
            Fill = Brushes.Red,
            Opacity = 0.7
        };

        Canvas.SetLeft(circle, x - 15);
        Canvas.SetTop(circle, y - 15);

        _canvas.Children.Add(circle);
    }

    private void DrawLine(double x1, double y1, double x2, double y2)
    {
        var line = new Line
        {
            StartPoint = new Avalonia.Point(x1, y1),
            EndPoint = new Avalonia.Point(x2, y2),
            Stroke = Brushes.Blue,
            StrokeThickness = 3
        };

        _canvas.Children.Add(line);
    }

    private async void OnStartButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _startButton.IsEnabled = false;
        _startButton.Content = "Running...";

        _canvas.Children.Clear();
        _screenshotCanvas.Children.Clear();
        _inputField.Text = "";

        // Reset drag-and-drop section
        Canvas.SetLeft(_draggableItem, DraggableInitialLeft);
        Canvas.SetTop(_draggableItem, DraggableInitialTop);
        _draggableItem.Opacity = 1.0;
        _dropZone.Background = Brushes.AliceBlue;
        _dropZone.BorderBrush = Brushes.DarkSlateBlue;
        _dropStatusText.IsVisible = false;

        try
        {
            await RunDemo();
        }
        finally
        {
            _startButton.IsEnabled = true;
            _startButton.Content = "Start";
        }
    }

    private async Task RunDemo()
    {
        // Wait a moment for UI to update
        await Task.Delay(200);

        // Get the canvas bounds in screen coordinates
        var canvasBounds = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var topLeft = _canvas.PointToScreen(new Avalonia.Point(0, 0));
            // macOS CGEvent uses logical points - same as PointToScreen and Bounds on mac.
            // Windows SendInput uses physical pixels - need Bounds * RenderScaling.
            var boundsScale = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 1.0 : RenderScaling;
            return new
            {
                Left = topLeft.X,
                Top = topLeft.Y,
                Width = (int)(_canvas.Bounds.Width * boundsScale),
                Height = (int)(_canvas.Bounds.Height * boundsScale)
            };
        });

        // Calculate square vertices (with some padding from edges)
        var padding = 80;
        var squareLeft = canvasBounds.Left + padding;
        var squareTop = canvasBounds.Top + padding;
        var squareRight = canvasBounds.Left + canvasBounds.Width - padding;
        var squareBottom = canvasBounds.Top + canvasBounds.Height - padding;

        // Make it a proper square (use smaller dimension)
        var squareSize = Math.Min(squareRight - squareLeft, squareBottom - squareTop);
        squareRight = squareLeft + squareSize;
        squareBottom = squareTop + squareSize;

        var vertices = new[]
        {
            new Point(squareLeft, squareTop),           // Top-left
            new Point(squareRight, squareTop),          // Top-right
            new Point(squareRight, squareBottom),       // Bottom-right
            new Point(squareLeft, squareBottom)         // Bottom-left
        };

        // Step 1: Click at each vertex to mark them with circles
        foreach (var vertex in vertices)
        {
            _lazbot.Mouse.JumpTo(vertex);
            await Task.Delay(100);
            _lazbot.Mouse.Click();
            await Task.Delay(300);
        }

        // Step 2: Slowly drag from vertex to vertex to draw edges
        // Draw all 4 edges: 0->1, 1->2, 2->3, 3->0
        for (var i = 0; i < 4; i++)
        {
            var startVertex = vertices[i];
            var endVertex = vertices[(i + 1) % 4];

            await SlowDrag(startVertex, endVertex, 500);
            await Task.Delay(200);
        }

        // Step 2.5: Test mouse movement with different path+easing combinations
        // Calculate 4 equidistant points near the right edge of the canvas
        var rightEdgeX = canvasBounds.Left + canvasBounds.Width - 40;
        var verticalSpacing = canvasBounds.Height / 5;
        var rightEdgePoints = new[]
        {
            new Point(rightEdgeX, canvasBounds.Top + verticalSpacing),
            new Point(rightEdgeX, canvasBounds.Top + verticalSpacing * 2),
            new Point(rightEdgeX, canvasBounds.Top + verticalSpacing * 3),
            new Point(rightEdgeX, canvasBounds.Top + verticalSpacing * 4)
        };

        // Click on each right-edge point to mark them with circles
        foreach (var point in rightEdgePoints)
        {
            _lazbot.Mouse.JumpTo(point);
            await Task.Delay(100);
            _lazbot.Mouse.Click();
            await Task.Delay(300);
        }

        var movementTests = new[]
        {
            (Path: MouseMoveFunction.Linear, Easing: MoveEasingFunction.Linear, Duration: null),
            (Path: MouseMoveFunction.Linear, Easing: MoveEasingFunction.EaseOutQuartic, Duration: TimeSpan.FromSeconds(3)),
            (Path: MouseMoveFunction.Bezier, Easing: MoveEasingFunction.EaseInOutCubic, Duration: TimeSpan.FromSeconds(4)),
            (Path: MouseMoveFunction.Bezier, Easing: MoveEasingFunction.EaseOutQuartic, Duration: (TimeSpan?)TimeSpan.FromSeconds(5))
        };

        // Drag from each square vertex to corresponding right-edge point
        for (var i = 0; i < 4; i++)
        {
            var test = movementTests[i];
            var startPoint = vertices[i];
            var endPoint = rightEdgePoints[i];

            _lazbot.Mouse.JumpTo(startPoint);
            await Task.Delay(100);
            _lazbot.Mouse.Press();
            await Task.Delay(50);

            // Run MoveTo on background thread so UI can process pointer events
            await Task.Run(() =>
                _lazbot.Mouse.MoveTo(startPoint, endPoint, test.Duration, test.Path, test.Easing));

            _lazbot.Mouse.Release();
            await Task.Delay(300);
        }

        // Step 3: Click in the input field
        var inputFieldCenter = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var center = new Avalonia.Point(_inputField.Bounds.Width / 2, _inputField.Bounds.Height / 2);
            var screenPoint = _inputField.PointToScreen(center);
            return new Point(screenPoint.X, screenPoint.Y);
        });

        _lazbot.Mouse.JumpTo(inputFieldCenter);
        await Task.Delay(100);
        _lazbot.Mouse.Click();
        await Task.Delay(300);

        // Step 4: Type the text character by character with delays
        var keysToType = new[]
        {
            Key.A, Key.B, Key.C, Key.D, Key.E,
            Key.One, Key.Two, Key.Three, Key.Four, Key.Five
        };

        foreach (var key in keysToType)
        {
            _lazbot.Keyboard.Stroke(key);
            await Task.Delay(100); // Delay between keystrokes for visual effect
        }

        // Step 4b: Type special characters (platform-dependent)
        // Run on background thread so UI can process key events
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: no Alt-code or clipboard fallback - type BMP characters
            // reachable via the standard layout (punctuation, Shift symbols, etc.)
            await Task.Run(() => _lazbot.Keyboard.Type(" Hello, World! @#$%"));
        }
        else
        {
            // Windows: type Windows-1252 symbols using Alt codes
            await Task.Run(() => _lazbot.Keyboard.Type(" ©®™—", clipboardFallback: true));

            // macOS/Windows: type emojis using clipboard fallback
            await Task.Run(() => _lazbot.Keyboard.Type(" 🎉🚀✨", clipboardFallback: true));
        }

        // Step 5: Automate drag-and-drop
        var (dragFrom, dragTo) = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var from = _draggableItem.PointToScreen(new Avalonia.Point(
                _draggableItem.Bounds.Width / 2,
                _draggableItem.Bounds.Height / 2));
            var to = _dropZone.PointToScreen(new Avalonia.Point(
                _dropZone.Bounds.Width / 2,
                _dropZone.Bounds.Height / 2));
            return (
                new Point(from.X, from.Y),
                new Point(to.X, to.Y));
        });

        _lazbot.Mouse.JumpTo(dragFrom);
        await Task.Delay(200);
        await Task.Run(() => _lazbot.Mouse.DragAndDrop(dragFrom, dragTo));
        await Task.Delay(500);

        // Step 6: Capture a screenshot of the window and display it
        await Task.Delay(500); // Let the UI settle

        var windowBounds = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var topLeft = this.PointToScreen(new Avalonia.Point(0, 0));
            var boundsScale = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 1.0 : RenderScaling;
            return (
                topLeft.X,
                topLeft.Y,
                (int)(Bounds.Width * boundsScale),
                (int)(Bounds.Height * boundsScale));
        });

        (byte[] Data, int Width, int Height) screenshot = default;
        try
        {
            screenshot = _lazbot.Screen.Capture(new (windowBounds.X, windowBounds.Y), windowBounds.Item3, windowBounds.Item4);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var errorText = new TextBlock
                {
                    Text = $"Screenshot capture failed: {ex.Message}",
                    Foreground = Brushes.Red,
                    FontSize = 14,
                    Margin = new Thickness(10)
                };
                _screenshotCanvas.Children.Add(errorText);
            });
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var writeableBitmap = new WriteableBitmap(
                new PixelSize(screenshot.Width, screenshot.Height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using (var fb = writeableBitmap.Lock())
            {
                Marshal.Copy(screenshot.Data, 0, fb.Address, screenshot.Data.Length);
            }

            var image = new Image
            {
                Source = writeableBitmap,
                Stretch = Stretch.Uniform
            };

            // Size the image to fill the screenshot canvas
            _screenshotCanvas.Children.Add(image);

            // Bind image size to canvas size
            _screenshotCanvas.LayoutUpdated += (_, _) =>
            {
                image.Width = _screenshotCanvas.Bounds.Width;
                image.Height = _screenshotCanvas.Bounds.Height;
            };
            image.Width = _screenshotCanvas.Bounds.Width;
            image.Height = _screenshotCanvas.Bounds.Height;
        });
    }

    private Border CreateDragDropSection()
    {
        var border = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(2),
            Margin = new Thickness(10, 5, 10, 0),
            Background = Brushes.WhiteSmoke,
            Height = 150
        };

        _dragDropCanvas = new Canvas { Background = Brushes.WhiteSmoke };

        var title = new TextBlock
        {
            Text = "Drag & Drop",
            FontSize = 11,
            Foreground = Brushes.Gray,
            FontStyle = FontStyle.Italic
        };
        Canvas.SetLeft(title, 6);
        Canvas.SetTop(title, 4);
        _dragDropCanvas.Children.Add(title);

        // Drop zone (right side)
        _dropZone = new Border
        {
            Width = 120,
            Height = 80,
            BorderBrush = Brushes.DarkSlateBlue,
            BorderThickness = new Thickness(2),
            Background = Brushes.AliceBlue,
            CornerRadius = new CornerRadius(8)
        };
        _dropZone.Child = new TextBlock
        {
            Text = "Drop Here",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            Foreground = Brushes.DarkSlateBlue
        };
        Canvas.SetLeft(_dropZone, 570);
        Canvas.SetTop(_dropZone, 35);
        _dragDropCanvas.Children.Add(_dropZone);

        // Draggable item (left side)
        _draggableItem = new Border
        {
            Width = 80,
            Height = 60,
            Background = Brushes.Coral,
            CornerRadius = new CornerRadius(8),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        _draggableItem.Child = new TextBlock
        {
            Text = "Drag Me",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13,
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold
        };
        Canvas.SetLeft(_draggableItem, DraggableInitialLeft);
        Canvas.SetTop(_draggableItem, DraggableInitialTop);
        _dragDropCanvas.Children.Add(_draggableItem);

        // Status / feedback text (center)
        _dropStatusText = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            IsVisible = false
        };
        Canvas.SetLeft(_dropStatusText, 310);
        Canvas.SetTop(_dropStatusText, 60);
        _dragDropCanvas.Children.Add(_dropStatusText);

        // Reset button (bottom-center)
        var resetButton = new Button
        {
            Content = "Reset",
            Width = 70,
            Height = 26,
            FontSize = 12
        };
        Canvas.SetLeft(resetButton, 350);
        Canvas.SetTop(resetButton, 112);
        resetButton.Click += OnDragDropReset;
        _dragDropCanvas.Children.Add(resetButton);

        _draggableItem.PointerPressed += OnDraggablePointerPressed;
        _draggableItem.PointerMoved += OnDraggablePointerMoved;
        _draggableItem.PointerReleased += OnDraggablePointerReleased;

        border.Child = _dragDropCanvas;
        return border;
    }

    private void OnDraggablePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_draggableItem).Properties.IsLeftButtonPressed) return;

        _isDragging = true;
        _dragOffset = e.GetPosition(_draggableItem);
        e.Pointer.Capture(_draggableItem);
        _draggableItem.Opacity = 0.8;

        // Reset any previous drop indication
        _dropZone.Background = Brushes.AliceBlue;
        _dropZone.BorderBrush = Brushes.DarkSlateBlue;
        _dropStatusText.IsVisible = false;

        e.Handled = true;
    }

    private void OnDraggablePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;

        var pos = e.GetPosition(_dragDropCanvas);
        Canvas.SetLeft(_draggableItem, pos.X - _dragOffset.X);
        Canvas.SetTop(_draggableItem, pos.Y - _dragOffset.Y);

        // Highlight drop zone when hovering over it
        _dropZone.Background = IsOverDropZone() ? Brushes.LightSkyBlue : Brushes.AliceBlue;

        e.Handled = true;
    }

    private void OnDraggablePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;

        _isDragging = false;
        e.Pointer.Capture(null);
        _draggableItem.Opacity = 1.0;

        if (IsOverDropZone())
        {
            _dropZone.Background = Brushes.LightGreen;
            _dropZone.BorderBrush = Brushes.Green;
            _dropStatusText.Text = "✓ Dropped!";
            _dropStatusText.Foreground = Brushes.DarkGreen;
        }
        else
        {
            _dropZone.Background = Brushes.AliceBlue;
            _dropZone.BorderBrush = Brushes.DarkSlateBlue;
            _dropStatusText.Text = "✗ Missed";
            _dropStatusText.Foreground = Brushes.OrangeRed;
        }
        _dropStatusText.IsVisible = true;

        e.Handled = true;
    }

    private bool IsOverDropZone()
    {
        var itemCenterX = Canvas.GetLeft(_draggableItem) + _draggableItem.Width / 2;
        var itemCenterY = Canvas.GetTop(_draggableItem) + _draggableItem.Height / 2;

        var dropLeft = Canvas.GetLeft(_dropZone);
        var dropTop = Canvas.GetTop(_dropZone);

        return itemCenterX >= dropLeft && itemCenterX <= dropLeft + _dropZone.Width &&
               itemCenterY >= dropTop && itemCenterY <= dropTop + _dropZone.Height;
    }

    private void OnDragDropReset(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Canvas.SetLeft(_draggableItem, DraggableInitialLeft);
        Canvas.SetTop(_draggableItem, DraggableInitialTop);
        _draggableItem.Opacity = 1.0;
        _dropZone.Background = Brushes.AliceBlue;
        _dropZone.BorderBrush = Brushes.DarkSlateBlue;
        _dropStatusText.IsVisible = false;
    }

    private async Task SlowDrag(Point start, Point end, int durationMs)
    {
        const int steps = 30;
        var stepDelay = durationMs / steps;

        _lazbot.Mouse.JumpTo(start);
        await Task.Delay(50);

        _lazbot.Mouse.Press();
        await Task.Delay(50);

        for (var i = 1; i <= steps; i++)
        {
            var t = (double)i / steps;
            var x = (int)(start.X + (end.X - start.X) * t);
            var y = (int)(start.Y + (end.Y - start.Y) * t);

            _lazbot.Mouse.JumpTo(new Point(x, y));
            Mouse temp = _lazbot.Mouse;
            await Task.Delay(stepDelay);
        }

        _lazbot.Mouse.Release();
        await Task.Delay(50);
    }
}
