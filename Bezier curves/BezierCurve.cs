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
    public class BezierCurve
    {
        private List<int> P_ID;
        private List<Vector2d> P;
        public Color4 color;
        public System.Windows.Media.Color swmColor;
        private int n;

        public BezierCurve(IEnumerable<Vector2d> P, IEnumerable<int> id)
        {
            n = P.Count() - 1;
            this.P = P.ToList();
            this.P_ID = id.ToList();
            color = new Color4(0, 1.0f, 0, 1);
        }

        public BezierCurve()
        {
            n = 0;
            P = new List<Vector2d>();
            P_ID = new List<int>();
            color = new Color4();
        }

        private double Cnk(int n, int k)
        {
            double result = 1;
            for (int i = n - k + 1; i <= n; i++)
            {
                result *= i;
                result /= i - n + k;
            }
            return result;
        }

        public void Update(List<Point> points)
        {
            P = new List<Vector2d>();
            foreach (int id in P_ID)
            {
                try
                {
                    P.Add(points[id].coords);
                }
                catch
                {
                    continue;
                }
            }
            n = P.Count() - 1;
        }

        public Vector2d Calculate(double t)
        {
            if (n > 0)
            {
                Vector2d result = Vector2d.Zero;
                for (int i = n; i >= 0; i--)
                {
                    result += Cnk(n, n - i) * Pow(t, n - i) * Pow(1 - t, i) * P[n - i];
                }
                return result;
            }
            else
            {
                return new Vector2d(double.NaN, double.NaN);
            }
        }

        public override string ToString()
        {
            string outString = "{";
            foreach (int id in P_ID)
            {
                outString += $"{id};";
            }
            if (outString.Length == 1)
            {
                return "{}";
            }
            else
            {
                return outString.Remove(outString.Length - 1, 1) + '}';
            }
        }

        public void Draw(double T, int n)
        {
            double step = T / n;
            GL.Begin(PrimitiveType.LineStrip);
            GL.Color4(color);
            for (int i = 0; i < n + 1; i++)
            {
                GL.Vertex2(Calculate(i * step));
            }
            GL.End();
        }
        public void Draw(double T, int n, Color4 color)
        {
            double step = T / n;
            GL.Begin(PrimitiveType.LineStrip);
            GL.Color4(color);
            for (int i = 0; i < n + 1; i++)
            {
                GL.Vertex2(Calculate(i * step));
            }
            GL.End();
        }
    }
}
