using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;
using System.Windows.Media.Media3D;
using System.Windows.Forms;
namespace ChuanKouTest
{
   public  class MathCalculate
    {     
        /// <summary>
        /// 由四元数计算矩阵
        /// </summary>
        /// <param name="pose"></param>
        /// <param name="posi"></param>
        /// <returns></returns>
        public vtkMatrix4x4 GetMatrixFromQuaternionAndPosition(double[] pose, double[] posi)
        {
            vtkMatrix4x4 matrixtemp = new vtkMatrix4x4();

            double m00 = 1 - 2 * (pose[2] * pose[2] + pose[3] * pose[3]);
            double m01= 2 * pose[1] * pose[2] - 2 * pose[0] * pose[3];
            double m02 = 2 * pose[1] * pose[3] + 2 * pose[0] * pose[2];
            double m10 = 2 * pose[1] * pose[2] + 2 * pose[0] * pose[3];
            double m11 = 1 - 2 * (pose[1] * pose[1] + pose[3] * pose[3]);
            double m12 = 2 * pose[2] * pose[3] - 2 * pose[0] * pose[1];
            double m20 = 2 * pose[1] * pose[3] - 2 * pose[0] * pose[2];
            double m21 = 2 * pose[2] * pose[3] + 2 * pose[0] * pose[1];
            double m22 = 1 - 2*(pose[1] * pose[1] + pose[2] * pose[2]);


            matrixtemp.SetElement(0, 0, m00);
            matrixtemp.SetElement(0, 1, m01);
            matrixtemp.SetElement(0, 2, m02);
            matrixtemp.SetElement(0, 3, posi[0]);

            matrixtemp.SetElement(1, 0, m10);
            matrixtemp.SetElement(1, 1, m11);
            matrixtemp.SetElement(1, 2, m12);
            matrixtemp.SetElement(1, 3, posi[1]);

            matrixtemp.SetElement(2, 0, m20);
            matrixtemp.SetElement(2, 1, m21);
            matrixtemp.SetElement(2, 2, m22);
            matrixtemp.SetElement(2, 3, posi[2]);

            matrixtemp.SetElement(3, 0, 0);
            matrixtemp.SetElement(3, 1, 0);
            matrixtemp.SetElement(3, 2, 0);
            matrixtemp.SetElement(3, 3, 1);

            return matrixtemp;
        }

        /// <summary>
        /// 二维向量单位化
        /// </summary>
        /// <param name="v1"></param>
        /// <returns></returns>
        public void Vector2DNormalize(ref double[] v1)
        {
            double norm = Math.Sqrt(v1[0] * v1[0] + v1[1] * v1[1]);
            double[] v = new double[3];
            v[0] = v1[0] / norm;
            v[1] = v1[1] / norm;          
            
        }
        public double[] Vector3DNormalize(double[] v1)
        {
            double norm = Math.Sqrt(v1[0] * v1[0] + v1[1] * v1[1] + v1[2] * v1[2]);
            double[] v = new double[3];
            v[0] = v1[0] / norm;
            v[1] = v1[1] / norm;
            v[2] = v1[2] / norm;
            return v;
        }
        /// <summary>
        /// vtkMatrix转Matrix
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public Matrix vtkMatrixToMatrix(vtkMatrix4x4 matrix)
        {
            Matrix M = new Matrix(4, 4);
            double[,] a = M.Detail;
            a[0, 0] = matrix.GetElement(0, 0); a[0, 1] = matrix.GetElement(0, 1); a[0, 2] = matrix.GetElement(0, 2); a[0, 3] = matrix.GetElement(0, 3);
            a[1, 0] = matrix.GetElement(1, 0); a[1, 1] = matrix.GetElement(1, 1); a[1, 2] = matrix.GetElement(1, 2); a[1, 3] = matrix.GetElement(1, 3);
            a[2, 0] = matrix.GetElement(2, 0); a[2, 1] = matrix.GetElement(2, 1); a[2, 2] = matrix.GetElement(2, 2); a[2, 3] = matrix.GetElement(2, 3);
            a[3, 0] = matrix.GetElement(3, 0); a[3, 1] = matrix.GetElement(3, 1); a[3, 2] = matrix.GetElement(3, 2); a[3, 3] = matrix.GetElement(3, 3);

            return M;
        }

        public void PointTtrans(vtkMatrix4x4 transmatrix, ref double[] point)
        {
            vtkMatrix4x4 inimatrix = vtkMatrix4x4.New();
            inimatrix.SetElement(0, 3, point[0]);
            inimatrix.SetElement(1, 3, point[1]);
            inimatrix.SetElement(2, 3, point[2]);
            vtkMatrix4x4.Multiply4x4(transmatrix, inimatrix, inimatrix);
            point[0] = inimatrix.GetElement(0, 3);
            point[1] = inimatrix.GetElement(1, 3);
            point[2] = inimatrix.GetElement(2, 3);

        }
        /// <summary>
        /// 向量叉乘
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public double[] CrossProduct(double[] v1, double[] v2)
        {
            double[] cp = new double[3];
            cp[0] = v1[1] * v2[2] - v1[2] * v2[1];
            cp[1] = v1[2] * v2[0] - v1[0] * v2[2];
            cp[2] = v1[0] * v2[1] - v1[1] * v2[0];
            return cp;
        }

        /// <summary>
        /// 三点建立坐标系
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public vtkMatrix4x4 BuildCoordinateSystem(double[] a, double[] b, double[] c)
        {
            //改了Y的方向
            double[] Xaxis = new double[3];
            double[] Yaxis = new double[3];
            double[] Zaxis = new double[3];
            double[] V = new double[3];

            Xaxis[0] = b[0] - a[0];
            Xaxis[1] = b[1] - a[1];
            Xaxis[2] = b[2] - a[2];
            Xaxis = Vector3DNormalize(Xaxis);
            V[0] = c[0] - a[0];
            V[1] = c[1] - a[1];
            V[2] = c[2] - a[2];
            V = Vector3DNormalize(V);

            Zaxis = CrossProduct(V, Xaxis);
            Zaxis = Vector3DNormalize(Zaxis);
            Yaxis = CrossProduct(Zaxis, Xaxis);
            Yaxis = Vector3DNormalize(Yaxis);

            vtkMatrix4x4 M = new vtkMatrix4x4();

            M.SetElement(0, 0, Xaxis[0]);
            M.SetElement(1, 0, Xaxis[1]);
            M.SetElement(2, 0, Xaxis[2]);
            M.SetElement(3, 0, 0);

            M.SetElement(0, 1, Yaxis[0]);
            M.SetElement(1, 1, Yaxis[1]);
            M.SetElement(2, 1, Yaxis[2]);
            M.SetElement(3, 1, 0);

            M.SetElement(0, 2, Zaxis[0]);
            M.SetElement(1, 2, Zaxis[1]);
            M.SetElement(2, 2, Zaxis[2]);
            M.SetElement(3, 2, 0);

            M.SetElement(0, 3, a[0]);
            M.SetElement(1, 3, a[1]);
            M.SetElement(2, 3, a[2]);
            M.SetElement(3, 3, 1);

            return M;
        }






    }
}
