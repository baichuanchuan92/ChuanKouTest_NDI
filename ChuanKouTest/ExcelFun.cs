using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using Missing = System.Reflection.Missing;
using System.IO;

namespace ChuanKouTest
{
    /// <summary>
    /// 包含了写入和写出Excel的函数
    /// </summary>
    public class ExcelFun
    {


        /// <summary>
        /// 读取Excel中某一范围的数据
        /// </summary>
        /// <param name="excelPath">待读取的Excel文件路径</param>
        /// <param name="stCell">起始单元格编号</param>
        /// <param name="edCell">终止单元格编号</param>
        /// <returns>存放连续读取的数据的二维数组</returns>
        public static object[,] GetExcelRangeData(string excelPath, string stCell, string edCell)
        {
            Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
            Workbook workBook = null;
            object oMissiong = Missing.Value;
            try
            {
                workBook = app.Workbooks.Open(excelPath, oMissiong, oMissiong, oMissiong, oMissiong, oMissiong,
                    oMissiong, oMissiong, oMissiong, oMissiong, oMissiong, oMissiong, oMissiong, oMissiong, oMissiong);
                if (workBook == null)
                    return null;

                Worksheet workSheet = (Worksheet)workBook.Worksheets.Item[1];
                //使用下述语句可以从头读取到最后，按需使用
                //var maxN = workSheet.Range[startCell].End[XlDirection.xlDown].Row;
                return workSheet.Range[stCell + ":" + edCell].Value2;
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                //COM组件方式调用完记得释放资源
                if (workBook != null)
                {
                    workBook.Close(false, oMissiong, oMissiong);
                    Marshal.ReleaseComObject(workBook);
                    app.Workbooks.Close();
                    app.Quit();
                    Marshal.ReleaseComObject(app);
                }
            }
        }


        /// <summary>
        /// excel处理
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="ExcelName"></param>
        /// <param name="Format"></param>
        public static void ExcelProcess(System.Data.DataTable dt,
            string ExcelName = "yyyy-MM-dd-HH-mm-ss",
            string Format = ".xls")
        {
            //Excel写出的内部函数
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
            string filePath = currentPath + DateTime.Now.ToString(ExcelName) + Format;
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


        /// <summary>
        /// 单球测量输出excel
        /// </summary>
        /// <param name="MarkerXRecord"></param>
        /// <param name="MarkerYRecord"></param>
        /// <param name="MarkerZRecord"></param>
        /// <param name="Motor1Length"></param>
        /// <param name="Motor2Length"></param>
        public static void ExportExcel(double[] MarkerXRecord,
            double[] MarkerYRecord, double[] MarkerZRecord,
            double[] Motor1Length, double[] Motor2Length)
        {
            //用于自动试验时输出excel
            System.Data.DataTable dt = new System.Data.DataTable();
            dt.Columns.Add("电机1伸长量", typeof(int));
            dt.Columns.Add("电机2伸长量", typeof(int));
            dt.Columns.Add("X", typeof(double));
            dt.Columns.Add("Y", typeof(double));
            dt.Columns.Add("Z", typeof(double));
            int ColLen = MarkerXRecord.Length;
            for (int i = 0; i < ColLen; i++)
            {
                dt.Rows.Add(Motor1Length[i],
                    Motor2Length[i],
                    MarkerXRecord[i],
                    MarkerYRecord[i],
                    MarkerZRecord[i]);
            }
            ExcelProcess(dt);

        }


        /// <summary>
        /// 输入表头和对应的数据，顺序和数量应该对应
        /// 输出excel
        /// exceldata里每个分数组长度应一样
        /// </summary>
        /// <param name="TableString"></param>
        /// <param name="ExcelData"></param>
        public static void ExportExcelGeneral(
            string[] TableString,
            double[][] ExcelData,
            string ExcelName = "yyyy-MM-dd-HH-mm-ss",
            string Format = ".xls"
            )
        {
            int j = 0;
            int DataLength = TableString.Length;
            int ColLen = ExcelData[0].Length;
            //用于自动试验时输出excel
            System.Data.DataTable dt = new System.Data.DataTable();
           

            for (j = 0; j < DataLength; j++)
            {
                dt.Columns.Add(TableString[j], typeof(double));
            }

            for (int i = 0; i < ColLen; i++)
            {
                System.Data.DataRow NewRowStewart = dt.NewRow();
                for(j=0;j<DataLength;j++)
                {
                    NewRowStewart[TableString[j]] = ExcelData[j][i]; 
                }
                dt.Rows.Add(NewRowStewart);
            }
            ExcelProcess(dt,ExcelName,Format);
        }




        /// <summary>
        /// 双球测量输出excel
        /// </summary>
        /// <param name="MarkerX1Record"></param>
        /// <param name="MarkerY1Record"></param>
        /// <param name="MarkerZ1Record"></param>
        /// <param name="MarkerX2Record"></param>
        /// <param name="MarkerY2Record"></param>
        /// <param name="MarkerZ2Record"></param>
        /// <param name="MarkerXavRecord"></param>
        /// <param name="MarkerYavRecord"></param>
        /// <param name="MarkerZavRecord"></param>
        /// <param name="Motor1Length"></param>
        /// <param name="Motor2Length"></param>
        public static void ExportExcelDoubleBall(
            double[] MarkerX1Record, double[] MarkerY1Record, double[] MarkerZ1Record,
            double[] MarkerX2Record, double[] MarkerY2Record, double[] MarkerZ2Record,
            double[] MarkerXavRecord, double[] MarkerYavRecord, double[] MarkerZavRecord,
            double[] Motor1Length, double[] Motor2Length)
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
            ExcelProcess(dt, "yyyy-MM-dd-HH-mm-ss-DB-JG1");

        }

