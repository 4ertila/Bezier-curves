using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using WpfMath;
using WpfMath.Controls;
using Xceed.Wpf.Toolkit;
using static System.Math;

namespace BezierCurves
{
    public partial class MainWindow : Window
    {
        double left, right, bottom, top;
        List<Point> points;
        int capturedPointID;
        bool isPointCreated;
        bool isPointCaptured;
        Vector2d lastMouseDownPoint;
        List<BezierCurve> bezierCurves;
        GLControl glControl;
        Color4 backgroundColor;
        Color4 allPointsColor;
        Color4 allCurvesColor;
        double r;
        float lineWidth = 4.0f;
        float pointSize = 12.0f;
        int N = 30;
        Bitmap image;

        public Bitmap MakeImage()
        {
            Bitmap bmp = new Bitmap(glControl.Width, glControl.Height);
            System.Drawing.Imaging.BitmapData data =
                bmp.LockBits(glControl.ClientRectangle, System.Drawing.Imaging.ImageLockMode.WriteOnly,
                             System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.ReadPixels(0, 0, glControl.Width, glControl.Height, PixelFormat.Bgra, PixelType.UnsignedByte,
                     data.Scan0);
            bmp.UnlockBits(data);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bmp;
        }

        public MainWindow()
        {
            bezierCurves = new List<BezierCurve>();
            points = new List<Point>();
            left = bottom = -1;
            right = top = 1;
            r = 0.02;
            glControl = new GLControl(new GraphicsMode(32, 24, 0, 8))
            {
                Name = "glControl",
            };
            glControl.Load += glControl_Load;
            glControl.Paint += glControl_Paint;
            glControl.MouseDown += glControl_MouseDown;
            glControl.MouseMove += glControl_MouseMove;
            glControl.MouseWheel += glControl_MouseWheel;
            glControl.MouseLeave += glControl_MouseLeave;
            InitializeComponent();
            glControl.Width = (int)WindowsFormsHost_GLControlContainer.Width;
            glControl.Height = (int)WindowsFormsHost_GLControlContainer.Height;
            WindowsFormsHost_GLControlContainer.Child = glControl;
        }

        private ValueTuple<System.Windows.Media.Color, Color4> RandomColor()
        {
            Random random = new Random();
            System.Windows.Media.Color swmRandomColor = new System.Windows.Media.Color();
            Color4 opentkRandomColor = new Color4();

            swmRandomColor.R = (byte)random.Next(0, 255);
            swmRandomColor.G = (byte)random.Next(0, 255);
            swmRandomColor.B = (byte)random.Next(0, 255);
            swmRandomColor.A = (byte)random.Next(155, 255);

            opentkRandomColor.R = (float)swmRandomColor.R / 255;
            opentkRandomColor.G = (float)swmRandomColor.G / 255;
            opentkRandomColor.B = (float)swmRandomColor.B / 255;
            opentkRandomColor.A = (float)swmRandomColor.A / 255;

            return (swmRandomColor, opentkRandomColor);
        }

        private ValueTuple<System.Windows.Media.Color, Color4> ParseColor(string str)
        {

            try
            {
                string[] values = str.Substring(1, str.Length - 2).Split(';');
                System.Windows.Media.Color swmColor = new System.Windows.Media.Color();
                Color4 opentkRandomColor = new Color4();
                swmColor.R = byte.Parse(values[0]);
                swmColor.G = byte.Parse(values[1]);
                swmColor.B = byte.Parse(values[2]);
                swmColor.A = byte.Parse(values[3]);
                opentkRandomColor.R = (float)swmColor.R / 255;
                opentkRandomColor.G = (float)swmColor.G / 255;
                opentkRandomColor.B = (float)swmColor.B / 255;
                opentkRandomColor.A = (float)swmColor.A / 255;

                return (swmColor, opentkRandomColor);
            }
            catch
            {
                throw new ArgumentException("Incorrect string format");
            }
        }

        private Point ParsePoint(string str)
        {
            if (str.Length > 3 && str[0] == '(' && str[str.Length - 1] == ')')
            {
                try
                {
                    string[] values = str.Substring(1, str.Length - 2).Split(';');
                    Vector2d vector = new Vector2d();
                    vector[0] = double.Parse(values[0]);
                    vector[1] = double.Parse(values[1]);
                    return new Point(vector);
                }
                catch
                {
                    throw new ArgumentException("Incorrect string format");
                }
            }
            else
            {
                throw new ArgumentException("Incorrect string format");
            }
        }

        private BezierCurve ParseBezierCurve(string str)
        {
            if(str.Length > 3 && str[0] == '{' && str[str.Length - 1] == '}')
            {
                string[] ids = str.Substring(1, str.Length - 2).Split(';');
                List<Vector2d> bezierCurvePoints = new List<Vector2d>();
                List<int> bezierCurvePointsID = new List<int>();
                for (int i = 0; i < ids.Length; i++)
                {
                    try
                    {
                        bezierCurvePointsID.Add(int.Parse(ids[i]));
                        try
                        {
                            bezierCurvePoints.Add(points[int.Parse(ids[i])].coords);
                        }
                        catch
                        {
                            throw new ArgumentException("Incorrect string format");
                        }
                    }
                    catch
                    {
                        throw new ArgumentException("Incorrect string format");
                    }
                }
                return new BezierCurve(bezierCurvePoints, bezierCurvePointsID);
            }
            else
            {
                throw new ArgumentException("Incorrect string format");
            }
        }

        private void AddPointToListBox(Point point)
        {
            FormulaControl pointName = new FormulaControl()
            {
                Formula = $"P_{{{points.Count() - 1}}}:",
                Style = this.FindResource("Style_PointName") as Style
            };

            TextBox pointRespresentation = new TextBox()
            {
                Name = $"TextBox_Point{points.Count() - 1}",
                Text = point.ToString(),
                Style = this.FindResource("Style_PointRepresent") as Style
            };
            pointRespresentation.TextChanged += (sender, e) =>
            {
                try
                {
                    int pointID = int.Parse(pointRespresentation.Name.Substring(13));
                    Point parsedPoint = ParsePoint(pointRespresentation.Text);
                    if (parsedPoint.coords.X > right - r)
                    {
                        parsedPoint.coords.X = right - r;
                    }
                    else if (parsedPoint.coords.X < left + r)
                    {
                        parsedPoint.coords.X = left + r;
                    }
                    if (parsedPoint.coords.Y > top - r)
                    {
                        parsedPoint.coords.Y = top - r;
                    }
                    else if (parsedPoint.coords.Y < bottom + r)
                    {
                        parsedPoint.coords.Y = bottom + r;
                    }
                    parsedPoint.color = points[pointID].color;
                    parsedPoint.swmColor = points[pointID].swmColor;
                    points[pointID] = parsedPoint;
                    glControl.Invalidate();
                }
                catch { }
            };

            StackPanel pointStackPanel = new StackPanel()
            {
                Style = this.FindResource("Style_StackPanel") as Style
            };

            Button buttonRemovePoint = new Button()
            {
                Name = $"Button_RemovePoint{points.Count - 1}",
                Width = 30,
                Margin = new Thickness(5, 0, 0, 0),
                BorderThickness = new Thickness(0),
                Content = "X",
                FontSize = 25,
                Background = System.Windows.Media.Brushes.IndianRed
            };
            buttonRemovePoint.Click += (sender, e) =>
            {
                int pointID = int.Parse(buttonRemovePoint.Name.Substring(18));
                points.RemoveAt(pointID);
                ListBox_Points.Items.RemoveAt(pointID);
                for(int i = pointID; i < points.Count; i++)
                {
                    var stackPanelChildren = ((ListBox_Points.Items[i] as ListBoxItem).Content as StackPanel).Children;
                    (stackPanelChildren[0] as FormulaControl).Formula = $"P_{{{i}}}:";
                    (stackPanelChildren[1] as TextBox).Name = $"TextBox_Point{i}";
                    (stackPanelChildren[2] as Button).Name = $"Button_RemovePoint{i}";
                }
                UpdateBezierCurves();
                glControl.Invalidate();
            };

            ColorPicker pointColorPicker = new ColorPicker()
            {
                Name = $"ColorPicker_Point{points.Count - 1}",
                Width = 60,
                Height = 30,
                SelectedColor = point.swmColor,
                Margin = new Thickness(5, 0, 0, 0),
                ColorMode = ColorMode.ColorCanvas
            };
            pointColorPicker.SelectedColorChanged += (sender, e) =>
            {
                int pointID = int.Parse(pointColorPicker.Name.Substring(17));
                var selectedColor = pointColorPicker.SelectedColor.Value;
                points[pointID].color = new Color4((float)selectedColor.R / 255,
                                                   (float)selectedColor.G / 255,
                                                   (float)selectedColor.B / 255,
                                                   (float)selectedColor.A / 255);
                points[pointID].swmColor = selectedColor;
                glControl.Invalidate();
            };

            pointStackPanel.Children.Add(pointName);
            pointStackPanel.Children.Add(pointRespresentation);
            pointStackPanel.Children.Add(buttonRemovePoint);
            pointStackPanel.Children.Add(pointColorPicker);
            ListBoxItem item = new ListBoxItem();
            item.Content = pointStackPanel;
            ListBox_Points.Items.Add(item);
        }

        private void AddCurveToListBox(BezierCurve curve)
        {
            FormulaControl curveName = new FormulaControl()
            {
                Formula = $"B_{{{bezierCurves.Count - 1}}}:",
                Style = this.FindResource("Style_PointName") as Style
            };

            TextBox curveRespresentation = new TextBox()
            {
                Name = $"TextBox_Curve{bezierCurves.Count - 1}",
                Style = this.FindResource("Style_PointRepresent") as Style,
                Text = curve.ToString()
            };
            curveRespresentation.TextChanged += (sender, e) =>
            {
                int curveID = int.Parse(curveRespresentation.Name.Substring(13));
                try
                {
                    BezierCurve parsedCurve = ParseBezierCurve(curveRespresentation.Text);
                    parsedCurve.color = bezierCurves[curveID].color;
                    parsedCurve.swmColor = bezierCurves[curveID].swmColor;

                    bezierCurves[curveID] = parsedCurve;
                }
                catch
                {
                    bezierCurves[curveID].Update(new List<Point>());
                }
                glControl.Invalidate();
            };

            StackPanel curveStackPanel = new StackPanel()
            {
                Style = this.FindResource("Style_StackPanel") as Style
            };

            Button buttonRemoveCurve = new Button()
            {
                Name = $"Button_RemoveCurve{bezierCurves.Count - 1}",
                Width = 30,
                Margin = new Thickness(5, 0, 0, 0),
                BorderThickness = new Thickness(0),
                Content = "X",
                FontSize = 25,
                Background = System.Windows.Media.Brushes.IndianRed
            };
            buttonRemoveCurve.Click += (sender, e) =>
            {
                int curveID = int.Parse(buttonRemoveCurve.Name.Substring(18));
                bezierCurves.RemoveAt(curveID);
                ListBox_BezierCurves.Items.RemoveAt(curveID + 1);
                for (int i = curveID; i < bezierCurves.Count; i++)
                {
                    var stackPanelChildren = ((ListBox_BezierCurves.Items[i + 1] as ListBoxItem).Content as StackPanel).Children;
                    (stackPanelChildren[0] as FormulaControl).Formula = $"B_{{{i}}}:";
                    (stackPanelChildren[1] as TextBox).Name = $"TextBox_Curve{i}";
                    (stackPanelChildren[2] as Button).Name = $"Button_RemoveCurve{i}";
                }
                glControl.Invalidate();
            };

            ColorPicker curveColorPicker = new ColorPicker()
            {
                Name = $"ColorPicker_Curve{bezierCurves.Count - 1}",
                Width = 60,
                Height = 30,
                SelectedColor = curve.swmColor,
                Margin = new Thickness(5, 0, 0, 0),
                ColorMode = ColorMode.ColorCanvas
            };
            curveColorPicker.SelectedColorChanged += (sender, e) =>
            {
                int curveID = int.Parse(curveColorPicker.Name.Substring(17));
                var selectedColor = curveColorPicker.SelectedColor.Value;
                bezierCurves[curveID].color = new Color4((float)selectedColor.R / 255,
                                                         (float)selectedColor.G / 255,
                                                         (float)selectedColor.B / 255,
                                                         (float)selectedColor.A / 255);
                bezierCurves[curveID].swmColor = selectedColor;
                glControl.Invalidate();
            };

            curveStackPanel.Children.Add(curveName);
            curveStackPanel.Children.Add(curveRespresentation);
            curveStackPanel.Children.Add(buttonRemoveCurve);
            curveStackPanel.Children.Add(curveColorPicker);
            ListBoxItem item = new ListBoxItem();
            item.Content = curveStackPanel;
            ListBox_BezierCurves.Items.Add(item);
        }

        private void UpdateBezierCurves()
        {
            foreach(BezierCurve curve in bezierCurves)
            {
                /*if (curve != null)
                {*/
                    curve.Update(points);
                //}
            }
        }


        private void Button_AddBezierCurve_Click(object sender, RoutedEventArgs e)
        {
            BezierCurve newBezierCurve = new BezierCurve();
            var randomColor = RandomColor();
            newBezierCurve.color = randomColor.Item2;
            newBezierCurve.swmColor = randomColor.Item1;
            bezierCurves.Add(newBezierCurve);
            AddCurveToListBox(newBezierCurve);
        }

        private void Slider_N_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Label_N.Content = Slider_N.Value.ToString();
            N = (int)Slider_N.Value;
            glControl.Invalidate();
        }

