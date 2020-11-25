using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Windows.Media.Media3D;
using Kitware.VTK;
using System.Collections.Generic;
using System.Drawing;


namespace ChuanKouTest
{
    public partial class chuankou : Form
    {

        VTK vtk = new VTK();
        /******************************************/
        //以下为一些中间变量的初始化

        //定义向量来记录下坐标变换前和后的坐标信息
        //double[] VectorTransOut = new double[4];
        //double[] VectorTransIn = new double[4];

        //记录希望到达的点和工作空间的圆心的距离
        //double DisP = 0;

        /***************************/
        //多线程
        private Thread ExpeThread;
        private Thread FBThread;
        private delegate void NDIExpe(double[] trackData);
        double[] MarkerData = new double[6];



        


        /***************************/
        //反馈控制相关
        

        //实时记录Tracker示数，防止其发生多线程的问题
        double X1GlobalRecord = 0, Y1GlobalRecord = 0, Z1GlobalRecord = 0;
        double X2GlobalRecord = 0, Y2GlobalRecord = 0, Z2GlobalRecord = 0;
        readonly object GlobalRecordLock = new object();
        private Thread NeedleThread;

        private Thread NeedleStewartThread;

        private Thread TestExpeThread;

        private Thread StewartExpeThread;

        private Thread CommandMotorFollowExpeThread;



        /**************************/


        /**************************
         Stewart并联机器人穿针相关 
         **************************/
        //表示穿针的目标点，目标点1是更靠近套筒的
        int Target1Index = 3;//目标点1的index，0-5
        int Target2Index = 0;//目标点2的index，0-5





        public chuankou()
        {
            InitializeComponent();
        }



        





        public void chuankou_Load(object sender, EventArgs e)
        {
            this.MaximumSize = this.Size; 
            this.MinimumSize = this.Size;
            this.MaximizeBox = false;
            
            
        }

        private void FeedBackControlTestThread() {
            MessageBox.Show("开始", "提示");
            FBThread.Abort();
        }



        private void LocalControl_Click(object sender, EventArgs e)
        {
            //xl_1 = Convert.ToDouble(GloCooX.Value);
            //yl_1 = Convert.ToDouble(GloCooY.Value);

            //MotorLengthCal(1, xl_1, yl_1);

            FBThread = new Thread(new ThreadStart(FeedBackControlTestThread));
            FBThread.SetApartmentState(ApartmentState.STA); //重点
            FBThread.Start();//启动新线程

        }

        private void PrepareWork() {
            //连接电机，连接NDI一气呵成
            //连接NDI
            NDIConnect();
            //连接电机
            Motor.MotorConnect();

        } 
        
        /// <summary>
        /// 关闭GUI时需要结束NDI和OT的线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chuankou_FormClosed(object sender, FormClosedEventArgs e)
        {
            //NDI
            NDI.NDIThread.Abort();
            //Optitrack
            //Optitrack.OptitrackThread.Abort();
            //Optitrack.TT_Shutdown();
        }


        private void button1_Click_3(object sender, EventArgs e)
        {
            //连接NDI的button
            NDIConnect();
        }


        //机构
        //public static string ToolPathMechanism = @"JGTracker1.rom";
        public static string ToolPathMechanism = @"stewartTracker_20200730.rom";
        //针/标尺
        public static string ToolPathNeedle = @"ToolTracker.rom";
        //目标
        public static string ToolPathTarget = @"TargetTracker.rom";

        /// <summary>
        /// NDI初始化，加载.rom文件，同时开始追踪
        /// </summary>
        public static void NDIInit()
        {
            //全为Tracker的场景使用
            NDI.loadPassiveTools(ToolPathMechanism);//机构
            NDI.loadPassiveTools(ToolPathNeedle);//工具/标尺
            NDI.loadPassiveTools(ToolPathTarget);//目标
            NDI.startTracking();
        }

