using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChuanKouTest
{
     public class Matrix
    {
        double[,] A;
        int m, n;
        string name;
        /// <summary>
        /// 构造矩阵m*n
        /// </summary>
        /// <param name="am"></param>
        /// <param name="an"></param>
        public Matrix(int am, int an)
        {
            m = am;
            n = an;
            A = new double[m, n];
            name = "Result";
        }
        public Matrix(int am, int an, string aName)
        {
            m = am;
            n = an;
            A = new double[m, n];
            name = aName;
        }

        /// <summary>
        /// 矩阵行数
        /// </summary>
        public int getM
        {
            get { return m; }
        }

        /// <summary>
        /// 矩阵列数
        /// </summary>
        public int getN
        {
            get { return n; }
        }

        /// <summary>
        /// 矩阵每个元素的值
        /// </summary>
        public double[,] Detail
        {
            get { return A; }
            set { A = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// 矩阵加法,Ma+Mb
        /// </summary>
        /// <param name="Ma"></param>
        /// <param name="Mb"></param>
        /// <returns></returns>
        public static Matrix MatrixAdd(Matrix Ma, Matrix Mb)
        {
            int m = Ma.getM;
            int n = Ma.getN;
            int m2 = Mb.getM;
            int n2 = Mb.getN;

            if ((m != m2) || (n != n2))
            {
                Exception myException = new Exception("数组维数不匹配");
                throw myException;
            }

            Matrix Mc = new Matrix(m, n);
            double[,] c = Mc.Detail;
            double[,] a = Ma.Detail;
            double[,] b = Mb.Detail;

            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    c[i, j] = a[i, j] + b[i, j];
            return Mc;
        }

        /// <summary>
        /// 矩阵减法Ma-Mb
        /// </summary>
        /// <param name="Ma"></param>
        /// <param name="Mb"></param>
        /// <returns></returns>
        public static Matrix MatrixSub(Matrix Ma, Matrix Mb)
        {
            int m = Ma.getM;
            int n = Ma.getN;
            int m2 = Mb.getM;
            int n2 = Mb.getN;
            if ((m != m2) || (n != n2))
            {
                Exception myException = new Exception("数组维数不匹配");
                throw myException;
            }
            Matrix Mc = new Matrix(m, n);
            double[,] c = Mc.Detail;
            double[,] a = Ma.Detail;
            double[,] b = Mb.Detail;

            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    c[i, j] = a[i, j] - b[i, j];
            return Mc;
        }

        /// <summary>
        /// 矩阵乘法，Ma*Mb
        /// </summary>
        /// <param name="Ma"></param>
        /// <param name="Mb"></param>
        /// <returns></returns>
        public static Matrix MatrixMulti(Matrix Ma, Matrix Mb)
        {
            int m = Ma.getM;
            int n = Ma.getN;
            int m2 = Mb.getM;
            int n2 = Mb.getN;

            if (n != m2)
            {
                Exception myException = new Exception("数组维数不匹配");
                throw myException;
            }

            Matrix Mc = new Matrix(m, n2);
            double[,] c = Mc.Detail;
            double[,] a = Ma.Detail;
            double[,] b = Mb.Detail;

            for (int i = 0; i < m; i++)
                for (int j = 0; j < n2; j++)
                {
                    c[i, j] = 0;
                    for (int k = 0; k < n; k++)
                        c[i, j] += a[i, k] * b[k, j];
                }
            return Mc;

        }

        /// <summary>
        /// 矩阵数乘，k*Ma
        /// </summary>
        /// <param name="k"></param>
        /// <param name="Ma"></param>
        /// <returns></returns>
        public static Matrix MatrixSimpleMulti(double k, Matrix Ma)
        {
            int m = Ma.getM;
            int n = Ma.getN;
            Matrix Mc = new Matrix(m, n);
            double[,] c = Mc.Detail;
            double[,] a = Ma.Detail;

            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    c[i, j] = a[i, j] * k;
            return Mc;
        }

        /// <summary>
        /// 矩阵转置
        /// </summary>
        /// <param name="MatrixOrigin"></param>
        /// <returns></returns>
        public static Matrix MatrixTrans(Matrix MatrixOrigin)
        {
            int m = MatrixOrigin.getM;
            int n = MatrixOrigin.getN;
            Matrix MatrixNew = new Matrix(n, m);
            double[,] c = MatrixNew.Detail;
            double[,] a = MatrixOrigin.Detail;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    c[i, j] = a[j, i];
            return MatrixNew;
        }      

        ///   <summary>         
        ///   矩阵的逆矩阵         
        ///   </summary>         
        ///   <param   name= "Ma "> </param>
        public static Matrix MatrixInv(Matrix Ma)
        {
            int i = 0;
            int row = Ma.getM;
            int col = Ma.getN;
            double[,] ma = Ma.Detail;
            if (row != col)
            {
                Exception myException = new Exception("矩阵不是方阵");
                throw myException;
            }

            double[,] MatrixTemp = new double[row, row * 2];
            Matrix MaInv = new Matrix(row, row);
            double[,] maInv = MaInv.Detail;

            for (i = 0; i < row; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    MatrixTemp[i, j] = ma[i, j];
                }
            }
            for (i = 0; i < row; i++)
            {
                for (int j = row; j < row * 2; j++)
                {
                    MatrixTemp[i, j] = 0;
                    if (i + row == j)
                        MatrixTemp[i, j] = 1;
                }
            }
            for (i = 0; i < row; i++)
            {
                if (MatrixTemp[i, i] != 0)
                {
                    double intTemp = MatrixTemp[i, i];
                    for (int j = 0; j < row * 2; j++)
                    {
                        MatrixTemp[i, j] = MatrixTemp[i, j] / intTemp;
                    }
                }
                for (int j = 0; j < row; j++)
                {
                    if (j == i)
                        continue;
                    double intTemp = MatrixTemp[j, i];
                    for (int k = 0; k < row * 2; k++)
                    {
                        MatrixTemp[j, k] = MatrixTemp[j, k] - MatrixTemp[i, k] * intTemp;
                    }
                }
            }
            for (i = 0; i < row; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    maInv[i, j] = MatrixTemp[i, j + row];
                }
            }
            return MaInv;
        }

    }   

}
