using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;
using System.Windows.Media.Media3D;

namespace ChuanKouTest
{
    /// <summary>
    /// 用于记录Stewart平台穿针实验中涉及的外部（而非Stewart平台自身）的参数
    /// </summary>
    class StewartExpe
    {

        //以下六个点为钢珠方盒上的钢珠坐标，以方盒上的track为坐标系
        public static Point3D TargetPoint1 = new Point3D(176.965,-50.115,-37.52);
        public static Point3D TargetPoint2 = new Point3D(202.77,- 40.25,- 87.065);
        public static Point3D TargetPoint3 = new Point3D(187.365,- 43.105,- 137.95);
        public static Point3D TargetPoint4 = new Point3D(197.19,33.84,-39.835);
        public static Point3D TargetPoint5 = new Point3D(177.275,31.725,- 92.34);
        public static Point3D TargetPoint6 = new Point3D(187.255,36.565,- 142.025);


        //以下六个点为钢珠方盒上的钢珠坐标，以机构为坐标系
        public static Point3D TargetPoint1ToolCoo = new Point3D();
        public static Point3D TargetPoint2ToolCoo = new Point3D();
        public static Point3D TargetPoint3ToolCoo = new Point3D();
        public static Point3D TargetPoint4ToolCoo = new Point3D();
        public static Point3D TargetPoint5ToolCoo = new Point3D();
        public static Point3D TargetPoint6ToolCoo = new Point3D();


        public static Point3D[] TargetPointArrayToolCoo = new Point3D[6];


        //记录从机器人工具端到NDI的变换矩阵，实时变化
        public static vtkMatrix4x4 TransMatrixTool2NDI = new vtkMatrix4x4();
        //记录从盒子坐标系到NDI的变换矩阵，实时变化
        public static vtkMatrix4x4 TransMatrixBox2NDI = new vtkMatrix4x4();


