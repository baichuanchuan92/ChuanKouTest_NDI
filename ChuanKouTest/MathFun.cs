using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;
using System.Windows.Media.Media3D;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;




namespace ChuanKouTest
{




    /// <summary>
    ///包含数学运算函数
    /// </summary>
    public class MathFun
    {

        

        public static double TotalErrX = 0;
        public static double TotalErrY = 0;
        public static double TotalErrZ = 0;





        /// <summary>
        ///表示矩阵左乘向量，用在坐标变换之时
        ///VectorOut为输出的向量
        /// </summary>
        /// <param name="Matrix"></param>
        /// <param name="Vector"></param>
        /// <param name="VectorOut"></param>
        public static void MatrixVectorMul(double[,] Matrix, double[] Vector, out double[] VectorOut)
        {
            
            int len = Vector.Length;
            VectorOut = new double[len];
            int i = 0;
            int j = 0;
            for (i = 0; i < len; i++)
            {//表示结果的向量行数
                for (j = 0; j < len; j++)
                {//相加四次
                    VectorOut[i] += Matrix[i, j] * Vector[j];
                }
            }


        }

        


        /// <summary>
        /// 输入三点，使用三矢量定姿获得转换矩阵
        /// 原点为a，x轴由ab确定，最后一点确定xy平面
        /// CT转换用
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static vtkMatrix4x4 BulidCoordinateSystem(Point3D a, Point3D b, Point3D c)//三点建立坐标系
        {
            //改了Y的方向
            Vector3D Xaxis = new Vector3D();
            Vector3D Yaxis = new Vector3D();
            Vector3D Zaxis = new Vector3D();
            Vector3D V = new Vector3D();


            Xaxis = (b - a);
            Xaxis.Normalize();
            V = (c - a);
            V.Normalize();

            //Zaxis = Vector3D.CrossProduct(V, Xaxis);
            Zaxis = Vector3D.CrossProduct(Xaxis,V);
            //TODO:和matlab中三矢量定姿不同？
            Zaxis.Normalize();
            Yaxis = Vector3D.CrossProduct(Zaxis, Xaxis);
            Yaxis.Normalize();


            vtkMatrix4x4 M = new vtkMatrix4x4();

            M.SetElement(0, 0, Xaxis.X);
            M.SetElement(1, 0, Xaxis.Y);
            M.SetElement(2, 0, Xaxis.Z);
            M.SetElement(3, 0, 0);

            M.SetElement(0, 1, Yaxis.X);
            M.SetElement(1, 1, Yaxis.Y);
            M.SetElement(2, 1, Yaxis.Z);
            M.SetElement(3, 1, 0);

            M.SetElement(0, 2, Zaxis.X);
            M.SetElement(1, 2, Zaxis.Y);
            M.SetElement(2, 2, Zaxis.Z);
            M.SetElement(3, 2, 0);

            M.SetElement(0, 3, a.X);
            M.SetElement(1, 3, a.Y);
            M.SetElement(2, 3, a.Z);
            M.SetElement(3, 3, 1);

            return M;
        }






        /// <summary>
        ///由tracker位姿计算坐标系变换矩阵
        ///输入3个位移分量，4个四元数分量
        ///输出变换矩阵
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        public static Matrix3D Quat2trans(Vector3D trans, Quaternion rot)
        {
            double d = rot.Angle;
            Matrix3D m = new Matrix3D();
            m.SetIdentity();
            m.Rotate(rot);
            m.Translate(trans);
            return m;
        }


        /// <summary>
        /// Matrix3D转VTKMatrix
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static vtkMatrix4x4 Matrix3D2VTKMatrix(Matrix3D m)
        {
            vtkMatrix4x4 matrix = new vtkMatrix4x4();
            matrix.SetElement(0, 0, m.M11);
            matrix.SetElement(1, 0, m.M12);
            matrix.SetElement(2, 0, m.M13);

            matrix.SetElement(0, 1, m.M21);
            matrix.SetElement(1, 1, m.M22);
            matrix.SetElement(2, 1, m.M23);

            matrix.SetElement(0, 2, m.M31);
            matrix.SetElement(1, 2, m.M32);
            matrix.SetElement(2, 2, m.M33);

            matrix.SetElement(0, 3, m.OffsetX);
            matrix.SetElement(1, 3, m.OffsetY);
            matrix.SetElement(2, 3, m.OffsetZ);


            return matrix;
        }

