﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
//using System.Drawing;
//using System.Drawing.Imaging;
using System.Threading.Tasks;

using System.Drawing.Drawing2D;

using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.FileProperties;

namespace JPEGexplorer.FFT
{
    /// <summary>
    /// Defining Structure for Complex Data type  N=R+Ii
    /// </summary>
    struct COMPLEX
    {
        public double real, imag;
        public COMPLEX(double x, double y)
        {
            real = x;
            imag = y;
        }
        public float Magnitude()
        {
            return ((float)Math.Sqrt(real * real + imag * imag));
        }
        public float Phase()
        {
            return ((float)Math.Atan(imag / real));
        }
    }

    class FFTService
    {
        public StorageFile SourceFile;
        public BitmapImage Obj;               // Input Object Image
        public BitmapImage FourierPlot;       // Generated Fourier Magnitude Plot
        public BitmapImage PhasePlot;         // Generated Fourier Phase Plot

        public int[,] GreyImage;         //GreyScale Image Array Generated from input Image
        public float[,] FourierMagnitude;
        public float[,] FourierPhase;

        float[,] FFTLog;                 // Log of Fourier Magnitude
        float[,] FFTPhaseLog;            // Log of Fourier Phase
        public int[,] FFTNormalized;     // Normalized FFT Magnitude : Scale 0-1
        public int[,] FFTPhaseNormalized;// Normalized FFT Phase : Scale 0-1
        int nx, ny;                      //Number of Points in Width & height
        int Width, Height;
        COMPLEX[,] Fourier;              //Fourier Magnitude  Array Used for Inverse FFT
        public COMPLEX[,] FFTShifted;    // Shifted FFT 
        public COMPLEX[,] Output;        // FFT Normal
        public COMPLEX[,] FFTNormal;     // FFT Shift Removed - required for Inverse FFT 

        public FFTService(StorageFile File)
        {
            SourceFile = File;
        }

        /// <summary>
        /// Function to Read Bitmap to greyscale Array
        /// </summary>
        public async Task ReadImage()
        {
            byte[] pixelBytes;

            using (IRandomAccessStream fileStream = await SourceFile.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                nx = Width = (int)decoder.PixelWidth;
                ny = Height = (int)decoder.PixelHeight;

                PixelDataProvider pixelData = await decoder.GetPixelDataAsync();

                pixelBytes = pixelData.DetachPixelData();
            }

            int powerOfTwo = HighestPowerof2(Math.Min(nx, ny));

            int bytesPerPixel = pixelBytes.Length / (Width * Height);
            int widthOffset = (Width - powerOfTwo) / 2;
            int heightOffset = (Height - powerOfTwo) / 2;

            GreyImage = new int[powerOfTwo, powerOfTwo];  //[Row,Column]

            for (int i = 0; i < powerOfTwo; i++)
            {
                for (int j = 0; j < powerOfTwo; j++)
                {
                    int byteIndex = ((i + heightOffset) * Width + j + widthOffset) * bytesPerPixel;

                    GreyImage[j, i] = (pixelBytes[byteIndex] + pixelBytes[byteIndex + 1] + pixelBytes[byteIndex + 2]) / 3;
                }
            }

            nx = Width = ny = Height = powerOfTwo;
            return;
        }

        public async Task<BitmapImage> Displayimage(int[,] image)
        {
            byte[] pixelBytes = new byte[image.Length * 4];
            int width = image.GetLength(0);
            int height = image.GetLength(1);

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int byteIndex = (i * width + j) * 4;

                    pixelBytes[byteIndex] = (byte)image[j, i];
                    pixelBytes[byteIndex + 1] = (byte)image[j, i];
                    pixelBytes[byteIndex + 2] = (byte)image[j, i];
                    pixelBytes[byteIndex + 3] = 255;
                }
            }

