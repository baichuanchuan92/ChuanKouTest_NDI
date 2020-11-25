using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
//using System.Windows.Media.Media3D;

namespace ChuanKouTest
{
    class Circles
    {
        MathCalculate mathcal = new MathCalculate();
        
        public void Canny(string filename)
        {
            Mat src = new Mat(filename);
            Mat dst = new Mat();
            Mat m1 = new Mat();
            Cv2.MedianBlur(src, m1, 7); //  ksize必须大于1且是奇数
            Cv2.Canny(m1, dst, 98, 196);
            using (new Window("src image", src))
            using (new Window("dst image", dst))
            {
                Cv2.WaitKey();
            }
        }

        CircleSegment[] cs;
        
        public void HoughCircles(string path, int p)
        {
            using (Mat src = new Mat(path, ImreadModes.AnyColor | ImreadModes.AnyDepth))
            using (Mat dst = new Mat())
            using (Mat outimage = new Mat())         
            {

                //1:因为霍夫圆检测对噪声比较敏感，所以首先对图像做一个中值滤波或高斯滤波(噪声如果没有可以不做)
                Mat m1 = new Mat();
                Cv2.MedianBlur(src, m1, 5); //  ksize必须大于1且是奇数
                
                //2：转为灰度图像
                //Mat m2 = new Mat();
                //Cv2.CvtColor(m1, m2, ColorConversionCodes.BGR2GRAY);

                //3：霍夫圆检测：使用霍夫变换查找灰度图像中的圆。
                /*
                 * 参数：
                 *      1：输入参数： 8位、单通道、灰度输入图像
                 *      2：实现方法：目前，唯一的实现方法是HoughCirclesMethod.Gradient
                 *      3: dp      :累加器分辨率与图像分辨率的反比。默认=1
                 *      4：minDist: 检测到的圆的中心之间的最小距离。(最短距离-可以分辨是两个圆的，否则认为是同心圆-src_gray.rows/8)
                 *      5:param1:   第一个方法特定的参数。[默认值是100] canny边缘检测阈值
                 *      6:param2:   第二个方法特定于参数。[默认值是100] 中心点累加器阈值 – 候选圆心
                 *      7:minRadius: 最小半径
                 *      8:maxRadius: 最大半径
                 * 
                 */
               // CircleSegment[] 
                cs = Cv2.HoughCircles(m1, HoughMethods.Gradient, 1, 20, 100, p * 2, 5, 12);
                src.CopyTo(dst);
                // Vec3d vec = new Vec3d();
                for (int i = 0; i < cs.Count(); i++)
                {
                    //画圆
                    Cv2.Circle(dst, (int)cs[i].Center.X, (int)cs[i].Center.Y, (int)cs[i].Radius, new Scalar(0), 2, LineTypes.AntiAlias);
                    //加强圆心显示
                    Cv2.Circle(dst, (int)cs[i].Center.X, (int)cs[i].Center.Y, 1, new Scalar(0), 2, LineTypes.AntiAlias);
                }
                Cv2.Resize(dst, outimage, outimage.Size(), 0.5, 0.5, InterpolationFlags.Linear);
                using (new Window("OutputImage", outimage))
                //using (new Window("InputImage", WindowMode.AutoSize, src))
                {
                    //Cv2.ResizeWindow("OutputImage",640, 480);
                    Cv2.WaitKey(0);
                }

            }
        }
       