        /// <summary>
        /// 将盒子上的点转化为机构坐标系下表示，从而用来作为穿针目标
        /// 结果存储在类中的成员：TargetPointNToolCoo中，N=1，2，3...6
        /// </summary>
        public static void CooTransBox2Tool()
        {

            //钢珠坐标初始赋值
            double TargetPoint1ToolCooX = TargetPoint1.X;
            double TargetPoint1ToolCooY = TargetPoint1.Y;
            double TargetPoint1ToolCooZ = TargetPoint1.Z;

            double TargetPoint2ToolCooX = TargetPoint2.X;
            double TargetPoint2ToolCooY = TargetPoint2.Y;
            double TargetPoint2ToolCooZ = TargetPoint2.Z;

            double TargetPoint3ToolCooX = TargetPoint3.X;
            double TargetPoint3ToolCooY = TargetPoint3.Y;
            double TargetPoint3ToolCooZ = TargetPoint3.Z;

            double TargetPoint4ToolCooX = TargetPoint4.X;
            double TargetPoint4ToolCooY = TargetPoint4.Y;
            double TargetPoint4ToolCooZ = TargetPoint4.Z;

            double TargetPoint5ToolCooX = TargetPoint5.X;
            double TargetPoint5ToolCooY = TargetPoint5.Y;
            double TargetPoint5ToolCooZ = TargetPoint5.Z;

            double TargetPoint6ToolCooX = TargetPoint6.X;
            double TargetPoint6ToolCooY = TargetPoint6.Y;
            double TargetPoint6ToolCooZ = TargetPoint6.Z;




            vtkMatrix4x4 VTKMatrixRight0 = new vtkMatrix4x4();
            //读取NDI位姿
            //盒子到NDI
            Vector3D trans2 = new Vector3D(NDI.trackdata[2*9 + 6], NDI.trackdata[2 * 9 + 7], NDI.trackdata[2 * 9 + 8]);
            System.Windows.Media.Media3D.Quaternion qua2 = new System.Windows.Media.Media3D.Quaternion(NDI.trackdata[2 * 9 + 3], NDI.trackdata[2 * 9 + 4], NDI.trackdata[2 * 9 + 5], NDI.trackdata[2 * 9 + 2]);
            Matrix3D matrixRight = MathFun.Quat2trans(trans2, qua2);
            VTKMatrixRight0 = MathFun.Matrix3D2VTKMatrix(matrixRight);

            TransMatrixBox2NDI = VTKMatrixRight0;


            //将六个点转换到NDI坐标系中
            MathFun.point3dtrans(VTKMatrixRight0, 
                ref TargetPoint1ToolCooX, 
                ref TargetPoint1ToolCooY, 
                ref TargetPoint1ToolCooZ);

            MathFun.point3dtrans(VTKMatrixRight0,
                ref TargetPoint2ToolCooX,
                ref TargetPoint2ToolCooY,
                ref TargetPoint2ToolCooZ);

            MathFun.point3dtrans(VTKMatrixRight0,
                ref TargetPoint3ToolCooX,
                ref TargetPoint3ToolCooY,
                ref TargetPoint3ToolCooZ);

            MathFun.point3dtrans(VTKMatrixRight0,
                ref TargetPoint4ToolCooX,
                ref TargetPoint4ToolCooY,
                ref TargetPoint4ToolCooZ);

            MathFun.point3dtrans(VTKMatrixRight0,
                ref TargetPoint5ToolCooX,
                ref TargetPoint5ToolCooY,
                ref TargetPoint5ToolCooZ);

            MathFun.point3dtrans(VTKMatrixRight0,
                ref TargetPoint6ToolCooX,
                ref TargetPoint6ToolCooY,
                ref TargetPoint6ToolCooZ);


            //工具端到NDI
            Vector3D trans = new Vector3D(NDI.trackdata[0*9+6], NDI.trackdata[0 * 9 + 7], NDI.trackdata[0 * 9 + 8]);
            System.Windows.Media.Media3D.Quaternion qua = new System.Windows.Media.Media3D.Quaternion(NDI.trackdata[0 * 9 + 3], NDI.trackdata[0 * 9 + 4], NDI.trackdata[0 * 9 + 5], NDI.trackdata[0 * 9 + 2]);
            Matrix3D matrix1 = MathFun.Quat2trans(trans, qua);
            vtkMatrix4x4 toolVTKMatrix = MathFun.Matrix3D2VTKMatrix(matrix1);
            //求逆矩阵，使得新矩阵表达从NDI到Tool
            vtkMatrix4x4 temp = new vtkMatrix4x4();
            temp.DeepCopy(toolVTKMatrix);
            temp.Invert();

            TransMatrixTool2NDI = toolVTKMatrix;

            //将六个点转换到Tool坐标系中
            MathFun.point3dtrans(temp,
                ref TargetPoint1ToolCooX,
                ref TargetPoint1ToolCooY,
                ref TargetPoint1ToolCooZ);

            MathFun.point3dtrans(temp,
                ref TargetPoint2ToolCooX,
                ref TargetPoint2ToolCooY,
                ref TargetPoint2ToolCooZ);

            MathFun.point3dtrans(temp,
                ref TargetPoint3ToolCooX,
                ref TargetPoint3ToolCooY,
                ref TargetPoint3ToolCooZ);

            MathFun.point3dtrans(temp,
                ref TargetPoint4ToolCooX,
                ref TargetPoint4ToolCooY,
                ref TargetPoint4ToolCooZ);

            MathFun.point3dtrans(temp,
                ref TargetPoint5ToolCooX,
                ref TargetPoint5ToolCooY,
                ref TargetPoint5ToolCooZ);

            MathFun.point3dtrans(temp,
                ref TargetPoint6ToolCooX,
                ref TargetPoint6ToolCooY,
                ref TargetPoint6ToolCooZ);


            //将机构坐标系中的盒子上的点赋值到Point3D的成员中
            TargetPoint1ToolCoo.X = TargetPoint1ToolCooX;
            TargetPoint1ToolCoo.Y = TargetPoint1ToolCooY;
            TargetPoint1ToolCoo.Z = TargetPoint1ToolCooZ;

            TargetPoint2ToolCoo.X = TargetPoint2ToolCooX;
            TargetPoint2ToolCoo.Y = TargetPoint2ToolCooY;
            TargetPoint2ToolCoo.Z = TargetPoint2ToolCooZ;

            TargetPoint3ToolCoo.X = TargetPoint3ToolCooX;
            TargetPoint3ToolCoo.Y = TargetPoint3ToolCooY;
            TargetPoint3ToolCoo.Z = TargetPoint3ToolCooZ;

            TargetPoint4ToolCoo.X = TargetPoint4ToolCooX;
            TargetPoint4ToolCoo.Y = TargetPoint4ToolCooY;
            TargetPoint4ToolCoo.Z = TargetPoint4ToolCooZ;

            TargetPoint5ToolCoo.X = TargetPoint5ToolCooX;
            TargetPoint5ToolCoo.Y = TargetPoint5ToolCooY;
            TargetPoint5ToolCoo.Z = TargetPoint5ToolCooZ;

            TargetPoint6ToolCoo.X = TargetPoint6ToolCooX;
            TargetPoint6ToolCoo.Y = TargetPoint6ToolCooY;
            TargetPoint6ToolCoo.Z = TargetPoint6ToolCooZ;


            TargetPointArrayToolCoo[0] = TargetPoint1ToolCoo;
            TargetPointArrayToolCoo[1] = TargetPoint2ToolCoo;
            TargetPointArrayToolCoo[2] = TargetPoint3ToolCoo;
            TargetPointArrayToolCoo[3] = TargetPoint4ToolCoo;
            TargetPointArrayToolCoo[4] = TargetPoint5ToolCoo;
            TargetPointArrayToolCoo[5] = TargetPoint6ToolCoo;



        }

