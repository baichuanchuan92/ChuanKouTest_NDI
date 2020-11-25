using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Kitware.VTK;
using System.Windows.Media.Media3D;

namespace ChuanKouTest
{
    /// <summary>
    /// Optitrack的参数和函数，应和NDI相对应
    /// 为x86的
    /// </summary>
    public class Optitrack
    {
        private const string DLLName = "NPTrackingTools.dll";
        /// <summary>
        /// 初始化
        /// </summary>
        [DllImport(DLLName, EntryPoint = "TT_Initialize", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TT_Initialize();
        /// <summary>
        /// 更新数据流
        /// </summary>
        [DllImport(DLLName, EntryPoint = "TT_Update", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TT_Update();
        /// <summary>
        /// 读取标定文件
        /// </summary>
        [DllImport(DLLName, EntryPoint = "TT_LoadProfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TT_LoadProfile(string fileName);
        [DllImport(DLLName, EntryPoint = "TT_LoadRigidBodies", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TT_LoadRigidBodies(string fileName);
        [DllImport(DLLName, EntryPoint = "TT_AddRigidBodies", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TT_AddRigidBodies(string fileName);
        /// <summary>
        /// 刚体位置获取
        /// </summary>
        [DllImport(DLLName, EntryPoint = "TT_RigidBodyLocation", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TT_RigidBodyLocation(int rbIndex, ref float x, ref float y, ref float z, ref float qx, ref float qy, ref float qz, ref float qw, ref float yaw, ref float pitch, ref float roll);
        /// <summary>
        /// 确定刚体的目标数
        /// </summary>
        [DllImport(DLLName, EntryPoint = "TT_RigidBodyCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TT_RigidBodyCount();
        /// <summary>
        /// 是否跟踪到
        /// </summary>
        [DllImport(DLLName, EntryPoint = "TT_IsRigidBodyTracked", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TT_IsRigidBodyTracked(int rbIndex);
        /// <summary>
        /// 获得Marker（Marker指的是那几个小球）数据
        /// </summary>
        [DllImport(DLLName, EntryPoint = "TT_RigidBodyPointCloudMarker", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TT_RigidBodyPointCloudMarker(int rbIndex, int markerIndex, ref bool tracked, ref float x, ref float y, ref float z);
        /// <summary>
        /// 关闭所有连接的摄像头
        /// </summary>
        [DllImport(DLLName, EntryPoint = "TT_Shutdown", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TT_Shutdown();
        /// <summary>
        /// Saves 3D coordinates of a solved rigid body marker in respect to respective rigid body's local space.
        /// </summary>
        [DllImport(DLLName, EntryPoint = "TT_RigidBodyMarker", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TT_RigidBodyMarker(int rbIndex, int markerIndex, ref float x,ref float y, ref float z);
        /// <summary>
        /// Changes and updates the rigid body marker positions.
        /// </summary>
        [DllImport(DLLName, EntryPoint = "TT_RigidBodyUpdateMarker", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TT_RigidBodyUpdateMarker(int rbIndex, int markerIndex, ref float x, ref float y, ref float z);
        /// <summary>
        /// Gets total number of reconstruected markers in a frame.
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLName, EntryPoint = "TT_FrameMarkerCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TT_FrameMarkerCount();
        /// <summary>
        /// Returns x-position of a reconstructed marker.
        /// </summary>
        /// <param name="markerIndex"></param>
        /// <returns></returns>
        [DllImport(DLLName, EntryPoint = "TT_FrameMarkerX", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern float TT_FrameMarkerX(int markerIndex);
        /// <summary>
        /// Returns y-position of a reconstructed marker.
        /// </summary>
        /// <param name="markerIndex"></param>
        /// <returns></returns>
        [DllImport(DLLName, EntryPoint = "TT_FrameMarkerY", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern float TT_FrameMarkerY(int markerIndex);
        /// <summary>
        /// Returns z-position of a reconstructed marker.
        /// </summary>
        /// <param name="markerIndex"></param>
        /// <returns></returns>
        [DllImport(DLLName, EntryPoint = "TT_FrameMarkerZ", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern float TT_FrameMarkerZ(int markerIndex);









        //TODO:使用记事本导入
        //格式为.tra
        //TODO：后期仔细标定，重新导入，目前有针和机构的，机构的index为0，针的index为1,骨盆的index为2
        //针
        public static string ToolPathNeedle = @"C:\Users\giuyyt\Desktop\ZHENTracker.tra";
        //机构
        public static string ToolPathMechanism = @"C:\Users\giuyyt\Desktop\JGTracker.tra";
        //骨盆
        public static string ToolPathPelvis = @"C:\Users\giuyyt\Desktop\GUPEN3Tracker.tra";


        //多线程用
        public static Thread OptitrackThread;

        //记录追踪的数据
        public static double[] OptitrackData = new double[100];//3*24+7

        //委托
        public delegate void ShowOptitrackData(double[] OptitrackData);

        //m换算成mm
        //OT自带长度单位为m
        public const int CONVERT2MM = 1000;

        const int DIM = 4;



        //根据matlab三矢量定姿获得，从OT自带坐标系转换到和NDI的.rom对应的坐标系
        //这里的变量名中提到inverse的,在matlab中使用三矢量定姿求出Matrix后需要inv()处理再导入c#
        public static double[,] TransMatrixMechanismOTDefault2NDIROMInverse = 
            { { 0.3448,-0.8862,0.3095,70.3914 }, 
            { 0.0746,-0.3028,-0.9501,-0.1972 }, 
            { 0.9357,0.3507,-0.0383,-0.3774 }, 
            { 0, 0, 0, 1 } };

        public static double[,] TransMatrixNeedleOTDefault2NDIROM =
            { { -0.2654,0.9635,0.0354,-4.2657 },
            { -0.5952,-0.1348,-0.7922,30.7202 },
            { -0.7585,-0.2313,0.6092,-22.6904 },
            { 0, 0, 0, 1 } };

        public static double[,] TransMatrixPelvisOTDefault2NDIROM =
            { { 0.9679,-0.2216,0.1188,-39.0618 },
            {  0.2373,0.6490,-0.7229,-8.4122 },
            { 0.0831,0.7278,0.6807,-2.3385 },
            { 0, 0, 0, 1 } };



        /// <summary>
        /// Optitrack初始化，加载.tra文件，同时开始追踪
        /// </summary>
        public static void OptitrackInit()
        {
            TT_Initialize();
            TT_Update();
            //导入,第一个.tra用Load，后面的用add
            TT_LoadRigidBodies(ToolPathMechanism);
            TT_AddRigidBodies(ToolPathNeedle);
            TT_AddRigidBodies(ToolPathPelvis);


        }


        /// <summary>
        /// Optitrack刷新数据
        /// </summary>
        public static void Updata()
        {


            float x=0, y=0, z=0, qx=0, qy=0, qz=0, qw=0, yaw=0, pitch=0, roll=0;
            //Marker坐标
            float X1=0, Y1=0, Z1=0;
            float X2=0, Y2=0, Z2=0;
            float X3=0, Y3=0, Z3=0;
            float X4=0, Y4=0, Z4=0;
            bool IfTracked1 = false, 
                    IfTracked2 = false, 
                    IfTracked3 = false, 
                    IfTracked4 = false;
            bool TrackeMotive = false;



            int rbcount = TT_RigidBodyCount();//最多3个
            int i = 0;
            //遍历.tra
            for (i = 0; i < rbcount; i++)
            {

                TT_Update();

                TrackeMotive = TT_IsRigidBodyTracked(i);

                TT_RigidBodyLocation(i, ref x, ref y, ref z, ref qx, ref qy, ref qz, ref qw, ref yaw, ref pitch, ref roll);

                TT_RigidBodyPointCloudMarker(i, 0, ref IfTracked1, ref X1, ref Y1, ref Z1);
                TT_RigidBodyPointCloudMarker(i, 1, ref IfTracked2, ref X2, ref Y2, ref Z2);
                TT_RigidBodyPointCloudMarker(i, 2, ref IfTracked3, ref X3, ref Y3, ref Z3);
                TT_RigidBodyPointCloudMarker(i, 3, ref IfTracked4, ref X4, ref Y4, ref Z4);
                //TT_RigidBodyMarker(i, 0, ref X1, ref Y1, ref Z1);
                //TT_RigidBodyMarker(i, 1, ref X2, ref Y2, ref Z2);
                //TT_RigidBodyMarker(i, 2, ref X3, ref Y3, ref Z3);
                //TT_RigidBodyMarker(i, 3, ref X4, ref Y4, ref Z4);

                x *= CONVERT2MM;
                y *= CONVERT2MM;
                z *= CONVERT2MM;

                X1*= CONVERT2MM;
                Y1*= CONVERT2MM;
                Z1*= CONVERT2MM;

                X2 *= CONVERT2MM;
                Y2 *= CONVERT2MM;
                Z2 *= CONVERT2MM;

                X3 *= CONVERT2MM;
                Y3 *= CONVERT2MM;
                Z3 *= CONVERT2MM;

                X4 *= CONVERT2MM;
                Y4 *= CONVERT2MM;
                Z4 *= CONVERT2MM;










             
               
                OptitrackData[0 + 24 * i] = x;
                OptitrackData[1 + 24 * i] = y;
                OptitrackData[2 + 24 * i] = z;
                OptitrackData[3 + 24 * i] = qx;
                OptitrackData[4 + 24 * i] = qy;
                OptitrackData[5 + 24 * i] = qz;
                OptitrackData[6 + 24 * i] = qw;
                OptitrackData[7 + 24 * i] = Convert.ToDouble(TrackeMotive);

                OptitrackData[8 + 24 * i] = X1;
                OptitrackData[9 + 24 * i] = Y1;
                OptitrackData[10 + 24 * i] = Z1;
                OptitrackData[11 + 24 * i] = Convert.ToDouble(IfTracked1);

                OptitrackData[12 + 24 * i] = X2;
                OptitrackData[13 + 24 * i] = Y2;
                OptitrackData[14 + 24 * i] = Z2;
                OptitrackData[15 + 24 * i] = Convert.ToDouble(IfTracked2);

                OptitrackData[16 + 24 * i] = X3;
                OptitrackData[17 + 24 * i] = Y3;
                OptitrackData[18 + 24 * i] = Z3;
                OptitrackData[19 + 24 * i] = Convert.ToDouble(IfTracked3);

                OptitrackData[20 + 24 * i] = X4;
                OptitrackData[21 + 24 * i] = Y4;
                OptitrackData[22 + 24 * i] = Z4;
                OptitrackData[23 + 24 * i] = Convert.ToDouble(IfTracked4);



                //MathFun.point3dtrans(temp, ref OptitrackData[8 + 24 * i], ref OptitrackData[9 + 24 * i], ref OptitrackData[10 + 24 * i]);
                //MathFun.point3dtrans(temp, ref OptitrackData[12 + 24 * i], ref OptitrackData[13 + 24 * i], ref OptitrackData[14 + 24 * i]);
                //MathFun.point3dtrans(temp, ref OptitrackData[16 + 24 * i], ref OptitrackData[17 + 24 * i], ref OptitrackData[18 + 24 * i]);
                //MathFun.point3dtrans(temp, ref OptitrackData[20 + 24 * i], ref OptitrackData[21 + 24 * i], ref OptitrackData[22 + 24 * i]);



            }

            //扫描散点
            int FrameMarkerCount = TT_FrameMarkerCount()-8;
            int j = 0;
            int k = 8;
            OptitrackData[72] = FrameMarkerCount;
            for (j = 0; j < FrameMarkerCount; j++) {
                OptitrackData[73 + 3 * j] = TT_FrameMarkerX(j+k);
                OptitrackData[74 + 3 * j] = TT_FrameMarkerY(j+k);
                OptitrackData[75 + 3 * j] = TT_FrameMarkerZ(j+k);
            }



        }


        /// <summary>
        /// 将目前OT的内置数据(.tra)的路径导出成txt
        /// </summary>
        public static void OTParamsExport() {
            string pathnow = Directory.GetCurrentDirectory();
            //写入.tra文件
            //分成两行，分别是：机构、针
            using (StreamWriter WrTra = new StreamWriter(pathnow + @"\OTtra.txt", false, System.Text.Encoding.GetEncoding("GB2312")))
            {
                WrTra.WriteLine("机构的.tra绝对地址::"+ToolPathMechanism);
                WrTra.WriteLine("针的.tra绝对地址::" + ToolPathNeedle);
                WrTra.Close();
            }
            //写入机构的.tra的点的相对坐标
            using (StreamWriter WrTraCooMechanism = new StreamWriter(pathnow + @"\OTtraCooMechanism.txt", false, System.Text.Encoding.GetEncoding("GB2312"))) {
                int i = 0;
                WrTraCooMechanism.WriteLine("P1::"
                    + OptitrackData[8 + 24 * i]
                    + ","
                    + OptitrackData[9 + 24 * i]
                    + ","
                    + OptitrackData[10 + 24 * i]);

                WrTraCooMechanism.WriteLine("P2::"
                    + OptitrackData[12 + 24 * i]
                    + ","
                    + OptitrackData[13 + 24 * i]
                    + ","
                    + OptitrackData[14 + 24 * i]);

                WrTraCooMechanism.WriteLine("P3::"
                    + OptitrackData[16 + 24 * i]
                    + ","
                    + OptitrackData[17 + 24 * i]
                    + ","
                    + OptitrackData[18 + 24 * i]);

                WrTraCooMechanism.WriteLine("P4::"
                    + OptitrackData[20 + 24 * i]
                    + ","
                    + OptitrackData[21 + 24 * i]
                    + ","
                    + OptitrackData[22 + 24 * i]);

                WrTraCooMechanism.Close();
            }
            //写入针的.tra的点的相对坐标
            using (StreamWriter WrTraCooNeedle = new StreamWriter(pathnow + @"\OTtraCooNeedle.txt", false, System.Text.Encoding.GetEncoding("GB2312")))
            {
                int i = 1;
                WrTraCooNeedle.WriteLine("P1::" 
                    + OptitrackData[8 + 24 * i]
                    +","
                    + OptitrackData[9 + 24 * i]
                    + ","
                    + OptitrackData[10 + 24 * i]);

                WrTraCooNeedle.WriteLine("P2::"
                    + OptitrackData[12 + 24 * i]
                    + ","
                    + OptitrackData[13 + 24 * i]
                    + ","
                    + OptitrackData[14 + 24 * i]);

                WrTraCooNeedle.WriteLine("P3::"
                    + OptitrackData[16 + 24 * i]
                    + ","
                    + OptitrackData[17 + 24 * i]
                    + ","
                    + OptitrackData[18 + 24 * i]);

                WrTraCooNeedle.WriteLine("P4::"
                    + OptitrackData[20 + 24 * i]
                    + ","
                    + OptitrackData[21 + 24 * i]
                    + ","
                    + OptitrackData[22 + 24 * i]);



                WrTraCooNeedle.Close();
            }
            //写入骨盆的.tra的点相对坐标
            using (StreamWriter WrTraCooPelvis = new StreamWriter(pathnow + @"\OTtraCooPelvis.txt", false, System.Text.Encoding.GetEncoding("GB2312")))
            {
                int i = 2;
                WrTraCooPelvis.WriteLine("P1::"
                    + OptitrackData[8 + 24 * i]
                    + ","
                    + OptitrackData[9 + 24 * i]
                    + ","
                    + OptitrackData[10 + 24 * i]);

                WrTraCooPelvis.WriteLine("P2::"
                    + OptitrackData[12 + 24 * i]
                    + ","
                    + OptitrackData[13 + 24 * i]
                    + ","
                    + OptitrackData[14 + 24 * i]);

                WrTraCooPelvis.WriteLine("P3::"
                    + OptitrackData[16 + 24 * i]
                    + ","
                    + OptitrackData[17 + 24 * i]
                    + ","
                    + OptitrackData[18 + 24 * i]);

                WrTraCooPelvis.WriteLine("P4::"
                    + OptitrackData[20 + 24 * i]
                    + ","
                    + OptitrackData[21 + 24 * i]
                    + ","
                    + OptitrackData[22 + 24 * i]);



                WrTraCooPelvis.Close();
            }
            //转换矩阵，机构
            using (StreamWriter WrMatrixMachanism = new StreamWriter(pathnow + @"\OTMatrixMachanism.txt", false, System.Text.Encoding.GetEncoding("GB2312")))
            {
                IOFun.MatrixOutput(TransMatrixMechanismOTDefault2NDIROMInverse, DIM, DIM, WrMatrixMachanism);
            }
            //转换矩阵，骨盆
            using (StreamWriter WrMatrixPelvis = new StreamWriter(pathnow + @"\OTMatrixPelvis.txt", false, System.Text.Encoding.GetEncoding("GB2312")))
            {
                IOFun.MatrixOutput(TransMatrixPelvisOTDefault2NDIROM, DIM, DIM, WrMatrixPelvis);
            }
            //转换矩阵，针
            using (StreamWriter WrMatrixNeedle = new StreamWriter(pathnow + @"\OTMatrixNeedle.txt", false, System.Text.Encoding.GetEncoding("GB2312")))
            {
                IOFun.MatrixOutput(TransMatrixNeedleOTDefault2NDIROM, DIM, DIM, WrMatrixNeedle);
            }
        }


        /// <summary>
        /// 导入.tra文件以及转换矩阵
        /// </summary>
        public static void OTParamsImport() {
            string pathnow = Directory.GetCurrentDirectory();
            //分成两行，分别是：机构、针
            using (StreamReader ReTra = new StreamReader(pathnow + @"\OTtra.txt"))
            {
                string Line;
                string[] PathTemp;
                //string[] ddd = ccc[i].Split(new string[] { "," }, StringSplitOptions.None);
                int Index = 0;
                // Read and display lines from
                //the file until the end of
                // the file is reached.
                while ((Line = ReTra.ReadLine()) != null)
                {
                    //Index=0,机构
                    if (Index == 0)
                    {
                        PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                        ToolPathMechanism = PathTemp[1];
                        MessageBox.Show(ToolPathMechanism, "机构");
                    }

                    //Index=1,针
                    else if (Index == 1)
                    {
                        PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                        ToolPathNeedle = PathTemp[1];
                        MessageBox.Show(ToolPathNeedle, "针");
                    }

                    

                    else { }

                    Index++;
                }


                ReTra.Close();
            }

            using (StreamReader ReMatrixMachanism = new StreamReader(pathnow + @"\OTMatrixMachanism.txt"))
            {
                IOFun.MatrixInput(ref TransMatrixMechanismOTDefault2NDIROMInverse, DIM, DIM, ReMatrixMachanism);
            }

            using (StreamReader ReMatrixPelvis = new StreamReader(pathnow + @"\OTMatrixPelvis.txt"))
            {
                IOFun.MatrixInput(ref TransMatrixPelvisOTDefault2NDIROM, DIM, DIM, ReMatrixPelvis);
            }

            using (StreamReader ReMatrixNeedle = new StreamReader(pathnow + @"\OTMatrixNeedle.txt"))
            {
                IOFun.MatrixInput(ref TransMatrixNeedleOTDefault2NDIROM, DIM, DIM, ReMatrixNeedle);
            }


        }


        /// <summary>
        /// OT下转换任意一个相机坐标系下的点的坐标到OT的tracker坐标系下
        /// 用于机构
        /// 分为两步
        /// 1.乘以OT自带的Matrix
        /// 2.乘以matlab的Matrix
        /// </summary>
        public static void OTCooTransformMechanism(ref double XL,ref double YL,ref double ZL) {
            const int i = 0;
            //乘以OT自带的Matrix，从相机到机构
            //方式和NDI相同，根据7个数构造出转换矩阵
            Vector3D trans = new Vector3D(
                OptitrackData[0 + 24 * i],
                OptitrackData[1 + 24 * i],
                OptitrackData[2 + 24 * i]);
            System.Windows.Media.Media3D.Quaternion qua = new System.Windows.Media.Media3D.Quaternion(
                OptitrackData[3 + 24 * i],
                OptitrackData[4 + 24 * i],
                OptitrackData[5 + 24 * i],
                OptitrackData[6 + 24 * i]);
            Matrix3D matrix1 = MathFun.Quat2trans(trans, qua);
            vtkMatrix4x4 toolVTKMatrix = MathFun.Matrix3D2VTKMatrix(matrix1);

            vtkMatrix4x4 temp = new vtkMatrix4x4();
            temp.DeepCopy(toolVTKMatrix);
            temp.Invert();

            MathFun.point3dtrans(temp, ref XL, ref YL, ref ZL);//将对应点转换到TOOL坐标系中


            //乘以matlab中三矢量定姿已经算好的矩阵
            //类似于机构中全局和局部的相乘方式
            double[] VectorTransInTemp = new double[4];
            double[] VectorTransOutTemp = new double[4];

            //VTIT初始化
            VectorTransInTemp[0] = XL;
            VectorTransInTemp[1] = YL;
            VectorTransInTemp[2] = ZL;
            VectorTransInTemp[3] = 1;


            //相乘
            MathFun.MatrixVectorMul(TransMatrixMechanismOTDefault2NDIROMInverse, 
                VectorTransInTemp, 
                out VectorTransOutTemp);

            //赋值回X,Y,Z，结束
            XL = VectorTransOutTemp[0];
            YL = VectorTransOutTemp[1];
            ZL = VectorTransOutTemp[2];

        }




        /// <summary>
        /// OT下转换任意一个相机坐标系下的点的坐标到OT的tracker坐标系下
        /// 用于针
        /// 分为两步
        /// 1.乘以matlab的Matrix
        /// 2.乘以OT自带的Matrix(不用invert)
        /// </summary>
        public static void OTCooTransformNeedle(ref double XL, ref double YL, ref double ZL) {

            //乘以matlab中三矢量定姿已经算好的矩阵
            //类似于机构中全局和局部的相乘方式
            double[] VectorTransInTemp = new double[4];
            double[] VectorTransOutTemp = new double[4];

            //VTIT初始化
            VectorTransInTemp[0] = XL;
            VectorTransInTemp[1] = YL;
            VectorTransInTemp[2] = ZL;
            VectorTransInTemp[3] = 1;


            //相乘
            MathFun.MatrixVectorMul(TransMatrixNeedleOTDefault2NDIROM,
                VectorTransInTemp,
                out VectorTransOutTemp);

            //赋值回X,Y,Z，结束
            XL = VectorTransOutTemp[0];
            YL = VectorTransOutTemp[1];
            ZL = VectorTransOutTemp[2];





            //乘以OT自带的Matrix，从针到相机
            //方式和NDI相同，根据7个数构造出转换矩阵
            const int i = 1;
            Vector3D trans = new Vector3D(
                OptitrackData[0 + 24 * i],
                OptitrackData[1 + 24 * i],
                OptitrackData[2 + 24 * i]);
            System.Windows.Media.Media3D.Quaternion qua = new System.Windows.Media.Media3D.Quaternion(
                OptitrackData[3 + 24 * i],
                OptitrackData[4 + 24 * i],
                OptitrackData[5 + 24 * i],
                OptitrackData[6 + 24 * i]);
            Matrix3D matrix1 = MathFun.Quat2trans(trans, qua);
            vtkMatrix4x4 toolVTKMatrix = MathFun.Matrix3D2VTKMatrix(matrix1);


            MathFun.point3dtrans(toolVTKMatrix, ref XL, ref YL, ref ZL);//将对应点转换到相机坐标系中

        }



        /// <summary>
        /// OT下转换任意一个相机坐标系下的点的坐标到OT的tracker坐标系下
        /// 用于骨盆
        /// 分为两步
        /// 1.乘以matlab的Matrix
        /// 2.乘以OT自带的Matrix(不用invert) 
        /// </summary>
        /// <param name="XL"></param>
        /// <param name="YL"></param>
        /// <param name="ZL"></param>
        public static void OTCooTransformPelvis(ref double XL, ref double YL, ref double ZL) {
            //乘以matlab中三矢量定姿已经算好的矩阵
            //类似于机构中全局和局部的相乘方式
            double[] VectorTransInTemp = new double[4];
            double[] VectorTransOutTemp = new double[4];

            //VTIT初始化
            VectorTransInTemp[0] = XL;
            VectorTransInTemp[1] = YL;
            VectorTransInTemp[2] = ZL;
            VectorTransInTemp[3] = 1;


            //相乘
            MathFun.MatrixVectorMul(TransMatrixPelvisOTDefault2NDIROM,
                VectorTransInTemp,
                out VectorTransOutTemp);

            //赋值回X,Y,Z，结束
            XL = VectorTransOutTemp[0];
            YL = VectorTransOutTemp[1];
            ZL = VectorTransOutTemp[2];





            //乘以OT自带的Matrix，从针到相机
            //方式和NDI相同，根据7个数构造出转换矩阵
            const int i = 2;
            Vector3D trans = new Vector3D(
                OptitrackData[0 + 24 * i],
                OptitrackData[1 + 24 * i],
                OptitrackData[2 + 24 * i]);
            System.Windows.Media.Media3D.Quaternion qua = new System.Windows.Media.Media3D.Quaternion(
                OptitrackData[3 + 24 * i],
                OptitrackData[4 + 24 * i],
                OptitrackData[5 + 24 * i],
                OptitrackData[6 + 24 * i]);
            Matrix3D matrix1 = MathFun.Quat2trans(trans, qua);
            vtkMatrix4x4 toolVTKMatrix = MathFun.Matrix3D2VTKMatrix(matrix1);


            MathFun.point3dtrans(toolVTKMatrix, ref XL, ref YL, ref ZL);//将对应点转换到相机坐标系中
        }









    }
}
