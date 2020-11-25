using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Windows.Media.Media3D;
using Kitware.VTK;

namespace ChuanKouTest
{
    /// <summary>
    /// 与电机的底层串口通讯等相关
    /// </summary>
    public class Motor
    {

        //一些常量
        public const byte FrameHead1 = 0x55;
        public const byte FrameHead2 = 0xAA;

        public const UInt16 MotorMaxNum = 2000;//电机最大长度
        public const double MotorMaxLength = 30;//电机最大长度（mm）


        public static SerialPort sp = null;
        public static bool isOpen = false;
        public static bool isSetProperty = false;
        public static bool isHex = false;

        public static byte[] CommandArray = new byte[50];

        //在做旋转和平移时分别对应的寄存器
        //旋转
        public const UInt16 RegisterAngleX = 1100;
        public const UInt16 RegisterAngleY = 1102;
        public const UInt16 RegisterAngleZ = 1104;

        //平移
        //将x轴平移和y轴平移交换,官方程序是反的，这里纠正过来
        public const UInt16 RegisterTransX = 1108;
        public const UInt16 RegisterTransY = 1106;
        public const UInt16 RegisterTransZ = 1110;

        //枚举，表示控制并联机器人的哪个维度
        public enum MotionDim {
            AngleX, AngleY, AngleZ,
            TransX,TransY,TransZ,
        };






        /// <summary>
        /// 输入参数：
        /// 串口号、波特率、停止位、数据位、奇偶校验位
        /// </summary>
        /// <returns></returns>

        /// <summary>
        /// </summary>
        public static void SetPortProperty(string PortName, string BaudRate,
            string StopBit, string DataBits, string Pariti)
        {
            //输入参数：
            //串口号、波特率、停止位、数据位、奇偶校验位
            sp = new SerialPort();
            sp.PortName = PortName.Trim();
            sp.BaudRate = Convert.ToInt32(BaudRate.Trim());

            //设置停止位
            float f = Convert.ToSingle(StopBit.Trim());

            if (f == 0)
            {
                sp.StopBits = StopBits.None;
            }
            else if (f == 1.5)
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
            sp.DataBits = Convert.ToInt16(DataBits.Trim());

            string s = Pariti.Trim(); //设置奇偶校验位
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


            isHex = true;

            
        }

        /// <summary>
        /// 连接电机，综合性函数
        /// </summary>
        public static void MotorConnect()
        {
            string ComName = "Silicon Labs CP210x USB to UART Bridge";

            string COMStr = "COM" + SerialRecognition.GetSpecifiedSerialPortNum(ComName).ToString();
            //MessageBox.Show(COMStr, COMStr);
            //连接电机
            if (isOpen == false)
            {

                if (!isSetProperty) //串口
                {
                    //因时的并联机器人和自带的电控板波特率不一样
                    //自带：921600
                    //并联机器人：115200
                    SetPortProperty("COM3",
                        "921600",
                        "1",
                        "8",
                        "无");
                    isSetProperty = true;
                }

                try
                {
                    sp.Open();
                    isOpen = true;

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
                    

                }
                catch (Exception)
                {
                    MessageBox.Show("关闭串口时发生错误", "Error");
                }
            }
        }

        /// <summary>
        /// 输入一个电机的脉冲(最大2000)
        /// 输出对应电机伸长量(最大16mm)
        /// </summary>
        public static double Position2Length(UInt16 Position) {
            return Convert.ToDouble(Position) * MotorMaxLength / Convert.ToDouble(MotorMaxNum);
        }

        /// <summary>
        /// 输入对应电机伸长量(最大16mm)
        /// 输出一个电机的脉冲(最大2000)
        /// </summary>
        /// <param name="Length"></param>
        /// <returns></returns>
        public static UInt16 Length2Position(double Length) {
            return Convert.ToUInt16(Length * MotorMaxNum / MotorMaxLength);
        }