        /// <summary>
        /// NDI连接，需要开启新的线程
        /// </summary>
        private void NDIConnect()
        {
            
            if (File.Exists(ToolPathMechanism) && File.Exists(ToolPathNeedle) && File.Exists(ToolPathTarget))
            {
                if ((NDI.NDIVegaConnect(NDI.hostname)) == 0)
                {

                    NDIInit();
                    //getTransformData(ref trackdata[0]);
                    NDI.NDIThread = new Thread(new ThreadStart(NDITrack));
                    NDI.NDIThread.Start();

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

        /// <summary>
        /// NDI追踪tracker的主要函数
        /// </summary>
        private void NDITrack()
        {
            trackdata[1] = 255;
            trackdata[10] = 255;
            trackdata[19] = 255;
            //只追踪tracker的场景下使用
            while (NDI.needTrack)
            {
                NDI.getTransformData(ref NDI.trackdata[0]);
                if (InvokeRequired)
                {
                    Invoke(new NDI.ShowTrackData(showTrackData), new Object[] { NDI.trackdata });
                }
                else
                {
                    showTrackData(NDI.trackdata);
                }
            }

        }

        




        public static double[] MechanismTrackerPose = new double[4];
        public static double[] MechanismTrackerPosi = new double[3];
        public static double[] ToolTrackerPose = new double[4];
        public static double[] ToolTrackerPosi = new double[3];
        public static double[] TargetTrackerPose = new double[4];
        public static double[] TargetTrackerPosi = new double[3];

        public static int MTrackStatus = 0;
        public static int ToolTrackStatus = 0;
        public static int TargetTrackStatus = 0;

        /// <summary>
        /// 在获得NDI数据后处理数据
        /// </summary>
        /// <param name="trackData"></param>
        private void showTrackData(double[] trackData)
        {
            //显示三个tracker状态，0正常，256失踪           
            Status1Box.Text = trackData[1 + 0 * 9].ToString();
            Status2Box.Text = trackData[1 + 1 * 9].ToString();
            Status3Box.Text = trackData[1 + 2 * 9].ToString();

            if (trackData[1] == 0)
            {
                MTrackStatus = 1;
                
                MechanismTrackerPose[0] = trackData[2]; MechanismTrackerPose[1] = trackData[3]; MechanismTrackerPose[2] = trackData[4]; MechanismTrackerPose[3] = trackData[5];
                MechanismTrackerPosi[0] = trackData[6]; MechanismTrackerPosi[1] = trackData[7]; MechanismTrackerPosi[2] = trackData[8];
            }
            else
            {
                MTrackStatus = 0;                

            }
            if (trackData[10] == 0)
            {
                ToolTrackStatus = 1;
                
                ToolTrackerPose[0] = trackData[11]; ToolTrackerPose[1] = trackData[12]; ToolTrackerPose[2] = trackData[13]; ToolTrackerPose[3] = trackData[14];
                ToolTrackerPosi[0] = trackData[15]; ToolTrackerPosi[1] = trackData[16]; ToolTrackerPosi[2] = trackData[17];

            }
            else
            {
                ToolTrackStatus = 0;                

            }
            if (trackData[19] == 0)
            {
                TargetTrackStatus = 1;
                
                TargetTrackerPose[0] = trackData[20]; TargetTrackerPose[1] = trackData[21]; TargetTrackerPose[2] = trackData[22]; TargetTrackerPose[3] = trackData[23];
                TargetTrackerPosi[0] = trackData[24]; TargetTrackerPosi[1] = trackData[25]; TargetTrackerPosi[2] = trackData[26];
            }
            else
            {
                TargetTrackStatus = 0;                
            }

            //将机构上小钢珠坐标从盒子坐标系转换到机构坐标系
            StewartExpe.CooTransBox2Tool();
            //在窗口上显示出来
            p1BoxXValue.Text = StewartExpe.TargetPoint1ToolCoo.X.ToString();
            p1BoxYValue.Text = StewartExpe.TargetPoint1ToolCoo.Y.ToString();
            p1BoxZValue.Text = StewartExpe.TargetPoint1ToolCoo.Z.ToString();

            p2BoxXValue.Text = StewartExpe.TargetPoint2ToolCoo.X.ToString();
            p2BoxYValue.Text = StewartExpe.TargetPoint2ToolCoo.Y.ToString();
            p2BoxZValue.Text = StewartExpe.TargetPoint2ToolCoo.Z.ToString();

            p3BoxXValue.Text = StewartExpe.TargetPoint3ToolCoo.X.ToString();
            p3BoxYValue.Text = StewartExpe.TargetPoint3ToolCoo.Y.ToString();
            p3BoxZValue.Text = StewartExpe.TargetPoint3ToolCoo.Z.ToString();

            p4BoxXValue.Text = StewartExpe.TargetPoint4ToolCoo.X.ToString();
            p4BoxYValue.Text = StewartExpe.TargetPoint4ToolCoo.Y.ToString();
            p4BoxZValue.Text = StewartExpe.TargetPoint4ToolCoo.Z.ToString();

            p5BoxXValue.Text = StewartExpe.TargetPoint5ToolCoo.X.ToString();
            p5BoxYValue.Text = StewartExpe.TargetPoint5ToolCoo.Y.ToString();
            p5BoxZValue.Text = StewartExpe.TargetPoint5ToolCoo.Z.ToString();

            p6BoxXValue.Text = StewartExpe.TargetPoint6ToolCoo.X.ToString();
            p6BoxYValue.Text = StewartExpe.TargetPoint6ToolCoo.Y.ToString();
            p6BoxZValue.Text = StewartExpe.TargetPoint6ToolCoo.Z.ToString();



            //实时解算指定的两小球和套筒直线的距离（用向量表示）
            //机构坐标系表示

            //存储垂足的变量
            double pN1X = 0; double pN1Y = 0; double pN1Z = 0;
            double pN2X = 0; double pN2Y = 0; double pN2Z = 0;
            //存储目标偏差向量的变量
            double BiasVector1X = 0; double BiasVector1Y = 0; double BiasVector1Z = 0;
            double BiasVector2X = 0; double BiasVector2Y = 0; double BiasVector2Z = 0;

            //目标点1更靠近套筒点
            double Target1X = StewartExpe.TargetPointArrayToolCoo[Target1Index].X;
            double Target1Y = StewartExpe.TargetPointArrayToolCoo[Target1Index].Y;
            double Target1Z = StewartExpe.TargetPointArrayToolCoo[Target1Index].Z;

            double Target2X = StewartExpe.TargetPointArrayToolCoo[Target2Index].X;
            double Target2Y = StewartExpe.TargetPointArrayToolCoo[Target2Index].Y;
            double Target2Z = StewartExpe.TargetPointArrayToolCoo[Target2Index].Z;

            //目标点1与套筒直线距离
            MathFun.Point2LineFoot3D(
                Target1X, Target1Y, Target1Z,
                Stewart.SleevePoint1CADStewart.X-Stewart.CooTransBiasVector.X, 
                Stewart.SleevePoint1CADStewart.Y - Stewart.CooTransBiasVector.Y, 
                Stewart.SleevePoint1CADStewart.Z - Stewart.CooTransBiasVector.Z,

                Stewart.SleevePoint2CADStewart.X - Stewart.CooTransBiasVector.X, 
                Stewart.SleevePoint2CADStewart.Y - Stewart.CooTransBiasVector.Y, 
                Stewart.SleevePoint2CADStewart.Z-Stewart.CooTransBiasVector.Z,
                out pN1X, out pN1Y, out pN1Z
                );
            BiasVector1X = pN1X - Target1X;
            BiasVector1Y = pN1Y - Target1Y;
            BiasVector1Z = pN1Z - Target1Z;

            //目标点2与套筒直线距离
            MathFun.Point2LineFoot3D(
                Target2X, Target2Y, Target2Z,
                Stewart.SleevePoint1CADStewart.X - Stewart.CooTransBiasVector.X, 
                Stewart.SleevePoint1CADStewart.Y - Stewart.CooTransBiasVector.Y, 
                Stewart.SleevePoint1CADStewart.Z - Stewart.CooTransBiasVector.Z,

                Stewart.SleevePoint2CADStewart.X - Stewart.CooTransBiasVector.X,
                Stewart.SleevePoint2CADStewart.Y - Stewart.CooTransBiasVector.Y,
                Stewart.SleevePoint2CADStewart.Z - Stewart.CooTransBiasVector.Z,
                out pN2X, out pN2Y, out pN2Z
                );

            BiasVector2X = pN2X - Target2X;
            BiasVector2Y = pN2Y - Target2Y;
            BiasVector2Z = pN2Z - Target2Z;


            //显示
            BiasVector1XValue.Text = BiasVector1X.ToString();
            BiasVector1YValue.Text = BiasVector1Y.ToString();
            BiasVector1ZValue.Text = BiasVector1Z.ToString();

            BiasVector2XValue.Text = BiasVector2X.ToString();
            BiasVector2YValue.Text = BiasVector2Y.ToString();
            BiasVector2ZValue.Text = BiasVector2Z.ToString();

            //实时结算套筒点2和目标点1的偏差
            //机构坐标系

            double BiasPointVectorX = 0;
            double BiasPointVectorY = 0;
            double BiasPointVectorZ = 0;

            BiasPointVectorX = StewartExpe.TargetPointArrayToolCoo[Target1Index].X - Stewart.SleevePoint2CADStewart.X+Stewart.CooTransBiasVector.X;
            BiasPointVectorY = StewartExpe.TargetPointArrayToolCoo[Target1Index].Y - Stewart.SleevePoint2CADStewart.Y + Stewart.CooTransBiasVector.Y;
            BiasPointVectorZ = StewartExpe.TargetPointArrayToolCoo[Target1Index].Z - Stewart.SleevePoint2CADStewart.Z + Stewart.CooTransBiasVector.Z;

            BiasPointVectorXValue.Text = BiasPointVectorX.ToString();
            BiasPointVectorYValue.Text = BiasPointVectorY.ToString();
            BiasPointVectorZValue.Text = BiasPointVectorZ.ToString();








        }
        //double[] toolpoint1 = new double[3] { -18.3981685, -0.095453826, -30.4153698 };
        //double[] toolpoint2 = new double[3] { -19.28325048, -0.616108987, -180.4118549 };

        

        


        



        double X1NedGoal = 0, Y1NedGoal = 0, Z1NedGoal = 0;
        double X2NedGoal = 0, Y2NedGoal = 0, Z2NedGoal = 0;




        


        /// <summary>
        /// 输入1个目标点，输出其和两个套筒点的距离之和
        /// 为机器人末端坐标系
        /// </summary>
        /// <param name="xNed"></param>
        /// <param name="yNed"></param>
        /// <param name="zNed"></param>
        /// <returns></returns>
        private double PointNeedleDisCal(
            double xNed,double yNed,double zNed
            ) {
            double DisTotal = 0;

            double DisP1 = 0;
            double DisP2 = 0;

            DisP1 = MathFun.DistancePoint2Point(
                xNed, yNed, zNed,
                Stewart.SleevePoint1CADStewart.X,
                Stewart.SleevePoint1CADStewart.Y,
                Stewart.SleevePoint1CADStewart.Z
                );


            DisP2 = MathFun.DistancePoint2Point(
                xNed, yNed, zNed,
                Stewart.SleevePoint2CADStewart.X,
                Stewart.SleevePoint2CADStewart.Y,
                Stewart.SleevePoint2CADStewart.Z
                );

            DisTotal = DisP1 + DisP2;

            return DisTotal;
        }

        /// <summary>
        /// 输入两个目标点（在机构之外,坐标为机器人末端坐标系），算出stewart平台需要到达的目标点
        /// 控制stewart平台到达目标位置和姿态
        /// 包含PI闭环反馈
        /// </summary>
        /// <param name="x1Ned"></param>
        /// <param name="y1Ned"></param>
        /// <param name="z1Ned"></param>
        /// <param name="x2Ned"></param>
        /// <param name="y2Ned"></param>
        /// <param name="z2Ned"></param>
        /// <param name="DisBalls"></param>
        private void NeedleMoveStewartFeedbackControl(
            double x1Ned,double y1Ned,double z1Ned,
            double x2Ned,double y2Ned,double z2Ned,
            double DisBalls,
            double PVal,double IVal,
            int MaxNum,
            double ErrMin
            )
        {
            #region 计算是顺位还是逆位
            //表示是否是顺位
            //顺位：套筒点2靠近p1Ned，点1远离p1Ned
            //若反之，则算是逆位
            int IsForwardPos = 1;//1为顺位，0为逆位

            double StewartPoint1Dis = 0;
            double StewartPoint2Dis = 0;

            //套筒点1和p1Ned距离
            StewartPoint1Dis = MathFun.DistancePoint2Point(
                Stewart.SleevePoint1CADStewart.X,
                Stewart.SleevePoint1CADStewart.Y,
                Stewart.SleevePoint1CADStewart.Z,
                x1Ned, y1Ned, z1Ned
                );
            //套筒点2和p1Ned距离
            StewartPoint2Dis = MathFun.DistancePoint2Point(
                Stewart.SleevePoint2CADStewart.X,
                Stewart.SleevePoint2CADStewart.Y,
                Stewart.SleevePoint2CADStewart.Z,
                x1Ned, y1Ned, z1Ned
                );
            //判断谁距离小，若套筒点2距离小，则顺位
            //顺位
            if (StewartPoint2Dis<=StewartPoint1Dis)
            {
                IsForwardPos = 1;
            }
            //逆位
            else
            {
                IsForwardPos = 0;
            }


            #endregion


            //表示输出excel的数据个数
            const int DataNum = 53;

            //获得此时机构到NDI的转换坐标系
            vtkMatrix4x4 TransMatrixTool2NDIOld = new vtkMatrix4x4();
            vtkMatrix4x4 TransMatrixTool2NDINew = new vtkMatrix4x4();

            TransMatrixTool2NDIOld.DeepCopy(StewartExpe.TransMatrixTool2NDI);
            




            //指令中给出的目标点的初始化
            double x1NedNow = x1Ned;
            double y1NedNow = y1Ned;
            double z1NedNow = z1Ned;

            double x2NedNow = x2Ned;
            double y2NedNow = y2Ned;
            double z2NedNow = z2Ned;
            //记录变量
            double ErrPoint1 = 0;//点1误差值
            double ErrPoint2 = 0;//点2误差值

            int Num = 0;//记录反馈次数

            //目标点1和套筒直线的距离
            double ErrX1 = 0;
            double ErrY1 = 0;
            double ErrZ1 = 0;

            double FactX1 = 0;
            double FactY1 = 0;
            double FactZ1 = 0;

            double GoalX1 = 0;
            double GoalY1 = 0;
            double GoalZ1 = 0;

            //目标点2和套筒直线的距离
            double ErrX2 = 0;
            double ErrY2 = 0;
            double ErrZ2 = 0;

            double FactX2 = 0;
            double FactY2 = 0;
            double FactZ2 = 0;

            double GoalX2 = 0;
            double GoalY2 = 0;
            double GoalZ2 = 0;

            //总误差，积分环节需要用到，分正负
            double TotalErrStewartX1 = 0;
            double TotalErrStewartY1 = 0;
            double TotalErrStewartZ1 = 0;

            double TotalErrStewartX2 = 0;
            double TotalErrStewartY2 = 0;
            double TotalErrStewartZ2 = 0;


            vtkMatrix4x4 TransMatrixNow = new vtkMatrix4x4();//记录当前变换矩阵，不断左乘
            //初始化为单位矩阵
            TransMatrixNow.SetElement(0, 0, 1);
            TransMatrixNow.SetElement(1, 1, 1);
            TransMatrixNow.SetElement(2, 2, 1);
            TransMatrixNow.SetElement(3, 3, 1);


            //记录本次控制的信息到数组中
            double[][] DataExport = new double[DataNum][];
            for (int i = 0; i < DataNum; i++)
            {
                DataExport[i] = new double[MaxNum];
            }

            //记录数据
            string[] TableName = new string[DataNum];

            #region TableName
            TableName[0] = "FactX1";
            TableName[1] = "FactY1";
            TableName[2] = "FactZ1";

            TableName[3] = "GoalX1";
            TableName[4] = "GoalY1";
            TableName[5] = "GoalZ1";

            TableName[6] = "ErrX1";
            TableName[7] = "ErrY1";
            TableName[8] = "ErrZ1";

            TableName[9] = "ErrPoint1";

            TableName[10] = "FactX2";
            TableName[11] = "FactY2";
            TableName[12] = "FactZ2";

            TableName[13] = "GoalX2";
            TableName[14] = "GoalY2";
            TableName[15] = "GoalZ2";

            TableName[16] = "ErrX2";
            TableName[17] = "ErrY2";
            TableName[18] = "ErrZ2";

            TableName[19] = "ErrPoint2";

            TableName[20] = "TransX";
            TableName[21] = "TransY";
            TableName[22] = "TransZ";

            TableName[23] = "EulerX";
            TableName[24] = "EulerY";
            TableName[25] = "EulerZ";

            TableName[26] = "PVal";
            TableName[27] = "IVal";
            TableName[28] = "ErrMin";

            TableName[29] = "TotalErrStewartX1";
            TableName[30] = "TotalErrStewartY1";
            TableName[31] = "TotalErrStewartZ1";

            TableName[32] = "TotalErrStewartX2";
            TableName[33] = "TotalErrStewartY2";
            TableName[34] = "TotalErrStewartZ2";

            TableName[35] = "FactX1Box";
            TableName[36] = "FactY1Box";
            TableName[37] = "FactZ1Box";

            TableName[38] = "FactX2Box";
            TableName[39] = "FactY2Box";
            TableName[40] = "FactZ2Box";

            TableName[41] = "x1NedNewBox";
            TableName[42] = "y1NedNewBox";
            TableName[43] = "z1NedNewBox";

            TableName[44] = "x2NedNewBox";
            TableName[45] = "y2NedNewBox";
            TableName[46] = "z2NedNewBox";

            TableName[47] = "ExecuteErrX1";
            TableName[48] = "ExecuteErrY1";
            TableName[49] = "ExecuteErrZ1";

            TableName[50] = "ExecuteErrX2";
            TableName[51] = "ExecuteErrY2";
            TableName[52] = "ExecuteErrZ2";
            #endregion





            //进入循环
            while (true)
            {
                /*******************************************************
               //给出目标两点，规划出目标直线，并控制机器人末端到目标位置
               *********************************************************/
                double TransX = 0;
                double TransY = 0;
                double TransZ = 0;

                double EulerAngleX = 0;
                double EulerAngleY = 0;
                double EulerAngleZ = 0;

                //将Fact转化为第三方的盒子坐标系，便于和pNed比较
                double FactX1Box = 0;
                double FactY1Box = 0;
                double FactZ1Box = 0;

                double FactX2Box = 0;
                double FactY2Box = 0;
                double FactZ2Box = 0;

                //将xNed转化为第三方的盒子坐标系，便于比较
                double x1NedNewBox = 0;
                double y1NedNewBox = 0;
                double z1NedNewBox = 0;

                double x2NedNewBox = 0;
                double y2NedNewBox = 0;
                double z2NedNewBox = 0;

                #region 将pNed转化为盒子坐标系
                x1NedNewBox = x1NedNow;
                y1NedNewBox = y1NedNow;
                z1NedNewBox = z1NedNow;

                x2NedNewBox = x2NedNow;
                y2NedNewBox = y2NedNow;
                z2NedNewBox = z2NedNow;

                //转化为机器人工具坐标系
                x1NedNewBox -= Stewart.CooTransBiasVector.X;
                y1NedNewBox -= Stewart.CooTransBiasVector.Y;
                z1NedNewBox -= Stewart.CooTransBiasVector.Z;

                x2NedNewBox -= Stewart.CooTransBiasVector.X;
                y2NedNewBox -= Stewart.CooTransBiasVector.Y;
                z2NedNewBox -= Stewart.CooTransBiasVector.Z;

                vtkMatrix4x4 MatrixTool2BoxStart = StewartExpe.TransMatrixTool2Box();
                MathFun.point3dtrans(MatrixTool2BoxStart,
                    ref x1NedNewBox, ref y1NedNewBox, ref z1NedNewBox);
                MathFun.point3dtrans(MatrixTool2BoxStart,
                    ref x2NedNewBox, ref y2NedNewBox, ref z2NedNewBox);
                #endregion


                NeedleMoveStewartModule(
                    x1NedNow, y1NedNow, z1NedNow,
                    x2NedNow, y2NedNow, z2NedNow,
                    DisBalls,
                    IsForwardPos,
                    ref TransMatrixNow,
                    out TransX,out TransY,out TransZ,
                    out EulerAngleX,out EulerAngleY,out EulerAngleZ
                    );

                System.Threading.Thread.Sleep(2000);
                /***************************************
                检查是否满足误差要求或者达到最大反馈次数
                ***************************************/
                //点1
                NeedleStewartErrSolve(
                        x1Ned,y1Ned,z1Ned,
                        TransMatrixTool2NDIOld,
                        out FactX1,out FactY1,out FactZ1,
                        out GoalX1, out GoalY1, out GoalZ1,
                        out ErrX1, out ErrY1, out ErrZ1
                        );
                ErrPoint1 = Math.Sqrt(
                    Math.Pow(ErrX1, 2) +
                    Math.Pow(ErrY1, 2) +
                    Math.Pow(ErrZ1, 2));

                //点2
                NeedleStewartErrSolve(
                        x2Ned, y2Ned, z2Ned,
                        TransMatrixTool2NDIOld,
                        out FactX2,out FactY2, out FactZ2,
                        out GoalX2, out GoalY2, out GoalZ2,
                        out ErrX2, out ErrY2, out ErrZ2
                        );
                ErrPoint2 = Math.Sqrt(
                    Math.Pow(ErrX2, 2) +
                    Math.Pow(ErrY2, 2) +
                    Math.Pow(ErrZ2, 2));


                //string Mege1 =
                //    "FactX1:"+FactX1.ToString() + "\n"+
                //    "FactY1:" + FactY1.ToString() + "\n" +
                //    "FactZ1:" + FactZ1.ToString() + "\n" +
                //    "GoalX1:" + GoalX1.ToString() + "\n"+
                //    "GoalY1:" + GoalY1.ToString() + "\n" +
                //    "GoalZ1:" + GoalZ1.ToString() + "\n" +
                //    "ErrX1:" + ErrX1.ToString() + "\n" +
                //    "ErrY1:" + ErrY1.ToString() + "\n" +
                //    "ErrZ1:" + ErrZ1.ToString() + "\n"+
                //    "ErrPoint1:" + ErrPoint1.ToString() + "\n"
                //    ;
                //MessageBox.Show(Mege1, "提示");


                //string Mege2 =
                //    "FactX2:" + FactX2.ToString() + "\n" +
                //    "FactY2:" + FactY2.ToString() + "\n" +
                //    "FactZ2:" + FactZ2.ToString() + "\n" +
                //    "GoalX2:" + GoalX2.ToString() + "\n" +
                //    "GoalY2:" + GoalY2.ToString() + "\n" +
                //    "GoalZ2:" + GoalZ2.ToString() + "\n" +
                //    "ErrX2:" + ErrX2.ToString() + "\n" +
                //    "ErrY2:" + ErrY2.ToString() + "\n" +
                //    "ErrZ2:" + ErrZ2.ToString() + "\n"+
                //    "ErrPoint2:" + ErrPoint2.ToString() + "\n"
                //    ;
                //MessageBox.Show(Mege2, "提示");

         


                #region 将Fact转化成盒子坐标系

                FactX1Box = FactX1;
                FactY1Box = FactY1;
                FactZ1Box = FactZ1;

                FactX2Box = FactX2;
                FactY2Box = FactY2;
                FactZ2Box = FactZ2;

                //转化为机器人工具坐标系
                FactX1Box -= Stewart.CooTransBiasVector.X;
                FactY1Box -= Stewart.CooTransBiasVector.Y;
                FactZ1Box -= Stewart.CooTransBiasVector.Z;

                FactX2Box -= Stewart.CooTransBiasVector.X;
                FactY2Box -= Stewart.CooTransBiasVector.Y;
                FactZ2Box -= Stewart.CooTransBiasVector.Z;


                vtkMatrix4x4 MatrixTool2Box = StewartExpe.TransMatrixTool2Box();
                MathFun.point3dtrans(MatrixTool2Box,
                    ref FactX1Box, ref FactY1Box, ref FactZ1Box);
                MathFun.point3dtrans(MatrixTool2Box,
                    ref FactX2Box, ref FactY2Box, ref FactZ2Box);


                #endregion

                //记录FactBox-pNedNewBox
                //盒子坐标系
                double ExecuteErrX1 = FactX1Box - x1NedNewBox;
                double ExecuteErrY1 = FactY1Box - y1NedNewBox;
                double ExecuteErrZ1 = FactZ1Box - z1NedNewBox;

                double ExecuteErrX2 = FactX2Box - x2NedNewBox;
                double ExecuteErrY2 = FactY2Box - y2NedNewBox;
                double ExecuteErrZ2 = FactZ2Box - z2NedNewBox;


                #region DataExport
                DataExport[0][Num] = FactX1;
                DataExport[1][Num] = FactY1;
                DataExport[2][Num] = FactZ1;

                DataExport[3][Num] = GoalX1;
                DataExport[4][Num] = GoalY1;
                DataExport[5][Num] = GoalZ1;

                DataExport[6][Num] = ErrX1;
                DataExport[7][Num] = ErrY1;
                DataExport[8][Num] = ErrZ1;

                DataExport[9][Num] = ErrPoint1;

                DataExport[10][Num] = FactX2;
                DataExport[11][Num] = FactY2;
                DataExport[12][Num] = FactZ2;

                DataExport[13][Num] = GoalX2;
                DataExport[14][Num] = GoalY2;
                DataExport[15][Num] = GoalZ2;

                DataExport[16][Num] = ErrX2;
                DataExport[17][Num] = ErrY2;
                DataExport[18][Num] = ErrZ2;

                DataExport[19][Num] = ErrPoint2;

                DataExport[20][Num] = TransX;
                DataExport[21][Num] = TransY;
                DataExport[22][Num] = TransZ;

                DataExport[23][Num] = EulerAngleX;
                DataExport[24][Num] = EulerAngleY;
                DataExport[25][Num] = EulerAngleZ;

                DataExport[26][Num] = PVal;
                DataExport[27][Num] = IVal;
                DataExport[28][Num] = ErrMin;

                DataExport[29][Num] = TotalErrStewartX1;
                DataExport[30][Num] = TotalErrStewartY1;
                DataExport[31][Num] = TotalErrStewartZ1;

                DataExport[32][Num] = TotalErrStewartX2;
                DataExport[33][Num] = TotalErrStewartY2;
                DataExport[34][Num] = TotalErrStewartZ2;

                DataExport[35][Num] = FactX1Box;
                DataExport[36][Num] = FactY1Box;
                DataExport[37][Num] = FactZ1Box;

                DataExport[38][Num] = FactX2Box;
                DataExport[39][Num] = FactY2Box;
                DataExport[40][Num] = FactZ2Box;

                DataExport[41][Num] = x1NedNewBox;
                DataExport[42][Num] = y1NedNewBox;
                DataExport[43][Num] = z1NedNewBox;

                DataExport[44][Num] = x2NedNewBox;
                DataExport[45][Num] = y2NedNewBox;
                DataExport[46][Num] = z2NedNewBox;

                DataExport[47][Num] = ExecuteErrX1;
                DataExport[48][Num] = ExecuteErrY1;
                DataExport[49][Num] = ExecuteErrZ1;

                DataExport[50][Num] = ExecuteErrX2;
                DataExport[51][Num] = ExecuteErrY2;
                DataExport[52][Num] = ExecuteErrZ2;
                #endregion


                /***************************************
                //是，则退出
                ***************************************/
                if ((ErrPoint1 < ErrMin && ErrPoint2 < ErrMin) || Num >= MaxNum-1)
                {
                    string ExcelName =
                        "yyyy_MM_dd_HH_mm_ss";
                    ExcelFun.ExportExcelGeneral(TableName, DataExport,ExcelName,".xls"
                        );
                    MessageBox.Show("保存完毕", "保存完毕");
                    break;
                }
                /***************************************
                //否，使用PI反馈，根据当前误差得出下一次机器人末端的目标位置
                ***************************************/
                else
                {
                    //点1
                    double FBOutX1 = 0;
                    double FBOutY1 = 0;
                    double FBOutZ1 = 0;


                    #region 记录上一次反馈时Err数值，用于PI调节中积分器设置
                    double ErrX1History = 0;
                    double ErrY1History = 0;
                    double ErrZ1History = 0;

                    if (Num > 0)
                    {
                        ErrX1History = DataExport[6][Num-1];
                        ErrY1History = DataExport[7][Num - 1];
                        ErrZ1History = DataExport[8][Num - 1];
                    }
                    #endregion
                    //获得当前两个目标点在机器人末端坐标系下坐标
                    MathFun.PIControlStewart(
                        ErrX1, ErrY1, ErrZ1,
                        PVal, IVal,
                        ErrX1History,ErrY1History,ErrZ1History,
                        ref TotalErrStewartX1, ref TotalErrStewartY1, ref TotalErrStewartZ1,
                        out FBOutX1, out FBOutY1, out FBOutZ1
                        );

                    x1NedNow = FBOutX1 + FactX1;
                    y1NedNow = FBOutY1 + FactY1;
                    z1NedNow = FBOutZ1 + FactZ1;

                    

                    //点2
                    double FBOutX2 = 0;
                    double FBOutY2 = 0;
                    double FBOutZ2 = 0;


                    #region 记录上一次反馈时Err数值，用于PI调节中积分器设置
                    double ErrX2History = 0;
                    double ErrY2History = 0;
                    double ErrZ2History = 0;

                    if (Num > 0)
                    {
                        ErrX2History = DataExport[16][Num - 1];
                        ErrY2History = DataExport[17][Num - 1];
                        ErrZ2History = DataExport[18][Num - 1];
                    }
                    #endregion
                    MathFun.PIControlStewart(
                        ErrX2, ErrY2, ErrZ2,
                        PVal, IVal,
                        ErrX2History, ErrY2History, ErrZ2History,
                        ref TotalErrStewartX2, ref TotalErrStewartY2, ref TotalErrStewartZ2,
                        out FBOutX2, out FBOutY2, out FBOutZ2
                        );

                    x2NedNow = FBOutX2 + FactX2;
                    y2NedNow = FBOutY2 + FactY2;
                    z2NedNow = FBOutZ2 + FactZ2;

                    //调整次数+1
                    Num++;




                    //询问是否中途保存
                    MessageBoxButtons SaveButton = MessageBoxButtons.OKCancel;
                    DialogResult dr = MessageBox.Show("确定要保存吗？", "提示", SaveButton);
                    if (dr == DialogResult.OK)
                    {
                        string ExcelName =
                         "yyyy_MM_dd_HH_mm_ss";
                        ExcelFun.ExportExcelGeneral(TableName, DataExport, ExcelName, ".xls"
                            );
                        MessageBox.Show("保存完毕", "保存完毕");
                    }

                }

            }

        }

        /// <summary>
        /// 输入目标球的index
        /// 输出其与套筒直线的误差
        /// 包含均值滤波环节，因此需在多线程中使用
        /// 均在机器人末端坐标系下进行
        /// </summary>
        /// <param name="TargetIndexIn"></param>
        /// <param name="ErrXOut"></param>
        /// <param name="ErrYOut"></param>
        /// <param name="ErrZOut"></param>
        private void NeedleStewartErrSolve(
            double xNed,double yNed,double zNed,
            vtkMatrix4x4 TransMatrixToolOld2NDI,
            out double FactX,out double FactY,out double FactZ,
            out double GoalX,out double GoalY,out double GoalZ,
            out double ErrXOut,out double ErrYOut,out double ErrZOut 
            )
        {
            /******************************
            //目标点1误差
            *******************************/
            

            //在当前的机器人末端坐标系下，目标点的坐标
            double xNedNew = 0;
            double yNedNew = 0;
            double zNedNew = 0;

            


            //求出当前误差
            double FootPointX = 0;
            double FootPointY = 0;
            double FootPointZ = 0;

            //目标，通过程序计算得出
            GoalX = 0;
            GoalY = 0;
            GoalZ = 0;



            #region 均值滤波环节
            int i = 0;
            int NumFilter = 20; int TimeWaitFilter = 50;
            for (i = 0; i < NumFilter; i++)
            {
                //进行变换
                xNedNew = xNed;
                yNedNew = yNed;
                zNedNew = zNed;

                #region 旧机器人末端坐标系到旧机器人工具端坐标系
                xNedNew -= Stewart.CooTransBiasVector.X;
                yNedNew -= Stewart.CooTransBiasVector.Y;
                zNedNew -= Stewart.CooTransBiasVector.Z;
                #endregion

                #region 旧机器人工具端坐标系到新机器人工具端坐标系

                vtkMatrix4x4 TransMatrixTool2NDINew = new vtkMatrix4x4();
                TransMatrixTool2NDINew.DeepCopy(StewartExpe.TransMatrixTool2NDI);
                //NDI到新机器人工具端坐标系转换矩阵
                vtkMatrix4x4 TransMatrixNDI2ToolNew = new vtkMatrix4x4();
                TransMatrixNDI2ToolNew.DeepCopy(TransMatrixTool2NDINew);
                TransMatrixNDI2ToolNew.Invert();


                MathFun.point3dtrans(TransMatrixToolOld2NDI,
                    ref xNedNew, ref yNedNew, ref zNedNew);
                MathFun.point3dtrans(TransMatrixNDI2ToolNew,
                    ref xNedNew, ref yNedNew, ref zNedNew);
                #endregion
                #region 新机器人工具端坐标系到新机器人末端坐标系
                xNedNew += Stewart.CooTransBiasVector.X;
                yNedNew += Stewart.CooTransBiasVector.Y;
                zNedNew += Stewart.CooTransBiasVector.Z;
                #endregion

                //开始叠加
                GoalX += xNedNew;
                GoalY += yNedNew;
                GoalZ += zNedNew;


                System.Threading.Thread.Sleep(TimeWaitFilter);
            }
            GoalX /= NumFilter;
            GoalY /= NumFilter;
            GoalZ /= NumFilter;
            #endregion


            //在机器人末端坐标系下进行
            MathFun.Point2LineFoot3D(
                GoalX,
                GoalY,
                GoalZ,

                Stewart.SleevePoint1CADStewart.X,
                Stewart.SleevePoint1CADStewart.Y,
                Stewart.SleevePoint1CADStewart.Z,

                Stewart.SleevePoint2CADStewart.X,
                Stewart.SleevePoint2CADStewart.Y,
                Stewart.SleevePoint2CADStewart.Z,

                out FootPointX, out FootPointY, out FootPointZ
                );

            


            //求出实际
            FactX = FootPointX;
            FactY = FootPointY;
            FactZ = FootPointZ;


            ErrXOut = GoalX - FactX;
            ErrYOut = GoalY - FactY;
            ErrZOut = GoalZ - FactZ;


        }

        /// <summary>
        /// 输入两个目标点（在机构之外,坐标为机器人末端坐标系），算出stewart平台需要到达的目标点
        /// 控制stewart平台到达目标位置和姿态
        /// 开环控制时用
        /// </summary>
        /// <param name="x1Ned"></param>
        /// <param name="y1Ned"></param>
        /// <param name="z1Ned"></param>
        /// <param name="x2Ned"></param>
        /// <param name="y2Ned"></param>
        /// <param name="z2Ned"></param>
        private void NeedleMoveStewartSingle(
            double x1Ned, double y1Ned, double z1Ned,
            double x2Ned, double y2Ned, double z2Ned,
            double DisBalls
            )
        {
            vtkMatrix4x4 TargetPosiStewartCoo = new vtkMatrix4x4();

            Point3D SleevePoint1New = new Point3D();
            Point3D SleevePoint2New = new Point3D();
            Point3D StewartPointNew = new Point3D();

            //TODO:开环控制中没有判断正反向功能
            //NeedleMoveTransMatrixSolve(
            //x1Ned, y1Ned, z1Ned,
            //x2Ned, y2Ned, z2Ned,
            //DisBalls,
            //IsForwardPos,
            //out SleevePoint1New,
            //out SleevePoint2New,
            //out StewartPointNew,
            //out TargetPosiStewartCoo
            //);

            vtkMatrix4x4 TransPosiMatrix = new vtkMatrix4x4();

            TransPosiMatrix = TargetPosiStewartCoo;






            //将TransMatrixTool2Stewart变量导出
            IOFun.ParamsWrite(
                "TransPosiMatrix",
                "1,2,3,4为第一行第1，2，3，4列的；以此类推",
                TransPosiMatrix.GetElement(0, 0),
                TransPosiMatrix.GetElement(0, 1),
                TransPosiMatrix.GetElement(0, 2),
                TransPosiMatrix.GetElement(0, 3),

                TransPosiMatrix.GetElement(1, 0),
                TransPosiMatrix.GetElement(1, 1),
                TransPosiMatrix.GetElement(1, 2),
                TransPosiMatrix.GetElement(1, 3),

                TransPosiMatrix.GetElement(2, 0),
                TransPosiMatrix.GetElement(2, 1),
                TransPosiMatrix.GetElement(2, 2),
                TransPosiMatrix.GetElement(2, 3),

                TransPosiMatrix.GetElement(3, 0),
                TransPosiMatrix.GetElement(3, 1),
                TransPosiMatrix.GetElement(3, 2),
                TransPosiMatrix.GetElement(3, 3)
                );







            string Mege1 =
                    TargetPosiStewartCoo.GetElement(0,0)+","
                    +TargetPosiStewartCoo.GetElement(0, 1) + ","
                    + TargetPosiStewartCoo.GetElement(0, 2) + ","
                    + TargetPosiStewartCoo.GetElement(0, 3) + ","
                    + "\n"+
                    TargetPosiStewartCoo.GetElement(1, 0) + ","
                    + TargetPosiStewartCoo.GetElement(1, 1) + ","
                    + TargetPosiStewartCoo.GetElement(1, 2) + ","
                    + TargetPosiStewartCoo.GetElement(1, 3) + ","
                    + "\n"+
                    TargetPosiStewartCoo.GetElement(2, 0) + ","
                    + TargetPosiStewartCoo.GetElement(2, 1) + ","
                    + TargetPosiStewartCoo.GetElement(2, 2) + ","
                    + TargetPosiStewartCoo.GetElement(2, 3) + ","
                    + "\n" +
                    TargetPosiStewartCoo.GetElement(3, 0) + ","
                    + TargetPosiStewartCoo.GetElement(3, 1) + ","
                    + TargetPosiStewartCoo.GetElement(3, 2) + ","
                    + TargetPosiStewartCoo.GetElement(3, 3) + ","
                    + "\n" 
                    ;
            MessageBox.Show(Mege1, "提示");

            double SleevePoint1NewTestX = Stewart.SleevePoint1CADStewart.X;
            double SleevePoint1NewTestY = Stewart.SleevePoint1CADStewart.Y;
            double SleevePoint1NewTestZ = Stewart.SleevePoint1CADStewart.Z;

            double SleevePoint2NewTestX = Stewart.SleevePoint2CADStewart.X;
            double SleevePoint2NewTestY = Stewart.SleevePoint2CADStewart.Y;
            double SleevePoint2NewTestZ = Stewart.SleevePoint2CADStewart.Z;

            double StewartPointNewTestX = Stewart.StewartPointCADStewart.X;
            double StewartPointNewTestY = Stewart.StewartPointCADStewart.Y;
            double StewartPointNewTestZ = Stewart.StewartPointCADStewart.Z;

            MathFun.point3dtrans(TransPosiMatrix,
                ref SleevePoint1NewTestX,
                ref SleevePoint1NewTestY,
                ref SleevePoint1NewTestZ);

            MathFun.point3dtrans(TransPosiMatrix,
                ref SleevePoint2NewTestX,
                ref SleevePoint2NewTestY,
                ref SleevePoint2NewTestZ);

            MathFun.point3dtrans(TransPosiMatrix,
                ref StewartPointNewTestX,
                ref StewartPointNewTestY,
                ref StewartPointNewTestZ);


            








            //赋给机器人的角度值
            double EulerAngleX = 0;
            double EulerAngleY = 0;
            double EulerAngleZ = 0;
            //赋给机器人的平动值
            double TransX = 0;
            double TransY = 0;
            double TransZ = 0;
            //目标矩阵转化为欧拉角，顺序ZYX
            MathFun.TransMatrix2Euler(
                TargetPosiStewartCoo,
                out EulerAngleX, out EulerAngleY, out EulerAngleZ
                );
            MathFun.TransMatrix2Translation(
                TargetPosiStewartCoo,
                out TransX, out TransY, out TransZ
                );

            //发送指令
            //运行时间和机器人ID设置
            const UInt16 RunTime = 300;
            const UInt16 ID = 1;

            //显示移动量

            string Mege =
                    "AngleX:" + EulerAngleX.ToString() + "\n"
                    + "AngleY:" + EulerAngleY.ToString() + "\n"
                    + "AngleZ:" + EulerAngleZ.ToString() + "\n"
                    +"TransX:" + TransX.ToString() + "\n"
                    + "TransY:" + TransY.ToString() + "\n"
                    + "TransZ:" + TransZ.ToString() + "\n"
                    + "第一个点目标X" + SleevePoint1New.X + "\n"
                    + "第一个点目标Y" + SleevePoint1New.Y + "\n"
                    + "第一个点目标Z" + SleevePoint1New.Z + "\n"
                    + "第二个点目标X" + SleevePoint2New.X + "\n"
                    + "第二个点目标Y" + SleevePoint2New.Y + "\n"
                    + "第二个点目标Z" + SleevePoint2New.Z + "\n"
                    + "第三个点目标X" + StewartPointNew.X + "\n"
                    + "第三个点目标Y" + StewartPointNew.Y + "\n"
                    + "第三个点目标Z" + StewartPointNew.Z + "\n"
                    ;
            MessageBox.Show(Mege, "提示");


            //发送指令
            //机器人末端的x方向反了，平动和旋转均加上负号
            Motor.CommandStewart(
                -EulerAngleX,EulerAngleY,EulerAngleZ,
                -TransX,TransY,TransZ,
                RunTime,
                ID
                );






        }




        /// <summary>
        /// 输入两个目标点（在机构之外,坐标为机器人末端坐标系），算出stewart平台需要到达的目标点
        /// 控制stewart平台到达目标位置和姿态
        /// 闭环反馈控制时用，作为一个模块出现
        /// </summary>
        /// <param name="x1Ned"></param>
        /// <param name="y1Ned"></param>
        /// <param name="z1Ned"></param>
        /// <param name="x2Ned"></param>
        /// <param name="y2Ned"></param>
        /// <param name="z2Ned"></param>
        /// <param name="DisBalls"></param>
        private void NeedleMoveStewartModule(
            double x1Ned, double y1Ned, double z1Ned,
            double x2Ned, double y2Ned, double z2Ned,
            double DisBalls,
            int IsForwardPos,
            ref vtkMatrix4x4 TransMatrixTotal,
            out double TransXOut,out double TransYOut,out double TransZOut,
            out double EulerAngleXOut, out double EulerAngleYOut, out double EulerAngleZOut
            )
        {


            vtkMatrix4x4 TargetPosiStewartCoo = new vtkMatrix4x4();

            Point3D SleevePoint1New = new Point3D();
            Point3D SleevePoint2New = new Point3D();
            Point3D StewartPointNew = new Point3D();

            //这里认为Ned1点离机构更近，而2点离机构更远
            NeedleMoveTransMatrixSolve(
            x1Ned, y1Ned, z1Ned,
            x2Ned, y2Ned, z2Ned,
            DisBalls,
            IsForwardPos,
            out SleevePoint1New,
            out SleevePoint2New,
            out StewartPointNew,
            out TargetPosiStewartCoo
            );

            vtkMatrix4x4 TransPosiMatrix = new vtkMatrix4x4();

            TransPosiMatrix = TargetPosiStewartCoo;



            //赋给机器人的角度值
            double EulerAngleX = 0;
            double EulerAngleY = 0;
            double EulerAngleZ = 0;
            //赋给机器人的平动值
            double TransX = 0;
            double TransY = 0;
            double TransZ = 0;
            //目标矩阵转化为欧拉角，顺序ZYX
            MathFun.TransMatrix2Euler(
                TransPosiMatrix,
                out EulerAngleX, out EulerAngleY, out EulerAngleZ
                );
            MathFun.TransMatrix2Translation(
                TransPosiMatrix,
                out TransX, out TransY, out TransZ
                );


            //新矩阵左乘
            vtkMatrix4x4 ExecuteMatrix = new vtkMatrix4x4();
            MathFun.Euler2TransMatrixTotal(
                -TransX, TransY, TransZ,
                -EulerAngleX, EulerAngleY, EulerAngleZ,
                out ExecuteMatrix
                );

            vtkMatrix4x4 temp = new vtkMatrix4x4();
            temp.DeepCopy(TransMatrixTotal);

            vtkMatrix4x4.Multiply4x4(
                ExecuteMatrix, temp, TransMatrixTotal
                );


            //目标矩阵转化为欧拉角，顺序ZYX
            //这里矩阵是total矩阵
            MathFun.TransMatrix2Euler(
                TransMatrixTotal,
                out EulerAngleX, out EulerAngleY, out EulerAngleZ
                );
            MathFun.TransMatrix2Translation(
                TransMatrixTotal,
                out TransX, out TransY, out TransZ
                );




            //将TransMatrixTool2Stewart变量导出
            IOFun.ParamsWrite(
                "TransPosiMatrix",
                "1,2,3,4为第一行第1，2，3，4列的；以此类推",
                TransPosiMatrix.GetElement(0, 0),
                TransPosiMatrix.GetElement(0, 1),
                TransPosiMatrix.GetElement(0, 2),
                TransPosiMatrix.GetElement(0, 3),

                TransPosiMatrix.GetElement(1, 0),
                TransPosiMatrix.GetElement(1, 1),
                TransPosiMatrix.GetElement(1, 2),
                TransPosiMatrix.GetElement(1, 3),

                TransPosiMatrix.GetElement(2, 0),
                TransPosiMatrix.GetElement(2, 1),
                TransPosiMatrix.GetElement(2, 2),
                TransPosiMatrix.GetElement(2, 3),

                TransPosiMatrix.GetElement(3, 0),
                TransPosiMatrix.GetElement(3, 1),
                TransPosiMatrix.GetElement(3, 2),
                TransPosiMatrix.GetElement(3, 3)
                );







            //string Mege1 =
            //        TransMatrixTotal.GetElement(0, 0) + ","
            //        + TransMatrixTotal.GetElement(0, 1) + ","
            //        + TransMatrixTotal.GetElement(0, 2) + ","
            //        + TransMatrixTotal.GetElement(0, 3) + ","
            //        + "\n" +
            //        TransMatrixTotal.GetElement(1, 0) + ","
            //        + TransMatrixTotal.GetElement(1, 1) + ","
            //        + TransMatrixTotal.GetElement(1, 2) + ","
            //        + TransMatrixTotal.GetElement(1, 3) + ","
            //        + "\n" +
            //        TransMatrixTotal.GetElement(2, 0) + ","
            //        + TransMatrixTotal.GetElement(2, 1) + ","
            //        + TransMatrixTotal.GetElement(2, 2) + ","
            //        + TransMatrixTotal.GetElement(2, 3) + ","
            //        + "\n" +
            //        TransMatrixTotal.GetElement(3, 0) + ","
            //        + TransMatrixTotal.GetElement(3, 1) + ","
            //        + TransMatrixTotal.GetElement(3, 2) + ","
            //        + TransMatrixTotal.GetElement(3, 3) + ","
            //        + "\n"
            //        ;
            //MessageBox.Show(Mege1, "提示");

            double SleevePoint1NewTestX = Stewart.SleevePoint1CADStewart.X;
            double SleevePoint1NewTestY = Stewart.SleevePoint1CADStewart.Y;
            double SleevePoint1NewTestZ = Stewart.SleevePoint1CADStewart.Z;

            double SleevePoint2NewTestX = Stewart.SleevePoint2CADStewart.X;
            double SleevePoint2NewTestY = Stewart.SleevePoint2CADStewart.Y;
            double SleevePoint2NewTestZ = Stewart.SleevePoint2CADStewart.Z;

            double StewartPointNewTestX = Stewart.StewartPointCADStewart.X;
            double StewartPointNewTestY = Stewart.StewartPointCADStewart.Y;
            double StewartPointNewTestZ = Stewart.StewartPointCADStewart.Z;

            MathFun.point3dtrans(TransPosiMatrix,
                ref SleevePoint1NewTestX,
                ref SleevePoint1NewTestY,
                ref SleevePoint1NewTestZ);

            MathFun.point3dtrans(TransPosiMatrix,
                ref SleevePoint2NewTestX,
                ref SleevePoint2NewTestY,
                ref SleevePoint2NewTestZ);

            MathFun.point3dtrans(TransPosiMatrix,
                ref StewartPointNewTestX,
                ref StewartPointNewTestY,
                ref StewartPointNewTestZ);





            //发送指令
            //运行时间和机器人ID设置
            const UInt16 RunTime = 300;
            const UInt16 ID = 1;

            //显示移动量

            string Mege =
                    "AngleX:" + EulerAngleX.ToString() + "\n"
                    + "AngleY:" + EulerAngleY.ToString() + "\n"
                    + "AngleZ:" + EulerAngleZ.ToString() + "\n"
                    + "TransX:" + TransX.ToString() + "\n"
                    + "TransY:" + TransY.ToString() + "\n"
                    + "TransZ:" + TransZ.ToString() + "\n"
                    + "第一个点目标X" + SleevePoint1New.X + "\n"
                    + "第一个点目标Y" + SleevePoint1New.Y + "\n"
                    + "第一个点目标Z" + SleevePoint1New.Z + "\n"
                    + "第二个点目标X" + SleevePoint2New.X + "\n"
                    + "第二个点目标Y" + SleevePoint2New.Y + "\n"
                    + "第二个点目标Z" + SleevePoint2New.Z + "\n"
                    + "第三个点目标X" + StewartPointNew.X + "\n"
                    + "第三个点目标Y" + StewartPointNew.Y + "\n"
                    + "第三个点目标Z" + StewartPointNew.Z + "\n"
                    ;
            MessageBox.Show(Mege, "提示");


            //发送指令
            Motor.CommandStewart(
                EulerAngleX, EulerAngleY, EulerAngleZ,
                TransX, TransY, TransZ,
                RunTime,
                ID
                );


            TransXOut = TransX;
            TransYOut = TransY;
            TransZOut = TransZ;

            EulerAngleXOut = EulerAngleX;
            EulerAngleYOut = EulerAngleY;
            EulerAngleZOut = EulerAngleZ;


        }


        /// <summary>
        /// 并联机器人控制程序中，负责求解变换矩阵的模块
        /// </summary>
        /// <param name="x1NedIn"></param>
        /// <param name="y1NedIn"></param>
        /// <param name="z1NedIn"></param>
        /// <param name="x2NedIn"></param>
        /// <param name="y2NedIn"></param>
        /// <param name="z2NedIn"></param>
        /// <param name="DisBallsIn"></param>
        /// <param name="SleevePoint1New"></param>
        /// <param name="SleevePoint2New"></param>
        /// <param name="StewartPointNew"></param>
        /// <param name="TargetPosiStewartCoo"></param>
        private void NeedleMoveTransMatrixSolve(
            double x1NedIn, double y1NedIn, double z1NedIn,
            double x2NedIn, double y2NedIn, double z2NedIn,
            double DisBallsIn,
            int IsForwardPos,
            out Point3D SleevePoint1New,
            out Point3D SleevePoint2New,
            out Point3D StewartPointNew,
            out vtkMatrix4x4 TargetPosiStewartCoo
            )
        {
            //两个目标小钢珠转化为P3D类型
            Point3D TargetPoint1 = new Point3D(x1NedIn, y1NedIn, z1NedIn);
            Point3D TargetPoint2 = new Point3D(x2NedIn, y2NedIn, z2NedIn);
            //Stewart机构上三点的分别的目标点声明
            SleevePoint1New = new Point3D();
            SleevePoint2New = new Point3D();

            StewartPointNew = new Point3D();


            //计算目标直线的方向向量
            //由Ned2点指向Ned1点
            Vector3D LineVector = new Vector3D();
            LineVector.X = x1NedIn - x2NedIn;
            LineVector.Y = y1NedIn - y2NedIn;
            LineVector.Z = z1NedIn - z2NedIn;
            LineVector.Normalize();
            //计算点2（套筒上）的目标位置：通过给定其与最近点的距离而定，且其在目标直线上
            //这里认为Ned1点离机构更近，而2点离机构更远
            //DisBalls即为Ned1点和套筒上点2的距离
            SleevePoint2New = LineVector * DisBallsIn + TargetPoint1;

            //计算点1（套筒上）的目标位置：通过方向向量            

            //计算点1的位置
            SleevePoint1New = SleevePoint2New + Stewart.SleeveLength * LineVector;


            double StewartPlaneA = 0;
            double StewartPlaneB = 0;
            double StewartPlaneC = 0;
            double StewartPlaneD = 0;

            Point3D CircleCenterNew = new Point3D();

            //第三点计算
            ToolRedundancyPointCal(
            SleevePoint1New,
            SleevePoint2New,
            LineVector,
            IsForwardPos,
            out StewartPlaneA, out StewartPlaneB, out StewartPlaneC, out StewartPlaneD,
            out CircleCenterNew, out StewartPointNew
            );




            //根据三个点的目标位置算出目标位姿，并转换到官方的平台坐标系下
            //原始位姿，机构坐标系
            vtkMatrix4x4 OriginPosi = new vtkMatrix4x4();
            //根据顺位和逆位的不同，这里的原始坐标系建立也不一样
            //顺位
            if (IsForwardPos == 1)
            {
                OriginPosi = MathFun.BulidCoordinateSystem(
                Stewart.SleevePoint1CADStewart, Stewart.SleevePoint2CADStewart, Stewart.StewartPointCADStewart);
            }
            //逆位
            else
            {
                OriginPosi = MathFun.BulidCoordinateSystem(
                Stewart.SleevePoint2CADStewart, Stewart.SleevePoint1CADStewart, Stewart.StewartPointCADStewart);
            }
            

            //求逆
            vtkMatrix4x4 OriginPosiInvert = new vtkMatrix4x4();
            OriginPosiInvert.DeepCopy(OriginPosi);
            OriginPosiInvert.Invert();

            //目标位姿，机构坐标系
            vtkMatrix4x4 TargetPosi = MathFun.BulidCoordinateSystem(
                SleevePoint1New, SleevePoint2New, StewartPointNew);

            //变换矩阵
            vtkMatrix4x4 TransPosiMatrix = new vtkMatrix4x4();
            vtkMatrix4x4.Multiply4x4(TargetPosi, OriginPosiInvert, TransPosiMatrix);
  

            TargetPosiStewartCoo = new vtkMatrix4x4();//目标位姿，官方坐标系
            TargetPosiStewartCoo = TransPosiMatrix;


            string Comment =
                "第1个：p1_N" + "\r" +
                "第2个：p2_N" + "\r" +
                "第3个：p3_N" + "\r" +
                "第4个：sp_A" + "\r" +
                "第5个：sp_B" + "\r" +
                "第6个：sp_C" + "\r" +
                "第7个：sp_D" + "\r" +
                "第8个：CR" + "\r" +
                "第9个：CCN" + "\r" +
                "第10个：p1Ned" + "\r" +
                "第11个：p2Ned" + "\r";

            IOFun.ParamsWrite(
                "StewartParaStream" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm"),
                Comment,
                SleevePoint1New,
                SleevePoint2New,
                StewartPointNew,
                StewartPlaneA,
                StewartPlaneB,
                StewartPlaneC,
                StewartPlaneD,
                Stewart.CircleRadius,
                CircleCenterNew,
                TargetPoint1,
                TargetPoint2
                );



        }


        /// <summary>
        /// 求工具端第三个点的函数
        /// 输入之前求出的套筒点1（新）和套筒直线方向向量
        /// 输出第三个点的坐标，机构坐标系;平面的四个参数；
        /// </summary>
        private void ToolRedundancyPointCal(
            Point3D SleevePoint1NewIn,
            Point3D SleevePoint2NewIn,
            Vector3D LineVectorIn,
            int IsForwardPos,
            out double StewartPlaneAOut,out double StewartPlaneBOut,out double StewartPlaneCOut, out double StewartPlaneDOut,
            out Point3D CircleCenterNewOut,out Point3D StewartPointNewOut
            )
        {
            Point3D SleevePointNewBehind = new Point3D();
            //根据顺位还是逆位决定求点3的方式
            //1:顺位,赋值为SleevePoint1NewIn
            if (IsForwardPos == 1)
            {
                SleevePointNewBehind.X = SleevePoint1NewIn.X;
                SleevePointNewBehind.Y = SleevePoint1NewIn.Y;
                SleevePointNewBehind.Z = SleevePoint1NewIn.Z;
            }
            //0：逆位，赋值为SleevePoint2NewIn
            else
            {
                SleevePointNewBehind.X = SleevePoint2NewIn.X;
                SleevePointNewBehind.Y = SleevePoint2NewIn.Y;
                SleevePointNewBehind.Z = SleevePoint2NewIn.Z;
            }



            //计算点3（套筒外）的目标位置：投影
            //计算目标圆心



            CircleCenterNewOut = SleevePointNewBehind + Stewart.CircleCenterBiasLength * LineVectorIn;



            //1.表示出该圆所在的平面（点法式）
            //目标平面
            StewartPlaneAOut = LineVectorIn.X;
            StewartPlaneBOut = LineVectorIn.Y;
            StewartPlaneCOut = LineVectorIn.Z;
            StewartPlaneDOut = -(
                LineVectorIn.X * CircleCenterNewOut.X
                + LineVectorIn.Y * CircleCenterNewOut.Y
                + LineVectorIn.Z * CircleCenterNewOut.Z);



            //2.当前位置的第三点向目标平面投影
            double xProject = 0;
            double yProject = 0;
            double zProject = 0;
            MathFun.PointProject2Plane3D(
                Stewart.StewartPointCADStewart.X, Stewart.StewartPointCADStewart.Y, Stewart.StewartPointCADStewart.Z,
                StewartPlaneAOut, StewartPlaneBOut, StewartPlaneCOut, StewartPlaneDOut,
                out xProject, out yProject, out zProject
                );


            Point3D PointProject = new Point3D(xProject, yProject, zProject);//表示投影目标点





            //3.投影目标点与圆心连线，交点为第三点的目标点
            double ProjectRadius = (CircleCenterNewOut - PointProject).Length;

            StewartPointNewOut = (Stewart.CircleRadius / ProjectRadius) * (PointProject - CircleCenterNewOut) + CircleCenterNewOut;//第三点目标位置


        }
  


        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (Motor.isOpen)
            {
                try
                {
                    Motor.sp.Write(Motor.CommandArray,0,18);
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
            Motor.MotorConnect();
            if (btnOpenCom.Text == "打开电机串口")
            {
                btnOpenCom.Text = "关闭电机串口";
            }
            else {
                btnOpenCom.Text = "打开电机串口";
            }
        
        }

        private void PrepareBtn_Click(object sender, EventArgs e)
        {
            PrepareWork();
        }

        private void px1Box_TextChanged(object sender, EventArgs e)
        {

        }

        private void sp_DataReceived(object sender, EventArgs e)
        {
            System.Threading.Thread.Sleep(100); //延迟100ms，等待接受数据完成

            //this.Invoke 就是跨线程访问 ui 的方法
            this.Invoke((EventHandler)(delegate
            {
                if (Motor.isHex == false)
                {
                }
                else
                {
                    Byte[] ReceivedData = new Byte[Motor.sp.BytesToRead]; //创建接收字节数组 
                    Motor.sp.Read(ReceivedData, 0, ReceivedData.Length);
                    String RecvDataText = null;
                    for (int i = 0; i < ReceivedData.Length; i++)
                    {
                        RecvDataText += ("0x" + ReceivedData[i].ToString("X2") + "");
                    }
                    
                }
                Motor.sp.DiscardInBuffer();//丢弃接收缓冲区数据
            }));
        }

        private void btnCleanData_Click(object sender, EventArgs e)
        {
            CommandMotorFollowExpeThread = new Thread(new ThreadStart(CommandMotorFollowExpeFunction));
            CommandMotorFollowExpeThread.SetApartmentState(ApartmentState.STA); //重点
            CommandMotorFollowExpeThread.Start();//启动新线程

     
            


            #region 反解
            //double xp = 0; 
            //double yp = 0;
            //double zp = 92.34;

            //double x = 0;
            //double y = 0;
            //double z = 0;

            //double roll = 0;
            //double pitch = 0;
            //double yaw = 0;

            //double rUp = 124;
            //double rDown = 500;

            //double[] UpAngleArray =
            //    {
            //    -133.5,
            //    -46.5,
            //    -13.5,
            //     73.5,
            //    106.5,
            //    193.5};

            //double[] DownAngleArray =
            //    {
            //    -110,
            //    -70,
            //    10,
            //    50,
            //    130,
            //    170};


            //double[] LenArray = new double[6];
            //Vector3D[] DirArray = new Vector3D[6];
            //Motor.StewartInverseKinematic(
            //    roll,pitch,yaw,
            //    x,y,z,
            //    xp,yp,zp,
            //    rUp,rDown,
            //    UpAngleArray,DownAngleArray,
            //    out LenArray,out DirArray
            //    );

            #endregion





        }

        private void CommandMotorFollowExpeFunction() 
        {
            UInt16 pos = 500;
            int num = 10;
            for (int i = 0; i < num; i++)
            {
                pos = Convert.ToUInt16(500 + i * 50);
                Motor.CommandAllMotorFollow
                (
                pos,
                pos,
                pos,
                pos,
                pos,
                pos
                );
                System.Threading.Thread.Sleep(200);

            }
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

        private void Status2Box_TextChanged(object sender, EventArgs e)
        {

        }

        private void OTConnect_Click(object sender, EventArgs e)
        {
            OptitrackConnect();
        }

        /// <summary>
        /// OT连接，会开新线程
        /// </summary>
        private void OptitrackConnect() {
            Optitrack.OptitrackInit();
            Optitrack.OptitrackThread = new Thread(new ThreadStart(OptitrackTrack));
            Optitrack.OptitrackThread.Start();

        }

        /// <summary>
        /// OT追踪tracker时使用
        /// </summary>
        private void OptitrackTrack()
        {
            //只追踪tracker的场景下使用
            while (true)
            {
                Optitrack.Updata();
                
                Console.WriteLine(Optitrack.OptitrackData[7].ToString());
                if (InvokeRequired)
                {
                    //Invoke(new Optitrack.ShowOptitrackData(ShowTrackDataOptitrack), new Object[] { Optitrack.OptitrackData });
                }
                else
                {
                    //ShowTrackDataOptitrack(Optitrack.OptitrackData);
                }
            }

        }

        #region OptiTrack显示数据的函数
        /// <summary>
        /// OptiTrack显示数据的函数
        /// 显示tracker这个刚体的位置与姿态以及上面的点的位置
        /// </summary>
        /// <param name="OptitrackData"></param>
        //private void ShowTrackDataOptitrack(double[] OptitrackData) {

        //    //i=0时是机构，i=1时是针，i=2时是骨盆
        //    const int i = 2;


        //    X.Text = OptitrackData[0 + 24 * i].ToString();
        //    Y.Text = OptitrackData[1 + 24 * i].ToString();
        //    Z.Text = OptitrackData[2 + 24 * i].ToString();
        //    Qx.Text = OptitrackData[3 + 24 * i].ToString();
        //    Qy.Text = OptitrackData[4 + 24 * i].ToString();
        //    Qz.Text = OptitrackData[5 + 24 * i].ToString();
        //    Qw.Text = OptitrackData[6 + 24 * i].ToString();
        //    TrackerMotive.Text = OptitrackData[7 + 24 * i].ToString();
        //    //TrackerMotive.Text = OptitrackData[72].ToString();

        //    X1.Text = OptitrackData[8 + 24 * i].ToString();
        //    Y1.Text = OptitrackData[9 + 24 * i].ToString();
        //    Z1.Text = OptitrackData[10 + 24 * i].ToString();
        //    Status1.Text = OptitrackData[11 + 24 * i].ToString();

        //    //X1.Text = OptitrackData[73].ToString();
        //    //Y1.Text = OptitrackData[74].ToString();
        //    //Z1.Text = OptitrackData[75].ToString();          

        //    X2.Text = OptitrackData[12 + 24 * i].ToString();
        //    Y2.Text = OptitrackData[13 + 24 * i].ToString();
        //    Z2.Text = OptitrackData[14 + 24 * i].ToString();
        //    Status2.Text = OptitrackData[15 + 24 * i].ToString();

        //    //X2.Text = OptitrackData[76].ToString();
        //    //Y2.Text = OptitrackData[77].ToString();
        //    //Z2.Text = OptitrackData[78].ToString();

        //    X3.Text = OptitrackData[16 + 24 * i].ToString();
        //    Y3.Text = OptitrackData[17 + 24 * i].ToString();
        //    Z3.Text = OptitrackData[18 + 24 * i].ToString();
        //    Status3.Text = OptitrackData[19 + 24 * i].ToString();

        //    X4.Text = OptitrackData[20 + 24 * i].ToString();
        //    Y4.Text = OptitrackData[21 + 24 * i].ToString();
        //    Z4.Text = OptitrackData[22 + 24 * i].ToString();
        //    Status4.Text = OptitrackData[23 + 24 * i].ToString();



        //    //骨盆上的操作
        //    PelvisExpe.CT2PelvisOT();

        //    //显示骨盆上的点在机构坐标系下坐标
        //    axBox.Text = PelvisExpe.ax.ToString();
        //    ayBox.Text = PelvisExpe.ay.ToString();
        //    azBox.Text = PelvisExpe.az.ToString();

        //    bxBox.Text = PelvisExpe.bx.ToString();
        //    byBox.Text = PelvisExpe.by.ToString();
        //    bzBox.Text = PelvisExpe.bz.ToString();

        //    cxBox.Text = PelvisExpe.cx.ToString();
        //    cyBox.Text = PelvisExpe.cy.ToString();
        //    czBox.Text = PelvisExpe.cz.ToString();

        //    dxBox.Text = PelvisExpe.dx.ToString();
        //    dyBox.Text = PelvisExpe.dy.ToString();
        //    dzBox.Text = PelvisExpe.dz.ToString();

        //    //显示三个tracker状态，0正常，256失踪
        //    //机构
        //    Status1Box.Text = OptitrackData[7 + 0 * 24].ToString();
        //    //针
        //    Status2Box.Text = OptitrackData[7 + 1 * 24].ToString();
        //    //骨盆
        //    Status3Box.Text = OptitrackData[7 + 2 * 24].ToString();



           
        //    //针上的点变换
        //    double x1N = PelvisExpe.x1Nd, y1N = PelvisExpe.y1Nd, z1N = PelvisExpe.z1Nd;
        //    double x2N = PelvisExpe.x2Nd, y2N = PelvisExpe.y2Nd, z2N = PelvisExpe.z2Nd;


        //    Optitrack.OTCooTransformNeedle(ref x1N, ref y1N, ref z1N);
        //    Optitrack.OTCooTransformNeedle(ref x2N, ref y2N, ref z2N);

        //    Optitrack.OTCooTransformMechanism(ref x1N, ref y1N, ref z1N);
        //    Optitrack.OTCooTransformMechanism(ref x2N, ref y2N, ref z2N);



        //    double xg1, yg1, zg1;
        //    double xg2, yg2, zg2;

        //    MathFun.CooPointProject2Plane(x1N, y1N, z1N,
        //    x2N, y2N, z2N,
        //    Mechanism.aPlane1, Mechanism.bPlane1, Mechanism.cPlane1, Mechanism.dPlane1,
        //    Mechanism.aPlane2, Mechanism.bPlane2, Mechanism.cPlane2, Mechanism.dPlane2,
        //    out xg1, out yg1, out zg1,
        //    out xg2, out yg2, out zg2);

        //    lock (GlobalRecordLock)
        //    {
        //        X1GlobalRecord = xg1;
        //        Y1GlobalRecord = yg1;
        //        Z1GlobalRecord = zg1;

        //        X2GlobalRecord = xg2;
        //        Y2GlobalRecord = yg2;
        //        Z2GlobalRecord = zg2;
        //    }



        //    //double a;//全局
        //    //a = xg1;//在这个函数里

        //    //FactX = a;
        //    //进程互锁：保证一个变量同时只被一个进程读或写


        //    px1Box.Text = X1GlobalRecord.ToString();
        //    py1Box.Text = Y1GlobalRecord.ToString();
        //    pz1Box.Text = Z1GlobalRecord.ToString();

        //    px2Box.Text = X2GlobalRecord.ToString();
        //    py2Box.Text = Y2GlobalRecord.ToString();
        //    pz2Box.Text = Z2GlobalRecord.ToString();



        //}
        #endregion
        /// <summary>
        /// 测试Optitrack用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
  

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_2(object sender, EventArgs e)
        {

        }

        private void Z1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            ExportParams();
            MessageBox.Show("已经导出参数！", "导出");
        }

        /// <summary>
        /// 导出所有参数存放在.txt中
        /// </summary>
        private void ExportParams() {
            //导出NDI参数
            //NDI.NDIParamsExport();
            //导出Optitrack参数
            //Optitrack.OTParamsExport();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ImportParams();
            MessageBox.Show("已经导入参数！", "导入");
        }

        /// <summary>
        /// 导入.txt中所有参数，位置与.exe在同一路径
        /// </summary>
        private void ImportParams() {
            //导入NDI参数
            //NDI.NDIParamsImport();
            //导入OT参数
            //Optitrack.OTParamsImport();
            //导入Stewart平台内参
            Stewart.StewartParamsImport();
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
            //if (Motor.isOpen)
            //{
            //    try
            //    {

            //        Motor.CommandBoardCastAuto((UInt16)ElecMachine1Position.Value,
            //            (UInt16)ElecMachine2Position.Value,
            //            (UInt16)ElecMachine11Position.Value,
            //            (UInt16)ElecMachine12Position.Value);
            //    }
            //    catch (Exception)
            //    {
            //        MessageBox.Show("发送数据时发生错误", "Error");
            //    }
            //}
            //else
            //{
            //    MessageBox.Show("串口未打开", "Error");
            //}
        }


        /// <summary>
        /// 测试用按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click_1(object sender, EventArgs e)
        {
            #region MathFun.PointProject2Plane3D测试
            //double xp = 0;
            //double yp = 0;
            //double zp = 0;
            //MathFun.PointProject2Plane3D(
            //    3,2,5,
            //    0,0,1,0,
            //    out xp, out yp, out zp
            //    );

            //string Mege =
            //    "xp:" + xp.ToString() + "\n"
            //    + "yp:" + yp.ToString() + "\n"
            //    + "zp:" + zp.ToString() + "\n"
            //    ;
            //MessageBox.Show(Mege, "提示");
            #endregion

            #region 采集某个点的坐标，测试被动臂是否有晃动
            //TestExpeThread = new Thread(new ThreadStart(TestPointAQ));
            //TestExpeThread.SetApartmentState(ApartmentState.STA); //重点
            //TestExpeThread.Start();//启动新线程
            #endregion


            StewartExpeThread = new Thread(new ThreadStart(StewartErrorExpeFunction));
            StewartExpeThread.SetApartmentState(ApartmentState.STA); //重点
            StewartExpeThread.Start();//启动新线程



            #region
            //ExpeThread = new Thread(new ThreadStart(ExpeContinue));
            //ExpeThread.SetApartmentState(ApartmentState.STA); //重点
            //ExpeThread.Start();//启动新线程
            //double[] xx = {
            //};
            //double[] yy = {
            //};
            //double[] zz = {  
            //};
            //int Len = zz.Length;

            //List<Point3D> Datas = new List<Point3D>();
            //int i = 0;
            //for (i = 0; i < Len; i++) {
            //    Point3D Ptemp = new Point3D(xx[i], yy[i], zz[i]);
            //    Datas.Add(Ptemp);
            //}

            //double x0, y0, z0;
            //double aLine, bLine, cLine;

            //MathFun.LeastSquareLine3D(Datas,
            //    out x0,out y0,out z0,
            //    out aLine,out bLine,out cLine);

            //string Mege =
            //    "x0:" + x0.ToString() + "\n"
            //    + "y0:" + y0.ToString() + "\n"
            //    + "z0:" + z0.ToString() + "\n"
            //    +"aLine:" + aLine.ToString() + "\n"
            //    + "bLine:" + bLine.ToString() + "\n"
            //    + "cLine:" + cLine.ToString() + "\n"
            //    ;
            //        MessageBox.Show(Mege, "提示"); 

            //ppt来自https://wenku.baidu.com/view/195e17d6195f312b3169a546.html?re=view
            //34页
            //double aPlane, bPlane, cPlane, dPlane;
            //double x0, y0, z0, aLine, bLine, cLine;
            //double xi, yi, zi;

            //aPlane = 1;
            //bPlane = 0;//k
            //cPlane = -5;
            //dPlane = -10;

            //x0 = 5;
            //y0 = -4;
            //z0 = 1;

            //aLine = 1;
            //bLine = -2;
            //cLine = 3;


            //MathFun.LinePlaneIntersect2SolvePoint(aPlane, bPlane, cPlane, dPlane,
            //    x0, y0, z0, aLine, bLine, cLine,
            //    out xi, out yi, out zi);

            //string Mege =
            //    "xi:" + xi.ToString() + "\n"
            //    + "yi:" + yi.ToString() + "\n"
            //    + "zi:" + zi.ToString() + "\n"
            //    ;
            //MessageBox.Show(Mege, "提示");
            #endregion


        }


        private void StewartErrorExpeFunction()
        {
            #region 机器人运动方向标定,最终需要在盒子坐标系中表达
            ushort RunTime = 300;
            ushort ID = 1;


            int xPointNum = 50;
            int yPointNum = 50;
            int zPointNum = 50;

            //调节范围为正负maxlen
            double xMaxLen = 22.6;
            double yMaxLen = 24;
            double zMaxLen = 8.1;


            double MaxLenTemp = 0;
            double MaxPointNumTemp = 0;


            //记录数据
            //机器人末端坐标系，理想位置
            double[] PointCooArrayStewartGoalX = new double[xPointNum + yPointNum + zPointNum];
            double[] PointCooArrayStewartGoalY = new double[xPointNum + yPointNum + zPointNum];
            double[] PointCooArrayStewartGoalZ = new double[xPointNum + yPointNum + zPointNum];
            //机器人末端坐标系，实际位置
            double[] PointCooArrayStewartFactX = new double[xPointNum + yPointNum + zPointNum];
            double[] PointCooArrayStewartFactY = new double[xPointNum + yPointNum + zPointNum];
            double[] PointCooArrayStewartFactZ = new double[xPointNum + yPointNum + zPointNum];
            //盒子坐标系，理想位置
            double[] PointCooArrayBoxGoalX = new double[xPointNum + yPointNum + zPointNum];
            double[] PointCooArrayBoxGoalY = new double[xPointNum + yPointNum + zPointNum];
            double[] PointCooArrayBoxGoalZ = new double[xPointNum + yPointNum + zPointNum];
            //盒子坐标系，实际位置
            double[] PointCooArrayBoxFactX = new double[xPointNum + yPointNum + zPointNum];
            double[] PointCooArrayBoxFactY = new double[xPointNum + yPointNum + zPointNum];
            double[] PointCooArrayBoxFactZ = new double[xPointNum + yPointNum + zPointNum];

            //记录初始的机器人工具端坐标系
            vtkMatrix4x4 MatrixToolCooOrigin = new vtkMatrix4x4();
            MatrixToolCooOrigin.DeepCopy(StewartExpe.TransMatrixTool2NDI);


            MessageBox.Show("开始实验", "开始实验");


            #region 求出机器人（原）末端坐标系的理想位置

            //x轴
            MaxLenTemp = xMaxLen;
            MaxPointNumTemp = xPointNum;
            for (int i = 0; i < MaxPointNumTemp; i++)
            {
                PointCooArrayStewartGoalX[i] = -MaxLenTemp + i * (MaxLenTemp * 2 / MaxPointNumTemp);
                PointCooArrayStewartGoalY[i] = 0;
                PointCooArrayStewartGoalZ[i] = 0;
            }


            //y轴
            MaxLenTemp = yMaxLen;
            MaxPointNumTemp = yPointNum;
            for (int i = 0; i < MaxPointNumTemp; i++)
            {
                PointCooArrayStewartGoalX[i + xPointNum] = 0;
                PointCooArrayStewartGoalY[i + xPointNum] = -MaxLenTemp + i * (MaxLenTemp * 2 / MaxPointNumTemp);
                PointCooArrayStewartGoalZ[i + xPointNum] = 0;
            }


            //z轴
            MaxLenTemp = zMaxLen;
            MaxPointNumTemp = zPointNum;
            for (int i = 0; i < MaxPointNumTemp; i++)
            {
                PointCooArrayStewartGoalX[i + xPointNum + yPointNum] = 0;
                PointCooArrayStewartGoalY[i + xPointNum + yPointNum] = 0;
                PointCooArrayStewartGoalZ[i + xPointNum + yPointNum] = -MaxLenTemp + i * (MaxLenTemp * 2 / MaxPointNumTemp);
            }
            #endregion

            #region 求出盒子坐标系下点理想位置
            for (int i = 0; i < xPointNum + yPointNum + zPointNum; i++)
            {
                PointCooTransStewart2Box(
                    PointCooArrayStewartGoalX[i],
                    PointCooArrayStewartGoalY[i],
                    PointCooArrayStewartGoalZ[i],
                    out PointCooArrayBoxGoalX[i],
                    out PointCooArrayBoxGoalY[i],
                    out PointCooArrayBoxGoalZ[i]
                    );

            }
            #endregion

            #region 求出盒子坐标系下的点的实际位置 
            Point3D RefPoint = new Point3D();
            double RefPointOriginX = 0;
            double RefPointOriginY = 0;
            double RefPointOriginZ = 0;

            RefPoint.X = RefPointOriginX;
            RefPoint.Y = RefPointOriginY;
            RefPoint.Z = RefPointOriginZ;

            #region 标定x轴
            MaxLenTemp = xMaxLen;
            MaxPointNumTemp = xPointNum;

            //归零
            Motor.CommandStewart(
                0, 0, 0,
                0, 0, 0,
                RunTime,
                ID
                );
            //等待到位
            System.Threading.Thread.Sleep(1000);
            //自动采点
            for (int i = 0; i < MaxPointNumTemp; i++)
            {
                RefPoint.X = RefPointOriginX;
                RefPoint.Y = RefPointOriginY;
                RefPoint.Z = RefPointOriginZ;

                double xLen = -MaxLenTemp + i * (MaxLenTemp * 2 / MaxPointNumTemp);
                Motor.CommandStewart(
                0, 0, 0,
                xLen, 0, 0,
                RunTime,
                ID
                );
                System.Threading.Thread.Sleep(1000);

                //参照点的机器人末端坐标系位置永远是（0，0，0）RefPoint
                //从机器人末端坐标系到机器人工具端坐标系
                RefPoint.X -= Stewart.CooTransBiasVector.X;
                RefPoint.Y -= Stewart.CooTransBiasVector.Y;
                RefPoint.Z -= Stewart.CooTransBiasVector.Z;
                //从机器人工具端坐标系到盒子坐标系
                double RefPointX = RefPoint.X;
                double RefPointY = RefPoint.Y;
                double RefPointZ = RefPoint.Z;

                MathFun.point3dtrans(StewartExpe.TransMatrixTool2Box(),
                    ref RefPointX, ref RefPointY, ref RefPointZ);

                RefPoint.X = RefPointX;
                RefPoint.Y = RefPointY;
                RefPoint.Z = RefPointZ;
                //记录坐标
                PointCooArrayBoxFactX[i] = RefPoint.X;
                PointCooArrayBoxFactY[i] = RefPoint.Y;
                PointCooArrayBoxFactZ[i] = RefPoint.Z;




            }
            #endregion

            MessageBox.Show("x轴完成", "x轴完成");


            #region 标定y轴
            MaxLenTemp = yMaxLen;
            MaxPointNumTemp = yPointNum;
            //归零
            Motor.CommandStewart(
                0, 0, 0,
                0, 0, 0,
                RunTime,
                ID
                );
            //等待到位
            System.Threading.Thread.Sleep(1000);
            //自动采点
            for (int i = 0; i < MaxPointNumTemp; i++)
            {
                RefPoint.X = RefPointOriginX;
                RefPoint.Y = RefPointOriginY;
                RefPoint.Z = RefPointOriginZ;


                double yLen = -MaxLenTemp + i * (MaxLenTemp * 2 / MaxPointNumTemp);
                Motor.CommandStewart(
                0, 0, 0,
                0, yLen, 0,
                RunTime,
                ID
                );
                System.Threading.Thread.Sleep(1000);
                //参照点的机器人末端坐标系位置永远是（0，0，0）RefPoint
                //从机器人末端坐标系到机器人工具端坐标系
                RefPoint.X -= Stewart.CooTransBiasVector.X;
                RefPoint.Y -= Stewart.CooTransBiasVector.Y;
                RefPoint.Z -= Stewart.CooTransBiasVector.Z;
                //从机器人工具端坐标系到盒子坐标系
                double RefPointX = RefPoint.X;
                double RefPointY = RefPoint.Y;
                double RefPointZ = RefPoint.Z;

                MathFun.point3dtrans(StewartExpe.TransMatrixTool2Box(),
                    ref RefPointX, ref RefPointY, ref RefPointZ);

                RefPoint.X = RefPointX;
                RefPoint.Y = RefPointY;
                RefPoint.Z = RefPointZ;
                //记录坐标
                PointCooArrayBoxFactX[i + xPointNum] = RefPoint.X;
                PointCooArrayBoxFactY[i + xPointNum] = RefPoint.Y;
                PointCooArrayBoxFactZ[i + xPointNum] = RefPoint.Z;



            }
            #endregion

            MessageBox.Show("y轴完成", "y轴完成");


            #region 标定z轴
            MaxLenTemp = zMaxLen;
            MaxPointNumTemp = zPointNum;
            //归零
            Motor.CommandStewart(
                0, 0, 0,
                0, 0, 0,
                RunTime,
                ID
                );
            //等待到位
            System.Threading.Thread.Sleep(1000);
            //自动采点
            for (int i = 0; i < MaxPointNumTemp; i++)
            {
                RefPoint.X = RefPointOriginX;
                RefPoint.Y = RefPointOriginY;
                RefPoint.Z = RefPointOriginZ;


                double zLen = -MaxLenTemp + i * (MaxLenTemp * 2 / MaxPointNumTemp);
                Motor.CommandStewart(
                0, 0, 0,
                0, 0, zLen,
                RunTime,
                ID
                );
                System.Threading.Thread.Sleep(1000);

                //参照点的机器人末端坐标系位置永远是（0，0，0）RefPoint
                //从机器人末端坐标系到机器人工具端坐标系
                RefPoint.X -= Stewart.CooTransBiasVector.X;
                RefPoint.Y -= Stewart.CooTransBiasVector.Y;
                RefPoint.Z -= Stewart.CooTransBiasVector.Z;
                //从机器人工具端坐标系到盒子坐标系
                double RefPointX = RefPoint.X;
                double RefPointY = RefPoint.Y;
                double RefPointZ = RefPoint.Z;

                MathFun.point3dtrans(StewartExpe.TransMatrixTool2Box(),
                    ref RefPointX, ref RefPointY, ref RefPointZ);

                RefPoint.X = RefPointX;
                RefPoint.Y = RefPointY;
                RefPoint.Z = RefPointZ;
                //记录坐标
                PointCooArrayBoxFactX[i + xPointNum + yPointNum] = RefPoint.X;
                PointCooArrayBoxFactY[i + xPointNum + yPointNum] = RefPoint.Y;
                PointCooArrayBoxFactZ[i + xPointNum + yPointNum] = RefPoint.Z;


            }
            #endregion

            MessageBox.Show("z轴完成", "z轴完成");


            #endregion

            #region 求出机器人(原)末端坐标系下点的实际位置
            vtkMatrix4x4 MatrixBox2OldTool = new vtkMatrix4x4();

            vtkMatrix4x4 MatrixNDI2ToolOrigin = new vtkMatrix4x4();
            MatrixNDI2ToolOrigin.DeepCopy(MatrixToolCooOrigin);
            MatrixNDI2ToolOrigin.Invert();


            vtkMatrix4x4.Multiply4x4
                (
                MatrixNDI2ToolOrigin,
                StewartExpe.TransMatrixBox2NDI,
                MatrixBox2OldTool);

            for (int i = 0; i < xPointNum + yPointNum + zPointNum; i++)
            {
                PointCooArrayStewartFactX[i] = PointCooArrayBoxFactX[i];
                PointCooArrayStewartFactY[i] = PointCooArrayBoxFactY[i];
                PointCooArrayStewartFactZ[i] = PointCooArrayBoxFactZ[i];

                MathFun.point3dtrans(MatrixBox2OldTool,
                    ref PointCooArrayStewartFactX[i],
                    ref PointCooArrayStewartFactY[i],
                    ref PointCooArrayStewartFactZ[i]);

                PointCooArrayStewartFactX[i] += Stewart.CooTransBiasVector.X;
                PointCooArrayStewartFactY[i] += Stewart.CooTransBiasVector.Y;
                PointCooArrayStewartFactZ[i] += Stewart.CooTransBiasVector.Z;


                //坐标系符号反了
                PointCooArrayStewartFactX[i] *= -1;
                PointCooArrayStewartFactY[i] *= 1;
                PointCooArrayStewartFactZ[i] *= 1;
            }
            #endregion

            MessageBox.Show("末端实际完成", "末端实际完成");

            #region 表头
            string[] OutputDataName = new string[12];

            OutputDataName[0] = "StewartGoalX";
            OutputDataName[1] = "StewartGoalY";
            OutputDataName[2] = "StewartGoalZ";

            OutputDataName[3] = "StewartFactX";
            OutputDataName[4] = "StewartFactY";
            OutputDataName[5] = "StewartFactZ";

            OutputDataName[6] = "BoxGoalX";
            OutputDataName[7] = "BoxGoalY";
            OutputDataName[8] = "BoxGoalZ";

            OutputDataName[9] = "BoxFactX";
            OutputDataName[10] = "BoxFactY";
            OutputDataName[11] = "BoxFactZ";
            #endregion

            #region 输出结果
            double[][] OutPutDataExpe = new double[12][];

            OutPutDataExpe[0] = PointCooArrayStewartGoalX;
            OutPutDataExpe[1] = PointCooArrayStewartGoalY;
            OutPutDataExpe[2] = PointCooArrayStewartGoalZ;

            OutPutDataExpe[3] = PointCooArrayStewartFactX;
            OutPutDataExpe[4] = PointCooArrayStewartFactY;
            OutPutDataExpe[5] = PointCooArrayStewartFactZ;

            OutPutDataExpe[6] = PointCooArrayBoxGoalX;
            OutPutDataExpe[7] = PointCooArrayBoxGoalY;
            OutPutDataExpe[8] = PointCooArrayBoxGoalZ;

            OutPutDataExpe[9] = PointCooArrayBoxFactX;
            OutPutDataExpe[10] = PointCooArrayBoxFactY;
            OutPutDataExpe[11] = PointCooArrayBoxFactZ;

            #endregion

            ExcelFun.ExportExcelGeneral(
                OutputDataName,
                OutPutDataExpe,
                "yyyy_MM_dd_HH_mm_ss"
                );

            MessageBox.Show("保存完成", "保存完成");






            #endregion
        }


        /// <summary>
        /// 输入一个点在并联机器人末端坐标系下的坐标
        /// 获得其在盒子坐标系下的坐标
        /// </summary>
        /// <param name="xStewartIn"></param>
        /// <param name="yStewartIn"></param>
        /// <param name="zStewartIn"></param>
        /// <param name="xBoxOut"></param>
        /// <param name="yBoxOut"></param>
        /// <param name="zBoxOut"></param>
        private void PointCooTransStewart2Box(
            double xStewartIn, double yStewartIn, double zStewartIn,
            out double xBoxOut, out double yBoxOut, out double zBoxOut
            )
        {
            xBoxOut = 0;
            yBoxOut = 0;
            zBoxOut = 0;

            //机器人末端坐标系初始点坐标
            double xExpeOrigin = xStewartIn;
            double yExpeOrigin = yStewartIn;
            double zExpeOrigin = zStewartIn;
            //将初始点转化为盒子坐标系下坐标
            //并且计算出目标点在盒子坐标系下坐标
            Point3D PExpeOrigin = new Point3D();
            PExpeOrigin.X = xExpeOrigin;
            PExpeOrigin.Y = yExpeOrigin;
            PExpeOrigin.Z = zExpeOrigin;
            //末端坐标系转化到工具端坐标系
            Point3D PExpeOriginToolCoo = PExpeOrigin;
            PExpeOriginToolCoo.X -= Stewart.CooTransBiasVector.X;
            PExpeOriginToolCoo.Y -= Stewart.CooTransBiasVector.Y;
            PExpeOriginToolCoo.Z -= Stewart.CooTransBiasVector.Z;
            //工具端坐标系转化到盒子坐标系
            Point3D PExpeOriginBoxCoo = new Point3D();
            PExpeOriginBoxCoo.X = PExpeOriginToolCoo.X;
            PExpeOriginBoxCoo.Y = PExpeOriginToolCoo.Y;
            PExpeOriginBoxCoo.Z = PExpeOriginToolCoo.Z;

            double PExpeOriginBoxCooX = PExpeOriginBoxCoo.X;
            double PExpeOriginBoxCooY = PExpeOriginBoxCoo.Y;
            double PExpeOriginBoxCooZ = PExpeOriginBoxCoo.Z;

            vtkMatrix4x4 MatrixTool2BoxCooOrigin = new vtkMatrix4x4();//初始工具坐标系到盒子坐标系的转换矩阵
            MatrixTool2BoxCooOrigin.DeepCopy(StewartExpe.TransMatrixTool2Box());

            MathFun.point3dtrans(MatrixTool2BoxCooOrigin,
                ref PExpeOriginBoxCooX, ref PExpeOriginBoxCooY, ref PExpeOriginBoxCooZ);

            PExpeOriginBoxCoo.X = PExpeOriginBoxCooX;
            PExpeOriginBoxCoo.Y = PExpeOriginBoxCooY;
            PExpeOriginBoxCoo.Z = PExpeOriginBoxCooZ;


            xBoxOut = PExpeOriginBoxCoo.X;
            yBoxOut = PExpeOriginBoxCoo.Y;
            zBoxOut = PExpeOriginBoxCoo.Z;
        }



        /// <summary>
        /// 采集盒子上某点在机器人工具端坐标系下坐标，使用均值滤波
        /// 用来检验被动臂是否晃动
        /// </summary>
        private void TestPointAQ()
        {
            int TestPointIndex = 0;//0-5

            double TestCooX = 0;
            double TestCooY = 0;
            double TestCooZ = 0;


            //均值滤波环节
            int i = 0;
            int NumFilter = 20; int TimeWaitFilter = 50;
            for (i = 0; i < NumFilter; i++)
            {
                //开始叠加
                TestCooX += StewartExpe.TargetPointArrayToolCoo[TestPointIndex].X;
                TestCooY += StewartExpe.TargetPointArrayToolCoo[TestPointIndex].Y;
                TestCooZ += StewartExpe.TargetPointArrayToolCoo[TestPointIndex].Z;


                System.Threading.Thread.Sleep(TimeWaitFilter);
            }
            TestCooX /= NumFilter;
            TestCooY /= NumFilter;
            TestCooZ /= NumFilter;



            string Mege =
                    "TestCooX:" + TestCooX.ToString() + "\n"
                    + "TestCooY:" + TestCooY.ToString() + "\n"
                    + "TestCooZ:" + TestCooZ.ToString() + "\n"
                    ;
            MessageBox.Show(Mege, "提示");






        }




        MathCalculate mathcal = new MathCalculate();
        Calibration cal = new Calibration();
        Circles circles = new Circles();
        //VTK vtk = new VTK();
        //MatrixOperator mo = new MatrixOperator();
        string filename;
        double[,] imagea = new double[10, 2];
        double[,] imageb = new double[10, 2];
        double[,] coorda = new double[8, 3];//标志点在正位空间坐标
        double[,] coordb = new double[8, 3];//标志点在侧位空间坐标
        double[] pointIn = new double[3];//入点
        double[] pointOut = new double[3];//出点

        double[] trackdata = new double[27];

        private void button4_Click(object sender, EventArgs e)
        {
            filename = "18.bmp";
            circles.HoughCircles(filename, 10);            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            imagea = circles.XCirclesSort();
        }

        private void CommandStewartBtn_Click(object sender, EventArgs e)
        {
            CommandStewartTotalFunction();
        }

        private void NeedleCtrlBtn_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 向机器人平台发送指令的总函数
        /// </summary>
        private void CommandStewartTotalFunction()
        {

            //角度设置，绕动轴，为欧拉角
            double AngleXSet = Convert.ToDouble(AngleXSetNumValue.Value);
            double AngleYSet = Convert.ToDouble(AngleYSetNumValue.Value);
            double AngleZSet = Convert.ToDouble(AngleZSetNumValue.Value);

            //平动
            double TransXSet = Convert.ToDouble(TransXSetNumValue.Value);
            double TransYSet = Convert.ToDouble(TransYSetNumValue.Value);
            double TransZSet = Convert.ToDouble(TransZSetNumValue.Value);

            //内参
            //double xp = 0; double yp = 0; double zp = 105.66;
            //double rUp = 20.72; double rDown = 27.19;
            //double MotorLenOri = 106.092;//电机及其连接件整体的基准长度，即500脉冲时长度
            //double[] UpAngleArray = {
            //    49.7376586849622,
            //    130.262341315038,
            //    169.742143627780,
            //    250.253163394574,
            //    289.746836605426,
            //    10.2578563722203
            //};
            //double[] DownAngleArray = {
            //    66.8497836705629,
            //    113.150216329437,
            //    186.821853077528,
            //    233.176451334680,
            //    306.823548665320,
            //    353.178146922472
            //};

            //输出量
            double[] LenArray = new double[6];
            Vector3D[] DirArray = new Vector3D[6];


            //TODO:电机伸到500长度时绝对长度
            Stewart.StewartInverseKinematic(
            AngleXSet, AngleYSet, AngleZSet,
            TransXSet, TransYSet, TransZSet,
            Stewart.xp, Stewart.yp, Stewart.zp,//trans和angle均为0时动平台相对于静平台的移动量
            Stewart.rUp, Stewart.rDown,//上下圆盘半径,rUp相当于matlab中的R，rDown相当于matlab中的r。
            Stewart.UpAngleArray,
            Stewart.DownAngleArray,
            out LenArray,
            out DirArray
            );


            //将电机绝对长度转化为脉冲
            const int MotorNum = 6;
            UInt16[] MotorCommandArray = new UInt16[MotorNum];
            double tempLen = 0;
            for (int i = 0; i < MotorNum; i++)
            {
                tempLen = LenArray[i] - Stewart.MotorLenOri;
                MotorCommandArray[i] = Convert.ToUInt16(tempLen * Motor.MotorMaxNum / Motor.MotorMaxLength + 500);
            }

            string Mege2 =
                "angleX:" + AngleXSet.ToString() + "\n" +
                "angleY:" + AngleYSet.ToString() + "\n" +
                "angleZ:" + AngleZSet.ToString() + "\n" +
                "translationX:" + TransXSet.ToString() + "\n" +
                "translationY:" + TransYSet.ToString() + "\n" +
                "translationZ:" + TransZSet.ToString() + "\n" +
                "Motor1:" + MotorCommandArray[0].ToString() + "\n" +
                "Motor2:" + MotorCommandArray[1].ToString() + "\n" +
                "Motor3:" + MotorCommandArray[2].ToString() + "\n" +
                "Motor4:" + MotorCommandArray[3].ToString() + "\n" +
                "Motor5:" + MotorCommandArray[4].ToString() + "\n" +
                "Motor6:" + MotorCommandArray[5].ToString() + "\n"
                ;
            MessageBox.Show(Mege2, "提示");

            //发送指令
            Motor.CommandAllMotor(
                MotorCommandArray[0],
                MotorCommandArray[1],
                MotorCommandArray[2],
                MotorCommandArray[3],
                MotorCommandArray[4],
                MotorCommandArray[5]
                );
        }


        /// <summary>
        /// 控制运动总函数，开了多线程，以应对延时
        /// </summary>
        private void StewartExpeTotalFunction() {
            NeedleStewartThread = new Thread(new ThreadStart(StewartExpeExecuteFunction));
            NeedleStewartThread.SetApartmentState(ApartmentState.STA); //重点
            NeedleStewartThread.Start();//启动新线程

        }

        /// <summary>
        /// 并联机器人控制运动的多线程执行的函数
        /// </summary>
        private void StewartExpeExecuteFunction()
        {
            //TODO:多次重复实验，验证无方向性功能是否正确

            #region 输入的点的坐标
            //转换成机器人末端坐标系
            //TargetIndex为全局变量，在61行可以找到
            double x1Ball = StewartExpe.TargetPointArrayToolCoo[Target1Index].X + Stewart.CooTransBiasVector.X;
            double y1Ball = StewartExpe.TargetPointArrayToolCoo[Target1Index].Y + Stewart.CooTransBiasVector.Y;
            double z1Ball = StewartExpe.TargetPointArrayToolCoo[Target1Index].Z + Stewart.CooTransBiasVector.Z;

            double x2Ball = StewartExpe.TargetPointArrayToolCoo[Target2Index].X + Stewart.CooTransBiasVector.X;
            double y2Ball = StewartExpe.TargetPointArrayToolCoo[Target2Index].Y + Stewart.CooTransBiasVector.Y;
            double z2Ball = StewartExpe.TargetPointArrayToolCoo[Target2Index].Z + Stewart.CooTransBiasVector.Z;
            #endregion
            //计算点1和点2相对于机构的距离
            double p1NedDis = 0;
            double p2NedDis = 0;

            p1NedDis = PointNeedleDisCal(
            x1Ball, y1Ball, z1Ball
            );
            p2NedDis = PointNeedleDisCal(
            x2Ball, y2Ball, z2Ball
            );



            double x1Ned = 0;
            double y1Ned = 0;
            double z1Ned = 0;

            double x2Ned = 0;
            double y2Ned = 0;
            double z2Ned = 0;
            //点1更近
            if (p1NedDis <= p2NedDis)
            {
                //x1Ned更近
                x1Ned = x1Ball;
                y1Ned = y1Ball;
                z1Ned = z1Ball;

                x2Ned = x2Ball;
                y2Ned = y2Ball;
                z2Ned = z2Ball;
            }
            //点2更近
            else
            {
                //x1Ned更近
                x1Ned = x2Ball;
                y1Ned = y2Ball;
                z1Ned = z2Ball;

                x2Ned = x1Ball;
                y2Ned = y1Ball;
                z2Ned = z1Ball;
            }


            Point3D p1Ned = new Point3D(x1Ned, y1Ned, z1Ned);
            Vector3D DisVector1 = (Vector3D)p1Ned - (Vector3D)Stewart.SleevePoint2CADStewart;
            Vector3D DisVector2 = (Vector3D)p1Ned - (Vector3D)Stewart.SleevePoint1CADStewart;


            double DisBalls = Math.Min(DisVector1.Length,DisVector2.Length);


            //输入机器人末端坐标系坐标
            //NeedleMoveStewartSingle(
            //    x1Ned, y1Ned, z1Ned,
            //    x2Ned, y2Ned, z2Ned,
            //    DisBalls
            //    );

            //Err = G-F
            //GNew = F+PVal*Err;P=1的话相当于一直给定目标点
            //1883行
            double PVal = 0.7; double IVal = 0.1;
            int MaxNum = 50;
            double ErrMin = 0.5;

            //输入的坐标点是机器人末端坐标系的
            NeedleMoveStewartFeedbackControl(
                x1Ned, y1Ned, z1Ned,
                x2Ned, y2Ned, z2Ned,
                DisBalls,
                PVal, IVal,
                MaxNum,
                ErrMin
                );
        }

        private void SingleDimExpe_Click(object sender, EventArgs e)
        {

            StewartExpeTotalFunction();

        }

        private void Status3Box_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_3(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            filename = "17.bmp";
            circles.HoughCircles(filename, 10);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            imageb = circles.XCirclesSort();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            coorda = cal.GetLandmarkCoordinates();
        }

    

        private void button9_Click(object sender, EventArgs e)
        {
            coordb = cal.GetLandmarkCoordinates();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            //cal.CaculatePoint(imagea,imageb);
            double[,] imageIn = new double[2, 2] { { 1098, 428 }, { 1024, 418 } };//入点在正侧位图像上的坐标
            double[,] imageOut = new double[2, 2] { { 845, 521 }, { 1228, 506 } };//出点在正侧位图像上的坐标


            //double[,] coorda = new double[8, 3] { { -20,20,40},{ 20,20,40},{ 20,-20,40},{ -20,-20,40},{ -10,10,-40},{ 10,10,-40},{ 10,-10,-40},{-10,-10,-40 } };
            double[,] coorda = new double[8, 3] { { 729.639323134964, 164.676769388120, 251.102443376753 },{ 714.126536808046,    201.262013120979,    255.671185149131 },{ 672.648879025130, 183.919440553415,    253.711827708006 },{ 688.161665352048, 147.334196820556,    249.143085935628 },
                                                    {711.591288610430 ,  178.059698180228,    170.206425756931 },{703.834895446971,  196.352320046658,    172.490796643120 },{685.400380876786,  188.644510016630,    171.619971113731 },{693.156774040245,  170.351888150200,    169.335600227542 } };
            double[,] coordb = new double[8, 3] { { 688.964170067708,    368.452512757927,    133.357543187036 }, { 683.924058387450, 367.132506218839,    93.6983089913625 }, { 647.577707685090 ,   341.173340254142,    99.1814187876712 }, { 652.617819365348,    342.493346793230,    138.840652983345 },
                                                    { 722.818796041814,  292.543610539310 ,   121.495719376261}, {720.298740201685,  291.883607269766,    101.666102278424 }, { 704.144806556192,    280.346200174345,    104.103039965673},{706.664862396321,   281.006203443889 ,   123.932657063509 } };
            double[,] imagea = new double[8, 2] { { 703, 750 }, { 996, 699 }, { 1056, 1030 }, { 762, 1084 }, { 870, 833 }, { 1033, 804 }, { 1063, 968 }, { 899, 998 } };
            double[,] imageb = new double[8, 2] { { 882, 194 }, { 1157, 236 }, { 1111, 546 }, { 835, 504 }, { 975, 245 }, { 1128, 269 }, { 1105, 420 }, { 952, 397 } };
            //double[,] imagedata = new double[2, 2] {{ 1097, 427 }, { 1024,418}};

            //double[,] coorda =new double[8,3]{ {13.64,13.21,-12.23 },{13.64,25.58,0.14 },{13.64,34.42,8.98 },{13.64,39.72,14.29 },{-13.64,44.14,-9.58 },{-13.64,33.54,1.03 },{-13.64,26.64,8.1 },{-13.64,22.93,11.63 } };
            //double[,] imagea = new double[8, 2] { {431,566 }, {522,656 }, {587,719 }, {627, 758 }, {458,802 }, {537,720 }, {590,665 },{617,639 } };

            Matrix M1 = cal.CalculateMatrix(coorda, imagea);
            Matrix M2 = cal.CalculateMatrix(coordb, imageb);
            pointIn = cal.CalculatePoints(M1, M2, imageIn);
            pointOut = cal.CalculatePoints(M1, M2, imageOut);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button12_Click(object sender, EventArgs e)
        {
            //double[] p1 = { -140.53,-49.92,-147.5};
            //double[] p2 = { -212.26,-60.15,-149.92 };
            //showdata(p1, p2);
        }

        private void renderWindowControlTest_Load(object sender, EventArgs e)
        {
            VTKTest.Test1(renderWindowControlTest);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            int r = trackBar1.Value;
            circles.HoughCircles(filename, r);
        }

     
        int t = 100;
        private void button11_Click(object sender, EventArgs e)
        {
            vtk.SceneRendering1(renderWindowControl1, "area.stl");
            vtk.SceneRendering2(renderWindowControl2, "area.stl");

            //pointIn[0] = 0; pointIn[1] = 0; pointIn[2] = 0;//目标点
            //pointOut[0] = 10; pointOut[1] = 10; pointOut[2] = 10;

            pointIn[0] = Convert.ToDouble(NeedlePoint1X.Text);  pointIn[1] = Convert.ToDouble(NeedlePoint1Y.Text); pointIn[2] = Convert.ToDouble(NeedlePoint1Z.Text);//目标点
            pointOut[0] = Convert.ToDouble(NeedlePoint2X.Text); pointOut[1] = Convert.ToDouble(NeedlePoint2Y.Text); pointOut[2] = Convert.ToDouble(NeedlePoint2Z.Text);
            vtkMatrix4x4 targettrans = mathcal.GetMatrixFromQuaternionAndPosition(TargetTrackerPose, TargetTrackerPosi);
            mathcal.PointTtrans(targettrans, ref pointIn);
            mathcal.PointTtrans(targettrans, ref pointOut);


            VTK.sphereReg1.SetCenter(pointIn[0], pointIn[1], pointIn[2]);
            VTK.sphereActorReg1.SetVisibility(1);
            VTK.sphereReg2.SetCenter(pointOut[0], pointOut[1], pointOut[2]);
            VTK.sphereActorReg2.SetVisibility(1);
            VTK.sphereReg3.SetCenter(pointIn[0], pointIn[1], pointIn[2]);
            VTK.sphereActorReg3.SetVisibility(1);
            VTK.sphereReg4.SetCenter(pointOut[0], pointOut[1], pointOut[2]);
            VTK.sphereActorReg4.SetVisibility(1);
            
            //double[] v1 = new double[3] { pointOut[0] - pointIn[0], pointOut[1] - pointIn[1], pointOut[2] - pointIn[2] };
            double[] v1 = new double[3] { pointIn[0] - pointOut[0], pointIn[1] - pointOut[1], pointIn[2] - pointOut[2] };
            double[] v= mathcal.Vector3DNormalize(v1);
            VTK.lineSource1.SetPoint1(pointOut[0], pointOut[1], pointOut[2]);
            VTK.lineSource1.SetPoint2(pointIn[0] + v[0] * t, pointIn[1] + v[1] * t, pointIn[2] + v[2] * t);
            VTK.lineSource2.SetPoint1(pointOut[0], pointOut[1], pointOut[2]);
            VTK.lineSource2.SetPoint2(pointIn[0] + v[0] * t, pointIn[1] + v[1] * t, pointIn[2] + v[2] * t);

            VTK.tube1.SetInput(VTK.lineSource1.GetOutput());
            VTK.tube1.SetRadius(1.0);
            VTK.tube1.SetNumberOfSides(20);
            VTK.tube1.Update();
            vtkPolyDataMapper tube1Mapper = vtkPolyDataMapper.New();
            tube1Mapper.SetInput(VTK.tube1.GetOutput());
            VTK.tube1Actor.SetMapper(tube1Mapper);

            VTK.tube2.SetInput(VTK.lineSource2.GetOutput());
            VTK.tube2.SetRadius(1.0);
            VTK.tube2.SetNumberOfSides(20);
            VTK.tube2.Update();
            vtkPolyDataMapper tube2Mapper = vtkPolyDataMapper.New();
            tube2Mapper.SetInput(VTK.tube2.GetOutput());
            VTK.tube2Actor.SetMapper(tube2Mapper);

            VTK.tube1Actor.GetProperty().SetColor(0, 1, 0);
            VTK.tube2Actor.GetProperty().SetColor(0, 1, 0);

            VTK.renderer1.AddActor(VTK.tube1Actor);
            VTK.renderer2.AddActor(VTK.tube2Actor);

            double[] toolpoint1 = new double[3] { -18.3981685, -0.095453826, -30.4153698 };
            //double[] toolpoint2 = new double[3] { -18.51, 1.67, -157.4 };
            double[] toolpoint2 = new double[3] { -19.28325048, -0.616108987, -180.4118549 };
            //工具套筒显示
            vtkMatrix4x4 toolTrans = new vtkMatrix4x4();
            toolTrans = mathcal.GetMatrixFromQuaternionAndPosition(ToolTrackerPose, ToolTrackerPosi);
            mathcal.PointTtrans(toolTrans, ref toolpoint1);
            mathcal.PointTtrans(toolTrans, ref toolpoint2);

            double[] v2 = new double[3] { toolpoint1[0] - toolpoint2[0], toolpoint1[1] - toolpoint2[1], toolpoint1[2] - toolpoint2[2] };
            double[] vt = mathcal.Vector3DNormalize(v2);
            VTK.toollineSource1.SetPoint1(toolpoint2[0], toolpoint2[1], toolpoint2[2]);
            VTK.toollineSource1.SetPoint2(toolpoint1[0] + vt[0] * t, toolpoint1[1] + vt[1] * t, toolpoint1[2] + vt[2] * t);
            VTK.toollineSource2.SetPoint1(toolpoint2[0], toolpoint2[1], toolpoint2[2]);
            VTK.toollineSource2.SetPoint2(toolpoint1[0] + vt[0] * t, toolpoint1[1] + vt[1] * t, toolpoint1[2] + vt[2] * t);

            VTK.tooltube1.SetInput(VTK.toollineSource1.GetOutput());
            VTK.tooltube1.SetRadius(2.0);
            VTK.tooltube1.SetNumberOfSides(20);
            VTK.tooltube1.Update();
            vtkPolyDataMapper tooltube1Mapper = vtkPolyDataMapper.New();
            tooltube1Mapper.SetInput(VTK.tooltube1.GetOutput());
            VTK.tooltube1Actor.SetMapper(tooltube1Mapper);

            VTK.tooltube2.SetInput(VTK.toollineSource2.GetOutput());
            VTK.tooltube2.SetRadius(2.0);
            VTK.tooltube2.SetNumberOfSides(20);
            VTK.tooltube2.Update();
            vtkPolyDataMapper tooltube2Mapper = vtkPolyDataMapper.New();
            tooltube2Mapper.SetInput(VTK.tooltube2.GetOutput());
            VTK.tooltube2Actor.SetMapper(tooltube2Mapper);

            //VTK.tooltube1Actor.GetProperty().SetOpacity(0.5);
            //VTK.tooltube2Actor.GetProperty().SetOpacity(0.5);
            VTK.tooltube1Actor.GetProperty().SetColor(1, 1, 0);
            VTK.tooltube2Actor.GetProperty().SetColor(1, 1, 0);

            VTK.renderer1.AddActor(VTK.tooltube1Actor);
            VTK.renderer2.AddActor(VTK.tooltube2Actor);

            VTK.renderer1.ResetCamera();
            VTK.renderer2.ResetCamera();

            VTK.renderWindowInteractor1.Render();
            VTK.renderWindowInteractor2.Render();

            timer1.Enabled = true;

            //GetPointProject2Plane();
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            double[] a0 = new double[3] { 0, 0, 0 };
            double[] b0 = new double[3] { 20, -37.48, -149.18 };
            double[] c0 = new double[3] { -20, -37.48, -149.18 };
            vtkMatrix4x4 designMatrix = mathcal.BuildCoordinateSystem(a0, b0, c0);

            double[] a = new double[3] { 0, 0, 0 };
            double[] b = new double[3] { 19.42277802, -33.70830254, -149.6193807 };
            double[] c = new double[3] { -20.46277802, -36.73169746, -149.5806193 };
            vtkMatrix4x4 realMatrix = mathcal.BuildCoordinateSystem(a, b, c);
            vtkMatrix4x4 temp0 = new vtkMatrix4x4();
            temp0.DeepCopy(designMatrix);
            temp0.Invert();
            vtkMatrix4x4 transMatrix = new vtkMatrix4x4();
            vtkMatrix4x4.Multiply4x4(realMatrix, temp0, transMatrix);     
            
            vtkMatrix4x4 jgtrans = new vtkMatrix4x4();
            jgtrans = mathcal.GetMatrixFromQuaternionAndPosition(MechanismTrackerPose, MechanismTrackerPosi);
            vtkMatrix4x4 areaMatrix = new vtkMatrix4x4();
            vtkMatrix4x4.Multiply4x4(jgtrans, transMatrix, areaMatrix);

            VTK.transform1.SetMatrix(areaMatrix);
            VTK.transform2.SetMatrix(areaMatrix);

            double[] toolpoint1 = new double[3] { -18.3981685, -0.095453826, -30.4153698 };
            double[] toolpoint2 = new double[3] { -19.28325048, -0.616108987, -180.4118549 };
            //工具套筒显示
            vtkMatrix4x4 toolTrans = new vtkMatrix4x4();
            toolTrans = mathcal.GetMatrixFromQuaternionAndPosition(ToolTrackerPose, ToolTrackerPosi);
            mathcal.PointTtrans(toolTrans, ref toolpoint1);
            mathcal.PointTtrans(toolTrans, ref toolpoint2);
            
            double[] v2 = new double[3] { toolpoint1[0] - toolpoint2[0], toolpoint1[1] - toolpoint2[1], toolpoint1[2] - toolpoint2[2] };
            double[] vt = mathcal.Vector3DNormalize(v2);
            VTK.toollineSource1.SetPoint1(toolpoint2[0], toolpoint2[1], toolpoint2[2]);
            VTK.toollineSource1.SetPoint2(toolpoint1[0] + vt[0] * t, toolpoint1[1] + vt[1] * t, toolpoint1[2] + vt[2] * t);
            VTK.toollineSource2.SetPoint1(toolpoint2[0], toolpoint2[1], toolpoint2[2]);
            VTK.toollineSource2.SetPoint2(toolpoint1[0] + vt[0] * t, toolpoint1[1] + vt[1] * t, toolpoint1[2] + vt[2] * t);
            //VTK.tooltube1.Update();
            //VTK.tooltube2.Update();

            //VTK.renderer1.ResetCamera();
            //VTK.renderer2.ResetCamera();

            VTK.renderWindowInteractor1.Render();
            VTK.renderWindowInteractor2.Render();

            //GetPointProject2Plane();
        }

        private void renderWindowControl1_Load(object sender, EventArgs e)
        {
            vtk.InitialSceneRendering1(renderWindowControl1);
        }

        private void renderWindowControl2_Load(object sender, EventArgs e)
        {
            vtk.InitialSceneRendering2(renderWindowControl2);
        }



        //public static string filenameid;
        public static double[] CoordinateSystemPosi = new double[3];
        public static double[] CoordinateSystemPose = new double[4];

     
    }
}