        /// <summary>
        /// 输入点坐标和变换矩阵，进行坐标变换
        /// </summary>
        /// <param name="transmatrix"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public static void point3dtrans(vtkMatrix4x4 transmatrix, ref double x, ref double y, ref double z)
        {
            vtkMatrix4x4 inimatrix = vtkMatrix4x4.New();
            inimatrix.SetElement(0, 3, x);
            inimatrix.SetElement(1, 3, y);
            inimatrix.SetElement(2, 3, z);
            vtkMatrix4x4.Multiply4x4(transmatrix, inimatrix, inimatrix);
            x = inimatrix.GetElement(0, 3);
            y = inimatrix.GetElement(1, 3);
            z = inimatrix.GetElement(2, 3);
        }






        /// <summary>
        /// PI控制，用于并联机器人控制
        /// 用于对目标点进行直接调整
        /// </summary>
        /// <param name="ErrX"></param>
        /// <param name="ErrY"></param>
        /// <param name="ErrZ"></param>
        /// <param name="PVal"></param>
        /// <param name="IVal"></param>
        /// <param name="TotalErrStewartX"></param>
        /// <param name="TotalErrStewartY"></param>
        /// <param name="TotalErrStewartZ"></param>
        /// <param name="FBOutX"></param>
        /// <param name="FBOutY"></param>
        /// <param name="FBOutZ"></param>
        public static void PIControlStewart(
            double ErrX, double ErrY, double ErrZ,
            double PVal, double IVal,
            double ErrXHistory,double ErrYHistory,double ErrZHistory,
            ref double TotalErrStewartX,ref double TotalErrStewartY,ref double TotalErrStewartZ,
            out double FBOutX, out double FBOutY, out double FBOutZ
            )
        {
            //PI环节，输入误差和PI的参数大小，输出应该加到Goal上的值
            //默认三个方向参数相同
            //使用全局TotalErr变量记录以往误差值，起到积分作用
            //TotalErrX += ErrX; TotalErrY += ErrY; TotalErrZ += ErrZ;

           

            

            //只有当误差足够小的时候才为1，否则为0
            double alphaX = 1;
            double alphaY = 1;
            double alphaZ = 1;

            #region 变号清零,防止I的惯性导致大回环
            //TotalErrStewartX
            if (ErrX * ErrXHistory < 0)
            {
                TotalErrStewartX = 0;
            }
            //TotalErrStewartY
            if (ErrY * ErrYHistory < 0)
            {
                TotalErrStewartY = 0;
            }
            //TotalErrStewartZ
            if (ErrZ * ErrZHistory < 0)
            {
                TotalErrStewartZ = 0;
            }
            #endregion


            #region 积分分离
            //防止积分饱和现象
            //依据文中的积分分离：https://zhuanlan.zhihu.com/p/49572763
            //阈值
            //TODO：暂时不设
            const double ErrIValMax = 999;

            if (Math.Abs(ErrX) > ErrIValMax)
            {
                alphaX = 0;
            }
            if (Math.Abs(ErrY) > ErrIValMax)
            {
                alphaY = 0;
            }
            if (Math.Abs(ErrZ) > ErrIValMax)
            {
                alphaZ = 0;
            }
            #endregion

            


            #region PI控制器输入设置上限
            //输入到PI控制器中的数值
            double PInX = 0;
            double PInY = 0;
            double PInZ = 0;

            double IInX = 0;
            double IInY = 0;
            double IInZ = 0;

            const double PInMax = 1;
            const double IInMax = 1;


            //PInX
            PInX = PIInputSolve(ErrX, PInMax, PVal);
            //PInY
            PInY = PIInputSolve(ErrY, PInMax, PVal);
            //PInZ
            PInZ = PIInputSolve(ErrZ, PInMax, PVal);

            TotalErrStewartX += ErrX;
            TotalErrStewartY += ErrY;
            TotalErrStewartZ += ErrZ;

            //IInX
            IInX = PIInputSolve(TotalErrStewartX, IInMax, IVal);
            //IInY
            IInY = PIInputSolve(TotalErrStewartY, IInMax, IVal);
            //IInZ
            IInZ= PIInputSolve(TotalErrStewartZ, IInMax, IVal);
            #endregion





            FBOutX = PInX + IInX;
            FBOutY = PInY + IInY;
            FBOutZ = PInZ + IInZ;
        }