        /// <summary>
        /// 总转换矩阵，从盒子坐标系转换到工具坐标系
        /// </summary>
        /// <returns></returns>
        public static vtkMatrix4x4 TransMatrixBox2Tool()
        {
            vtkMatrix4x4 VTKMatrixRight0 = new vtkMatrix4x4();
            //读取NDI位姿
            //盒子到NDI
            Vector3D trans2 = new Vector3D(NDI.trackdata[2 * 9 + 6], NDI.trackdata[2 * 9 + 7], NDI.trackdata[2 * 9 + 8]);
            System.Windows.Media.Media3D.Quaternion qua2 = new System.Windows.Media.Media3D.Quaternion(NDI.trackdata[2 * 9 + 3], NDI.trackdata[2 * 9 + 4], NDI.trackdata[2 * 9 + 5], NDI.trackdata[2 * 9 + 2]);
            Matrix3D matrixRight = MathFun.Quat2trans(trans2, qua2);
            VTKMatrixRight0 = MathFun.Matrix3D2VTKMatrix(matrixRight);

            //Tool到NDI
            Vector3D trans = new Vector3D(NDI.trackdata[0 * 9 + 6], NDI.trackdata[0 * 9 + 7], NDI.trackdata[0 * 9 + 8]);
            System.Windows.Media.Media3D.Quaternion qua = new System.Windows.Media.Media3D.Quaternion(NDI.trackdata[0 * 9 + 3], NDI.trackdata[0 * 9 + 4], NDI.trackdata[0 * 9 + 5], NDI.trackdata[0 * 9 + 2]);
            Matrix3D matrix1 = MathFun.Quat2trans(trans, qua);
            vtkMatrix4x4 toolVTKMatrix = MathFun.Matrix3D2VTKMatrix(matrix1);
            //求逆矩阵，使得新矩阵表达从NDI到Tool
            vtkMatrix4x4 temp = new vtkMatrix4x4();
            temp.DeepCopy(toolVTKMatrix);
            temp.Invert();

            //总转换矩阵
            vtkMatrix4x4 TransMatrixOut = new vtkMatrix4x4();
            vtkMatrix4x4.Multiply4x4(temp, VTKMatrixRight0, TransMatrixOut);

            return TransMatrixOut;
        }


        /// <summary>
        /// 总转换矩阵，从工具坐标系转换到盒子坐标系
        /// </summary>
        /// <returns></returns>
        public static vtkMatrix4x4 TransMatrixTool2Box()
        {
            vtkMatrix4x4 VTKMatrixRight0 = new vtkMatrix4x4();
            //读取NDI位姿
            //Tool到NDI
            Vector3D trans2 = new Vector3D(NDI.trackdata[0 * 9 + 6], NDI.trackdata[0 * 9 + 7], NDI.trackdata[0 * 9 + 8]);
            System.Windows.Media.Media3D.Quaternion qua2 = new System.Windows.Media.Media3D.Quaternion(NDI.trackdata[0 * 9 + 3], NDI.trackdata[0 * 9 + 4], NDI.trackdata[0 * 9 + 5], NDI.trackdata[0 * 9 + 2]);
            Matrix3D matrixRight = MathFun.Quat2trans(trans2, qua2);
            VTKMatrixRight0 = MathFun.Matrix3D2VTKMatrix(matrixRight);

            //盒子到NDI
            Vector3D trans = new Vector3D(NDI.trackdata[2 * 9 + 6], NDI.trackdata[2 * 9 + 7], NDI.trackdata[2 * 9 + 8]);
            System.Windows.Media.Media3D.Quaternion qua = new System.Windows.Media.Media3D.Quaternion(NDI.trackdata[2 * 9 + 3], NDI.trackdata[2 * 9 + 4], NDI.trackdata[2 * 9 + 5], NDI.trackdata[2 * 9 + 2]);
            Matrix3D matrix1 = MathFun.Quat2trans(trans, qua);
            vtkMatrix4x4 toolVTKMatrix = MathFun.Matrix3D2VTKMatrix(matrix1);
            //求逆矩阵，使得新矩阵表达从NDI到盒子
            vtkMatrix4x4 temp = new vtkMatrix4x4();
            temp.DeepCopy(toolVTKMatrix);
            temp.Invert();

            //总转换矩阵
            vtkMatrix4x4 TransMatrixOut = new vtkMatrix4x4();
            vtkMatrix4x4.Multiply4x4(temp, VTKMatrixRight0, TransMatrixOut);

            return TransMatrixOut;
        }
    }
}