        public double[,] CirclesSort()
        {
            //int[,] data = cs;
            //CircleSegment[] cs

            Cv2.DestroyWindow("OutputImage");

            double[] x0 = new double[10];
            double[] y0 = new double[10];
            double[] r0 = new double[10]; //{ 7, 10, 7, 7, 8, 7, 8, 8, 8,  10 };
            for (int i = 0; i < 10; i++)
            {
                x0[i] = (int)cs[i].Center.X;
                y0[i] = (int)cs[i].Center.Y;
                r0[i] = (int)cs[i].Radius;             

            }
            double[] rlist = new double[10];
            Array.Copy(r0,rlist,10);
            Array.Sort(rlist);//半径从小到大排序

            double[,] landMark = new double[2, 3];// { { 558, 416, 11 }, { 496, 594, 11 } };
            double[] x = new double[8];// { 319, 498, 583, 339, 609, 490, 564, 601 };
            double[] y = new double[8];// { 494, 621, 729, 743, 615, 508, 479, 501 };
            double[] r = new double[8];

            int m = 0;
            int n = 0;
            for (int i = 0; i < 10; i++)
            {
                if (r0[i] > rlist[7])
                {
                    landMark[m, 0] = x0[i];
                    landMark[m, 1] = y0[i];
                    landMark[m, 2] = r0[i];
                    m = m + 1;
                }
                else
                {
                    x[n] = x0[i];
                    y[n] = y0[i];
                    r[n] = r0[i];
                    n = n + 1;
                }               
            }            

            //排序后点的坐标
            double[] X = new double[10];
            double[] Y = new double[10];

            //8个点中和第一个标志点共线的点的索引值
            double[,] temp1 = new double[8, 3];
            int k = 0;
            for (int i = 0; i<8; i++)
            {
                double a = Math.Sqrt((x[i]-landMark[0, 0])* (x[i] - landMark[0, 0])+ (y[i] - landMark[0, 1]) * (y[i] - landMark[0,1]));
                temp1[k, 0] = (x[i] - landMark[0, 0]) / a;
                temp1[k, 1] = (y[i] - landMark[0, 1]) / a;
                temp1[k, 2] = i;
                k = k + 1;
            }
            double[,] result1 = new double[28, 3];
            int k1 = 0;
            for (int i = 0; i < 7; i++)
            {
                for (int j = i+1; j< 8; j++)
                {
                    result1[k1, 0] = Math.Abs(temp1[i, 0] * temp1[j, 1] - temp1[i, 1] * temp1[j, 0]);
                    result1[k1, 1] = i;
                    result1[k1, 2] = j;
                    k1 = k1 + 1;
                }
            }
            int[,] index1 = new int[2, 2];
            int k2 = 0;
            for (int i = 0; i < 28; i++)
            {
                if (result1[i, 0] < 0.1)
                {
                    index1[k2, 0] = (int)result1[i, 1];
                    index1[k2, 1] = (int)result1[i, 2];
                    k2 = k2 + 1;
                }
            }
            //8个点中和第一个标志点共线的点的索引值
            double[,] temp2 = new double[8, 3];
            int k3 = 0;
            for (int i = 0; i < 8; i++)
            {
                double a = Math.Sqrt((x[i] - landMark[1, 0]) * (x[i] - landMark[1, 0]) + (y[i] - landMark[1, 1]) * (y[i] - landMark[1, 1]));
                temp2[k3, 0] = (x[i] - landMark[1, 0]) / a;
                temp2[k3, 1] = (y[i] - landMark[1, 1]) / a;
                temp2[k3, 2] = i;
                k3 = k3 + 1;
            }
            double[,] result2 = new double[28, 3];
            int k4 = 0;
            for (int i = 0; i < 7; i++)
            {
                for (int j = i+1; j < 8; j++)
                {
                    result2[k4, 0]= Math.Abs(temp2[i, 0] * temp2[j, 1] - temp2[i, 1] * temp2[j, 0]);
                    result2[k4, 1] = i;
                    result2[k4, 2] = j;
                    k4 = k4 + 1;
                }
            }
            int[,] index2 = new int[2, 2];
            int k5 = 0;
            for (int i = 0; i < 28; i++)
            {
                if (result2[i, 0] < 0.1)
                {
                    index2[k5, 0] = (int)result2[i, 1];
                    index2[k5, 1] = (int)result2[i, 2];
                    k5 = k5 + 1;
                }
            }
            //和标志点一共线的两组点，分别构成的向量
            double[,] v1 = new double[2, 4];
            int k6 = 0;
            for (int i = 0; i < 2; i++)
            {
                double a = Math.Sqrt((x[index1[i, 0]] - x[index1[i, 1]]) * (x[index1[i, 0]] -x[index1[i, 1]])
                                   + (y[index1[i, 0]] - y[index1[i, 1]]) * (y[index1[i, 0]] - y[index1[i, 1]]));
                v1[k6, 0] = (x[index1[i, 0]] - x[index1[i, 1]]) / a;
                v1[k6, 1] = (y[index1[i, 0]] - y[index1[i, 1]]) / a;
                v1[k6, 2] = index1[i, 0];
                v1[k6, 3] = index1[i, 1];
                k6=k6+1;
            }
            //和标志点二共线的两组点，分别构成的向量
            double[,] v2 = new double[2, 4];
            int k7 = 0;
            for (int i = 0; i < 2; i++)
            {
                double a = Math.Sqrt((x[index2[i, 0]] - x[index2[i, 1]]) * (x[index2[i, 0]] - x[index2[i, 1]])
                                    + (y[index2[i, 0]] - y[index2[i, 1]]) * (y[index2[i, 0]] - y[index2[i, 1]]));
                v2[k7, 0] = (x[index2[i, 0]] - x[index2[i, 1]]) / a;
                v2[k7, 1] = (y[index2[i, 0]] - y[index2[i, 1]]) / a;
                v2[k7, 2] = index2[i, 0];
                v2[k7, 3] = index2[i, 1];
                k7 = k7 + 1;
            }
            //判断平行的两组点，记录索引值
            int[] f1 = new int[2];
            int[] f2 = new int[2];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    double a = Math.Abs(v1[i, 0] * v2[j, 1] - v2[j, 0] * v1[i, 1]);
                    if (a < 0.1)
                    {
                        f1[0] = (int)v1[i, 2];f1[1] = (int)v1[i, 3];
                        f2[0] = (int)v2[j, 2];f2[1] = (int)v2[j, 3];

                    }
                }
            }

