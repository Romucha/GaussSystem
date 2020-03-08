using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace GaussParallelConsole
{
    class Program
    {
        //width of A matrix
        static int MatrixWidth;

        //width of border of A matrix
        static int BorderWidth;

        //block-diagonal bordering matrix of coefficients 
        static double[] A;

        //vector of right values
        static double[] B;

        //vector of unknown values
        static double[] X;
        static void Main(string[] args)
        {
            //instead of suggested multithreading we're gonna use parallel calculations and get rid of thread number setting
            //so there's gonna be only two arguments of our program
            if (args.Count() < 2)
                throw new Exception("Not enough arguments");

            if(!int.TryParse(args[0],out MatrixWidth))
                throw new Exception("Matrix width is not digit");

            if (!int.TryParse(args[1], out BorderWidth))
                throw new Exception("Border width is not digit");

            if (MatrixWidth < BorderWidth)
                throw new Exception("Matrix width must be bigger than border width");

            //generate random matrices
            GenerateMatrix();
            GenerateVector();

            //create copies of matrices for each case
            //consecutive by columns
            var ACons = new double[MatrixWidth * MatrixWidth];
            var BCons = new double[MatrixWidth * MatrixWidth];
            //parallel
            var APar = new double[MatrixWidth * MatrixWidth];
            var BPar = new double[MatrixWidth];
            A.CopyTo(ACons, 0);
            B.CopyTo(BCons, 0);
            A.CopyTo(APar, 0);
            B.CopyTo(BPar, 0);

            //get results
            GaussDirectConsecutive(ACons, BCons, out int StartCons);
            GaussReverseConsecutive(ACons, BCons, out int EndCons);
            Console.WriteLine($"Consecutive time = {EndCons - StartCons}");
            GaussDirectParallel(APar, BPar, out int StartPar);
            GaussReverseParallel(APar, BPar, out int EndPar);
            Console.WriteLine($"Consecutive time = {EndPar - StartPar}");
            Console.WriteLine($"Time gain is {(EndCons - StartCons) / (EndPar - StartPar):F4}");

            //print results into file
            MatrixOutput();
            Console.WriteLine("Results are written into file Result.txt");
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
                    //get rid of zeros in diagonal elements
                    if (A[i * MatrixWidth + i] == 0F)
                        A[i * MatrixWidth + i] += 0.1;
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
                    var multiplier = A[j * MatrixWidth + i] / A[i * MatrixWidth + i];
                    SubstractRows(i, j, multiplier, A, B);
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
            for (var j = 0; j < MatrixWidth; j++)
            {
                a[n * MatrixWidth + j] -= a[i * MatrixWidth + j] * multiplier;
            }
            b[n] -= b[i] * multiplier;
        }        

        //reverse walkthrough of parallel Gauss method
        static void GaussReverseParallel(double[] a, double[] b, out int time)
        {
            //find X[i]
            void MatrixIteration(int i)
            {
                X[i] = (b[i] - X[MatrixWidth - 1] * a[i * MatrixWidth + MatrixWidth - 1]) / a[i * MatrixWidth + i];
            }

            X = new double[MatrixWidth];
            X[MatrixWidth - 1] = b[MatrixWidth - 1] / a[(MatrixWidth - 1) * MatrixWidth + MatrixWidth - 1];
            //let's use built-in tools for parallel calculations
            Parallel.For(0, MatrixWidth - 2, MatrixIteration);
            time = Environment.TickCount;
        }

        //direct walkthrough of parallel Gauss method
        static void GaussDirectParallel(double[] a, double[] b, out int time)
        {
            time = Environment.TickCount;
            
            for (var i = 0; i < MatrixWidth - 1; i++)
            {
                if (Math.Abs(A[i * MatrixWidth + i]) < Math.Abs(A[(MatrixWidth - 1) * MatrixWidth + i]))
                {
                    SwapRows(i, MatrixWidth - 1, A, B);
                }
                //single step of matrix substraction
                void MatrixIteration(int j)
                {
                        var multiplier = a[j * MatrixWidth + i] / a[i * MatrixWidth + i];
                        SubstractRows(i, j, multiplier, a, b);
                }
                Parallel.For(i + 1, MatrixWidth, MatrixIteration);
            }
        }

        //create text file and copy results into it
        static void MatrixOutput()
        {
            var fileName = "Result";
            var extention = ".txt";
            var path = "";
            //create new file if there's already one with the same name
            if (!File.Exists(fileName + extention))
            {
                path = fileName + extention;
                File.Create(path).Close();
            }
            else
            {
                
                for (var i = 1; ; i++)
                {
                    path = fileName + "(" + (i).ToString() + ")" + extention;
                    if (!File.Exists(path))
                    {
                        File.Create(path).Close();
                        break;
                    }
                }
            }

            StringBuilder output = new StringBuilder("Input:\n");
            
            for (int i = 0; i < MatrixWidth; i++)
            {
                for (int j = 0; j < MatrixWidth; j++)
                {
                    output.AppendFormat(A[i * MatrixWidth + j] >= 0F ? " {0:F4} " : "{0:F4} ", A[i * MatrixWidth + j]);
                }
                output.AppendFormat(B[i] >= 0 ? "|  {0:F4}\n" : "| {0:F4}\n", B[i]);
            }
            output.Append("X:\n");
            foreach (var x in X)
                output.AppendFormat("{0:F4} ", x);
            File.WriteAllText(path, output.ToString());
        }
    }
}
