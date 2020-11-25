using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;
using System.Windows.Forms;

namespace ChuanKouTest
{
    /// <summary>
    /// 包括与输入输出参数相关的函数
    /// </summary>
    class IOFun
    {


        /// <summary>
        /// 输出一个double[,]类型的变量到.txt中
        /// </summary>
        /// <param name="Matrix"></param>
        /// <param name="RowNum"></param>
        /// <param name="ColomnNum"></param>
        /// <param name="WrMat"></param>
        public static void MatrixOutput(double[,] Matrix,int RowNum,int ColomnNum,
            StreamWriter WrMat) {
            int i = 0;
            int j = 0;
            for (i = 0; i < RowNum; i++) {
                WrMat.Write("第" + (i+1) + "行::");
                for (j = 0; j < ColomnNum; j++) {
                    WrMat.Write(Matrix[i, j]);

                    if (j == ColomnNum - 1) { WrMat.Write("\r\n"); }//最后一个
                    else { WrMat.Write(","); }
                    
                }
            }

        }



        /// <summary>
        /// 输入一个double[,]类型的变量，应确保输入时指针读向Matrix的上一行
        /// </summary>
        /// <param name="Matrix"></param>
        /// <param name="RowNum"></param>
        /// <param name="ColomnNum"></param>
        /// <param name="ReMat"></param>
        public static void MatrixInput(ref double[,] Matrix, int RowNum, int ColomnNum,
            StreamReader ReMat)
        {
            string LineMat;
            string[] MatNumTemp;//表示某行元素的数组
            int i = 0;
            int j = 0;
            for (i = 0; i < RowNum; i++) {
                //读取某行
                LineMat = ReMat.ReadLine();
                //根据，和：：分割,index=1是第一个元素，以此类推
                MatNumTemp = LineMat.Split(new string[] { "::","," }, StringSplitOptions.None);
                for (j = 0; j < ColomnNum; j++) {
                    Matrix[i, j] = Convert.ToDouble(MatNumTemp[j+1]);
                    //MessageBox.Show("第" + (i + 1) + "行，"
                    //    + "第" + (j + 1) + "列："
                    //    + Convert.ToString(Matrix[i, j])
                    //    , "Mat元素");
                }
            }


        }


        /// <summary>
        /// 输入要写入的参数，以及文件名
        /// 输出txt文件，记录参数的大小，但是名称需要自己记住
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="ObjectWritten"></param>
        public static void ParamsWrite(
            string FileName,
            string Comment="",
            params object[] ObjectWritten
            )
        {
            FileStream StewartParaStream = new FileStream(FileName + ".txt", FileMode.OpenOrCreate);
            StreamWriter ew2 = new StreamWriter(StewartParaStream);

            int i = 0;

            foreach (object o in ObjectWritten)
            {
                i++;
                ew2.Write("Object"+i.ToString()+":"); ew2.WriteLine(o);
            }

            //空三行
            ew2.WriteLine("");
            ew2.WriteLine("");
            ew2.WriteLine("");

            //内容下面是注释
            ew2.WriteLine("/************************************************/");
            ew2.WriteLine(Comment);
            ew2.WriteLine("/************************************************/");


            ew2.Flush();
            ew2.Close();
        }







    }
}