        //TODO:新增函数
        /// <summary>
        ///控制单个电机的伸缩
        ///输入电机的ID和位置，控制电机运动
        /// </summary>
        public static void CommandSingleMotor(UInt16 Position,UInt16 ID)
        {

            const int MotorNum = 1;


            //广播模式
            UInt16 TxLen = 1 + 3 * MotorNum;


            UInt16[] IDArray = {ID};
            UInt16[] PositionArray = {Position};

            int CommandLen = TxLen + 5;


            UInt16 CheckSum = 0;
            CommandArray[0] = FrameHead1;
            CommandArray[1] = FrameHead2;
            CommandArray[2] = (byte)TxLen;//表示帧数据长度
            CommandArray[3] = 0xFF;//广播ID
            CommandArray[4] = 0xF2;//定位标志
            for (int j = 0; j < MotorNum; j++)
            {
                CommandArray[5 + 3 * j] = (byte)(IDArray[j] & 0xFF);//ID号
                CommandArray[6 + 3 * j] = (byte)(PositionArray[j] & 0xFF);//数据段 低八位
                CommandArray[7 + 3 * j] = (byte)((PositionArray[j] >> 8) & 0xFF);//数据段 高八位
            }
            int i = 0;
            for (i = 2; i < (CommandLen-1); i++)
            {
                CheckSum += Convert.ToUInt16(CommandArray[i]);
            }
            CommandArray[CommandLen-1] = (byte)(CheckSum & 0xFF);//校验和

            //发送
            if (isOpen)
            {
                try
                {
                    int a = 0;
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




        /// <summary>
        ///广播模式控制6个电机，不读取空格里的数值，可以自由切换,无返回
        ///ID从1到6
        /// </summary>
        public static void CommandAllMotor(
            UInt16 Position1, 
            UInt16 Position2, 
            UInt16 Position3, 
            UInt16 Position4, 
            UInt16 Position5,
            UInt16 Position6
            )
        {

            
            //与制造通讯内容的函数相比，本函数可以直接自动发送出去


            //控制四个电机，广播模式
            UInt16 TxLen = 1 + 3 * 6;


            UInt16[] ID = {
                (UInt16)1,
                (UInt16)2,
                (UInt16)3,
                (UInt16)4,
                (UInt16)5,
                (UInt16)6
                };
            UInt16[] Position = {Position1,
                Position2,
                Position3,
                Position4,
                Position5,
                Position6};


            UInt16 CheckSum = 0;
            CommandArray[0] = FrameHead1;
            CommandArray[1] = FrameHead2;
            CommandArray[2] = (byte)TxLen;//表示帧数据长度
            CommandArray[3] = 0xFF;//广播ID
            CommandArray[4] = 0xF2;//定位标志
            for (int j = 0; j < 6; j++)
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
            CommandArray[23] = (byte)(CheckSum & 0xFF);//校验和

            //发送
            if (isOpen)
            {
                try
                {
                    sp.Write(CommandArray, 0, 24);

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



        /// <summary>
        ///广播模式控制6个电机，不读取空格里的数值，可以自由切换,无返回
        ///ID从1到6
        ///随动模式
        /// </summary>
        public static void CommandAllMotorFollow(
            UInt16 Position1,
            UInt16 Position2,
            UInt16 Position3,
            UInt16 Position4,
            UInt16 Position5,
            UInt16 Position6
            ) 
        {


            //与制造通讯内容的函数相比，本函数可以直接自动发送出去


            //控制四个电机，广播模式
            UInt16 TxLen = 1 + 3 * 6;


            UInt16[] ID = {
                (UInt16)1,
                (UInt16)2,
                (UInt16)3,
                (UInt16)4,
                (UInt16)5,
                (UInt16)6
                };
            UInt16[] Position = {Position1,
                Position2,
                Position3,
                Position4,
                Position5,
                Position6};


            UInt16 CheckSum = 0;
            CommandArray[0] = FrameHead1;
            CommandArray[1] = FrameHead2;
            CommandArray[2] = (byte)TxLen;//表示帧数据长度
            CommandArray[3] = 0xFF;//广播ID
            CommandArray[4] = 0xF3;//定位标志
            for (int j = 0; j < 6; j++)
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
            CommandArray[23] = (byte)(CheckSum & 0xFF);//校验和

            //发送
            if (isOpen)
            {
                try
                {
                    sp.Write(CommandArray, 0, 24);

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









        /// <summary>
        /// 输入要发送的指令的数组以及其长度，将指令发送出去
        /// </summary>
        /// <param name="commandArray"></param>
        /// <param name="commandLen"></param>
        public static void SendCommand(byte[] commandArray, int commandLen)
        {
            //发送
            if (isOpen)
            {
                try
                {
                    sp.Write(commandArray, 0, commandLen);
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








        /// <summary>
        ///使用串口控制stewart平台函数
        ///给定x轴、y轴、z轴的平移（mm）和旋转（°）的量，以及运动时间（ms）
        ///会返回帧
        /// </summary>
        /// <param name="Position1"></param>
        /// <param name="Position2"></param>
        /// <param name="Position11"></param>
        /// <param name="Position12"></param>
        public static void CommandStewart(
            double angleX, double angleY, double angleZ,
            double translationX, double translationY, double translationZ,
            UInt16 runTime,
            UInt16 ID
            )
        {

            const int commandLen = 22;//代表指令的长度

            byte[] commandArray = new byte[commandLen];

            const UInt16 startRegister = 1100;




            UInt16 checkSum = 0;//校验和
            UInt16 registerLength = 14;//数据部分的长度
            //指令的生成
            commandArray[0] = 0xEB;//包头
            commandArray[1] = 0x90;//包头
            commandArray[2] = (byte)ID;//表示id号
            commandArray[3] = (byte)(registerLength + 3);//该帧数据部分长度
            commandArray[4] = 0x12;//写寄存器命令标志

            //寄存器起始地址
            commandArray[5] = startRegister & 0xFF;//寄存器起始地址低八位
            commandArray[6] = startRegister >> 8 & 0xFF;//寄存器起始地址高八位

            //角度在输入时要乘100,最小分辨角度0.01°
            //angleX
            commandArray[7] = (byte)((Int16)(angleX * 100) & 0xFF);//angleX低八位
            commandArray[8] = (byte)((Int16)(angleX * 100) >> 8 & 0xFF);//angleX高八位

            //angleY
            commandArray[9] = (byte)((Int16)(angleY * 100) & 0xFF);//angleY低八位
            commandArray[10] = (byte)((Int16)(angleY * 100) >> 8 & 0xFF);//angleY高八位

            //angleZ
            commandArray[11] = (byte)(Convert.ToInt16(angleZ * 100) & 0xFF);//angleZ低八位
            commandArray[12] = (byte)(Convert.ToInt16(angleZ * 100) >> 8 & 0xFF);//angleZ高八位

            //位移输入时乘1000，最小分辨位移0.001mm
            //官方程序中标定是反的，这里修正过来
            //按照机器人下方的纸条方向为准

            //官方的坐标系是左手系，为了适应NDI，将x取反变成右手系

            //translationX

            commandArray[15] = (byte)((Int16)(translationX * 1000) & 0xFF);//translationX低八位
            commandArray[16] = (byte)((Int16)(translationX * 1000) >> 8 & 0xFF);//translationX高八位

            //translationY
            commandArray[13] = (byte)((Int16)(translationY * 1000) & 0xFF);//translationY低八位
            commandArray[14] = (byte)((Int16)(translationY * 1000) >> 8 & 0xFF);//translationY高八位

            //translationZ
            commandArray[17] = (byte)(Convert.ToInt16(translationZ * 1000) & 0xFF);//translationY低八位
            commandArray[18] = (byte)(Convert.ToInt16(translationZ * 1000) >> 8 & 0xFF);//translationY高八位

            //runTime
            commandArray[19] = (byte)(runTime & 0xFF);//runTime低八位
            commandArray[20] = (byte)(runTime >> 8 & 0xFF);//runTime高八位

            //校验和
            int i = 0;
            for (i = 2; i < commandLen-1; i++)
            {
                checkSum += Convert.ToUInt16(commandArray[i]);
            }
            commandArray[commandLen - 1] = (byte)(checkSum & 0xFF);//校验和


            //发送指令
            SendCommand(commandArray, commandLen);




        }

        



    }
}
