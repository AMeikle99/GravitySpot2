using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Algorithm;

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

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData TempBmpData = TempBmp.LockBits(new Rectangle(0, 0, TempBmp.Width, TempBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

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
        }

        public static BitmapSource ApplyDistortion(string filename, double effectSize)
        {
            ImageData data = new ImageData();
            string err;
            data.LoadImage(filename, out err);

            WaveAlgorithm alg = new WaveAlgorithm();
            alg.SetImageData(data);
            var options = alg.GetOptions();
            List<AlgorithmParameter> parameters =  new List<AlgorithmParameter> { options[0].Options["Wave 1"] };
            return alg.ApplyEffect(parameters, effectSize);
        }

    }
}