        private void Slider_LineWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Label_LineWidth.Content = Slider_LineWidth.Value.ToString();
            lineWidth = (float)Slider_LineWidth.Value;
            glControl.Invalidate();
        }

        private void Slider_PointSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Label_PointSize.Content = Slider_PointSize.Value.ToString();
            pointSize = (float)Slider_PointSize.Value;
            glControl.Invalidate();
        }


        private void Button_SaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog saveImageDialog = new Microsoft.Win32.SaveFileDialog();
                saveImageDialog.ShowDialog();
                if (saveImageDialog.FileName != "")
                {
                    image = MakeImage();
                    image.Save(saveImageDialog.FileName);

                    string path = saveImageDialog.FileName;
                    int i = path.Length - 1;
                    while (path[i] != '.')
                    {
                        i--;
                    }

                    StreamWriter txtPoint = File.CreateText(path.Remove(i + 1, path.Length - i - 1) + "txt");
                    txtPoint.WriteLine("Points");
                    foreach (Point point in points)
                    {
                        txtPoint.WriteLine(point.ToString() + "|" + $"({point.swmColor.R};{point.swmColor.G};{point.swmColor.B};{point.swmColor.A})");
                    }
                    txtPoint.WriteLine("Bezier curves");
                    foreach (BezierCurve curve in bezierCurves)
                    {
                        txtPoint.WriteLine(curve.ToString() + "|" + $"({curve.swmColor.R};{curve.swmColor.G};{curve.swmColor.B};{curve.swmColor.A})");
                    }
                    txtPoint.Close();
                }
            }
            catch
            {
                Xceed.Wpf.Toolkit.MessageBox.Show("Failed to create image");
            }
        }
        private void Button_LoadImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog loadImageDialog = new Microsoft.Win32.OpenFileDialog();
            loadImageDialog.ShowDialog();
            if(loadImageDialog.FileName != "")
            {
                string[] textLines = new string[] { };
                try
                { 
                    textLines = File.ReadAllLines(loadImageDialog.FileName);
                }
                catch
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show("Failed to read file");
                }
                BezierCurve curve;
                List<BezierCurve> parsedCurves = new List<BezierCurve>();
                Point point;                    
                List<Point> parsedPoints = new List<Point>();

                points.Clear();
                bezierCurves.Clear();
                ListBox_Points.Items.Clear();
                var buttonAddCurve = ListBox_BezierCurves.Items[0];
                ListBox_BezierCurves.Items.Clear();
                ListBox_BezierCurves.Items.Add(buttonAddCurve);

                int i = 1;

                while (textLines[i] != "Bezier curves")
                {
                    try
                    { 
                        string coords = textLines[i].Split('|')[0];
                        string color = textLines[i].Split('|')[1];
                        point = ParsePoint(coords);
                        var pointColor = ParseColor(color);
                        point.color = pointColor.Item2;
                        point.swmColor = pointColor.Item1;
                        points.Add(point);
                        AddPointToListBox(point);
                    }
                    catch
                    {
                        Xceed.Wpf.Toolkit.MessageBox.Show($"Failed to parse point at line: {i}");
                    }
                    i++;
                }

                i++;
                while (i < textLines.Length)
                {
                    try
                    {
                        string curvePoints = textLines[i].Split('|')[0];
                        string color = textLines[i].Split('|')[1];
                        curve = ParseBezierCurve(curvePoints);
                        var curveColor = ParseColor(color);
                        curve.color = curveColor.Item2;
                        curve.swmColor = curveColor.Item1;
                        bezierCurves.Add(curve);
                        AddCurveToListBox(curve);
                    }
                    catch
                    {
                        Xceed.Wpf.Toolkit.MessageBox.Show($"Failed to parse curve at line: {i}");
                    }
                    i++;
                }

                glControl.Invalidate();
            }
        }


        private void ColorPicker_BackgroundColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            var selectedColor = e.NewValue.Value;
            backgroundColor = new Color4((float)selectedColor.R / 255,
                                         (float)selectedColor.G / 255,
                                         (float)selectedColor.B / 255,
                                         (float)selectedColor.A / 255);
            glControl.Invalidate();
        }
        private void ColorPicker_ColorAllPoints_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            var selectedColor = e.NewValue.Value;
            allPointsColor = new Color4((float)selectedColor.R / 255,
                                         (float)selectedColor.G / 255,
                                         (float)selectedColor.B / 255,
                                         (float)selectedColor.A / 255);
            if(CheckBox_ColorAllPoints.IsChecked is true)
            {
                glControl.Invalidate();
            }
        }
        private void ColorPicker_ColorAllCurves_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            var selectedColor = e.NewValue.Value;
            allCurvesColor = new Color4((float)selectedColor.R / 255,
                                         (float)selectedColor.G / 255,
                                         (float)selectedColor.B / 255,
                                         (float)selectedColor.A / 255);
            if(CheckBox_ColorAllCurves.IsChecked is true)
            {
                glControl.Invalidate();
            }
        }


        private void CheckBox_ColorAllPoints_Click(object sender, RoutedEventArgs e)
        {
            glControl.Invalidate();
        }
        private void CheckBox_ColorAllCurves_Click(object sender, RoutedEventArgs e)
        {
            glControl.Invalidate();
        }

        private void glControl_MouseLeave(object sender, EventArgs e)
        {
            isPointCreated = false;
            isPointCaptured = false;
        }
        private void glControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left && (isPointCreated || isPointCaptured))
            {
                Vector2d cursorPos = new Vector2d((right - left) / glControl.Width * (e.X - glControl.Width / 2),
                        (top - bottom) / glControl.Height * (-e.Y + glControl.Height / 2));
                if (cursorPos.X > right)
                {
                    cursorPos.X = right;
                }
                else if (cursorPos.X < left)
                {
                    cursorPos.X = left;
                }

                if (cursorPos.Y > top)
                {
                    cursorPos.Y = top;
                }
                else if (cursorPos.Y < bottom)
                {
                    cursorPos.Y = bottom;
                }

                points[capturedPointID] = new Point(cursorPos, points[capturedPointID].color, points[capturedPointID].swmColor);
                (((ListBox_Points.Items[capturedPointID] as ListBoxItem).Content as StackPanel)
                                                                        .Children[1] as TextBox)
                                                                        .Text = cursorPos.ToString();
                UpdateBezierCurves();
                glControl.Invalidate();
            }
        }
        private void glControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                left -= 0.1;
                right -= 0.1;
                bottom -= 0.1;
                top -= 0.1;
                r /= 2;
            }
            glControl.Invalidate();
        }
        private void glControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if(e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                isPointCreated = true;
                isPointCaptured = false;
                lastMouseDownPoint = new Vector2d((right - left) / glControl.Width * (e.X - glControl.Width / 2),
                        (top - bottom) / glControl.Height * (-e.Y + glControl.Height / 2));

                for(int i = 0; i < points.Count(); i++)
                {
                    if ((points[i].coords - lastMouseDownPoint).Length <= r)
                    {
                        capturedPointID = i;
                        isPointCreated = false;
                        isPointCaptured = true;
                        break;
                    }
                }

                if (isPointCreated)
                {
                    capturedPointID = points.Count;
                    Point newPoint = new Point(lastMouseDownPoint, RandomColor());
                    points.Add(newPoint);
                    AddPointToListBox(newPoint);
                }
                UpdateBezierCurves();
                glControl.Invalidate();
            }
            else if(e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                lastMouseDownPoint = new Vector2d((right - left) / glControl.Width * (e.X - glControl.Width / 2),
                        (top - bottom) / glControl.Height * (-e.Y + glControl.Height / 2));

                for (int i = 0; i < points.Count(); i++)
                {
                    if ((points[i].coords - lastMouseDownPoint).Length <= r)
                    {
                        ButtonAutomationPeer peer = new ButtonAutomationPeer((((ListBox_Points.Items[i] as ListBoxItem).Content as StackPanel)
                                                                  .Children[2] as Button));
                        IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                        invokeProv.Invoke();
                        UpdateBezierCurves();
                        break;
                    }
                }
                glControl.Invalidate();
            }
        }
        private void glControl_Load(object sender, EventArgs e)
        {
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.CullFace);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            GL.Ortho(left, right, bottom, top, 0, 1);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
        }
        private void glControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            GL.ClearColor(backgroundColor);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Ortho(left, right, bottom, top, 0, 1);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.LineWidth(lineWidth);
            GL.PointSize(pointSize);
            if (CheckBox_ColorAllPoints.IsChecked is true)
            {
                foreach (Point point in points)
                {
                    point.Draw(allPointsColor);
                }
            }
            else
            {
                foreach (Point point in points)
                {
                    point.Draw();
                }
            }
            if (CheckBox_ColorAllCurves.IsChecked is true)
            {
                foreach (BezierCurve curve in bezierCurves)
                {
                    curve.Draw(1, N, allCurvesColor);
                }
            }
            else
            {
                foreach (BezierCurve curve in bezierCurves)
                {
                    curve.Draw(1, N);
                }
            }
            glControl.SwapBuffers();
        }
    }
}
