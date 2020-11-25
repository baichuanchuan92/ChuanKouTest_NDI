using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;


namespace ChuanKouTest
{
    
    class Calibration
    {
        MathCalculate mathcal = new MathCalculate();

        /// <summary>
        /// 计算变换矩阵
        /// </summary>
        /// <param name="Coordinate"></param>标志点空间坐标
        /// <param name="imagea"></param>标志点在图像上的坐标
        /// <returns></returns>
        public Matrix CalculateMatrix(double[,] Coordinate,double[,] imagea)
        {

            //点空间坐标  
            double ax0 = Coordinate[0, 0]; double ay0 = Coordinate[0, 1]; double az0 = Coordinate[0, 2];
            double ax1 = Coordinate[1, 0]; double ay1 = Coordinate[1, 1]; double az1 = Coordinate[1, 2];
            double ax2 = Coordinate[2, 0]; double ay2 = Coordinate[2, 1]; double az2 = Coordinate[2, 2];
            double ax3 = Coordinate[3, 0]; double ay3 = Coordinate[3, 1]; double az3 = Coordinate[3, 2];
            double ax4 = Coordinate[4, 0]; double ay4 = Coordinate[4, 1]; double az4 = Coordinate[4, 2];
            double ax5 = Coordinate[5, 0]; double ay5 = Coordinate[5, 1]; double az5 = Coordinate[5, 2];
            double ax6 = Coordinate[6, 0]; double ay6 = Coordinate[6, 1]; double az6 = Coordinate[6, 2];
            double ax7 = Coordinate[7, 0]; double ay7 = Coordinate[7, 1]; double az7 = Coordinate[7, 2];

            Matrix matrixA = new Matrix(16, 11);            

            double[,] a = matrixA.Detail;
            a[0, 0] = ax0; a[0, 1] = ay0; a[0, 2] = az0; a[0, 3] = 1; a[0, 4] = 0; a[0, 5] = 0; a[0, 6] = 0; a[0, 7] = 0;
            a[0, 8] = -imagea[0, 0] * ax0; a[0, 9] = -imagea[0, 0] * ay0; a[0, 10] = -imagea[0, 0] * az0;
            a[1, 0] = 0; a[1, 1] = 0; a[1, 2] = 0; a[1, 3] = 0; a[1, 4] = ax0; a[1, 5] = ay0; a[1, 6] = az0; a[1, 7] = 1;
            a[1, 8] = -imagea[0, 1] * ax0; a[1, 9] = -imagea[0, 1] * ay0; a[1, 10] = -imagea[0, 1] * az0;

            a[2, 0] = ax1; a[2, 1] = ay1; a[2, 2] = az1; a[2, 3] = 1; a[2, 4] = 0; a[2, 5] = 0; a[2, 6] = 0; a[2, 7] = 0;
            a[2, 8] = -imagea[1, 0] * ax1; a[2, 9] = -imagea[1, 0] * ay1; a[2, 10] = -imagea[1, 0] * az1;
            a[3, 0] = 0; a[3, 1] = 0; a[3, 2] = 0; a[3, 3] = 0; a[3, 4] = ax1; a[3, 5] = ay1; a[3, 6] = az1; a[3, 7] = 1;
            a[3, 8] = -imagea[1, 1] * ax1; a[3, 9] = -imagea[1, 1] * ay1; a[3, 10] = -imagea[1, 1] * az1;

            a[4, 0] = ax2; a[4, 1] = ay2; a[4, 2] = az2; a[4, 3] = 1; a[4, 4] = 0; a[4, 5] = 0; a[4, 6] = 0; a[4, 7] = 0;
            a[4, 8] = -imagea[2, 0] * ax2; a[4, 9] = -imagea[2, 0] * ay2; a[4, 10] = -imagea[2, 0] * az2;
            a[5, 0] = 0; a[5, 1] = 0; a[5, 2] = 0; a[5, 3] = 0; a[5, 4] = ax2; a[5, 5] = ay2; a[5, 6] = az2; a[5, 7] = 1;
            a[5, 8] = -imagea[2, 1] * ax2; a[5, 9] = -imagea[2, 1] * ay2; a[5, 10] = -imagea[2, 1] * az2;

            a[6, 0] = ax3; a[6, 1] = ay3; a[6, 2] = az3; a[6, 3] = 1; a[6, 4] = 0; a[6, 5] = 0; a[6, 6] = 0; a[6, 7] = 0;
            a[6, 8] = -imagea[3, 0] * ax3; a[6, 9] = -imagea[3, 0] * ay3; a[6, 10] = -imagea[3, 0] * az3;
            a[7, 0] = 0; a[7, 1] = 0; a[7, 2] = 0; a[7, 3] = 0; a[7, 4] = ax3; a[7, 5] = ay3; a[7, 6] = az3; a[7, 7] = 1;
            a[7, 8] = -imagea[3, 1] * ax3; a[7, 9] = -imagea[3, 1] * ay3; a[7, 10] = -imagea[3, 1] * az3;

            a[8, 0] = ax4; a[8, 1] = ay4; a[8, 2] = az4; a[8, 3] = 1; a[8, 4] = 0; a[8, 5] = 0; a[8, 6] = 0; a[8, 7] = 0;
            a[8, 8] = -imagea[4, 0] * ax4; a[8, 9] = -imagea[4, 0] * ay4; a[8, 10] = -imagea[4, 0] * az4;
            a[9, 0] = 0; a[9, 1] = 0; a[9, 2] = 0; a[9, 3] = 0; a[9, 4] = ax4; a[9, 5] = ay4; a[9, 6] = az4; a[9, 7] = 1;
            a[9, 8] = -imagea[4, 1] * ax4; a[9, 9] = -imagea[4, 1] * ay4; a[9, 10] = -imagea[4, 1] * az4;

            a[10, 0] = ax5; a[10, 1] = ay5; a[10, 2] = az5; a[10, 3] = 1; a[10, 4] = 0; a[10, 5] = 0; a[10, 6] = 0; a[10, 7] = 0;
            a[10, 8] = -imagea[5, 0] * ax5; a[10, 9] = -imagea[5, 0] * ay5; a[10, 10] = -imagea[5, 0] * az5;
            a[11, 0] = 0; a[11, 1] = 0; a[11, 2] = 0; a[11, 3] = 0; a[11, 4] = ax5; a[11, 5] = ay5; a[11, 6] = az5; a[11, 7] = 1;
            a[11, 8] = -imagea[5, 1] * ax5; a[11, 9] = -imagea[5, 1] * ay5; a[11, 10] = -imagea[5, 1] * az5;

            a[12, 0] = ax6; a[12, 1] = ay6; a[12, 2] = az6; a[12, 3] = 1; a[12, 4] = 0; a[12, 5] = 0; a[12, 6] = 0; a[12, 7] = 0;
            a[12, 8] = -imagea[6, 0] * ax6; a[12, 9] = -imagea[6, 0] * ay6; a[12, 10] = -imagea[6, 0] * az6;
            a[13, 0] = 0; a[13, 1] = 0; a[13, 2] = 0; a[13, 3] = 0; a[13, 4] = ax6; a[13, 5] = ay6; a[13, 6] = az6; a[13, 7] = 1;
            a[13, 8] = -imagea[6, 1] * ax6; a[13, 9] = -imagea[6, 1] * ay6; a[13, 10] = -imagea[6, 1] * az6;

            a[14, 0] = ax7; a[14, 1] = ay7; a[14, 2] = az7; a[14, 3] = 1; a[14, 4] = 0; a[14, 5] = 0; a[14, 6] = 0; a[14, 7] = 0;
            a[14, 8] = -imagea[7, 0] * ax7; a[14, 9] = -imagea[7, 0] * ay7; a[14, 10] = -imagea[7, 0] * az7;
            a[15, 0] = 0; a[15, 1] = 0; a[15, 2] = 0; a[15, 3] = 0; a[15, 4] = ax7; a[15, 5] = ay7; a[15, 6] = az7; a[15, 7] = 1;
            a[15, 8] = -imagea[7, 1] * ax7; a[15, 9] = -imagea[7, 1] * ay7; a[15, 10] = -imagea[7, 1] * az7;

            Matrix U = new Matrix(16, 1);
            double[,] u = U.Detail;
            u[0, 0] = imagea[0, 0]; u[1, 0] = imagea[0, 1]; u[2, 0] = imagea[1, 0]; u[3, 0] = imagea[1, 1];
            u[4, 0] = imagea[2, 0]; u[5, 0] = imagea[2, 1]; u[6, 0] = imagea[3, 0]; u[7, 0] = imagea[3, 1];
            u[8, 0] = imagea[4, 0]; u[9, 0] = imagea[4, 1]; u[10, 0] = imagea[5, 0]; u[11, 0] = imagea[5, 1];
            u[12, 0] = imagea[6, 0]; u[13, 0] = imagea[6, 1]; u[14, 0] = imagea[7, 0]; u[15, 0] = imagea[7, 1];

            Matrix p = Matrix.MatrixTrans(matrixA);
            Matrix q = Matrix.MatrixMulti(p, matrixA);
            Matrix r = Matrix.MatrixInv(q);
            //Matrix r1 = MatrixOperator.MatrixInvByCom(q1);            
            Matrix matrixAInvert = Matrix.MatrixMulti(r, p);
            Matrix M = Matrix.MatrixMulti(matrixAInvert, U);

            return M;
        }