        /// <summary>
        /// 输入误差，最大限度，P或I参数值
        /// 输出对应的PI输入量，其绝对值不超过MaxNum
        /// </summary>
        /// <param name="Err"></param>
        /// <returns></returns>
        public static double PIInputSolve(double Err,double MaxNum,double Val)
        {
            double InputNum = 0;
            if (Err * Val < MaxNum && Err * Val > -MaxNum)
            {
                InputNum = Err * Val;
            }
            else if (Err * Val >= MaxNum)
            {
                InputNum = MaxNum;
            }
            else
            {
                InputNum = -MaxNum;
            }


            return InputNum;


        }



        /// <summary>
        /// 输入两点坐标，输出这两点连成的空间直线的方向向量
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="z2"></param>
        /// <param name="DirX"></param>
        /// <param name="DirY"></param>
        /// <param name="DirZ"></param>
        public static void Points2Dir(
        double x1, double y1, double z1,
        double x2, double y2, double z2,
        out double DirX, out double DirY, out double DirZ)
        {

            DirX = x2 - x1;
            DirY = y2 - y1;
            DirZ = z2 - z1;
        }


        /// <summary>
        /// 给定空间中一点和空间中一条直线，求出点到直线的距离
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="z0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double Point2LineDis3D(
            double x0, double y0, double z0,
            double x1,double y1,double z1,double m,double n,double p
            ) {
            double t,d;
            double xc, yc, zc;
            double d1, d2, d3;

            t = (m * (x0 - x1) + n * (y0 - y1) + p * (z0 - z1)) / (m * m + n * n + p * p);

            xc = m * t + x1;
            yc = n * t + y1;
            zc = p * t + z1;

            d1 = Math.Pow(x0 - xc, 2);
            d2 = Math.Pow(y0 - yc, 2);
            d3 = Math.Pow(z0 - zc, 2);

            d = Math.Sqrt(d1 + d2 + d3);

            return d;


        }

        /// <summary>
        /// 输入3维空间下一点（x0，y0,z0）和一条直线上两点（x1,y1,z1）和（x2,y2,z2）
        /// 输出垂足的空间坐标（xN,yN,zN）
        /// https://blog.csdn.net/zhouschina/article/details/14647587
        /// 公式已手算验证
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="z0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="z2"></param>
        /// <param name="xN"></param>
        /// <param name="yN"></param>
        /// <param name="zN"></param>
        /// <returns></returns>
        public static void Point2LineFoot3D(
            double x0,double y0,double z0,
            double x1,double y1,double z1,
            double x2, double y2, double z2,
            out double xN,out double yN, out double zN
            )
        {

            double a = (x0 - x1) * (x2 - x1) + (y0 - y1) * (y2 - y1) + (z0 - z1) * (z2 - z1);
            double b = (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1) + (z2 - z1) * (z2 - z1);
            double k = a/b;

            xN = k * (x2 - x1) + x1;
            yN = k * (y2 - y1) + y1;
            zN = k * (z2 - z1) + z1;
        }






        /// <summary>
        /// 给定若干3D点（存储于Datas）
        /// 最小二乘法拟合出对应的空间直线,返回6个参数：x0,y0,z0,a,b,c
        /// 方程形式：(x-x0)/a=(y-y0)/b=(z-z0)/c
        /// 来源：https://zhuanlan.zhihu.com/p/93245517
        /// http://caves.org/section/commelect/DUSI/openmag/pdf/SphereFitting.pdf
        /// </summary>
        /// <param name="Datas"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="z0"></param>
        /// <param name="aLine"></param>
        /// <param name="bLine"></param>
        /// <param name="cLine"></param>
        public static void LeastSquareLine3D(List<Point3D>Datas,
            out double x0,out double y0,out double z0,
            out double aLine,out double bLine,out double cLine) {

            //求平均点，拟合后的直线一定过这个点，这个点也就是(x0,y0,z0)
            double sum_x = 0;
            double sum_y = 0;
            double sum_z = 0;
            foreach (Point3D temp in Datas)
            {
                sum_x += temp.X;
                sum_y += temp.Y;
                sum_z += temp.Z;
            }
            sum_x /= Datas.Count;
            sum_y /= Datas.Count;
            sum_z /= Datas.Count;

            DenseMatrix Jacobian = new DenseMatrix(Datas.Count, 3);
            foreach (Point3D temp in Datas)
            {
                Vector<double> gradient = new DenseVector(3);
                gradient[0] = temp.X - sum_x;
                gradient[1] = temp.Y - sum_y;
                gradient[2] = temp.Z - sum_z;
                Jacobian.SetRow(Datas.IndexOf(temp), gradient);
            }

            //SVD
            Svd<double> svd = Jacobian.Svd(true);
            // get matrix of right singular vectors
            Matrix<double> V = svd.VT.Transpose();

            //矩阵V的第一列即是(a,b,c)
            Vector<double> parameters = new DenseVector(3);
            parameters = V.Column(0);
            x0 = sum_x;
            y0 = sum_y;
            z0 = sum_z;
            aLine = parameters[0];
            bLine = parameters[1];
            cLine = parameters[2];
        }



