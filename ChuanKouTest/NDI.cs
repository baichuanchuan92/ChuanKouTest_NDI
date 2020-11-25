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
    /// 包含NDI连接相关变量和NDI的dll里自带函数，均为static
    /// </summary>
    public class NDI
    {
        [DllImport("NDIVegaLsbt.dll", EntryPoint = "NDIVegaConnect", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int NDIVegaConnect([MarshalAs(UnmanagedType.LPStr)] string addr);

        [DllImport("NDIVegaLsbt.dll", EntryPoint = "loadPassiveTools", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void loadPassiveTools([MarshalAs(UnmanagedType.LPStr)] string path);

        [DllImport("NDIVegaLsbt.dll", EntryPoint = "startTracking", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void startTracking();

        [DllImport("NDIVegaLsbt.dll", EntryPoint = "stopTracking", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void stopTracking();

        [DllImport("NDIVegaLsbt.dll", EntryPoint = "getTransformData", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void getTransformData(ref double transformdata);

        [DllImport("NDIVegaLsbt.dll", EntryPoint = "printTrackingNDIData", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void printTrackingNDIData();

        //TODO:使用记事本导入
        //机构的tracker
        //public static string ToolPath1 = "C://Users//giuyyt//Desktop//20191021_340//340_202012.rom";
        //骨盆的tracker
        //public static string ToolPath2 = "C://Users//giuyyt//Desktop//20191021_340//Right-20191108.rom";
        //针的tracker
        //public static string ToolPath3 = "C://Users//giuyyt//Desktop//20191021_340//8700449.rom";

        public static string hostname = "COM4";

        //多线程用
        public static Thread NDIThread;

        public static double[] trackdata = new double[27];//3*9

        public delegate void ShowTrackData(double[] trackData);

        public static bool needTrack = true;

      


        /// <summary>
        /// 将目前NDI的内置数据导出成txt
        /// 包括机构，骨盆，针的.rom文件的路径
        /// 以及COM口
        /// </summary>
        public static void NDIParamsExport() {
            string pathnow = Directory.GetCurrentDirectory();
            //写入.rom文件
            //分成三行，分别是：机构、骨盆、针
            //using (StreamWriter WrRom = new StreamWriter(pathnow + @"\NDIrom.txt", false, System.Text.Encoding.GetEncoding("GB2312")))
            //{
            //    WrRom.WriteLine("机构.rom绝对路径::"+ToolPath1);
            //    WrRom.WriteLine("骨盆.rom绝对路径::" + ToolPath2);
            //    WrRom.WriteLine("针.rom绝对路径::" + ToolPath3);
            //    WrRom.Close();
            //}
            ////写入NDICOM文件
            //using (StreamWriter WrCom = new StreamWriter(pathnow + @"\NDIcom.txt", false, System.Text.Encoding.GetEncoding("GB2312")))
            //{
            //    WrCom.WriteLine(hostname);
            //    WrCom.Close();
            //}



        }


        ///// <summary>
        ///// 导入NDI的内置数据
        ///// 包括机构，骨盆，针的.rom文件的路径
        ///// 以及COM口 
        ///// </summary>
        public static void NDIParamsImport()
        {
            string pathnow = Directory.GetCurrentDirectory();
            //导入.rom
            //分别为机构、骨盆、针
            using (StreamReader ReRom = new StreamReader(pathnow + @"\NDIrom.txt"))
            {
                string Line;
                string[] PathTemp;
                //string[] ddd = ccc[i].Split(new string[] { "," }, StringSplitOptions.None);
                int Index = 0;
                // Read and display lines from
                //the file until the end of
                // the file is reached.
                while ((Line = ReRom.ReadLine()) != null)
                {

                    ////Index = 0,机构
                    //if (Index == 0)
                    //{
                    //    PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                    //    ToolPath1 = PathTemp[1];
                    //MessageBox.Show(ToolPath1, "机构");
                    //}

                    ////Index=1,骨盆
                    //else if (Index == 1)
                    //{
                    //    PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                    //    ToolPath2 = PathTemp[1];
                    //    MessageBox.Show(ToolPath2, "骨盆");
                    //}

                    ////Index=2,针 
                    //else if (Index == 2)
                    //{
                    //    PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                    //    ToolPath3 = PathTemp[1];
                    //    MessageBox.Show(ToolPath3, "针");
                    //}

                    //else { }

                    Index++;
                }
                ReRom.Close();
            }


            //导入NDICom
                using (StreamReader ReCom = new StreamReader(pathnow + @"\NDIcom.txt"))
            {
                hostname = ReCom.ReadLine();
                MessageBox.Show(hostname, "NDICom");
                ReCom.Close();

            }
        }



        /// <summary>
        /// 输入NDI相机坐标系下显示的位置（矢量）和姿态（四元数）
        /// 输出从tracker坐标系到NDI的变换矩阵（vtkMatrix4x4）
        /// </summary>
        public static vtkMatrix4x4 Trans2NDITransMatrix(
            double transX, double transY, double transZ,
            double quaX, double quaY, double quaZ, double qua0
            )
        {
            //工具端到NDI
            Vector3D trans = new Vector3D(transX, transY, transZ);
            System.Windows.Media.Media3D.Quaternion qua = new System.Windows.Media.Media3D.Quaternion(quaX, quaY, quaZ, qua0);
            Matrix3D matrix1 = MathFun.Quat2trans(trans, qua);
            vtkMatrix4x4 toolVTKMatrix = MathFun.Matrix3D2VTKMatrix(matrix1);

            return toolVTKMatrix;
        }


        /// <summary>
        /// 输入NDI相机坐标系下显示的位置（矢量）和姿态（四元数）
        /// 输出从NDI坐标系到Tool坐标系的变换矩阵（vtkMatrix4x4）
        /// </summary>
        /// <param name="transX"></param>
        /// <param name="transY"></param>
        /// <param name="transZ"></param>
        /// <param name="quaX"></param>
        /// <param name="quaY"></param>
        /// <param name="quaZ"></param>
        /// <param name="qua0"></param>
        /// <returns></returns>
        public static vtkMatrix4x4 NDITrans2ToolTransMatrix(
            double transX, double transY, double transZ,
            double quaX, double quaY, double quaZ, double qua0
            )
        {
            vtkMatrix4x4 toolVTKMatrix = new vtkMatrix4x4();
            //工具端到NDI
            toolVTKMatrix = Trans2NDITransMatrix(
            transX, transY, transZ,
            quaX, quaY, quaZ, qua0
                );

            //求逆矩阵，使得新矩阵表达从NDI到Tool
            vtkMatrix4x4 temp = new vtkMatrix4x4();
            temp.DeepCopy(toolVTKMatrix);
            temp.Invert();



            return temp;
        }












    }
}
