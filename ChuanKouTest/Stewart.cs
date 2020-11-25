using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitware.VTK;
using System.Windows.Media.Media3D;
using System.IO;
using System.Windows.Forms;

namespace ChuanKouTest
{

    /// <summary>
    /// 和并联机器人（stewart平台）相关的一些参数
    /// </summary>
    class Stewart
    {
        //套筒坐标点，使用ndi测量得到，为机器人工具端坐标系
        public static List<Point3D> SleevePointListToolCoo = new List<Point3D>();



        //圆心坐标，机构坐标系，求第三点时需要用
        public static Point3D CircleCenterToolCoo = new Point3D();
        //半径长度，求第三点时需要用
        public static double CircleRadius;


        //圆心距离SteevePoint1的偏移量矢量，在机构坐标系下
        public static Vector3D CircleCenterBiasVector = new Vector3D();
        public static double CircleCenterBiasLength = 0;






        /*************************************************************
        使用在cad模型上测量的方法求出机器人工具端和机器人末端的转换矩阵
        ***************************************************************/

        #region 以下为外部输入的参数量
        //机器人末端坐标系下坐标
        public static Point3D SleevePoint2CADStewart = new Point3D(-69.86, 0, 173.21);//靠近目标小球的点，套筒点2
        public static Point3D SleevePoint1CADStewart = new Point3D(-84.86, 0, 147.23);//套筒点1

        //第三点坐标，机构坐标系下坐标，之后转换成末端坐标系
        public static Point3D StewartPointCADStewart = new Point3D(-49.61, -9.73, 60.78);

        //套筒点，在机器人机构坐标系下坐标
        public static int SleevePointNum = 5;//采集的套筒点数量
        public static Point3D[] SleevePointArray = new Point3D[SleevePointNum];


        //机器人工具端转换到机器人末端，需要平移量
        //末端到工具端，直接减去本向量
        //工具端到末端，加上本向量
        public static Vector3D CooTransBiasVector = new Vector3D(53.8, 0, 26.13);


        #endregion

        public static double SleeveLength = 0;



        //内参
        private const int DOF = 6;

        public static double xp = 0, yp = 0, zp = 0;
        public static double rUp = 0, rDown = 0;
        public static double MotorLenOri = 0;
        public static double[] UpAngleArray = new double[DOF];
        public static double[] DownAngleArray = new double[DOF];









        /// <summary>
        /// 静态构造函数，会计算类里的一些静态参数
        /// </summary>
        static Stewart()
        {
            CircleParaCalNew();
        }






