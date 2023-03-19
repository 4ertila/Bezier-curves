using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using static System.Math;

namespace BezierCurves
{
    public class Point
    {
        public Vector2d coords;
        public Color4 color;
        public System.Windows.Media.Color swmColor;

        public Point(Vector2d coords, (System.Windows.Media.Color, Color4) color)
        {
            this.coords = coords;
            this.color = color.Item2;
            this.swmColor = color.Item1;
        }

        public Point(Vector2d coords, Color4 color, System.Windows.Media.Color swmColor)
        {
            this.coords = coords;
            this.color = color;
            this.swmColor = swmColor;
        }

        public Point(Vector2d coords, Color4 color)
        {
            this.coords = coords;
            this.color = color;
        }

        public Point(Vector2d coords)
        {
            this.coords = coords;
            color = Color4.Red;
        }

        public Point()
        {
            this.coords = Vector2d.Zero;
            this.color = Color4.Red;
        }

        public override string ToString()
        {
            return $"({coords[0]};{coords[1]})";
        }

        public void Draw()
        {
            GL.Begin(PrimitiveType.Points);
            GL.Color4(color);
            GL.Vertex2(coords);
            GL.End();
        }

        public void Draw(Color4 color)
        {
            GL.Begin(PrimitiveType.Points);
            GL.Color4(color);
            GL.Vertex2(coords);
            GL.End();
        }
    }
}
