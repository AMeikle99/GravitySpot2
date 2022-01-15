﻿using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

// Inspired by Body Mask Representation Code by: https://github.com/Kinect/tutorial
// Adapted for this use case
namespace TestSuite
{
    internal class MirrorImageRenderer
    {
        private const int BytesPerPixel = 4;

        private Image mirrorImageCanvas;
        private KinectSensor kinectSensor;

        private WriteableBitmap bitmapImage;

        // Each of the Points in color space converted to that of depth space
        DepthSpacePoint[] colorToDepthPoints;

        /// <summary>
        /// Instantiates a MirrorImageRenderer, in charge of rendering the body mask for each person present
        /// </summary>
        /// <param name="mirrorImageCanvas">The Image to render the User Mirror Image onto</param>
        /// <param name="kinectSensor">A kinect sensor representing a real world kinect device</param>
        public MirrorImageRenderer(Image mirrorImageCanvas, KinectSensor kinectSensor)
        {
            this.mirrorImageCanvas = mirrorImageCanvas;
            this.kinectSensor = kinectSensor;

            FrameDescription colorFrameDesc = kinectSensor.ColorFrameSource.FrameDescription;

            colorToDepthPoints = new DepthSpacePoint[colorFrameDesc.Width * colorFrameDesc.Height];
            bitmapImage = new WriteableBitmap(colorFrameDesc.Width, colorFrameDesc.Height, 1, 1, PixelFormats.Bgra32, null);
        }

        public void UpdateAllMirrorImages(MultiSourceFrame frame)
        {
            ColorFrame colorFrame = null;
            BodyIndexFrame bodyIndexFrame = null;
            DepthFrame depthFrame = null;
            KinectBuffer depthBuffer = null;
            KinectBuffer bodyIndexBuffer = null;

            try
            {

                // Acquire relevant frames, color for picture, body index to identify if a pixel belongs to a body, depth to aid in color space to depth space (different resolutions)
                colorFrame = frame.ColorFrameReference.AcquireFrame();
                bodyIndexFrame = frame.BodyIndexFrameReference.AcquireFrame();
                depthFrame = frame.DepthFrameReference.AcquireFrame();

                // Ensure none are null
                if (colorFrame == null || bodyIndexFrame == null || depthFrame == null)
                {
                    return;
                }

                // Access the buffers for depth and bodyIndex
                depthBuffer = depthFrame.LockImageBuffer();
                bodyIndexBuffer = bodyIndexFrame.LockImageBuffer();

                int depthWidth = depthFrame.FrameDescription.Width;
                int depthHeight = depthFrame.FrameDescription.Height;

                // Multiply by 2 since each pixel is 2 bytes in size
                uint depthBufferSize = (uint)(depthWidth * depthHeight * 2);

                // Map Color Space to Depth Space (different resolutions)
                kinectSensor.CoordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(depthBuffer.UnderlyingBuffer, depthBufferSize, colorToDepthPoints);
                colorFrame.CopyConvertedFrameDataToIntPtr(bitmapImage.BackBuffer, (uint)colorToDepthPoints.Length * BytesPerPixel, ColorImageFormat.Bgra);

                UpdateMaskedBodies(depthWidth, depthHeight, bodyIndexBuffer);
                UpdateBitmapImage();
            }
            // IMPORTANT - Dispose of all frames and buffers, otherwise there is a severe fps hit
            finally
            {
                if (colorFrame != null) colorFrame.Dispose();
                if (bodyIndexFrame != null) bodyIndexFrame.Dispose();
                if (depthFrame != null) depthFrame.Dispose();
                if (depthBuffer != null) depthBuffer.Dispose();
                if (bodyIndexBuffer != null) bodyIndexBuffer.Dispose();
            }


        }

        /// <summary>
        /// Removes all body masked mirror images from the screen
        /// </summary>
        public void ClearAllMirrorImages()
        {
            mirrorImageCanvas.Source = null;
        }

        /// <summary>
        /// In a pixel-wise manner check if each color pixel corresponds to a tracked body index. If it does keep, otherwise make it transparent
        /// </summary>
        /// <param name="pixelWidth">How many pixels wide the body index frame is</param>
        /// <param name="pixelHeight">How many pixels tall the body index frame is</param>
        /// <param name="bodyIndexBuffer">Object which holds the bodyIndexFrame underlying buffer (Acquired with LockImageBuffer())</param>
        unsafe private void UpdateMaskedBodies(int pixelWidth, int pixelHeight, KinectBuffer bodyIndexBuffer)
        {
            // Create a static pointer to the data buffer which can be offset to access a specific entry
            IntPtr bodyIndexByteAccess = bodyIndexBuffer.UnderlyingBuffer;
            byte* bodyIndexBytePtr = (byte*)bodyIndexByteAccess.ToPointer();

            // Assign a pointer and prevent this array being Garbage Collected
            fixed (DepthSpacePoint* colorToDepthPointsPtr = colorToDepthPoints)
            {
                // Create a pointer to the BitmapImage Image buffer, first get the IntPtr, then convert to byte level pointer and finally an unisnged-int pointer 
                // Since each pixel is 4 bytes in size
                IntPtr bitmapBufferByteAccess = bitmapImage.BackBuffer;
                byte* bitmapBufferBytes = (byte*)bitmapBufferByteAccess.ToPointer();
                uint* bitmapPixelsPtr = (uint*)bitmapBufferBytes;

                int colorPixelLength = colorToDepthPoints.Length;

                // Iterate through each color pixel (which has been converted to a depth point equivalent, meaning some depth points may map to multiple color points)
                for (int pixelIndex = 0; pixelIndex < colorPixelLength; ++pixelIndex)
                {
                    float colorToDepthX = colorToDepthPoints[pixelIndex].X;
                    float colorToDepthY = colorToDepthPoints[pixelIndex].Y;

                    // Check the X/Y position is valid (-inf is the invalid value representation)
                    if (!float.IsNegativeInfinity(colorToDepthX) && !float.IsNegativeInfinity(colorToDepthY))
                    {
                        int depthX = (int)colorToDepthX;
                        int depthY = (int)colorToDepthY;

                        // Check that the value is in a valid range
                        if (depthX >= 0 && depthY >= 0 && depthX < pixelWidth && depthY < pixelHeight)
                        {
                            // Calculate the row/col offset for the pixel to access (Y is row, X is Col)
                            int depthPixelIndex = (depthY * pixelWidth) + depthX;

                            // Skip if value is not 255 (255 is the No Body Value, otherwise it would be the body index)
                            if (bodyIndexBytePtr[depthPixelIndex] != 0xff)
                            {
                                continue;
                            }
                        }
                    }

                    // If made it this far the pixel is invalid or doesn't belong to a body. Make it transparent
                    bitmapPixelsPtr[pixelIndex] = 0;
                }
            }
        }

        /// <summary>
        /// Lock the bitmap image and update the displayed image source to refresh the screen
        /// </summary>
        private void UpdateBitmapImage()
        {
            bitmapImage.Lock();
            // Crop the "dead" zones at either side (where there is a mismatch between FOV of RGB and Depth Cameras)
            CroppedBitmap croppedBitmap = new CroppedBitmap(bitmapImage, new Int32Rect(0, 0, bitmapImage.PixelWidth - 0, bitmapImage.PixelHeight));
            mirrorImageCanvas.Source = croppedBitmap;
            bitmapImage.Unlock();
        }

    }
}