        /// <summary>
        /// 计算目标点空间坐标
        /// </summary>
        /// <param name="M1"></param>正位变换矩阵
        /// <param name="M2"></param>侧位变换矩阵
        /// <param name="imagedata"></param>目标点分别在正侧位图像上的坐标
        public double[] CalculatePoints(Matrix M1, Matrix M2, double[,] imagedata)
        {
            //计算目标点坐标
            Matrix K = new Matrix(4, 3);
            double[,] k = K.Detail;
            double[,] m1 = M1.Detail;
            double[,] m2 = M2.Detail;

            k[0, 0] = imagedata[0, 0] * m1[8, 0] - m1[0, 0]; k[0, 1] = imagedata[0, 0] * m1[9, 0] - m1[1, 0]; k[0, 2] = imagedata[0, 0] * m1[10, 0] - m1[2, 0];
            k[1, 0] = imagedata[0, 1] * m1[8, 0] - m1[4, 0]; k[1, 1] = imagedata[0, 1] * m1[9, 0] - m1[5, 0]; k[1, 2] = imagedata[0, 1] * m1[10, 0] - m1[6, 0];
            k[2, 0] = imagedata[1, 0] * m2[8, 0] - m2[0, 0]; k[2, 1] = imagedata[1, 0] * m2[9, 0] - m2[1, 0]; k[2, 2] = imagedata[1, 0] * m2[10, 0] - m2[2, 0];
            k[3, 0] = imagedata[1, 1] * m2[8, 0] - m2[4, 0]; k[3, 1] = imagedata[1, 1] * m2[9, 0] - m2[5, 0]; k[3, 2] = imagedata[1, 1] * m2[10, 0] - m2[6, 0];

            Matrix W = new Matrix(4, 1);
            double[,] w = W.Detail;
            w[0, 0] = m1[3, 0] - imagedata[0, 0]; w[1, 0] = m1[7, 0] - imagedata[0, 1];
            w[2, 0] = m2[3, 0] - imagedata[1, 0]; w[3, 0] = m2[7, 0] - imagedata[1, 1];
            Matrix p = Matrix.MatrixTrans(K);
            Matrix q = Matrix.MatrixMulti(p, K);
            Matrix r = Matrix.MatrixInv(q);
            //Matrix r = MatrixOperator.MatrixInvByCom(q);
            Matrix K1Invert = Matrix.MatrixMulti(r, p);
            Matrix point = Matrix.MatrixMulti(K1Invert, W);
            double[,] pointvalue = point.Detail;
            double[] targetPoint = new double[3];
            targetPoint[0] = pointvalue[0, 0]; targetPoint[1] = pointvalue[1, 0]; targetPoint[2] = pointvalue[2, 0];

            return targetPoint;
        }

