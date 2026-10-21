using System;
using System.Windows;

namespace CGLab4.Models
{
    /// <summary>
    /// Матрица 3x3 для аффинных преобразований на однородных координатах.
    /// TODO (Участник 2): Добавить сюда статические методы для создания матриц поворота и масштабирования.
    /// </summary>
    public struct Matrix3x3
    {
        public double M11, M12, M13;
        public double M21, M22, M23;
        public double M31, M32, M33;

        public Matrix3x3(double m11, double m12, double m13,
                         double m21, double m22, double m23,
                         double m31, double m32, double m33)
        {
            M11 = m11; M12 = m12; M13 = m13;
            M21 = m21; M22 = m22; M23 = m23;
            M31 = m31; M32 = m32; M33 = m33;
        }

        public static Matrix3x3 Identity => new Matrix3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);

        public static Matrix3x3 Multiply(Matrix3x3 a, Matrix3x3 b)
        {
            return new Matrix3x3(
                a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
                a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
                a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,
                a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
                a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
                a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,
                a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
                a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
                a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33
            );
        }


        public Point Transform(Point p)
        {
            double x = M11 * p.X + M12 * p.Y + M13;
            double y = M21 * p.X + M22 * p.Y + M23;
            double w = M31 * p.X + M32 * p.Y + M33;

            return new Point(x / w, y / w);
        }

        public static Matrix3x3 CreateRotation(double angleDegrees)
        {
            double angleRadians = angleDegrees * Math.PI / 180.0;

            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);

            return new Matrix3x3(
                cos, -sin, 0,
                sin, cos, 0,
                0, 0, 1
            );
        }

        public static Matrix3x3 CreateScale(double scaleX, double scaleY)
        {
            return new Matrix3x3(
                scaleX, 0, 0,
                0, scaleY, 0,
                0, 0, 1
            );
        }

    }
}