        /// <summary>
        /// 类在初始化时应运行的函数:新
        /// 对应于CAD测量参数的方法
        /// </summary>
        public static void CircleParaCalNew()
        {
            #region CAD图获得套筒点在机器人末端坐标系下坐标
            //机器人末端坐标系下坐标


            SleeveLength = (SleevePoint1CADStewart - SleevePoint2CADStewart).Length;
            #endregion

            //ndi测量的套筒点输入
            SleevePointArray[0] = new Point3D(-174.03, 0.3, 54.44);
            SleevePointArray[1] = new Point3D(-166.68, -0.14, 68.56);
            SleevePointArray[2] = new Point3D(-151.48, 0.12, 95.69);
            SleevePointArray[3] = new Point3D(-175.49, -0.85, 53.41);
            SleevePointArray[4] = new Point3D(-135.05, 0.14, 124.19);

            #region NDI测量获得套筒点在机器人末端坐标系下坐标

            //将测量的点加入列表中
            for (int i = 0; i < SleevePointNum; i++)
            {
                SleevePointListToolCoo.Add(new Point3D(
                SleevePointArray[i].X + CooTransBiasVector.X,
                SleevePointArray[i].Y + CooTransBiasVector.Y,
                SleevePointArray[i].Z + CooTransBiasVector.Z
                ));
            }


            double x0Sleeve = 0;
            double y0Sleeve = 0;
            double z0Sleeve = 0;

            double aLineSleeve = 0;
            double bLineSleeve = 0;
            double cLineSleeve = 0;

            double t = 10;//随便取

            double x1Sleeve = 0;
            double y1Sleeve = 0;
            double z1Sleeve = 0;

            double x2Sleeve = 0;
            double y2Sleeve = 0;
            double z2Sleeve = 0;



            //最小二乘法
            MathFun.LeastSquareLine3D(
                SleevePointListToolCoo,
                out x0Sleeve, out y0Sleeve, out z0Sleeve,
                out aLineSleeve, out bLineSleeve, out cLineSleeve
                );

            x1Sleeve = x0Sleeve;
            y1Sleeve = y0Sleeve;
            z1Sleeve = z0Sleeve;

            x2Sleeve = x0Sleeve + t * aLineSleeve;
            y2Sleeve = y0Sleeve + t * bLineSleeve;
            z2Sleeve = z0Sleeve + t * cLineSleeve;

            //套筒近点
            double xSleeveIn = 0;
            double ySleeveIn = 0;
            double zSleeveIn = 0;

            //套筒远点
            double xSleeveOut = 0;
            double ySleeveOut = 0;
            double zSleeveOut = 0;

            //将CAD模式下获得的点投影到套筒直线上
            MathFun.Point2LineFoot3D(
                SleevePoint2CADStewart.X, SleevePoint2CADStewart.Y, SleevePoint2CADStewart.Z,
                x1Sleeve, y1Sleeve, z1Sleeve,
                x2Sleeve, y2Sleeve, z2Sleeve,
                out xSleeveIn, out ySleeveIn, out zSleeveIn
                );

            xSleeveOut = xSleeveIn - SleeveLength * aLineSleeve;
            ySleeveOut = ySleeveIn - SleeveLength * bLineSleeve;
            zSleeveOut = zSleeveIn - SleeveLength * cLineSleeve;


            //靠近目标小球的点，套筒点2
            SleevePoint2CADStewart.X = xSleeveIn;
            SleevePoint2CADStewart.Y = ySleeveIn;
            SleevePoint2CADStewart.Z = zSleeveIn;
            //套筒点1
            SleevePoint1CADStewart.X = xSleeveOut;
            SleevePoint1CADStewart.Y = ySleeveOut;
            SleevePoint1CADStewart.Z = zSleeveOut;


            #endregion





            //第三点转化到机器人末端坐标系
            StewartPointCADStewart.X += CooTransBiasVector.X;
            StewartPointCADStewart.Y += CooTransBiasVector.Y;
            StewartPointCADStewart.Z += CooTransBiasVector.Z;



            //圆心
            double CircleCenterToolCooX = 0;
            double CircleCenterToolCooY = 0;
            double CircleCenterToolCooZ = 0;
            MathFun.Point2LineFoot3D(
                StewartPointCADStewart.X, StewartPointCADStewart.Y, StewartPointCADStewart.Z,
                SleevePoint1CADStewart.X, SleevePoint1CADStewart.Y, SleevePoint1CADStewart.Z,
                SleevePoint2CADStewart.X, SleevePoint2CADStewart.Y, SleevePoint2CADStewart.Z,
                out CircleCenterToolCooX, out CircleCenterToolCooY, out CircleCenterToolCooZ
                );
            CircleCenterToolCoo.X = CircleCenterToolCooX;
            CircleCenterToolCoo.Y = CircleCenterToolCooY;
            CircleCenterToolCoo.Z = CircleCenterToolCooZ;

            //半径
            Vector3D CircleRadiusVector = CircleCenterToolCoo - StewartPointCADStewart;
            CircleRadius = CircleRadiusVector.Length;
            //CircleRadius = 50;

            //计算偏移量
            CircleCenterBiasVector = CircleCenterToolCoo - SleevePoint1CADStewart;
            CircleCenterBiasLength = CircleCenterBiasVector.Length;






        }