        /// <summary>
        /// 反馈测试时输出excel
        /// </summary>
        /// <param name="Motor1Length"></param>
        /// <param name="Motor2Length"></param>
        /// <param name="XData"></param>
        /// <param name="YData"></param>
        /// <param name="ZData"></param>
        /// <param name="Err"></param>
        /// <param name="lData"></param>
        /// <param name="tData"></param>
        /// <param name="ErrX"></param>
        /// <param name="ErrY"></param>
        /// <param name="ErrZ"></param>
        /// <param name="TestNum"></param>
        public static void ExportExcel2(
            int[] Motor1Length, int[] Motor2Length,
            double[] XData, double[] YData, double[] ZData,
           double[] Err,
           double[] lData, double[] tData,
           double[] ErrX, double[] ErrY, double[] ErrZ,
           int[] TestNum)
        {
            //用于自动试验时输出excel
            System.Data.DataTable dt = new System.Data.DataTable();
            dt.Columns.Add("电机1伸长量", typeof(int));
            dt.Columns.Add("电机2伸长量", typeof(int));
            dt.Columns.Add("X", typeof(double));
            dt.Columns.Add("Y", typeof(double));
            dt.Columns.Add("Z", typeof(double));
            dt.Columns.Add("Err", typeof(double));
            dt.Columns.Add("ErrX", typeof(double));
            dt.Columns.Add("ErrY", typeof(double));
            dt.Columns.Add("ErrZ", typeof(double));
            dt.Columns.Add("反馈次数", typeof(double));
            dt.Columns.Add("tData电机1", typeof(double));
            dt.Columns.Add("lData电机2", typeof(double));
            int ColLen = Motor1Length.Length;
            for (int i = 0; i < ColLen; i++)
            {
                dt.Rows.Add(Motor2Length[i],
                    Motor1Length[i],
                    XData[i],
                    YData[i],
                    ZData[i],
                    Err[i],
                    ErrX[i],
                    ErrY[i],
                    ErrZ[i],
                    TestNum[i],
                    tData[i],
                    lData[i]);
            }
            ExcelProcess(dt);
        }



        /// <summary>
        /// 用于输出反馈的信息，存放到excel中
        /// </summary>
        /// <param name="FactXList"></param>
        /// <param name="FactYList"></param>
        /// <param name="FactZList"></param>
        /// <param name="GoalXList"></param>
        /// <param name="GoalYList"></param>
        /// <param name="GoalZList"></param>
        /// <param name="VectorInXList"></param>
        /// <param name="VectorInYList"></param>
        /// <param name="VectorInZList"></param>
        /// <param name="ErrXList"></param>
        /// <param name="ErrYList"></param>
        /// <param name="ErrZList"></param>
        /// <param name="ErrList"></param>
        public static void ExportExcelFeedback(
            List<double>FactXList, List<double> FactYList, List<double> FactZList,
            List<double>GoalXList, List<double> GoalYList, List<double> GoalZList,
            List<double> VectorInXList, List<double> VectorInYList, List<double> VectorInZList,
            List<double> ErrXList, List<double> ErrYList, List<double> ErrZList,
            List<double>ErrList,
            int Index)
        {
            //用于自动试验时输出excel
            System.Data.DataTable dt = new System.Data.DataTable();

            dt.Columns.Add("FactX", typeof(double));
            dt.Columns.Add("FactY", typeof(double));
            dt.Columns.Add("FactZ", typeof(double));

            dt.Columns.Add("GoalX", typeof(double));
            dt.Columns.Add("GoalY", typeof(double));
            dt.Columns.Add("GoalZ", typeof(double));

            dt.Columns.Add("VectorInX", typeof(double));
            dt.Columns.Add("VectorInY", typeof(double));
            dt.Columns.Add("VectorInZ", typeof(double));

            dt.Columns.Add("ErrX", typeof(double));
            dt.Columns.Add("ErrY", typeof(double));
            dt.Columns.Add("ErrZ", typeof(double));

            dt.Columns.Add("ErrTotal", typeof(double));

            int ColLen = ErrList.Count;
            for (int i = 0; i < ColLen; i++)
            {
                dt.Rows.Add(
                    FactXList[i],
                    FactYList[i],
                    FactZList[i],
                    GoalXList[i],
                    GoalYList[i],
                    GoalZList[i],
                    VectorInXList[i],
                    VectorInYList[i],
                    VectorInZList[i],
                    ErrXList[i],
                    ErrYList[i],
                    ErrZList[i],
                    ErrList[i]
                    );
            }
            if (Index == 1) { ExcelProcess(dt, "yyyy-MM-dd-HH-mm-ss-FEED-INDEX1",".xls"); }
            else if (Index == 2) { ExcelProcess(dt, "yyyy-MM-dd-HH-mm-ss-FEED-INDEX2", ".xls"); }
            else { }

        }


    }
}