            double d1 = Math.Sqrt((x[f1[0]] - x[f1[1]]) * (x[f1[0]] - x[f1[1]]) + (y[f1[0]] - y[f1[1]]) * (y[f1[0]] - y[f1[1]]));
            double d11 = Math.Sqrt((x[f1[0]] - landMark[0, 0]) * (x[f1[0]] - landMark[0, 0]) + (y[f1[0]] - landMark[0, 1]) * (y[f1[0]] - landMark[0, 1]));
            double d12 = Math.Sqrt((x[f1[1]] - landMark[0, 0]) * (x[f1[1]] - landMark[0, 0]) + (y[f1[1]] - landMark[0, 1]) * (y[f1[1]] - landMark[0, 1]));
            double d2 = Math.Sqrt((x[f2[0]]-x[f2[1]])* (x[f2[0]] - x[f2[1]])+ (y[f2[0]] - y[f2[1]])* (y[f2[0]] - y[f2[1]]));
            double d21 = Math.Sqrt((x[f2[0]] - landMark[1, 0]) * (x[f2[0]] - landMark[1, 0]) + (y[f2[0]] - landMark[1, 1]) * (y[f2[0]] - landMark[1, 1]));
            double d22 = Math.Sqrt((x[f2[1]] - landMark[1, 0]) * (x[f2[1]] - landMark[1, 0]) + (y[f2[1]] - landMark[1, 1]) * (y[f2[1]] - landMark[1, 1]));
            
            //确定标志点顺序和与标志点共线的两组点序号
            if (d1 < d2)
            {
                X[8] = landMark[0, 0]; Y[8] = landMark[0, 1];
                X[9] = landMark[1, 0]; Y[9] = landMark[1, 1];
                if (d11 > d12)
                {
                    X[4] = x[f1[0]]; Y[4] = y[f1[0]];
                    X[7] = x[f1[1]]; Y[7] = y[f1[1]];
                }
                else
                {
                    X[4] = x[f1[1]]; Y[4] = y[f1[1]];
                    X[7] = x[f1[0]]; Y[7] = y[f1[0]];
                }
                if (d21 < d22)
                {
                    X[1] = x[f2[0]]; Y[1] = y[f2[0]];
                    X[2] = x[f2[1]]; Y[2] = y[f2[1]];
                }
                else
                {
                    X[1] = x[f2[1]]; Y[1] = y[f2[1]];
                    X[2] = x[f2[0]]; Y[2] = y[f2[0]];
                }
            }
            else
            {
                X[8] = landMark[1, 0]; Y[8] = landMark[1, 1];
                X[9] = landMark[0, 0]; Y[9] = landMark[0, 1];

                if (d11 < d12)
                {
                    X[1] = x[f1[0]]; Y[1] = y[f1[0]];
                    X[2] = x[f1[1]]; Y[2] = y[f1[1]];
                }
                else
                {
                    X[1] = x[f1[1]]; Y[1] = y[f1[1]];
                    X[2] = x[f1[0]]; Y[2] = y[f1[0]];
                }
                if (d21 > d22)
                {
                    X[4] = x[f2[0]]; Y[4] = y[f2[0]];
                    X[7] = x[f2[1]]; Y[7] = y[f2[1]];
                }
                else
                {
                    X[4] = x[f2[1]]; Y[4] = y[f2[1]];
                    X[7] = x[f2[0]]; Y[7] = y[f2[0]];
                }
            }

