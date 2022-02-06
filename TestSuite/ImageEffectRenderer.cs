using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TestSuite
{
    internal class ImageEffectRenderer
    {

        /// Credit for this code: https://www.programmingalgorithms.com/algorithm/normal-pixelate/
        /// <summary>
        /// Applies pixelation effect to the associated Bitmap Image
        /// </summary>
        /// <param name="bmp">Reference to the bitmap image to be pixelated</param>
        /// <param name="squareSize">The size of the pixelation effect (1,1) would be no effect</param>
        public static void ApplyNormalPixelate(ref Bitmap bmp, System.Drawing.Size squareSize)
        {
            Bitmap TempBmp = (Bitmap)bmp.Clone();

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapData TempBmpData = TempBmp.LockBits(new Rectangle(0, 0, TempBmp.Width, TempBmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                byte* TempPtr = (byte*)TempBmpData.Scan0.ToPointer();

                int stopAddress = (int)ptr + (bmpData.Stride * bmpData.Height);

                int Val = 0;
                int i = 0, X = 0, Y = 0;
                int BmpStride = bmpData.Stride;
                int BmpWidth = bmp.Width;
                int BmpHeight = bmp.Height;
                int SqrWidth = squareSize.Width;
                int SqrHeight = squareSize.Height;
                int XVal = 0, YVal = 0;

                while ((int)ptr != stopAddress)
                {
                    X = i % BmpWidth;
                    Y = i / BmpWidth;

                    XVal = X + (SqrWidth - X % SqrWidth);
                    YVal = Y + (SqrHeight - Y % SqrHeight);

                    if (XVal < 0 && XVal >= BmpWidth)
                        XVal = 0;

                    if (YVal < 0 && YVal >= BmpHeight)
                        YVal = 0;

                    if (XVal > 0 && XVal < BmpWidth && YVal > 0 && YVal < BmpHeight)
                    {
                        Val = (YVal * BmpStride) + (XVal * 3);

                        ptr[0] = TempPtr[Val];
                        ptr[1] = TempPtr[Val + 1];
                        ptr[2] = TempPtr[Val + 2];
                    }

                    ptr += 3;
                    i++;
                }
            }

            bmp.UnlockBits(bmpData);
            TempBmp.UnlockBits(TempBmpData);
            TempBmp.Dispose();
        }

        /// <summary>
        /// Applies a distorition effect to the associated bitmap image
        /// </summary>
        /// <param name="bmp">>Reference to the bitmap image to be distorted</param>
        /// <param name="effectSize">The scaling effect to apply to the distortion effect 0...inf</param>
        public static void ApplyDistortionParallel(ref Bitmap bmp, double effectSize)
        {
            // Temporary copy of the bitmao image
            Bitmap TempBmp = (Bitmap)bmp.Clone();

            unsafe
            {
                // Get the Underlying bitmap data
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapData TempBmpData = TempBmp.LockBits(new Rectangle(0, 0, TempBmp.Width, TempBmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                // Pointers to the data
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                byte* tempPtr = (byte*)TempBmpData.Scan0.ToPointer();
                int stride = bmpData.Stride;

                int width = bmp.Width;
                int height = bmp.Height;

                Parallel.For(0, height, new ParallelOptions(), (row) =>
                {
                    // Pointer to the first element in that row
                    byte* bmpRowPtr = ptr + (row * stride);
                    byte* bmpTmpRowPtr = tempPtr + (row * stride);

                    // Each row has its columns shifted according to a sin wave of the current row.
                    // This results in some rows shifting left/right and the strength of the shift increasing as the peak/trough is reached
                    double rowRadians = row * (Math.PI / 180);
                    int adjustedEffectSize = (int)Math.Floor(20 * effectSize * Math.Sin(12 * effectSize * rowRadians));

                    // Copy over each byte for each pixel in each column
                    for (int col = 0; col < width; col++)
                    {

                        int tmpColOffset = Mod((col + adjustedEffectSize) * 3, stride);
                        int colOffset = col * 3;

                        *(bmpRowPtr + colOffset) = *(bmpTmpRowPtr + tmpColOffset);
                        *(bmpRowPtr + colOffset + 1) = *(bmpTmpRowPtr + tmpColOffset + 1);
                        *(bmpRowPtr + colOffset + 2) = *(bmpTmpRowPtr + tmpColOffset + 2);
                    }
                });

                bmp.UnlockBits(bmpData);
                TempBmp.UnlockBits(TempBmpData);
                TempBmp.Dispose();
            }
            
        }

        public static void ApplyDistortion(ref Bitmap bmp, double effectSize)
        {
            // Temporary copy of the bitmao image
            Bitmap TempBmp = (Bitmap)bmp.Clone();

            unsafe
            {
                // Get the Underlying bitmap data
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                BitmapData TempBmpData = TempBmp.LockBits(new Rectangle(0, 0, TempBmp.Width, TempBmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                // Pointers to the data
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                byte* tempPtr = (byte*)TempBmpData.Scan0.ToPointer();
                int stride = bmpData.Stride;

                int width = bmp.Width;
                int height = bmp.Height;

                for (int row = 0; row < height; row++)
                {
                    // Pointer to the first element in that row
                    byte* bmpRowPtr = ptr + (row * stride);
                    byte* bmpTmpRowPtr = tempPtr + (row * stride);

                    // Each row has its columns shifted according to a sin wave of the current row.
                    // This results in some rows shifting left/right and the strength of the shift increasing as the peak/trough is reached
                    double rowRadians = row * (Math.PI / 180);
                    int adjustedEffectSize = (int)Math.Floor(20 * effectSize * Math.Sin(12 * effectSize * rowRadians));

                    // Copy over each byte for each pixel in each column
                    for (int col = 0; col < width; col++)
                    {

                        int tmpColOffset = Mod((col + adjustedEffectSize) * 3, stride);
                        int colOffset = col * 3;

                        *(bmpRowPtr + colOffset) = *(bmpTmpRowPtr + tmpColOffset);
                        *(bmpRowPtr + colOffset + 1) = *(bmpTmpRowPtr + tmpColOffset + 1);
                        *(bmpRowPtr + colOffset + 2) = *(bmpTmpRowPtr + tmpColOffset + 2);
                    }
                }

                bmp.UnlockBits(bmpData);
                TempBmp.UnlockBits(TempBmpData);
                TempBmp.Dispose();
            }

        }

        /// <summary>
        /// Performs a negative-safe mod, for negative values the result is incremented by the mod amount <paramref name="b"/> until >= 0
        /// </summary>
        /// <param name="a">Modulus opperand</param>
        /// <param name="b">Modulus value</param>
        /// <returns>a Mod b</returns>
        private static int Mod(int a, int b)
        {
            if (a < 0)
            {
                return Mod(a + b, b);
            } else
            {
                return a % b;
            }
        }

    }
}
