using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Media.Media3D;

using Excel = Microsoft.Office.Interop.Excel;
using Missing = System.Reflection.Missing;



namespace ChuanKouTest
{
    public partial class chuankou : Form
    {

        [DllImport("NDIVegaLsbt_new64.dll", EntryPoint = "NDIVegaConnect", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int NDIVegaConnect([MarshalAs(UnmanagedType.LPStr)] string addr);

        [DllImport("NDIVegaLsbt_new64.dll", EntryPoint = "loadPassiveTools", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void loadPassiveTools([MarshalAs(UnmanagedType.LPStr)] string path);

        [DllImport("NDIVegaLsbt_new64.dll", EntryPoint = "startTracking", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void startTracking();

        [DllImport("NDIVegaLsbt_new64.dll", EntryPoint = "stopTracking", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void stopTracking();

        //[DllImport("NDIVegaLsbt_new64.dll", EntryPoint = "getTransformData", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        // public static extern void getTransformData(ref double transformdata);

        [DllImport("NDIVegaLsbt_new64.dll", EntryPoint = "printTrackingNDIData", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void printTrackingNDIData();

        [DllImport("NDIVegaLsbt_new64.dll", EntryPoint = "getTransformData_Ne", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void getTransformData_Ne(ref double transformdata, UInt16 options);





        //一些常量
        const byte FrameHead1 = 0x55;
        const byte FrameHead2 = 0xAA;
        const UInt16 MotorMaxNum = 2000;//电机最大长度
        const double MotorMaxLength = 16;//电机最大长度（mm）






        SerialPort sp = null;
        bool isOpen = false;
        bool isSetProperty = false;
        bool isHex = false;

        byte[] CommandArray = new byte[18];


        //空间点的坐标，两个空间点确定一根针
        double x1Ned = 0, y1Ned = 0, z1Ned = 0;//点1的空间坐标，将从对话框中获得
        double x2Ned = 0, y2Ned = 0, z2Ned = 0;//点2的空间坐标，将从对话框中获得



        //以下为机构的参数，通过matlab计算得到，直接输入进来


        /********************************************************/
        //机构1的参数，z约为24左右
        const double m1 = 50;//两个电机旋转中心的距离，应为常数
        double l1 = 0;//短边长度
        const double l1Ori = 75.14;//短边基准长度（电机伸长量为0),应为常数
        double t1 = 0;//长边从旋转中心到两边交点的距离
        const double t1Ori = 75;//长边基准长度（电机伸长量为0),应为常数
        const double b1 = 68.4;//长边多出来的距离，应为常数

        double xl_1 = -70, yl_1 = 90, zl_1 = 0;//端点的局部坐标，希望到达的点
        double xg_1 = 0, yg_1 = 0, zg_1 = 0;//端点的全局坐标，希望到达的点

        const double xc1 = -42.9103, yc1 = 137.2485, zc1 = 0;//规定的工作空间的圆心，在约束工作空间时使用，为常量
        const double rc1 = 9999;//规定的工作空间的半径，在约束工作空间时使用，为常量(8)
        /// <summary>
        /// //TODO：定义现实到达的全局坐标（NDI得到）和局部坐标（全局坐标变换后得到）
        /// </summary>
        

        const double aPlane1 = -0.0189, bPlane1 = -0.0385, cPlane1 = -0.9991, dPlane1 = 23.6873;//平面机构1所在平面的系数：ax+by+cz+d=0，应为常数
        readonly double[,] TransMatrix1 = { { -0.9936, -0.0361, 0.2914, 53.5089 }, { 0.0249, -1.0013, -0.0857, 14.6336 }, { -0.0000, 0.0000, 1.0000, 25.9990 }, { 0, 0, 0, 1 } };
        //从全局到局部的坐标变换矩阵，左乘,[Row行,Colomn列]，应为常数

        readonly double[,] TransMatrixInv1;
        //从局部到全局的坐标变换矩阵，左乘,[Row行,Colomn列]，应为常数


        /********************************************************/
        //机构2的参数，z约为70左右
        const double m2 = 5;//两个电机旋转中心的距离，应为常数
        double l2 = 0;//短边长度
        const double l2Ori = 0;//短边基准长度（电机伸长量为0),应为常数
        double t2 = 0;//长边从旋转中心到两边交点的距离
        const double t2Ori = 0;//长边基准长度（电机伸长量为0),应为常数
        const double b2 = 62;//长边多出来的距离，应为常数

        double xl_2 = -7, yl_2 = 90, zl_2 = 0;//端点的局部坐标，希望到达的点
        double xg_2 = 0, yg_2 = 0, zg_2 = 0;//端点的全局坐标，希望到达的点

        const double xc2 = 0, yc2 = 0, zc2 = 0;//规定的工作空间的圆心，在约束工作空间时使用，为常量
        const double rc2 = 0;//规定的工作空间的半径，在约束工作空间时使用，为常量

        const double aPlane2 = -0.0189, bPlane2 = -0.0385, cPlane2 = -0.9991, dPlane2 = 23.6873;//平面机构2所在平面的系数：ax+by+cz+d=0，应为常数
        readonly double[,] TransMatrix2 = new double[4, 4] { { -0.9938, -0.0410, -0.1524, 34.7094 }, { 0.0253, -0.9973, -0.6455, -31.3819 }, { -0.0000, 0.0000, 1.0000, 75.6005 }, { 0, 0, 0, 1 } };
        //从全局到局部的坐标变换矩阵，左乘,[Row行,Colomn列]，应为常数

        readonly double[,] TransMatrixInv2;
        //从局部到全局的坐标变换矩阵，左乘,[Row行,Colomn列]，应为常数


        /******************************************/
        //以下为一些中间变量的初始化

        //定义向量来记录下坐标变换前和后的坐标信息
        //double[] VectorTransOut = new double[4];
        //double[] VectorTransIn = new double[4];

        //记录希望到达的点和工作空间的圆心的距离
        //double DisP = 0;

        /***************************/
        //NDI通讯相关
        double[] trackdata = new double[20];
        private Thread NDIThread;
        private Thread ExpeThread;
        private delegate void ShowTrackData(double[] trackData);
        private delegate void NDIExpe(double[] trackData);
        bool needTrack = true;
        string StrData = "23";
        double[] MarkerData = new double[6];
        UInt16 options = 0x0001 | 0x1000 | 0x0800;
        string hostname = "COM3";
        string ToolPath1 = "D://大三上//小并联结构//控制//20191021_340.rom";










        private void MatrixVectorMul(double[,]Matrix,double[]Vector,out double[]VectorOut){
            //表示矩阵左乘向量，用在坐标变换之时
            //VectorOut为输出的向量
            VectorOut = new double[4];
            int i = 0;
            int j = 0;
            for (i = 0; i < 4; i++) {//表示结果的向量行数
                for (j = 0; j < 4; j++) {//相加四次
                    VectorOut[i] += Matrix[i, j] * Vector[j];
                }
            }
            

        }







        //private void CommandCreate() {
        //    //控制一个电机，广播模式
        //    UInt16 TxLen = 4;
        //    UInt16 ID = (UInt16)ID1.Value;
        //    UInt16 Position = (UInt16)Position1.Value;
        //    UInt16 CheckSum = 0;
        //    int i = 0;
        //    for (i = 2; i < (TxLen + 4); i++) {
        //        CheckSum += Convert.ToUInt16(CommandArray[i]);
        //    }

        //    CommandArray[0] = FrameHead1;
        //    CommandArray[1] = FrameHead2;
        //    CommandArray[2] = (byte)TxLen;//表示帧数据长度
        //    CommandArray[3] = (byte)(ID&0xFF);//ID号
        //    CommandArray[4] = 0x02;//指令类型
        //    CommandArray[5] = 0x37;//控制表索引
        //    CommandArray[6] = (byte)(Position & 0xFF);//数据段 低八位
        //    CommandArray[7] = (byte)((Position>>8) & 0xFF);//数据段 高八位
        //    CommandArray[8] = (byte)(CheckSum&0xFF);//校验和
        //}
        private void CommandBoardCastAuto(UInt16 Position1,UInt16 Position2,UInt16 Position11,UInt16 Position12) {
            //广播模式控制四个电机，不读取四个空格里的数值，可以自由切换
            //与制造通讯内容的函数相比，本函数可以直接自动发送出去


            //控制四个电机，广播模式
            UInt16 TxLen = 1 + 3 * 4;
           

            UInt16[] ID = { (UInt16)1, (UInt16)2, (UInt16)11, (UInt16)12 };
            UInt16[] Position = {Position1,
                Position2,
                Position11,
                Position12};


            UInt16 CheckSum = 0;
            CommandArray[0] = FrameHead1;
            CommandArray[1] = FrameHead2;
            CommandArray[2] = (byte)TxLen;//表示帧数据长度
            CommandArray[3] = 0xFF;//广播ID
            CommandArray[4] = 0xF2;//定位标志
            for (int j = 0; j < 4; j++)
            {
                CommandArray[5 + 3 * j] = (byte)(ID[j] & 0xFF);//ID号
                CommandArray[6 + 3 * j] = (byte)(Position[j] & 0xFF);//数据段 低八位
                CommandArray[7 + 3 * j] = (byte)((Position[j] >> 8) & 0xFF);//数据段 高八位
            }
            int i = 0;
            for (i = 2; i < (TxLen + 4); i++)
            {
                CheckSum += Convert.ToUInt16(CommandArray[i]);
            }
            CommandArray[17] = (byte)(CheckSum & 0xFF);//校验和

            //发送
            if (isOpen)
            {
                try
                {
                    sp.Write(CommandArray, 0, 18);
                    t1 = t1Ori + (Position1 / MotorMaxNum)*MotorMaxLength;
                    l1 = l1Ori + (Position2 / MotorMaxNum) * MotorMaxLength;

                    t2 = t2Ori + (Position11 / MotorMaxNum) * MotorMaxLength;
                    l2 = l2Ori + (Position12 / MotorMaxNum) * MotorMaxLength;
                }
                catch (Exception)
                {
                    MessageBox.Show("发送数据时发生错误", "Error");
                }
            }
            else
            {
                MessageBox.Show("串口未打开", "Error");
            }
        }

        private void CommandCreateBoardcast() {
            //控制四个电机，广播模式
            UInt16 TxLen = 1+3*4;

            UInt16[] ID ={(UInt16)1,(UInt16)2,(UInt16)11,(UInt16)12};
            UInt16[] Position = {(UInt16)ElecMachine1Position.Value,
                (UInt16)ElecMachine2Position.Value,
                (UInt16)ElecMachine11Position.Value,
                (UInt16)ElecMachine12Position.Value};


            UInt16 CheckSum = 0;
            CommandArray[0] = FrameHead1;
            CommandArray[1] = FrameHead2;
            CommandArray[2] = (byte)TxLen;//表示帧数据长度
            CommandArray[3] = 0xFF;//广播ID
            CommandArray[4] = 0xF2;//定位标志
            for (int j = 0; j < 4; j++) {
                CommandArray[5 + 3 * j] = (byte)(ID[j] & 0xFF);//ID号
                CommandArray[6 + 3 * j] = (byte)(Position[j]& 0xFF);//数据段 低八位
                CommandArray[7 + 3 * j] = (byte)((Position[j]>> 8)& 0xFF);//数据段 高八位
            }
            int i = 0;
            for (i = 2; i < (TxLen + 4); i++)
            {
                CheckSum += Convert.ToUInt16(CommandArray[i]);
            }
            CommandArray[17] = (byte)(CheckSum & 0xFF);//校验和
        }

        public chuankou()
        {
            InitializeComponent();
        }


        private void MotorTotalCalGlobal(int Index, double xg, double yg, double zg) {
            //输入要控制的机构，要到达的目标点（全局坐标系）
            //经过比例反馈控制之后到达目标点
            double[] VectorTransIn = new double[4];
            double[] VectorTransOut = new double[4];

            double xl, yl, zl;//记录局部点的坐标
            double DisP = 0;//记录目标点是否在要求的圆区域内


            //给定全局坐标系下目标位置和要控制的机构的index，使得机构末端移动到目标位置
            VectorTransIn[0] = xg;
            VectorTransIn[1] = yg;
            VectorTransIn[2] = zg;
            VectorTransIn[3] = 1;

            //坐标变换 G2L
            if (Index == 1) { MatrixVectorMul(TransMatrix1, VectorTransIn, out VectorTransOut); }
            else { MatrixVectorMul(TransMatrix2, VectorTransIn, out VectorTransOut); }

            //希望到达的局部坐标系
            xl = VectorTransOut[0];
            yl = VectorTransOut[1];
            zl = VectorTransOut[2];

            //判断点是否在规定的工作空间内(圆)
            //TODO:是否考虑z坐标？
            if (Index == 1)
            {
                DisP = Math.Sqrt(Math.Pow(xl - xc1, 2) + Math.Pow(yl - yc1, 2));
                if (DisP > rc1)
                {//超出工作空间
                    MessageBox.Show("机构1超出规定工作空间！", "错误提示");
                    return;
                }
            }
            else {
                DisP = Math.Sqrt(Math.Pow(xl - xc2, 2) + Math.Pow(yl - yc2, 2));
                if (DisP > rc2)
                {//超出工作空间
                    MessageBox.Show("机构2超出规定工作空间！", "错误提示");
                    return;
                }
            }

            //粗调
            MotorLengthCal(Index,xl,yl);

            //细调，负反馈
            FeedBackControl(Index,
            xg, yg, zg,
            xl, yl, zl,
            0.8,1.0);
            //提示结束
            MessageBox.Show("调节结束！", "提示");




        }

        private void FeedBackControl(int Index,
            double xg, double yg,double zg,
            double xl,double yl,double zl,
            double ErrMin,double PVal) {
            //输入希望到达的全局坐标点和局部坐标点
            //以及可容忍的最小误差ErrMin，反馈比例系数PVal(应小于1)
            //反馈：若目标为（50，50），实际到达（49.8，49.8），下次到达（50.2，50.2）
            //反馈基于全局坐标系


            double errX=0, errY=0,errZ=0;
            double err=0;
            double FactX=0, FactY=0, FactZ=0;
            double GoalX, GoalY, GoalZ;

            GoalX = xg; GoalY = yg; GoalZ = zg;

            do {
                //根据误差控制电机
                //将目标点坐标化成局部坐标系下的表示
                //可以设置PVal来调整收敛幅度
                double[] VectorIn = new double[4];
                double[] VectorOut = new double[4];
                VectorIn[0] = GoalX + PVal * errX;
                VectorIn[1] = GoalY + PVal * errY;
                VectorIn[2] = GoalZ + PVal * errZ;
                VectorIn[3] = 1;
                MatrixVectorMul(TransMatrix1, VectorIn, out VectorOut);
                //控制电机
                MotorLengthCal(Index, VectorOut[0], VectorOut[1]);


                //给电机运动的时间
                //由于进入细调阶段，时间可以给的很短
                System.Threading.Thread.Sleep(100);


                //从NDI采集数据(全局坐标系)并记录
                /*DataSample(Index, 10,
                        out FactX,
                        out FactY,
                        out FactZ
                        );*/
                //计算误差
                errX = GoalX - FactX;
                errY = GoalY - FactY;
                errZ = GoalZ - FactZ;
                //计算总误差
                err = Math.Sqrt(Math.Pow(errX, 2) + Math.Pow(errY, 2) + Math.Pow(errZ, 2));
            }
            while (err >= ErrMin);
            

        }



        private void DataSample(int Index,int Num,
            out double X1,out double Y1,out double Z1,
            out double X2,out double Y2,out double Z2) {
            //采集数据Num回，取平均并记录
            //Index为1则采集Q，反之则采集H
            //输出对应的X，Y，Z
            int m = 0;
            double XQTotal = 0, YQTotal = 0, ZQTotal = 0;//z约为-20左右
            double XHTotal = 0, YHTotal = 0, ZHTotal = 0;//z约为-70左右
            for (m = 0; m < Num; m++)
            {
                if (Math.Pow(Convert.ToDouble(MarkerData[2]), 2) + Math.Pow(Convert.ToDouble(MarkerData[5]), 2) == 0)
                {
                    //正常情况下marker1和2的z坐标不可能都是0，若都为0表示有问题，则此时不采集点
                    m--;
                    continue;
                }
                //保证ZQ的绝对值永远要更大
                if (Math.Abs(MarkerData[5])> Math.Abs(MarkerData[2]))
                {
                    //2为前
                    XQTotal += Convert.ToDouble(MarkerData[3]);
                    YQTotal += Convert.ToDouble(MarkerData[4]);
                    ZQTotal += Convert.ToDouble(MarkerData[5]);

                    XHTotal += Convert.ToDouble(MarkerData[0]);
                    YHTotal += Convert.ToDouble(MarkerData[1]);
                    ZHTotal += Convert.ToDouble(MarkerData[2]);
                }
                else
                {
                    //1为前
                    XHTotal += Convert.ToDouble(MarkerData[3]);
                    YHTotal += Convert.ToDouble(MarkerData[4]);
                    ZHTotal += Convert.ToDouble(MarkerData[5]);

                    XQTotal += Convert.ToDouble(MarkerData[0]);
                    YQTotal += Convert.ToDouble(MarkerData[1]);
                    ZQTotal += Convert.ToDouble(MarkerData[2]);
                }
                //根据帧数决定等待时间
                System.Threading.Thread.Sleep(50);
            }
            //取平均，记录
            X1 = XQTotal / Num;
            Y1 = YQTotal / Num;
            Z1 = ZQTotal / Num;

            X2 = XHTotal / Num;
            Y2 = YHTotal / Num;
            Z2 = ZHTotal / Num;
        }







        //输入xl和yl坐标，输出短边和长边的长度，再和基准长度相减，得到电机长度
        //Index表示是机构1还是机构2(对应值为1和2)
        private void MotorLengthCal(int Index,double xl_1,double yl_1)
        {
            if (Index == 1)
            {
                //计算机构1
                //计算两个电机边的长度
                //中间量

                double a00 = Math.Pow(xl_1, 2) + Math.Pow(yl_1, 2);
                double a01 = b1 - Math.Pow(a00, 0.5);

                double a1 = Math.Pow(m1, 2);
                double a2 = Math.Pow(a01, 2);
                double a3 = 2 * m1 * a01;
                double a4 = Math.Pow(a00, 0.5) / xl_1;

                //计算两个电机伸长量
                t1 = Math.Pow(a00,0.5)-b1;
                l1 = Math.Sqrt(a1 + a2 - a3 / a4);

                //测试用
                //string Mege = "t1:" + t1.ToString()+"\n" + "l1:" + l1.ToString();
                //MessageBox.Show(Mege, "test");





            }
            if (Index == 2)
            {
                //计算机构2
                //计算两个电机边的长度
                //中间量

                double a00 = Math.Pow(xl_2, 2) + Math.Pow(yl_2, 2);
                double a01 = b2 - Math.Pow(a00, 0.5);

                double a1 = Math.Pow(m2, 2);
                double a2 = Math.Pow(a01, 2);
                double a3 = 2 * m2 * a01;
                double a4 = Math.Pow(a00, 0.5) / xl_2;

                //计算两个电机伸长量
                t2 = Math.Pow(a00, 0.5) - b2;
                l2 = Math.Sqrt(a1 + a2 - a3 / a4);

                //测试用
                //string Mege = "t2:" + t2.ToString() + "\n" + "l2:" + l2.ToString();
                //MessageBox.Show(Mege, "test");


            }
            //向电机发送指令
            CommandBoardCastAuto(Convert.ToUInt16(t1 - t1Ori), Convert.ToUInt16(l1 - l1Ori), Convert.ToUInt16(t2 - t2Ori), Convert.ToUInt16(l2 - l2Ori));

        }

        public void NeedleMove() {
            


            

            //操作机构1
            /*****************************************/
        //针和机构1的平面的交点,为全局坐标系的坐标
            xg_1 = -(dPlane1 * x1Ned - dPlane1 * x2Ned + bPlane1 * x1Ned * y2Ned - bPlane1 * x2Ned * y1Ned + cPlane1 * x1Ned * z2Ned - cPlane1 * x2Ned * z1Ned) / (aPlane1 * x1Ned - aPlane1 * x2Ned + bPlane1 * y1Ned - bPlane1 * y2Ned + cPlane1 * z1Ned - cPlane1 * z2Ned);
            yg_1 = -(dPlane1 * y1Ned - dPlane1 * y2Ned - aPlane1 * x1Ned * y2Ned + aPlane1 * x2Ned * y1Ned + cPlane1 * y1Ned * z2Ned - cPlane1 * y2Ned * z1Ned) / (aPlane1 * x1Ned - aPlane1 * x2Ned + bPlane1 * y1Ned - bPlane1 * y2Ned + cPlane1 * z1Ned - cPlane1 * z2Ned);
            zg_1 = -(dPlane1 * z1Ned - dPlane1 * z2Ned - aPlane1 * x1Ned * z2Ned + aPlane1 * x2Ned * z1Ned - bPlane1 * y1Ned * z2Ned + bPlane1 * y2Ned * z1Ned) / (aPlane1 * x1Ned - aPlane1 * x2Ned + bPlane1 * y1Ned - bPlane1 * y2Ned + cPlane1 * z1Ned - cPlane1 * z2Ned);


            
            //操作机构2
            /*****************************************/
            //针和机构2的平面的交点,为全局坐标系的坐标
            xg_2 = -(dPlane2 * x1Ned - dPlane2 * x2Ned + bPlane2 * x1Ned * y2Ned - bPlane2 * x2Ned * y1Ned + cPlane2 * x1Ned * z2Ned - cPlane2 * x2Ned * z1Ned) / (aPlane2 * x1Ned - aPlane2 * x2Ned + bPlane2 * y1Ned - bPlane2 * y2Ned + cPlane2 * z1Ned - cPlane2 * z2Ned);
            yg_2 = -(dPlane2 * y1Ned - dPlane2 * y2Ned - aPlane2 * x1Ned * y2Ned + aPlane2 * x2Ned * y1Ned + cPlane2 * y1Ned * z2Ned - cPlane2 * y2Ned * z1Ned) / (aPlane2 * x1Ned - aPlane2 * x2Ned + bPlane2 * y1Ned - bPlane2 * y2Ned + cPlane2 * z1Ned - cPlane2 * z2Ned);
            zg_2 = -(dPlane2 * z1Ned - dPlane2 * z2Ned - aPlane2 * x1Ned * z2Ned + aPlane2 * x2Ned * z1Ned - bPlane2 * y1Ned * z2Ned + bPlane2 * y2Ned * z1Ned) / (aPlane2 * x1Ned - aPlane2 * x2Ned + bPlane2 * y1Ned - bPlane2 * y2Ned + cPlane2 * z1Ned - cPlane2 * z2Ned);

            MotorTotalCalGlobal(1,xg_1,yg_1,zg_1);
            MotorTotalCalGlobal(2,xg_2,yg_2,zg_2);

        }
        

        public void chuankou_Load(object sender, EventArgs e)
        {
            this.MaximumSize = this.Size; 
            this.MinimumSize = this.Size;
            this.MaximizeBox = false;
            for(int i= 0; i<10; i++)//最大支持到串口 10，可根据自己需求增加 
            { 
                cbxCOMPort.Items.Add("COM"+(i+1).ToString());
            } 
            
            cbxCOMPort.SelectedIndex=0; //列出常用的波特率
            
            cbxBaudRate.Items.Add("1200"); 
            cbxBaudRate.Items.Add("2400"); 
            cbxBaudRate.Items.Add("4800");
            cbxBaudRate.Items.Add("9600"); 
            cbxBaudRate.Items.Add("19200"); 
            cbxBaudRate.Items.Add("38400");

            cbxBaudRate.Items.Add("43000");
            cbxBaudRate.Items.Add("56000");
            cbxBaudRate.Items.Add("57600"); 
            cbxBaudRate.Items.Add("921600");
            cbxBaudRate.SelectedIndex = 9; 
            
            //列出停止位
            cbxStopBits.Items.Add("0"); 
            cbxStopBits.Items.Add("1");
            cbxStopBits.Items.Add("1.5");
            cbxStopBits.Items.Add("2");
            cbxStopBits.SelectedIndex=1;

            //列出数据位
            cbxDataBits.Items.Add("8");
            cbxDataBits.Items.Add("7");
            cbxDataBits.Items.Add("6");
            cbxDataBits.Items.Add("5");
            cbxDataBits.SelectedIndex=0;
            
            //列出奇偶校验位
            cbxParity.Items.Add("无");
            cbxParity.Items.Add("奇校验"); 
            cbxParity.Items.Add("偶校验"); 
            cbxParity.SelectedIndex=0; 

            //默认为 Char 显示
            rbnChar.Checked=true;

        }

        private void LocalControl_Click(object sender, EventArgs e)
        {
            //xl_1 = Convert.ToDouble(GloCooX.Value);
            //yl_1 = Convert.ToDouble(GloCooY.Value);

            //MotorLengthCal(1, xl_1, yl_1);
            MotorTotalCalGlobal(1,
                Convert.ToDouble(GloCooX.Value),
                Convert.ToDouble(GloCooY.Value),
                Convert.ToDouble(GloCooZ.Value));
        }

        private void btnCheckCOM_Click(object sender,EventArgs e)//检测哪些串口可用
        {
            bool comExistence = false;//有可用串口标志位
            //清除当前串口号中的所有串口名称
            cbxCOMPort.Items.Clear();
            
            for (int i= 0;i<10;i++)
            { 
                try 
                { 
                    SerialPort sp= new SerialPort("COM"+(i+1).ToString());
                    sp.Open(); 
                    sp.Close();

                    cbxCOMPort.Items.Add("COM"+(i+1).ToString());
                    

                    comExistence =true; 
                } 
                catch(Exception) 
                {
                    continue;
                }
            }
            if (comExistence)
            {
                //使 ListBox 显示第 1 个添加的索引 
                cbxCOMPort.SelectedIndex = 0;
            } 
            else 
            { 
                MessageBox.Show("没有找到可用串口！","错误提示"); 
            }
          }

        private void ToolLoadBtn_Click(object sender, EventArgs e)
        {
            
        }


        void worker_socket_DoWork(object sender, DoWorkEventArgs e)
        {
            
        }


        private void button1_Click_2(object sender, EventArgs e)
        {
            
        }

  


        

        private void chuankou_FormClosed(object sender, FormClosedEventArgs e)
        {
            NDIThread.Abort();
        }

        private void button1_Click_3(object sender, EventArgs e)
        {
            //连接NDI的button


            if (File.Exists(ToolPath1))
            {
                if ((NDIVegaConnect(hostname)) == 0)
                {

                    loadPassiveTools(ToolPath1);
                    startTracking();
                    //printTrackingNDIData();
                    //getTransformData(ref trackdata[0]);
                    getTransformData_Ne(ref trackdata[0], options);//试验

                    NDIThread = new Thread(new ThreadStart(NDITrack));
                    NDIThread.Start();//启动新线程


                }
                else
                {
                    MessageBox.Show("NDI连接失败");
                }
            }
            else
            {
                MessageBox.Show("ROM文件不存在");
            }

        }


        private void NDITrack()
        {
            while (needTrack)
            {              
                //getTransformData(ref trackdata[0]); 
                getTransformData_Ne(ref trackdata[0], options);//试验


                if (this.InvokeRequired)
                {
                    this.Invoke(new ShowTrackData(showTrackData), new Object[] { trackdata });
                }
                else
                {
                    showTrackData(trackdata);
                }
            }
        }

        private void showTrackData(double[] trackData)
        {

            Status.Text = trackdata[8].ToString();
            MarkerCooTrans(trackdata, out MarkerData);
            //显示变换后的marker坐标
            Marker1X.Text = MarkerData[0].ToString();
            Marker1Y.Text = MarkerData[1].ToString();
            Marker1Z.Text = MarkerData[2].ToString();
            Marker2X.Text = MarkerData[3].ToString();
            Marker2Y.Text = MarkerData[4].ToString();
            Marker2Z.Text = MarkerData[5].ToString();


        }


        private void MarkerCooTrans(double[] TrackData, out double[] MarkerData)
        {
            //将NDI采集到的marker的坐标转化为相对tool的
            //MarkerData[6] = {Tx1',Ty1',Tz1',Tx2',Ty2',Tz2'}

            //TrackData[15];

            //TrackData[0] = q0;
            //TrackData[1] = qx;
            //TrackData[2] = qy;
            //TrackData[3] = qz;
            //TrackData[4] = Tx;
            //TrackData[5] = Ty;
            //TrackData[6] = Tz;
            //TrackData[7] = Err（误差）;
            //TrackData[8] = Status(31);
            //TrackData[9] = markernum(2);
            //TrackData[10] = Tx1;
            //TrackData[11] = Ty1;
            //TrackData[12] = Tz1;
            //TrackData[13] = Tx2;
            //TrackData[14] = Ty2;
            //TrackData[15] = Tz2;

            double q0, qx, qy, qz;//四元数
            double TxTool, TyTool, TzTool;//tool相对相机的平移量
            double Tx1, Ty1, Tz1;//相机坐标系下marker1坐标
            double Tx2, Ty2, Tz2;//相机坐标系下marker2坐标

            double Tx1New, Ty1New, Tz1New;//tool坐标系下marker1的坐标
            double Tx2New, Ty2New, Tz2New;//tool坐标系下marker2的坐标

            double[,] rotMatrixNDI = new double[3, 3];//四元数化为旋转矩阵

            double[][] MatrixTrans = new double[4][];//转换矩阵
            MatrixTrans[0] = new double[4];
            MatrixTrans[1] = new double[4];
            MatrixTrans[2] = new double[4];
            MatrixTrans[3] = new double[4];

            Matrix3D Matrix = new Matrix3D();
            Vector3D VecTrans = new Vector3D();
            Point3D Coo1Old = new Point3D();
            Point3D Coo2Old = new Point3D();

            Point3D Coo1New = new Point3D();
            Point3D Coo2New = new Point3D();

            double[] Coo1OldD = new double[4];
            double[] Coo2OldD = new double[4];

            double[] Coo1NewD = new double[4];
            double[] Coo2NewD = new double[4];



            q0 = TrackData[0];
            qx = TrackData[1];
            qy = TrackData[2];
            qz = TrackData[3];

            TxTool = TrackData[4];
            TyTool = TrackData[5];
            TzTool = TrackData[6];

            Tx1 = TrackData[10];
            Ty1 = TrackData[11];
            Tz1 = TrackData[12];

            Tx2 = TrackData[13];
            Ty2 = TrackData[14];
            Tz2 = TrackData[15];


            Quaternion Quat = new Quaternion();
            Quat.W = q0;
            Quat.X = qx;
            Quat.Y = qy;
            Quat.Z = qz;


            VecTrans.X = TxTool;
            VecTrans.Y = TyTool;
            VecTrans.Z = TzTool;

            Coo1Old.X = Tx1;
            Coo1Old.Y = Ty1;
            Coo1Old.Z = Tz1;

            Coo2Old.X = Tx2;
            Coo2Old.Y = Ty2;
            Coo2Old.Z = Tz2;

            
            Matrix.Rotate(Quat);
            Matrix.Translate(VecTrans);
            Matrix.Invert();

            Coo1New = Matrix.Transform(Coo1Old);
            Coo2New = Matrix.Transform(Coo2Old);

            MarkerData = new double[6];

            MarkerData[0] = Coo1New.X;
            MarkerData[1] = Coo1New.Y;
            MarkerData[2] = Coo1New.Z;
            MarkerData[3] = Coo2New.X;
            MarkerData[4] = Coo2New.Y;
            MarkerData[5] = Coo2New.Z;

        }







        void worker_socket_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }



        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void NeedleCtrlBtn_Click(object sender, EventArgs e)
        {
            //按下该按键后，根据对话框中读数确定针的方位，同时调整电机到相应位置
            x1Ned = Convert.ToDouble(NeedlePoint1X.Value);
            y1Ned = Convert.ToDouble(NeedlePoint1Y.Value);
            z1Ned = Convert.ToDouble(NeedlePoint1Z.Value);

            x2Ned = Convert.ToDouble(NeedlePoint2X.Value);
            y2Ned = Convert.ToDouble(NeedlePoint2Y.Value);
            z2Ned = Convert.ToDouble(NeedlePoint2Z.Value);
            

            NeedleMove();


        }

        private void NeedlePoint1X_ValueChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 检测串口是否设置
        /// </summary>
        /// <returns></returns>
        private bool CheckProtSetting() 
        {
            if (cbxCOMPort.Text.Trim() == "") return false;
            if (cbxBaudRate.Text.Trim() == "") return false;
            if (cbxDataBits.Text.Trim() == "") return false;
            if (cbxParity.Text.Trim() == "") return false;
            if (cbxStopBits.Text.Trim() == "") return false;

            return true;
        }

        /// <summary>
        /// 检测发送数据
        /// </summary>
        /// <returns></returns>

        /// <summary>
        /// 设置串口属性
        /// </summary>
        public void SetPortProperty()
        {
            sp = new SerialPort();
            sp.PortName = cbxCOMPort.Text.Trim();
            sp.BaudRate = Convert.ToInt32(cbxBaudRate.Text.Trim());

            //设置停止位
            float f = Convert.ToSingle(cbxStopBits.Text.Trim()); 

            if (f == 0)
            {
                sp.StopBits = StopBits.None;
            }
            else if(f == 1.5)
            {
                sp.StopBits = StopBits.OnePointFive;
            }
            else if (f == 1)
            {
                sp.StopBits = StopBits.One;
            }
            else if (f == 2)
            {
                sp.StopBits = StopBits.Two;
            }
            else
            {
                sp.StopBits = StopBits.One;
            }

            //设置数据位
            sp.DataBits = Convert.ToInt16(cbxDataBits.Text.Trim());

            string s = cbxParity.Text.Trim(); //设置奇偶校验位
            if (s.CompareTo("无") == 0)
            {
                sp.Parity = Parity.None;
            }
            else if (s.CompareTo("奇校验") == 0)
            {
                sp.Parity = Parity.Odd;
            }
            else if (s.CompareTo("偶校验") == 0)
            {
                sp.Parity = Parity.Even;
            }
            else
            {
                sp.Parity = Parity.None;
            }

            //设置超时读取时间
            sp.ReadTimeout = -1;
            sp.RtsEnable = true;

            sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);

            if (rbnHex.Checked)
            {
                isHex = true;
            }
            else
            {
                isHex = false;
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (isOpen)
            {
                try
                {
                    CommandCreateBoardcast();
                    sp.Write(CommandArray,0,18);
                }
                catch (Exception)
                {
                    MessageBox.Show("发送数据时发生错误", "Error");
                }
            }
            else 
            {
                MessageBox.Show("串口未打开","Error");
            }

        }

        private void btnOpenCom_Click(object sender, EventArgs e)
        {
            if (isOpen == false)
            {
                if (!CheckProtSetting()) //检查串口设置
                {
                    MessageBox.Show("串口未设置", "Error");
                    return;
                }

                if (!isSetProperty) //串口
                {
                    SetPortProperty();
                    isSetProperty = true;
                }

                try
                {
                    sp.Open();
                    isOpen = true;
                    btnOpenCom.Text = "关闭串口";

                    cbxCOMPort.Enabled = false;
                    cbxBaudRate.Enabled = false;
                    cbxDataBits.Enabled = false;
                    cbxParity.Enabled = false;
                    cbxStopBits.Enabled = false;
                    rbnChar.Enabled = false;
                    rbnHex.Enabled = false;

                }
                catch (Exception)
                {
                    isSetProperty = false;
                    isOpen = false;
                    MessageBox.Show("串口无效或已被占", "Error");
                }
            }
            else
            {
                try
                {
                    sp.Close();
                    isOpen = false;
                    isSetProperty = false;
                    btnOpenCom.Text = "打开串口";

                    cbxCOMPort.Enabled = true;
                    cbxBaudRate.Enabled = true;
                    cbxDataBits.Enabled = true;
                    cbxParity.Enabled = true;
                    cbxStopBits.Enabled = true;
                    rbnChar.Enabled = true;
                    rbnHex.Enabled = true;

                }
                catch (Exception)
                {
                    MessageBox.Show("关闭串口时发生错误", "Error");
                }
            }
        }

        private void sp_DataReceived(object sender, EventArgs e)
        {
            System.Threading.Thread.Sleep(100); //延迟100ms，等待接受数据完成

            //this.Invoke 就是跨线程访问 ui 的方法
            this.Invoke((EventHandler)(delegate
            {
                if (isHex == false)
                {
                    

                }
                else
                {
                    Byte[] ReceivedData = new Byte[sp.BytesToRead]; //创建接收字节数组 
                    sp.Read(ReceivedData, 0, ReceivedData.Length);
                    String RecvDataText = null;
                    for (int i = 0; i < ReceivedData.Length; i++)
                    {
                        RecvDataText += ("0x" + ReceivedData[i].ToString("X2") + "");
                    }
                    
                }
                sp.DiscardInBuffer();//丢弃接收缓冲区数据
            }));
        }

        private void btnCleanData_Click(object sender, EventArgs e)
        {
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void cbxCOMPort_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tbxRecvData_TextChanged(object sender, EventArgs e)
        {

        }

        private void cbxBaudRate_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void ID_ValueChanged(object sender, EventArgs e)
        {

        }

        private void btnSendBoardcast_Click(object sender, EventArgs e)
        {
            if (isOpen)
            {
                try
                {
                    CommandCreateBoardcast();
                    sp.Write(CommandArray, 0, 18);
                }
                catch (Exception)
                {
                    MessageBox.Show("发送数据时发生错误", "Error");
                }
            }
            else
            {
                MessageBox.Show("串口未打开", "Error");
            }
        }


        private void ExcelProcess(DataTable dt,int Index) {
            StringBuilder strbu = new StringBuilder();

            //写入标题
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                strbu.Append(dt.Columns[i].ColumnName.ToString() + "\t");
            }

            //加入换行字符串
            strbu.Append(Environment.NewLine);
            //写入内容
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    strbu.Append(dt.Rows[i][j].ToString() + "\t");
                }
                strbu.Append(Environment.NewLine);
            }
            System.Windows.Forms.Clipboard.SetText(strbu.ToString());

            //新建EXCEL应用
            Excel.Application excelApp = new Excel.Application();
            if (excelApp == null)
                return;

            //设置为不可见，操作在后台执行，为 true 的话会打开 Excel
            excelApp.Visible = false;
            //初始化工作簿
            Excel.Workbooks workbooks = excelApp.Workbooks;
            //新增加一个工作簿，Add（）方法也可以直接传入参数 true
            Excel.Workbook workbook = workbooks.Add(Excel.XlWBATemplate.xlWBATWorksheet);
            //同样是新增一个工作簿，但是会弹出保存对话框

            Excel.Worksheet worksheet = workbook.Worksheets.Add();


            worksheet.Paste();




            //新建一个 Excel 文件
            string currentPath = Directory.GetCurrentDirectory();
            string filePath;
            if (Index == 1)
            {filePath = currentPath + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-DoubleBall-Index1") + ".xls"; }
            else { filePath = currentPath + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-DoubleBall-Index2") + ".xls"; }
            //创建文件
            FileStream file = new FileStream(filePath, FileMode.CreateNew);
            //关闭释放流，不然没办法写入数据
            file.Close();
            file.Dispose();

            //保存写入的数据，这里还没有保存到磁盘
            workbook.Saved = true;
            //保存到指定的路径
            workbook.SaveCopyAs(filePath);
        }


        private void ExportExcel(
            double[] MarkerX1Record,double[] MarkerY1Record,double[] MarkerZ1Record,
            double[] MarkerX2Record, double[] MarkerY2Record, double[] MarkerZ2Record,
            double[] MarkerXavRecord, double[] MarkerYavRecord, double[] MarkerZavRecord,
            double[] Motor1Length, double[] Motor2Length,
            int Index)
        {
            //用于自动试验时输出excel
            System.Data.DataTable dt = new System.Data.DataTable();
            dt.Columns.Add("电机1伸长量", typeof(int));
            dt.Columns.Add("电机2伸长量", typeof(int));
            dt.Columns.Add("X1", typeof(double));
            dt.Columns.Add("Y1", typeof(double));
            dt.Columns.Add("Z1", typeof(double));
            dt.Columns.Add("X2", typeof(double));
            dt.Columns.Add("Y2", typeof(double));
            dt.Columns.Add("Z2", typeof(double));
            dt.Columns.Add("Xav", typeof(double));
            dt.Columns.Add("Yav", typeof(double));
            dt.Columns.Add("Zav", typeof(double));
            int ColLen = MarkerX1Record.Length;
            for (int i = 0; i < ColLen; i++)
            {
                dt.Rows.Add(Motor1Length[i], 
                    Motor2Length[i],
                    MarkerX1Record[i], 
                    MarkerY1Record[i], 
                    MarkerZ1Record[i],
                    MarkerX2Record[i],
                    MarkerY2Record[i],
                    MarkerZ2Record[i],
                    MarkerXavRecord[i],
                    MarkerYavRecord[i],
                    MarkerZavRecord[i]);
            }

            ExcelProcess(dt,Index);
        }



        private void AutoExpe() {
            //自动移动电机，并使用NDI自动采集数据，最终获得的结果会自动输出为.xlsx文件
            //务必确保电机和NDI都已经准备好
            const int Index = 2;
            UInt16 Pos11 = 0; UInt16 Pos12 = 0;
            const UInt16 delta = 100;
            const UInt16 ListLen = (2000/delta+1)* (2000 / delta + 1);
            int i = 0,j = 0;
            int k = 0;
            const int Num = 10;//采集数据num回并取平均
            //121为delta为200的数组大小
            double[] MarkerX1Record = new double[ListLen];
            double[] MarkerY1Record = new double[ListLen];
            double[] MarkerZ1Record = new double[ListLen];

            double[] MarkerX2Record = new double[ListLen];
            double[] MarkerY2Record = new double[ListLen];
            double[] MarkerZ2Record = new double[ListLen];

            double[] MarkerXavRecord = new double[ListLen];
            double[] MarkerYavRecord = new double[ListLen];
            double[] MarkerZavRecord = new double[ListLen];


            double[] Motor1LengthRecord = new double[ListLen];
            double[] Motor2LengthRecord = new double[ListLen];
            //初始化
            if (Index == 1) { CommandBoardCastAuto(0, 0, 0, 2000); }
            else { CommandBoardCastAuto(2000, 0, 0, 0); }
            MessageBox.Show("完成初始化", "完成");
            for (; Pos11 <= 2000; Pos11 += delta,i++)
            {
                for (Pos12 = 0,j=0; Pos12 <= 2000; Pos12 += delta,j++)
                {
                    if (Index == 1) { CommandBoardCastAuto(Pos11, Pos12, 0, 2000); }
                    else { CommandBoardCastAuto(2000, 0, Pos11, Pos12); }
                    //延迟,给电机移动的时间
                    if (Pos12 == 0) { System.Threading.Thread.Sleep(3000); }
                    else { System.Threading.Thread.Sleep(800); }
                    Motor1LengthRecord[k] = Pos11;
                    Motor2LengthRecord[k] = Pos12;
                    //采集数据并记录
                    DataSample(Index, Num,
                        out MarkerX1Record[k],
                        out MarkerY1Record[k],
                        out MarkerZ1Record[k],
                        out MarkerX2Record[k],
                        out MarkerY2Record[k],
                        out MarkerZ2Record[k]
                        );

                    MarkerXavRecord[k] = (MarkerX1Record[k] + MarkerX2Record[k]) / 2;
                    MarkerYavRecord[k] = (MarkerY1Record[k] + MarkerY2Record[k]) / 2;
                    MarkerZavRecord[k] = (MarkerZ1Record[k] + MarkerZ2Record[k]) / 2;

                    k++;

                }
            }
            //完成电机运动
            MessageBox.Show("完成电机运动", "完成");

            //输出excel
            ExportExcel(
            MarkerX1Record, MarkerY1Record, MarkerZ1Record,
            MarkerX2Record, MarkerY2Record, MarkerZ2Record,
            MarkerXavRecord, MarkerYavRecord, MarkerZavRecord,
            Motor1LengthRecord, Motor2LengthRecord,
            Index);
            //结束
            MessageBox.Show("完成excel输出", "完成");
            ExpeThread.Abort();
        }




        private void button1_Click_1(object sender, EventArgs e)
        {
            //AutoExpe();
            ExpeThread = new Thread(new ThreadStart(ExpeContinue));
            ExpeThread.SetApartmentState(ApartmentState.STA); //重点
            ExpeThread.Start();//启动新线程


            //this.Invoke 就是跨线程访问 ui 的方法

            /*xg_1 = 177.7228;
            yg_1 = -107.4525;
            zg_1 = 28.3674;


            VectorTransIn[0] = xg_1;
            VectorTransIn[1] = yg_1;
            VectorTransIn[2] = zg_1;
            VectorTransIn[3] = 1;

            //坐标变换 G2L
            MatrixVectorMul(TransMatrix1, VectorTransIn, out VectorTransOut);

            //希望到达的局部坐标系
            xl_1 = VectorTransOut[0];
            yl_1 = VectorTransOut[1];
            zl_1 = VectorTransOut[2];

            //判断点是否在规定的工作空间内(圆)
            //DisP = Math.Sqrt(Math.Pow(xl_1 - xc1, 2) + Math.Pow(yl_1 - yc1, 2));
            string Mege =
                "xl_1:" + xl_1.ToString() + "\n"
                + "yl_1:" + yl_1.ToString() + "\n"
                + "xc1:" + xc1.ToString() + "\n"
                + "yc1:" + yc1.ToString() + "\n"
                ;
            MessageBox.Show(Mege, "错误提示");*/





            //实验用，自动循环往复控制电机
            /*UInt16 Pos11 = 0; UInt16 Pos12 = 0;
            string Mege;
            const int delta = 200;
            for (; Pos11 <= 2000; Pos11 += delta) {
                for (Pos12 = 0; Pos12 <= 2000; Pos12 += delta) {
                    CommandBoardCastAuto(Pos11, Pos12,0,0);
                    Mege =
                "电机1伸长量:" + Pos11.ToString() + "\n"
                + "电机2伸长量:" + Pos12.ToString() + "\n"
                ;
                    MessageBox.Show(Mege,"提示");
                    //System.Threading.Thread.Sleep(10); //延迟10ms,避免串口发送过于频繁
                }
            }*/

            //MessageBox.Show("完成", "完成");







        }

        private void ExpeContinue()
        {
            AutoExpe();
        }










    }
}