        /// <summary>
        /// 输入一个平面：ax+by+cz+d=0
        /// 以及一条直线：(x-x0)/a=(y-y0)/b=(z-z0)/c=t
        /// 求两者交点（xi,yi,zi）
        /// </summary>
        public static void LinePlaneIntersect2SolvePoint(
            double aPlane,double bPlane,double cPlane,double dPlane,
            double x0,double y0,double z0,double aLine,double bLine,double cLine,
            out double xi,out double yi,out double zi
            ) {
            //求出直线方程中的t
            double tLine;
            tLine = -(aPlane * x0 + bPlane * y0 + cPlane * z0 + dPlane)
                / (aPlane * aLine + bPlane * bLine + cPlane * cLine);
            //求出交点(xi,yi,zi)
            xi = aLine * tLine + x0;
            yi = bLine * tLine + y0;
            zi = cLine * tLine + z0;

        }


        /// <summary>
        /// 输入一个导航仪实时采集到的点，将其均值滤波后输出
        /// 确保调用该函数时的线程不是主线程，否则会堵塞
        /// </summary>
        /// <param name="VarXIn"></param>
        /// <param name="VarYIn"></param>
        /// <param name="VarZIn"></param>
        /// <param name="Num"></param>
        /// <param name="TimeWait"></param>
        /// <param name="VarXOut"></param>
        /// <param name="VarYOut"></param>
        /// <param name="VarZOut"></param>
        public static void MeanFilter(
            double VarXIn,double VarYIn,double VarZIn,
            out double VarXOut,out double VarYOut,out double VarZOut,
            int Num=10, int TimeWait = 50
            ) {

            int i = 0;
            VarXOut = 0;VarYOut = 0;VarZOut = 0;
            for (i = 0; i < Num; i++)
            {
                VarXOut += VarXIn;
                VarYOut += VarYIn;
                VarZOut += VarZIn;

                System.Threading.Thread.Sleep(TimeWait);
            }
            VarXOut /= Num;
            VarYOut /= Num;
            VarZOut /= Num;

        }


        /// <summary>
        /// 给定空间中一点和平面，求出投影点的三维坐标
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="z0"></param>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <param name="D"></param>
        /// <param name="xp"></param>
        /// <param name="yp"></param>
        /// <param name="zp"></param>
        public static void PointProject2Plane3D(
            double x0,double y0,double z0,
            double A,double B,double C,double D,
            out double xp,out double yp,out double zp
            )
        {
            double x = (B * B + C * C) * x0 - A * (B * y0 + C * z0 + D);
            double y = (A * A + C * C) * y0 - B * (A * x0 + C * z0 + D);
            double z = (A * A + B * B) * z0 - C * (A * x0 + B * y0 + D);

            double a = A * A + B * B + C * C;

            xp = x / a;
            yp = y / a;
            zp = z / a;
        }