            //Vector3D v10 = new Vector3D();
            //Vector3D v20 = new Vector3D();
            //Vector3D vt1 = new Vector3D();
            //Vector3D vt2 = new Vector3D();
            //Vector3D vt3 = new Vector3D();
            //Vector3D vt4 = new Vector3D();

            double[] v10 = new double[2];
            double[] v20 = new double[2];
            double[] vt1 = new double[2];
            double[] vt2 = new double[2];
            double[] vt3 = new double[2];
            double[] vt4 = new double[2];

            v10[0] = X[1] - X[2]; v10[1] = Y[1] - Y[2]; mathcal.Vector2DNormalize(ref v10);
            v20[0] = X[4] - X[7]; v20[1] = Y[4] - Y[7]; mathcal.Vector2DNormalize(ref v20);
            //确定剩余4个点序号
            for (int i = 0; i < 8; i++)
            {
                if (i != f1[0] && i != f1[1] && i != f2[0] && i != f2[1])
                {
                    vt1[0] = x[i] - X[1]; vt1[1] = y[i] - Y[1]; mathcal.Vector2DNormalize(ref vt1);
                    vt2[0] = x[i] - X[2]; vt2[1] = y[i] - Y[2]; mathcal.Vector2DNormalize(ref vt2);
                    vt3[0] = x[i] - X[4]; vt3[1] = y[i] - Y[4]; mathcal.Vector2DNormalize(ref vt3);
                    vt4[0] = x[i] - X[7]; vt4[1] = y[i] - Y[7]; mathcal.Vector2DNormalize(ref vt4);

                    double a = vt1[0] * v10[0] + vt1[1] * v10[1];
                    double b = vt2[0] * v10[0] + vt2[1] * v10[1];
                    double c = vt3[0] * v20[0] + vt3[1] * v20[1];
                    double d = vt4[0] * v20[0] + vt4[1] * v20[1];                    

                    if (Math.Abs(a) < 0.1)
                    {
                        X[0] = x[i]; Y[0] = y[i];
                    }
                    if (Math.Abs(b) < 0.1)
                    {
                        X[3] = x[i]; Y[3] = y[i];
                    }
                    if (Math.Abs(c) < 0.1)
                    {
                        X[5] = x[i]; Y[5] = y[i];
                    }
                    if (Math.Abs(d) < 0.1)
                    {
                        X[6] = x[i]; Y[6] = y[i];
                    }
                }
            }