        /// <summary>
        /// 标尺上标志点空间坐标
        /// </summary>
        /// <returns></returns>
        public double[,] GetLandmarkCoordinates()
        {
            double wl = 12.5;//大矩形边长25
            double ws = 7.5;//小矩形边长15
            double d = 13.5;//两层之间的距离27
            double[,] coordinate = new double[8, 4];
            coordinate[0, 0] = -wl; coordinate[0, 1] = wl; coordinate[0, 2] = -d; coordinate[0, 3] = 1;
            coordinate[1, 0] = wl; coordinate[1, 1] = wl; coordinate[1, 2] = -d; coordinate[1, 3] = 1;
            coordinate[2, 0] = wl; coordinate[2, 1] = -wl; coordinate[2, 2] = -d; coordinate[2, 3] = 1;
            coordinate[3, 0] = -wl; coordinate[3, 1] = -wl; coordinate[3, 2] = -d; coordinate[3, 3] = 1;
            coordinate[4, 0] = -ws; coordinate[4, 1] = ws; coordinate[4, 2] = d; coordinate[4, 3] = 1;
            coordinate[5, 0] = ws; coordinate[5, 1] = ws; coordinate[5, 2] = d; coordinate[5, 3] = 1;
            coordinate[6, 0] = ws; coordinate[6, 1] = -ws; coordinate[6, 2] = d; coordinate[6, 3] = 1;
            coordinate[7, 0] = -ws; coordinate[7, 1] = -ws; coordinate[7, 2] = d; coordinate[7, 3] = 1;

            Matrix coords = new Matrix(8, 4);
            double[,] a = coords.Detail;
            Array.Copy(coordinate, a, 32);
            Matrix coordsTrans = Matrix.MatrixTrans(coords);

            vtkMatrix4x4 matrix1 = mathcal.GetMatrixFromQuaternionAndPosition(chuankou.ToolTrackerPose, chuankou.ToolTrackerPosi);

            Matrix trans = mathcal.vtkMatrixToMatrix(matrix1);
            Matrix Coords = Matrix.MatrixMulti(trans, coordsTrans);
            Matrix Coordinate = Matrix.MatrixTrans(Coords);
            double[,] value = Coordinate.Detail;
            return value;
        }
    }
}