        /// <summary>
        /// 输入并联机器人希望到达的位姿,以及并联机器人的几何参数
        /// 输出6个电机应该到的长度(数组形式)，以及方向向量（由静平台指向动平台，数组形式）
        /// </summary>
        public static void StewartInverseKinematic(
            double angleX, double angleY, double angleZ,
            double translationX, double translationY, double translationZ,
            double xp, double yp, double zp,//trans和angle均为0时动平台相对于静平台的移动量
            double rUp, double rDown,//上下圆盘半径,rUp相当于matlab中的R，rDown相当于matlab中的r。
            double[] UpAngleArray,
            double[] DownAngleArray,
            out double[] LenArray,
            out Vector3D[] DirArray
            )
        {

            const int HingePointNum = 6;
            const int HomoDim = 4;

            //动系相对于定系的平移量
            double TransUpX = translationX + xp;
            double TransUpY = translationY + yp;
            double TransUpZ = translationZ + zp;



            //1.动平台6个铰点，在动平台坐标系中的位置矢量
            Vector3D[] UpPosVectorArray = new Vector3D[HingePointNum];
            for (int i = 0; i < HingePointNum; i++)
            {
                //输入弧度！
                UpPosVectorArray[i].X = rUp * Math.Cos(UpAngleArray[i] * 2 * Math.PI / 360);
                UpPosVectorArray[i].Y = rUp * Math.Sin(UpAngleArray[i] * 2 * Math.PI / 360);
                UpPosVectorArray[i].Z = 0;
            }

            //2.静平台的6个铰点，在静平台坐标系中的位置矢量
            Vector3D[] DownPosVectorArray = new Vector3D[HingePointNum];
            for (int i = 0; i < HingePointNum; i++)
            {
                //输入弧度！
                DownPosVectorArray[i].X = rDown * Math.Cos(DownAngleArray[i] * 2 * Math.PI / 360);
                DownPosVectorArray[i].Y = rDown * Math.Sin(DownAngleArray[i] * 2 * Math.PI / 360);
                DownPosVectorArray[i].Z = 0;
            }

            //3.欧拉角ZYX，描述出动系相对于定系的旋转量
            vtkMatrix3x3 RotMatrix = new vtkMatrix3x3();
            MathFun.Euler2RotMatrixTotal(angleX, angleY, angleZ,
                out RotMatrix);

            //4.动平台的6个铰点，在静平台坐标系中的位置矢量
            Vector3D[] UpPosVectorDownCooArray = new Vector3D[HingePointNum];
            vtkMatrix4x4 HomoTransMatrix = new vtkMatrix4x4();
            MathFun.HomogeneousMatrixCreate(
                TransUpX, TransUpY, TransUpZ,
                RotMatrix,
                out HomoTransMatrix
                );

            double[][] HomoTransVector = new double[HingePointNum][];//动平台的6个铰点，在动平台中位置矢量，齐次坐标表达
            for (int i = 0; i < HingePointNum; i++)
            {
                HomoTransVector[i] = new double[HomoDim];

                //x,y,z,1
                HomoTransVector[i][0] = UpPosVectorArray[i].X;
                HomoTransVector[i][1] = UpPosVectorArray[i].Y;
                HomoTransVector[i][2] = UpPosVectorArray[i].Z;
                HomoTransVector[i][3] = 1;

                double[,] HomoTransCalMatrix = new double[HomoDim, HomoDim];
                MathFun.vtkMatrix4x42DoubleMatrix(
                    HomoTransMatrix,
                    out HomoTransCalMatrix
                    );

                double[] HomoVectorOut = new double[HomoDim];
                MathFun.MatrixVectorMul(HomoTransCalMatrix, HomoTransVector[i],
                    out HomoVectorOut);

                UpPosVectorDownCooArray[i].X = HomoVectorOut[0];
                UpPosVectorDownCooArray[i].Y = HomoVectorOut[1];
                UpPosVectorDownCooArray[i].Z = HomoVectorOut[2];

            }

            //5.动平台的6个铰点位置矢量，减去静平台的6个铰点位置矢量，得到每个杆长矢量
            Vector3D[] LenVectorArray = new Vector3D[HingePointNum];
            for (int i = 0; i < HingePointNum; i++)
            {
                LenVectorArray[i].X = UpPosVectorDownCooArray[i].X - DownPosVectorArray[i].X;
                LenVectorArray[i].Y = UpPosVectorDownCooArray[i].Y - DownPosVectorArray[i].Y;
                LenVectorArray[i].Z = UpPosVectorDownCooArray[i].Z - DownPosVectorArray[i].Z;

            }

            //6.求模，得到每个杆的杆长
            //同时将矢量数组单位化，获得每根杆的方向向量（由静平台指向动平台）
            LenArray = new double[HingePointNum];
            DirArray = new Vector3D[HingePointNum];
            for (int i = 0; i < HingePointNum; i++)
            {
                LenArray[i] = LenVectorArray[i].Length;

                //归一化，从而获得方向向量
                LenVectorArray[i].Normalize();
                DirArray[i] = LenVectorArray[i];



            }






        }