        /// <summary>
        /// 输入齐次变换矩阵
        /// 输出对应的欧拉角（ZYX）
        /// 对应网址：
        /// https://blog.csdn.net/Darlingqiang/article/details/80829666
        /// https://blog.csdn.net/qq_42189368/article/details/84320214
        /// </summary>
        /// <param name="TransMatrixIn"></param>
        /// <param name="EulerAngleX"></param>
        /// <param name="EulerAngleY"></param>
        /// <param name="EulerAngleZ"></param>
        public static void TransMatrix2Euler(
            vtkMatrix4x4 TransMatrixIn,
            out double EulerAngleX,out double EulerAngleY,out double EulerAngleZ
            )
        {
            double r11 = TransMatrixIn.GetElement(0, 0);
            double r21 = TransMatrixIn.GetElement(1, 0);
            double r31 = TransMatrixIn.GetElement(2, 0);
            double r32 = TransMatrixIn.GetElement(2, 1);
            double r33 = TransMatrixIn.GetElement(2, 2);





            double ThetaX = 0;
            double ThetaY = 0;
            double ThetaZ = 0;

            double b = Math.Sqrt(r32 * r32 + r33 * r33);

            ThetaX = Math.Atan2(r32, r33);
            ThetaY = Math.Atan2(-r31, b);
            ThetaZ = Math.Atan2(r21, r11);


            EulerAngleX = ThetaX*180/Math.PI;
            EulerAngleY = ThetaY * 180 / Math.PI;
            EulerAngleZ = ThetaZ * 180 / Math.PI;



        }


        /// <summary>
        /// 输入X轴欧拉角
        /// 输出对应旋转矩阵
        /// </summary>
        /// <param name="EulerAngleX"></param>
        /// <param name="RotMatrixXOut"></param>
        public static void Euler2RotMatrixX(
            double EulerAngleX,
            out vtkMatrix3x3 RotMatrixXOut
            )
        {
            double EulerAngleRadX = EulerAngleX * Math.PI / 180;
            RotMatrixXOut = new vtkMatrix3x3();

            RotMatrixXOut.SetElement(0, 0, 1);

            RotMatrixXOut.SetElement(1, 1, Math.Cos(EulerAngleRadX));
            RotMatrixXOut.SetElement(1, 2, -Math.Sin(EulerAngleRadX));

            RotMatrixXOut.SetElement(2, 1, Math.Sin(EulerAngleRadX));
            RotMatrixXOut.SetElement(2, 2, Math.Cos(EulerAngleRadX));
        }

        /// <summary>
        /// 输入Y轴欧拉角
        /// 输出对应旋转矩阵
        /// </summary>
        /// <param name="EulerAngleX"></param>
        /// <param name="RotMatrixXOut"></param>
        public static void Euler2RotMatrixY(
            double EulerAngleY,
            out vtkMatrix3x3 RotMatrixYOut
            )
        {
            double EulerAngleRadY = EulerAngleY * Math.PI / 180;
            RotMatrixYOut = new vtkMatrix3x3();

            RotMatrixYOut.SetElement(1, 1, 1);

            RotMatrixYOut.SetElement(0, 0, Math.Cos(EulerAngleRadY));
            RotMatrixYOut.SetElement(0, 2, Math.Sin(EulerAngleRadY));

            RotMatrixYOut.SetElement(2, 0, -Math.Sin(EulerAngleRadY));
            RotMatrixYOut.SetElement(2, 2, Math.Cos(EulerAngleRadY));
        }


        /// <summary>
        /// 输入Z轴欧拉角
        /// 输出对应旋转矩阵
        /// </summary>
        /// <param name="EulerAngleX"></param>
        /// <param name="RotMatrixXOut"></param>
        public static void Euler2RotMatrixZ(
            double EulerAngleZ,
            out vtkMatrix3x3 RotMatrixZOut
            )
        {
            double EulerAngleRadZ = EulerAngleZ * Math.PI / 180;
            RotMatrixZOut = new vtkMatrix3x3();

            RotMatrixZOut.SetElement(2, 2, 1);

            RotMatrixZOut.SetElement(0, 0, Math.Cos(EulerAngleRadZ));
            RotMatrixZOut.SetElement(0, 1, -Math.Sin(EulerAngleRadZ));

            RotMatrixZOut.SetElement(1, 0, Math.Sin(EulerAngleRadZ));
            RotMatrixZOut.SetElement(1, 1, Math.Cos(EulerAngleRadZ));
        }