            double[,] imagePoints = new double[8, 2];
            for (int i = 0; i < 8; i++)
            {
                imagePoints[i, 0] = X[i];
                imagePoints[i, 1] = Y[i];
            }
            return imagePoints;
        }

        public double[,] XCirclesSort()
        {
            Cv2.DestroyWindow("OutputImage");

            int[] x0 = new int[8];
            int[] y0 = new int[8];
            int[] r0 = new int[8]; 
            int[] xt1 = new int[4]; int[] yt1 = new int[4];
            int[] xt2 = new int[4]; int[] yt2 = new int[4];
            int[] X1 = new int[4]; int[] Y1 = new int[4];//排序后的第一组点
            int[] X2 = new int[4]; int[] Y2 = new int[4];//排序后的第二组点

            for (int i = 0; i < 8; i++)
            {
                x0[i] = (int)cs[i].Center.X;
                y0[i] = (int)cs[i].Center.Y;
                r0[i] = (int)cs[i].Radius;
            }
            //y值最小点，为某一组某一个端点
            int ymin = y0[0];
            for (int i = 1; i < 7; i++)
            {
                if (y0[i] < ymin)
                {
                    ymin = y0[i];
                }                    
            }
    
            for (int i = 0; i < 8; i++)
            {
                if (y0[i]==ymin)
                {
                    xt1[0] = x0[i];
                    yt1[0] = y0[i];
                }
            }
            //y值最小点与其余7点分别构成的向量
            double[,] v = new double[7, 3];
            int k1 = 0;
            for (int i = 0; i < 8; i++)
            {
                if ((x0[i] != xt1[0]) && (y0[i] != yt1[0]))
                {
                    double A = Math.Sqrt((xt1[0] - x0[i]) * (xt1[0] - x0[i]) + (yt1[0] - y0[i]) * (yt1[0] - y0[i]));
                    v[k1, 0] = (xt1[0] - x0[i]) / A;
                    v[k1, 1] = (yt1[0] - y0[i]) / A;
                    v[k1, 2] = i;
                    k1 = k1 + 1;
                }
            }
            //向量之间两两叉乘，结果<0.1认为互相平行
            double[,] result = new double[21, 3];
            int k2 = 0;
            for (int i = 0; i < 6; i++)
            {
                for (int j = i+1; j < 7; j++)
                {
                    result[k2, 0] = Math.Abs(v[i, 0] * v[j, 1] - v[i, 1] * v[j, 0]);
                    result[k2, 1] = v[i, 2];
                    result[k2, 2] = v[j, 2];
                    k2 = k2 + 1;
                }
            }

            double[] index = new double[6];
            int k3 = 0;
            for (int i = 0; i < 21; i++)
            {
                if (result[i, 0] < 0.1)
                {
                    index[k3] = result[i, 1];
                    index[k3 + 1] = result[i, 2];
                    k3 = k3 + 2;
                }
            }
            //三个点的序号各出现两次，排序
            double[] sindex = new double[6];
            Array.Copy(index, sindex, 6);
            Array.Sort(sindex);

            for (int i = 0; i < 3; i++)
            {
                int n = (int)sindex[2 * i];
                xt1[i + 1] = x0[n];
                yt1[i + 1] = y0[n];                
            }

            //另外一组四点
            int k4 = 0;
            for (int i = 0; i < 8; i++)
            {
                if (((x0[i] != xt1[0]) || (y0[i] != yt1[0]))&& ((x0[i] != xt1[1]) || (y0[i] != yt1[1]))
                    && ((x0[i] != xt1[2]) || (y0[i] != yt1[2]))&& ((x0[i] != xt1[3]) || (y0[i] != yt1[3])))
                {
                    xt2[k4] = x0[i];
                    yt2[k4] = y0[i];
                    k4 = k4 + 1;
                }
            }
            //计算每组点两两之间的距离
            double[,] d1 = new double[6, 3];
            int k5 = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    d1[k5, 0] = Math.Sqrt((xt1[i] - xt1[j]) * (xt1[i] - xt1[j]) + (yt1[i] - yt1[j]) * (yt1[i] - yt1[j]));
                    d1[k5, 1] = i;
                    d1[k5, 2] = j;
                    k5 = k5 + 1;
                }
            }
            double[,] d2 = new double[6, 3];
            int k6 = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    d2[k6, 0] = Math.Sqrt((xt2[i] - xt2[j]) * (xt2[i] - xt2[j]) + (yt2[i] - yt2[j]) * (yt2[i] - yt2[j]));
                    d2[k6, 1] = i;
                    d2[k6, 2] = j;
                    k6 = k6 + 1;
                }
            }
            //距离最小值与最大值
            double d1min = d1[0, 0];
            double d1max = d1[0, 0];
            for (int i = 1; i < 6; i++)
            {
                if (d1[i, 0] < d1min)
                {
                    d1min = d1[i, 0];
                }
                if (d1[i, 0] > d1max)
                {
                    d1max = d1[i, 0];
                }
            }        
            double d2min = d2[0, 0];
            double d2max = d2[0, 0];
            for (int i = 1; i < 6; i++)
            {
                if (d2[i, 0] < d2min)
                {
                    d2min = d2[i, 0];
                }
                if (d2[i, 0] > d2max)
                {
                    d2max = d2[i, 0];
                }
            }
            //找到最小距离与最大距离点的索引值
            int[] f1 = new int[4];
            for (int i = 0; i < 6; i++)
            {
                if (d1[i, 0] == d1min)
                {
                    f1[0] = (int)d1[i, 1];
                    f1[1] = (int)d1[i, 2];
                }
                if (d1[i, 0] == d1max)
                {
                    f1[2] = (int)d1[i, 1];
                    f1[3] = (int)d1[i, 2];
                }
            }
            //点索引值出现次数统计
            int[] sf1 = new int[4];
            Array.Copy(f1, sf1, 4);
            Array.Sort(sf1);
            int[,] r1 = new int[3, 2];            
            r1[0, 0] = sf1[0];
            int k7 = 1;
            for (int i = 0; i < 3; i++)
            {
                if (sf1[i] != sf1[i+1])
                {
                    r1[k7, 0] = sf1[i+1];
                    k7 = k7 + 1;
                } 
            }
            for (int i = 0; i < 4; i++)
            {
                if (sf1[i] == r1[0, 0])
                {
                    r1[0, 1] = r1[0, 1] + 1;
                }
                if (sf1[i] == r1[1, 0])
                {
                    r1[1, 1] = r1[1, 1] + 1;
                }
                if (sf1[i] == r1[2, 0])
                {
                    r1[2, 1] = r1[2, 1] + 1;
                }
            }
            //出现两次的为端点，序号为4号
            int temp1 = new int();
            for (int i = 0; i < 3; i++)
            {
                if (r1[i, 1] == 2)
                {
                    temp1 = r1[i, 0];
                    X1[3] = xt1[temp1];
                    Y1[3] = yt1[temp1];
                }                    
            }
            //距离为最小值的另一点序号为3号
            if (f1[0] == temp1)
            {
                int a = f1[1];
                X1[2] = xt1[a];
                Y1[2] = yt1[a];
            }
            else
            {
                int a = f1[0];
                X1[2] = xt1[a];
                Y1[2] = yt1[a];
            }
            //距离为最大值的另一点序号为1号
            if (f1[2] == temp1)
            {
                int a = f1[3];
                X1[0] = xt1[a];
                Y1[0] = yt1[a];
            }
            else
            {
                int a = f1[2];
                X1[0] = xt1[a];
                Y1[0] = yt1[a];
            }
            //剩余一点为2号（既不构成最大值也不构成最小值）
            for (int i = 0; i < 4; i++)
            {
                if (i != r1[0, 0] && i != r1[1, 0] && i != r1[2, 0])
                {
                    X1[1] = xt1[i];
                    Y1[1] = yt1[i];
                }
            }
            //另一组四个点排序
            int[] f2 = new int[4];
            for (int i = 0; i < 6; i++)
            {
                if (d2[i, 0] == d2min)
                {
                    f2[0] = (int)d2[i, 1];
                    f2[1] = (int)d2[i, 2];
                }
                if (d2[i, 0] == d2max)
                {
                    f2[2] = (int)d2[i, 1];
                    f2[3] = (int)d2[i, 2];
                }
            }
            int[] sf2 = new int[4];
            Array.Copy(f2, sf2, 4);
            Array.Sort(sf2);
            int[,] r2 = new int[3, 2];
            r2[0, 0] = sf2[0];
            int k8 = 1;
            for (int i = 0; i < 3; i++)
            {
                if (sf2[i] != sf2[i + 1])
                {
                    r2[k8, 0] = sf2[i + 1];
                    k8 = k8 + 1;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (sf2[i] == r2[0, 0])
                {
                    r2[0, 1] = r2[0, 1] + 1;
                }
                if (sf2[i] == r2[1, 0])
                {
                    r2[1, 1] = r2[1, 1] + 1;
                }
                if (sf2[i] == r2[2, 0])
                {
                    r2[2, 1] = r2[2, 1] + 1;
                }
            }
            int temp2 = new int();
            for (int i = 0; i < 3; i++)
            {
                if (r2[i, 1] == 2)
                {
                    temp2 = r2[i, 0];
                    X2[3] = xt2[temp2];
                    Y2[3] = yt2[temp2];
                }
            }
            if (f2[0] == temp2)
            {
                int a = f2[1];
                X2[2] = xt2[a];
                Y2[2] = yt2[a];
            }
            else
            {
                int a = f2[0];
                X2[2] = xt2[a];
                Y2[2] = yt2[a];
            }
            if (f2[2] == temp2)
            {
                int a = f2[3];
                X2[0] = xt2[a];
                Y2[0] = yt2[a];
            }
            else
            {
                int a = f2[2];
                X2[0] = xt2[a];
                Y2[0] = yt2[a];
            }
            for (int i = 0; i < 4; i++)
            {
                if (i != r2[0, 0] && i != r2[1, 0] && i != r2[2, 0])
                {
                    X2[1] = xt2[i];
                    Y2[1] = yt2[i];
                }
            }

            //第一组交比
            double a1 = Math.Sqrt((X1[0] - X1[2]) * (X1[0] - X1[2]) + (Y1[0] - Y1[2]) * (Y1[0] - Y1[2]));
            double a2 = Math.Sqrt((X1[1] - X1[3]) * (X1[1] - X1[3]) + (Y1[1] - Y1[3]) * (Y1[1] - Y1[3]));
            double a3 = Math.Sqrt((X1[1] - X1[2]) * (X1[1] - X1[2]) + (Y1[1] - Y1[2]) * (Y1[1] - Y1[2]));
            double a4 = Math.Sqrt((X1[0] - X1[3]) * (X1[0] - X1[3]) + (Y1[0] - Y1[3]) * (Y1[0] - Y1[3]));
            double rt1 = (a1 * a2) / (a3 * a4);
            //第二组交比
            double b1 = Math.Sqrt((X2[0] - X2[2]) * (X2[0] - X2[2]) + (Y2[0] - Y2[2]) * (Y2[0] - Y2[2]));
            double b2 = Math.Sqrt((X2[1] - X2[3]) * (X2[1] - X2[3]) + (Y2[1] - Y2[3]) * (Y2[1] - Y2[3]));
            double b3 = Math.Sqrt((X2[1] - X2[2]) * (X2[1] - X2[2]) + (Y2[1] - Y2[2]) * (Y2[1] - Y2[2]));
            double b4 = Math.Sqrt((X2[0] - X2[3]) * (X2[0] - X2[3]) + (Y2[0] - Y2[3]) * (Y2[0] - Y2[3]));
            double rt2 = (b1 * b2) / (b3 * b4);

            //标尺交比
            double p13 = 30; double p24 = 20; double p23 = 12.5; double p14 = 37.5;
            double R1 = (p13 * p24) / (p23 * p14);
            double P13 = 25; double P24 = 15; double P23 = 10; double P14 = 30;
            double R2 = (P13 * P24) / (P23 * P14);

            //根据交比，判断分别是哪一组
            int[] X = new int[8]; int[] Y = new int[8];
            if (Math.Abs(rt1 - R1) < 0.02)
            {
                X1.CopyTo(X, 0); X2.CopyTo(X, 4);
                Y1.CopyTo(Y, 0); Y2.CopyTo(Y, 4);
            }
            else
            {
                X2.CopyTo(X, 0); X1.CopyTo(X, 4);
                Y2.CopyTo(Y, 0); Y1.CopyTo(Y, 4);
            }
            double[,] imagePoints = new double[8, 2];
            for (int i = 0; i < 4; i++)
            {
                imagePoints[i, 0] = X1[i];
                imagePoints[i, 1] = Y1[i];
            }
            for (int i = 0; i < 4; i++)
            {
                imagePoints[i+4, 0] = X2[i];
                imagePoints[i+4, 1] = Y2[i];
            }
            return imagePoints;
        }
    }
}