        /// <summary>
        /// 导入stewart平台的内置数据
        /// 包括平台的竖直高度，上下平台的半径，上下平台铰点分布 
        /// </summary>
        public static void StewartParamsImport()
        {
            string pathnow = Directory.GetCurrentDirectory();
            //导入.rom
            //分别为机构、骨盆、针
            using (StreamReader ReStewart = new StreamReader(pathnow + @"\Stewartparams.txt"))
            {
                string Line;
                string[] PathTemp;




                int Index = 0;
                // Read and display lines from
                //the file until the end of
                // the file is reached.
                while ((Line = ReStewart.ReadLine()) != null)
                {

                    //Index = 0,xp
                    if (Index == 0)
                    {
                        PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                        xp = Convert.ToDouble(PathTemp[1]);
                        MessageBox.Show(Convert.ToString(xp), "xp");
                    }

                    //Index = 1,yp
                    if (Index == 1)
                    {
                        PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                        yp = Convert.ToDouble(PathTemp[1]);
                        MessageBox.Show(Convert.ToString(yp), "yp");
                    }

                    //Index = 2,zp
                    if (Index == 2)
                    {
                        PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                        zp = Convert.ToDouble(PathTemp[1]);
                        MessageBox.Show(Convert.ToString(zp), "zp");
                    }

                    //Index =3,rUp
                    if (Index == 3)
                    {
                        PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                        rUp = Convert.ToDouble(PathTemp[1]);
                        MessageBox.Show(Convert.ToString(rUp), "rUp");
                    }

                    //Index =4,rDown
                    if (Index == 4)
                    {
                        PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                        rDown = Convert.ToDouble(PathTemp[1]);
                        MessageBox.Show(Convert.ToString(rDown), "rDown");
                    }

                    //Index =5,MotorLenOri
                    if (Index == 5)
                    {
                        PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                        MotorLenOri = Convert.ToDouble(PathTemp[1]);
                        MessageBox.Show(Convert.ToString(MotorLenOri), "MotorLenOri");
                    }

                    //Index =6,UpAngleArray
                    if (Index == 6) 
                    {
                        string[] UpAngleStrArray;
                        PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                        UpAngleStrArray = PathTemp[1].Split(new string[] { "," }, StringSplitOptions.None);

                        for (int i = 0; i < DOF; i++) 
                        {
                            UpAngleArray[i] = Convert.ToDouble(UpAngleStrArray[i]);
                            MessageBox.Show(UpAngleStrArray[i], "UpAngleArray"+ Convert.ToString(i));
                        }

                    }

                    //Index =7,DownAngleArray
                    if (Index == 7)
                    {
                        string[] DownAngleStrArray;
                        PathTemp = Line.Split(new string[] { "::" }, StringSplitOptions.None);
                        DownAngleStrArray = PathTemp[1].Split(new string[] { "," }, StringSplitOptions.None);

                        for (int i = 0; i < DOF; i++)
                        {
                            DownAngleArray[i] = Convert.ToDouble(DownAngleStrArray[i]);
                            MessageBox.Show(DownAngleStrArray[i], "DownAngleArray" + Convert.ToString(i));
                        }

                    }



                    else { }

                    Index++;
                }
                ReStewart.Close();
            }


        }
    }
}