        /// <summary>
        /// 输入三个轴欧拉角
        /// 输出对应旋转矩阵 
        /// 对应网址：
        /// https://blog.csdn.net/Darlingqiang/article/details/80829666
        /// 顺序ZYX，绕动轴
        /// </summary>
        /// <param name="EulerAngleX"></param>
        /// <param name="EulerAngleY"></param>
        /// <param name="EulerAngleZ"></param>
        /// <param name="RotMatrixTotalOut"></param>
        public static void Euler2RotMatrixTotal(
            double EulerAngleX, double EulerAngleY, double EulerAngleZ,
            out vtkMatrix3x3 RotMatrixTotalOut
            )
        {
            vtkMatrix3x3 RotMatrixX = new vtkMatrix3x3();
            vtkMatrix3x3 RotMatrixY = new vtkMatrix3x3();
            vtkMatrix3x3 RotMatrixZ = new vtkMatrix3x3();
            RotMatrixTotalOut = new vtkMatrix3x3();

            Euler2RotMatrixX(EulerAngleX, out RotMatrixX);
            Euler2RotMatrixY(EulerAngleY, out RotMatrixY);
            Euler2RotMatrixZ(EulerAngleZ, out RotMatrixZ);

            //zyx
            vtkMatrix3x3.Multiply3x3(RotMatrixZ, RotMatrixY, RotMatrixTotalOut);

            vtkMatrix3x3 temp = new vtkMatrix3x3();
            temp.DeepCopy(RotMatrixTotalOut);

            vtkMatrix3x3.Multiply3x3(temp, RotMatrixX, RotMatrixTotalOut);



        }


        /// <summary>
        /// 输入三个轴欧拉角和平动量
        /// 输出对应变换矩阵 
        /// 对应网址：
        /// https://blog.csdn.net/Darlingqiang/article/details/80829666
        /// </summary>
        /// <param name="TransX"></param>
        /// <param name="TransY"></param>
        /// <param name="TransZ"></param>
        /// <param name="EulerAngleX"></param>
        /// <param name="EulerAngleY"></param>
        /// <param name="EulerAngleZ"></param>
        /// <param name="TransMatrixTotalOut"></param>
        public static void Euler2TransMatrixTotal(
            double TransX, double TransY, double TransZ,
            double EulerAngleX, double EulerAngleY, double EulerAngleZ,
            out vtkMatrix4x4 TransMatrixTotalOut
            )
        {
            vtkMatrix3x3 RotMatrix = new vtkMatrix3x3();
            TransMatrixTotalOut = new vtkMatrix4x4();

            Euler2RotMatrixTotal(
                EulerAngleX, EulerAngleY, EulerAngleZ,
                out RotMatrix
                );

            TransMatrixTotalOut.SetElement(0, 0, RotMatrix.GetElement(0, 0));
            TransMatrixTotalOut.SetElement(0, 1, RotMatrix.GetElement(0, 1));
            TransMatrixTotalOut.SetElement(0, 2, RotMatrix.GetElement(0, 2));
            TransMatrixTotalOut.SetElement(0, 3, TransX);

            TransMatrixTotalOut.SetElement(1, 0, RotMatrix.GetElement(1, 0));
            TransMatrixTotalOut.SetElement(1, 1, RotMatrix.GetElement(1, 1));
            TransMatrixTotalOut.SetElement(1, 2, RotMatrix.GetElement(1, 2));
            TransMatrixTotalOut.SetElement(1, 3, TransY);

            TransMatrixTotalOut.SetElement(2, 0, RotMatrix.GetElement(2, 0));
            TransMatrixTotalOut.SetElement(2, 1, RotMatrix.GetElement(2, 1));
            TransMatrixTotalOut.SetElement(2, 2, RotMatrix.GetElement(2, 2));
            TransMatrixTotalOut.SetElement(2, 3, TransZ);

            TransMatrixTotalOut.SetElement(3, 0, 0);
            TransMatrixTotalOut.SetElement(3, 1, 0);
            TransMatrixTotalOut.SetElement(3, 2, 0);
            TransMatrixTotalOut.SetElement(3, 3, 1);
        }



        /// <summary>
        /// 输入齐次变换矩阵
        /// 输出对应的平移量
        /// </summary>
        /// <param name="TransMatrixIn"></param>
        /// <param name="TransX"></param>
        /// <param name="TransY"></param>
        /// <param name="TransZ"></param>
        public static void TransMatrix2Translation(
            vtkMatrix4x4 TransMatrixIn,
            out double TransX,out double TransY,out double TransZ
            )
        {
            TransX = TransMatrixIn.GetElement(0, 3);
            TransY = TransMatrixIn.GetElement(1, 3);
            TransZ = TransMatrixIn.GetElement(2, 3);
        }


