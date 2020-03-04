using System;
using System.Linq;
using System.Threading.Tasks;

namespace GaussParallelConsole
{
    class Program
    {
        //width of A matrix
        static int MatrixWidth;

        //width of border of A matrix
        static int BorderWidth;

        //number of threads
        static int ThreadNumber;

        //block-diagonal bordering matrix of coefficients 
        static double[] A;

        //vector of right values
        static double[] B;

        //vector of unknown values
        static double[] X;
        static void Main(string[] args)
        {
            MatrixWidth = int.Parse(args[0]);
            BorderWidth = int.Parse(args[1]);
            if (MatrixWidth < BorderWidth)
                throw new Exception("Matrix width must be bigger than border width");
            ThreadNumber = int.Parse(args[2]);
            GenerateMatrix();
            GenerateVector();
            ShowMatrix(A, B);
            var ADef = new double[MatrixWidth * MatrixWidth];
            var BDef = new double[MatrixWidth];
            var ACons = new double[MatrixWidth * MatrixWidth];
            var BCons = new double[MatrixWidth * MatrixWidth];
            A.CopyTo(ADef, 0);
            B.CopyTo(BDef, 0);
            A.CopyTo(ACons, 0);
            B.CopyTo(BCons, 0);
            GaussDirectConsecutive(ADef, BDef, out int Start);
            GaussReverseConsecutive(ADef, BDef, out int End);
            Console.WriteLine($"Consecutive time = {End - Start}");
            Console.WriteLine();
            GaussDirectConsecutiveByColumns(ACons, BCons, out int StartCons);
            Console.WriteLine();
            GaussReverseConsecutive(ACons, BCons, out int EndCons);
            Console.WriteLine($"Consecutive time by columns = {EndCons - StartCons}");
        }

        //generate block-diagonal bordering matrix of size (n x n) and filled with random values from -1.0 to 1.0
        static void GenerateMatrix()
        {
            A = new double[MatrixWidth * MatrixWidth];
            Random rng = new Random();

            for (var i = 0; i < MatrixWidth; i++)
            {
                for (var j = 0; j < MatrixWidth; j++)
                {
                    if (
                       i == j
                       | (i == MatrixWidth - 1 && j >= MatrixWidth - BorderWidth)
                       | (j == MatrixWidth - 1 && i >= MatrixWidth - BorderWidth)
                       )
                        A[i * MatrixWidth + j] = rng.Next(-100, 100) * 0.01;                        
                    else
                        A[i * MatrixWidth + j] = 0F;
                }
            }
        }

        //generate vector of right values
        static void GenerateVector()
        {
            Random rng = new Random();
            B = new double[MatrixWidth];
            for (var i = 0; i < MatrixWidth; i++)
            {
                B[i] = 0F;
                for (var j = 0; j < MatrixWidth; j++)
                {
                    B[i] += A[i * MatrixWidth + j] * rng.Next(-10, 10);
                }
            }
        }

        //direct walkthrough of consecutive Gauss method
        static void GaussDirectConsecutive(double[] A, double[] B, out int time)
        {
            double multiplier = 0D;
            time = Environment.TickCount;
            for (var i = 0; i < MatrixWidth - 1; i++)
            {
                if (A[(MatrixWidth - 1) * MatrixWidth + i] != 0)
                {
                    multiplier = A[i * MatrixWidth + i] / A[(MatrixWidth - 1) * MatrixWidth + i];
                    for (var j = i; j < MatrixWidth; j++)
                    {
                        A[(MatrixWidth - 1) * MatrixWidth + j] = A[i * MatrixWidth + j] - A[(MatrixWidth - 1) * MatrixWidth + j] * multiplier;                        
                    }
                    B[MatrixWidth - 1] = -B[MatrixWidth - 1] * multiplier + B[i];
                }
            }
        }

        //reversed walkthrough of consecutive Gauss method
        static void GaussReverseConsecutive(double[] a, double[] b, out int time)
        {
            X = new double[MatrixWidth];
            X[MatrixWidth - 1] = b[MatrixWidth - 1] / a[(MatrixWidth - 1) * MatrixWidth + MatrixWidth - 1];
            for (var i = 0; i < MatrixWidth - 1; i++)
            {
                X[i] = (B[i] - X[MatrixWidth - 1] * A[i * MatrixWidth + MatrixWidth - 1]) / A[i * MatrixWidth + i];
            }
            time = Environment.TickCount;
            Console.WriteLine("X:");
            foreach (var x in X)
                Console.Write($"{x:F4} ");
            Console.WriteLine();
        }

        //method swaps to rows of matrix and vector
        static void SwapRows(int i, int n, double[] a, double[] b)
        {
            for (var j = i; j < MatrixWidth; j++)
            {
                var bufA = a[i * MatrixWidth + j];
                a[i * MatrixWidth + j] = a[n * MatrixWidth + j];
                a[n * MatrixWidth + j] = bufA;
            }
            var bufB = b[i];
            b[i] = b[n];
            b[n] = bufB;
        }

        //method substarcts one row from another
        static void SubstractRows(int i, int n, double multiplier, double[] a, double[] b)
        {
            //Console.WriteLine($"Substract {i} from {n}, multiplier is {multiplier}:");
            //ShowMatrix(a, b);
            for (var j = 0; j < MatrixWidth; j++)
            {
                a[n * MatrixWidth + j] -= a[i * MatrixWidth + j] * multiplier;
            }
            b[n] -= b[i] * multiplier;
            //Console.WriteLine("Substraction is done:");
            //ShowMatrix(a, b);
        }

        //direct walkthrough of consecutive Gauss method by columns
        static void GaussDirectConsecutiveByColumns(double[] A, double[] B, out int time)
        {
            time = Environment.TickCount;
            for (var i = 0; i < MatrixWidth - 1; i++)
            {
                //ShowMatrix(A, B);
                if (Math.Abs(A[i * MatrixWidth + i]) < Math.Abs(A[(MatrixWidth - 1) * MatrixWidth + i]))
                {
                    SwapRows(i, MatrixWidth - 1, A, B);
                }
                for (var j = i + 1; j < MatrixWidth; j++)
                {
                    //if (A[j * MatrixWidth + i] != 0)
                    {
                        var multiplier = A[j * MatrixWidth + i] / A[i * MatrixWidth + i];
                        SubstractRows(i, j, multiplier, A, B);
                    }
                }
            }
        }

        static void ShowMatrix(double[] A, double[] B)
        {
            for (int i = 0; i < MatrixWidth; i++)
            {
                for (int j = 0; j < MatrixWidth; j++)
                {
                    Console.Write(A[i * MatrixWidth + j] >= 0F ? " {0:F4} " : "{0:F4} ", A[i * MatrixWidth + j]);
                }
                Console.WriteLine(B[i] >= 0 ? "|  {0:F4}" : "| {0:F4}", B[i]);
            }
            Console.WriteLine();
        }
    }
}