            InMemoryRandomAccessStream inMemoryRandomAccessStream = new InMemoryRandomAccessStream();
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, inMemoryRandomAccessStream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)width, (uint)height, 96.0, 96.0, pixelBytes);
            await encoder.FlushAsync();
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.SetSource(inMemoryRandomAccessStream);
            return bitmapImage;
        }
        /// <summary>
        /// Calculate Fast Fourier Transform of Input Image
        /// </summary>
        public void ForwardFFT()
        {
            //Initializing Fourier Transform Array
            int i, j;
            Fourier = new COMPLEX[Width, Height];
            Output = new COMPLEX[Width, Height];
            //Copy Image Data to the Complex Array
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    Fourier[i, j].real = (double)GreyImage[i, j];
                    Fourier[i, j].imag = 0;
                }
            //Calling Forward Fourier Transform
            Output = FFT2D(Fourier, nx, ny, 1);
            return;
        }
        /// <summary>
        /// Shift The FFT of the Image
        /// </summary>
        public void FFTShift()
        {
            FFTShifted = new COMPLEX[nx, ny];

            int halfWidth = nx / 2;
            int halfHeight = ny / 2;

            for (int i = 0; i < halfWidth; i++)
                for (int j = 0; j < halfHeight; j++)
                {
                    FFTShifted[i + halfWidth, j + halfHeight] = Output[i            , j             ];
                    FFTShifted[i            , j             ] = Output[i + halfWidth, j + halfHeight];
                    FFTShifted[i + halfWidth, j             ] = Output[i            , j + halfHeight];
                    FFTShifted[i            , j + halfHeight] = Output[i + halfWidth, j             ];
                }

            return;
        }
        /// <summary>
        /// Removes FFT Shift for FFTshift Array
        /// </summary>
        public void RemoveFFTShift()
        {
            int i, j;
            FFTNormal = new COMPLEX[nx, ny];

            for (i = 0; i <= (nx / 2) - 1; i++)
                for (j = 0; j <= (ny / 2) - 1; j++)
                {
                    FFTNormal[i + (nx / 2), j + (ny / 2)] = FFTShifted[i, j];
                    FFTNormal[i, j] = FFTShifted[i + (nx / 2), j + (ny / 2)];
                    FFTNormal[i + (nx / 2), j] = FFTShifted[i, j + (ny / 2)];
                    FFTNormal[i, j + (nx / 2)] = FFTShifted[i + (nx / 2), j];
                }
            return;
        }
        /// <summary>
        /// FFT Plot Method for Shifted FFT
        /// </summary>
        /// <param name="Output"></param>
        public async Task FFTPlotAsync(COMPLEX[,] Output)
        {
            int i, j;
            float max;

            FFTLog = new float[nx, ny];
            FFTPhaseLog = new float[nx, ny];

            FourierMagnitude = new float[nx, ny];
            FourierPhase = new float[nx, ny];

            FFTNormalized = new int[nx, ny];
            FFTPhaseNormalized = new int[nx, ny];

            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    FourierMagnitude[i, j] = Output[i, j].Magnitude();
                    FourierPhase[i, j] = Output[i, j].Phase();
                    FFTLog[i, j] = (float)Math.Log(1 + FourierMagnitude[i, j]);
                    FFTPhaseLog[i, j] = (float)Math.Log(1 + Math.Abs(FourierPhase[i, j]));
                }
            //Generating Magnitude Bitmap
            max = FFTLog[0, 0];
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    if (FFTLog[i, j] > max)
                        max = FFTLog[i, j];
                }
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    FFTLog[i, j] = FFTLog[i, j] / max;
                }
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    FFTNormalized[i, j] = (int)(2000 * FFTLog[i, j]);
                }
            //Transferring Image to Fourier Plot
            FourierPlot = await Displayimage(FFTNormalized);

            //generating phase Bitmap
            FFTPhaseLog[0, 0] = 0;
            max = FFTPhaseLog[1, 1];
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    if (FFTPhaseLog[i, j] > max)
                        max = FFTPhaseLog[i, j];
                }
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    FFTPhaseLog[i, j] = FFTPhaseLog[i, j] / max;
                }
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    FFTPhaseNormalized[i, j] = (int)(255 * FFTPhaseLog[i, j]);
                }
            //Transferring Image to Fourier Plot
            PhasePlot = await Displayimage(FFTPhaseNormalized);

            return;
        }
        /// <summary>
        /// generate FFT Image for Display Purpose
        /// </summary>
        public async Task FFTPlotAsync()
        {
            int i, j;
            float max;
            FFTLog = new float[nx, ny];
            FFTPhaseLog = new float[nx, ny];

            FourierMagnitude = new float[nx, ny];
            FourierPhase = new float[nx, ny];

            FFTNormalized = new int[nx, ny];
            FFTPhaseNormalized = new int[nx, ny];

            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    FourierMagnitude[i, j] = Output[i, j].Magnitude();
                    FourierPhase[i, j] = Output[i, j].Phase();
                    FFTLog[i, j] = (float)Math.Log(1 + FourierMagnitude[i, j]);
                    FFTPhaseLog[i, j] = (float)Math.Log(1 + Math.Abs(FourierPhase[i, j]));
                }
            //Generating Magnitude Bitmap
            max = FFTLog[0, 0];
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    if (FFTLog[i, j] > max)
                        max = FFTLog[i, j];
                }
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    FFTLog[i, j] = FFTLog[i, j] / max;
                }
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    FFTNormalized[i, j] = (int)(1000 * FFTLog[i, j]);
                }
            //Transferring Image to Fourier Plot
            FourierPlot = await Displayimage(FFTNormalized);

            //generating phase Bitmap

            max = FFTPhaseLog[0, 0];
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    if (FFTPhaseLog[i, j] > max)
                        max = FFTPhaseLog[i, j];
                }
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    FFTPhaseLog[i, j] = FFTPhaseLog[i, j] / max;
                }
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    FFTPhaseNormalized[i, j] = (int)(2000 * FFTLog[i, j]);
                }
            //Transferring Image to Fourier Plot
            PhasePlot = await Displayimage(FFTPhaseNormalized);


        }
        /// <summary>
        /// Calculate Inverse from Complex [,]  Fourier Array
        /// </summary>
        public async Task InverseFFTAsync()
        {
            //Initializing Fourier Transform Array
            int i, j;

            //Calling Forward Fourier Transform
            Output = new COMPLEX[nx, ny];
            Output = FFT2D(Fourier, nx, ny, -1);

            Obj = null;  // Setting Object Image to Null
            //Copying Real Image Back to Greyscale
            //Copy Image Data to the Complex Array
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    GreyImage[i, j] = (int)Output[i, j].Magnitude();

                }
            Obj = await Displayimage(GreyImage);
            return;

        }
        /// <summary>
        /// Generates Inverse FFT of Given Input Fourier
        /// </summary>
        /// <param name="Fourier"></param>
        public async Task InverseFFTAsync(COMPLEX[,] Fourier)
        {
            //Initializing Fourier Transform Array
            int i, j;

            //Calling Forward Fourier Transform
            Output = new COMPLEX[nx, ny];
            Output = FFT2D(Fourier, nx, ny, -1);


            //Copying Real Image Back to Greyscale
            //Copy Image Data to the Complex Array
            for (i = 0; i <= Width - 1; i++)
                for (j = 0; j <= Height - 1; j++)
                {
                    GreyImage[i, j] = (int)Output[i, j].Magnitude();

                }
            Obj = await Displayimage(GreyImage);
            return;

        }
        /*-------------------------------------------------------------------------
            Perform a 2D FFT inplace given a complex 2D array
            The direction dir, 1 for forward, -1 for reverse
            The size of the array (nx,ny)
            Return false if there are memory problems or
            the dimensions are not powers of 2
        */
        public COMPLEX[,] FFT2D(COMPLEX[,] c, int nx, int ny, int dir)
        {
            int i, j;
            int m;//Power of 2 for current number of points
            double[] real;
            double[] imag;
            COMPLEX[,] output;//=new COMPLEX [nx,ny];
            output = c; // Copying Array
            // Transform the Rows 
            real = new double[nx];
            imag = new double[nx];

            for (j = 0; j < ny; j++)
            {
                for (i = 0; i < nx; i++)
                {
                    real[i] = c[i, j].real;
                    imag[i] = c[i, j].imag;
                }
                // Calling 1D FFT Function for Rows
                m = (int)Math.Log((double)nx, 2);//Finding power of 2 for current number of points e.g. for nx=512 m=9
                FFT1D(dir, m, ref real, ref imag);

                for (i = 0; i < nx; i++)
                {
                    //  c[i,j].real = real[i];
                    //  c[i,j].imag = imag[i];
                    output[i, j].real = real[i];
                    output[i, j].imag = imag[i];
                }
            }
            // Transform the columns  
            real = new double[ny];
            imag = new double[ny];

            for (i = 0; i < nx; i++)
            {
                for (j = 0; j < ny; j++)
                {
                    //real[j] = c[i,j].real;
                    //imag[j] = c[i,j].imag;
                    real[j] = output[i, j].real;
                    imag[j] = output[i, j].imag;
                }
                // Calling 1D FFT Function for Columns
                m = (int)Math.Log((double)ny, 2);//Finding power of 2 for current number of points e.g. for nx=512 m=9
                FFT1D(dir, m, ref real, ref imag);
                for (j = 0; j < ny; j++)
                {
                    //c[i,j].real = real[j];
                    //c[i,j].imag = imag[j];
                    output[i, j].real = real[j];
                    output[i, j].imag = imag[j];
                }
            }

            // return(true);
            return (output);
        }
        /*-------------------------------------------------------------------------
            This computes an in-place complex-to-complex FFT
            x and y are the real and imaginary arrays of 2^m points.
            dir = 1 gives forward transform
            dir = -1 gives reverse transform
            Formula: forward
                     N-1
                      ---
                    1 \         - j k 2 pi n / N
            X(K) = --- > x(n) e                  = Forward transform
                    N /                            n=0..N-1
                      ---
                     n=0
            Formula: reverse
                     N-1
                     ---
                     \          j k 2 pi n / N
            X(n) =    > x(k) e                  = Inverse transform
                     /                             k=0..N-1
                     ---
                     k=0
            */
        private void FFT1D(int dir, int m, ref double[] x, ref double[] y)
        {
            long nn, i, i1, j, k, i2, l, l1, l2;
            double c1, c2, tx, ty, t1, t2, u1, u2, z;
            /* Calculate the number of points */
            nn = 1;
            for (i = 0; i < m; i++)
                nn *= 2;
            /* Do the bit reversal */
            i2 = nn >> 1;
            j = 0;
            for (i = 0; i < nn - 1; i++)
            {
                if (i < j)
                {
                    tx = x[i];
                    ty = y[i];
                    x[i] = x[j];
                    y[i] = y[j];
                    x[j] = tx;
                    y[j] = ty;
                }
                k = i2;
                while (k <= j)
                {
                    j -= k;
                    k >>= 1;
                }
                j += k;
            }
            /* Compute the FFT */
            c1 = -1.0;
            c2 = 0.0;
            l2 = 1;
            for (l = 0; l < m; l++)
            {
                l1 = l2;
                l2 <<= 1;
                u1 = 1.0;
                u2 = 0.0;
                for (j = 0; j < l1; j++)
                {
                    for (i = j; i < nn; i += l2)
                    {
                        i1 = i + l1;
                        t1 = u1 * x[i1] - u2 * y[i1];
                        t2 = u1 * y[i1] + u2 * x[i1];
                        x[i1] = x[i] - t1;
                        y[i1] = y[i] - t2;
                        x[i] += t1;
                        y[i] += t2;
                    }
                    z = u1 * c1 - u2 * c2;
                    u2 = u1 * c2 + u2 * c1;
                    u1 = z;
                }
                c2 = Math.Sqrt((1.0 - c1) / 2.0);
                if (dir == 1)
                    c2 = -c2;
                c1 = Math.Sqrt((1.0 + c1) / 2.0);
            }
            /* Scaling for forward transform */
            if (dir == 1)
            {
                for (i = 0; i < nn; i++)
                {
                    x[i] /= (double)nn;
                    y[i] /= (double)nn;

                }
            }



            //  return(true) ;
            return;
        }

        static int HighestPowerof2(int n)
        {
            int p = (int)(Math.Log(n) /
                           Math.Log(2));
            return (int)Math.Pow(2, p);
        }

    }
}