        /// <summary>
        /// 输入两个空间点，求它们之间的距离
        /// </summary>
        /// <param name="p1X"></param>
        /// <param name="p1Y"></param>
        /// <param name="p1Z"></param>
        /// <param name="p2X"></param>
        /// <param name="p2Y"></param>
        /// <param name="p2Z"></param>
        /// <returns></returns>
        public static double DistancePoint2Point(
            double p1X, double p1Y, double p1Z,
            double p2X, double p2Y, double p2Z
            )
        {
            double Dis = 0;

            double DisX = Math.Pow(p1X - p2X, 2);
            double DisY = Math.Pow(p1Y - p2Y, 2);
            double DisZ = Math.Pow(p1Z - p2Z, 2);

            Dis = Math.Sqrt(DisX + DisY + DisZ);


            return Dis;
        }

        /// <summary>
        /// 输入平移量和旋转矩阵
        /// 输出齐次变换矩阵
        /// </summary>
        /// <param name="transX"></param>
        /// <param name="transY"></param>
        /// <param name="transZ"></param>
        /// <param name="RotMatrix"></param>
        /// <param name="HomoTransMatrix"></param>
        public static void HomogeneousMatrixCreate(
            double transX, double transY, double transZ,
            vtkMatrix3x3 RotMatrix,
            out vtkMatrix4x4 HomoTransMatrix
            ) 
        {
            HomoTransMatrix = new vtkMatrix4x4();


            HomoTransMatrix.SetElement(0, 0, RotMatrix.GetElement(0, 0));
            HomoTransMatrix.SetElement(0, 1, RotMatrix.GetElement(0, 1));
            HomoTransMatrix.SetElement(0, 2, RotMatrix.GetElement(0, 2));
            HomoTransMatrix.SetElement(0, 3, transX);

            HomoTransMatrix.SetElement(1, 0, RotMatrix.GetElement(1, 0));
            HomoTransMatrix.SetElement(1, 1, RotMatrix.GetElement(1, 1));
            HomoTransMatrix.SetElement(1, 2, RotMatrix.GetElement(1, 2));
            HomoTransMatrix.SetElement(1, 3, transY);

            HomoTransMatrix.SetElement(2, 0, RotMatrix.GetElement(2, 0));
            HomoTransMatrix.SetElement(2, 1, RotMatrix.GetElement(2, 1));
            HomoTransMatrix.SetElement(2, 2, RotMatrix.GetElement(2, 2));
            HomoTransMatrix.SetElement(2, 3, transZ);

            HomoTransMatrix.SetElement(3, 0, 0);
            HomoTransMatrix.SetElement(3, 1, 0);
            HomoTransMatrix.SetElement(3, 2, 0);
            HomoTransMatrix.SetElement(3, 3, 1);



        }

        /// <summary>
        /// 将vtkMatrix4x4转化为double数组形式的matrix
        /// </summary>
        public static void vtkMatrix4x42DoubleMatrix(
            vtkMatrix4x4 MatrixIn,
            out double[,]MatrixOut
            ) 
        {
            MatrixOut = new double[4,4];

            MatrixOut[0, 0] = MatrixIn.GetElement(0, 0);
            MatrixOut[0, 1] = MatrixIn.GetElement(0, 1);
            MatrixOut[0, 2] = MatrixIn.GetElement(0, 2);
            MatrixOut[0, 3] = MatrixIn.GetElement(0, 3);

            MatrixOut[1, 0] = MatrixIn.GetElement(1, 0);
            MatrixOut[1, 1] = MatrixIn.GetElement(1, 1);
            MatrixOut[1, 2] = MatrixIn.GetElement(1, 2);
            MatrixOut[1, 3] = MatrixIn.GetElement(1, 3);

            MatrixOut[2, 0] = MatrixIn.GetElement(2, 0);
            MatrixOut[2, 1] = MatrixIn.GetElement(2, 1);
            MatrixOut[2, 2] = MatrixIn.GetElement(2, 2);
            MatrixOut[2, 3] = MatrixIn.GetElement(2, 3);

            MatrixOut[3, 0] = MatrixIn.GetElement(3, 0);
            MatrixOut[3, 1] = MatrixIn.GetElement(3, 1);
            MatrixOut[3, 2] = MatrixIn.GetElement(3, 2);
            MatrixOut[3, 3] = MatrixIn.GetElement(3, 3);

        }







    }
}
