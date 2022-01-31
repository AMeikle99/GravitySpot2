// Cool Image Effects

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Algorithm {
    public class ImageData {
        BitmapSource originalImage;

        public BitmapSource OriginalImage {
            get {
                return originalImage;
            }
            set {
                originalImage = value;
            }
        }

        TransformedBitmap scaledImage;

        public TransformedBitmap ScaledImage {
            get {
                return scaledImage;
            }
            set {
                scaledImage = value;
            }
        }

        bool isOriginalPixelsPopulated;
        int currentHeight;
        int currentWidth;
        byte[] scaledPixels;
        byte[] originalPixels;
        string fileName;

        public List<byte> Pixels8RedResult {
            get {
                return pixels8RedResult;
            }
        }

        public List<byte> Pixels8GreenResult {
            get {
                return pixels8GreenResult;
            }
        }

        public List<byte> Pixels8BlueResult {
            get {
                return pixels8BlueResult;
            }
        }

        public List<byte> Pixels8RedCurrent {
            get {
                return pixels8RedCurrent;
            }
        }

        public List<byte> Pixels8GreenCurrent {
            get {
                return pixels8GreenCurrent;
            }
        }

        public List<byte> Pixels8BlueCurrent {
            get {
                return pixels8BlueCurrent;
            }
        }

        public List<byte> PixGray {
            get {
                return pixGray;
            }
            set {
                pixGray = value;
            }
        }

        public string FileName {
            get {
                return fileName;
            }
        }

        public int CurrentWidth {
            get {
                return currentWidth;
            }
        }

        public int CurrentHeight {
            get {
                return currentHeight;
            }
        }

        #region Scaled Pixels
        List<byte> pixels8RedScaled;
        List<byte> pixels8GreenScaled;
        List<byte> pixels8BlueScaled;
        List<byte> pixels8RedResultScaled;
        List<byte> pixels8GreenResultScaled;
        List<byte> pixels8BlueResultScaled;
        protected int scaledWidth;
        protected int scaledHeight;
        #endregion

        #region Original Pixels
        List<byte> pixels8RedOriginal;
        List<byte> pixels8GreenOriginal;
        List<byte> pixels8BlueOriginal;
        List<byte> pixels8RedResultOriginal;
        List<byte> pixels8GreenResultOriginal;
        List<byte> pixels8BlueResultOriginal;
        protected int originalWidth;
        protected int originalHeight;
        #endregion

        List<byte> pixels8RedCurrent;
        List<byte> pixels8GreenCurrent;
        List<byte> pixels8BlueCurrent;
        List<byte> pixels8RedResult;
        List<byte> pixels8GreenResult;
        List<byte> pixels8BlueResult;

        protected List<byte> pixGray;
        #region Public Methods
        /// <summary>
        /// Intializes all the arrays
        /// </summary>
        public ImageData() {
            pixels8RedScaled = new List<byte>();
            pixels8GreenScaled = new List<byte>();
            pixels8BlueScaled = new List<byte>();

            pixels8RedResultScaled = new List<byte>();
            pixels8GreenResultScaled = new List<byte>();
            pixels8BlueResultScaled = new List<byte>();

            pixels8RedOriginal = new List<byte>();
            pixels8GreenOriginal = new List<byte>();
            pixels8BlueOriginal = new List<byte>();

            pixels8RedResultOriginal = new List<byte>();
            pixels8GreenResultOriginal = new List<byte>();
            pixels8BlueResultOriginal = new List<byte>();
            pixGray = new List<byte>();
        }

        /// <summary>
        /// Return scaled image
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public BitmapSource UpdateImage() {

            int height = currentHeight;
            int width = currentWidth;

            int bitsPerPixel = 24;
            int stride = (width * bitsPerPixel + 7) / 8;

            byte[] pixelsToWrite = new byte[stride * height];
            int i1;

            for (int i = 0; i < pixelsToWrite.Count(); i += 3) {
                i1 = i / 3;
                pixelsToWrite[i] = pixels8RedResult[i1];
                pixelsToWrite[i + 1] = pixels8GreenResult[i1];
                pixelsToWrite[i + 2] = pixels8BlueResult[i1];
            }

            var newImage = BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24,
                null, pixelsToWrite, stride);

            // Clear Data
            //The following lists will be populated on next Apply effect operation.            
            //clearing the Lists.
            pixels8RedResultOriginal = null;
            pixels8GreenResultOriginal = null;
            pixels8BlueResultOriginal = null;

            pixels8RedCurrent = null;
            pixels8GreenCurrent = null;
            pixels8BlueCurrent = null;

            pixels8RedResult = null;
            pixels8GreenResult = null;
            pixels8BlueResult = null;

            pixels8RedResultOriginal = new List<byte>();
            pixels8GreenResultOriginal = new List<byte>();
            pixels8BlueResultOriginal = new List<byte>();
            pixels8RedCurrent = new List<byte>();
            pixels8GreenCurrent = new List<byte>();
            pixels8BlueCurrent = new List<byte>();
            pixels8RedResult = new List<byte>();
            pixels8GreenResult = new List<byte>();
            pixels8BlueResult = new List<byte>();
            pixelsToWrite = null;
            return newImage;
        }

        /// <summary>
        /// Set the background colour
        /// </summary>
        /// <param name="colour"></param>
        /// <param name="i1"></param>
        /// <param name="value"></param>
        /// <param name="isSave"></param> 
        public void SetBackgroundColour(AlgorithmParameter colour, int i1, byte value) {
            if (colour.Value == 1) { // Red only
                pixels8RedResult[i1] = value;
                pixels8GreenResult[i1] = 0;
                pixels8BlueResult[i1] = 0;
            } else if (colour.Value == 2) { // Green only
                pixels8RedResult[i1] = 0;
                pixels8GreenResult[i1] = value;
                pixels8BlueResult[i1] = 0;
            } else if (colour.Value == 3) { // Blue only
                pixels8RedResult[i1] = 0;
                pixels8GreenResult[i1] = 0;
                pixels8BlueResult[i1] = value;
            } else if (colour.Value == 4) { // Green and Blue = Cyan
                pixels8RedResult[i1] = 0;
                pixels8GreenResult[i1] = value;
                pixels8BlueResult[i1] = value;
            } else if (colour.Value == 5) { // Red and Blue = Magenta
                pixels8RedResult[i1] = value;
                pixels8GreenResult[i1] = 0;
                pixels8BlueResult[i1] = value;
            } else if (colour.Value == 6) { // Red and Green = Yellow
                pixels8RedResult[i1] = value;
                pixels8GreenResult[i1] = value;
                pixels8BlueResult[i1] = 0;
            } else {
                pixels8RedResult[i1] = value;
                pixels8GreenResult[i1] = value;
                pixels8BlueResult[i1] = value;
            }
        }

        /// <summary>
        /// Computes the gray scale image
        /// </summary>
        public void ComputeGrayscaleImage() {
            int k, el, index, w1;
            byte r, g, b, byteGray;
            double dGray;
            pixGray.Clear();

            // Grayscale value
            for (el = 0; el < currentHeight; ++el) {
                w1 = el * currentWidth;
                for (k = 0; k < currentWidth; ++k) {
                    index = w1 + k;
                    r = pixels8RedCurrent[index];
                    g = pixels8GreenCurrent[index];
                    b = pixels8BlueCurrent[index];

                    // Formula for grayscale
                    dGray = r * 0.1 + g * 0.6 + b * 0.3;

                    // Clamp the values
                    if (dGray > 255)
                        dGray = 255.0;
                    byteGray = (byte)dGray;
                    pixGray.Add(byteGray);
                }
            }
        }

        /// <summary>
        /// Points the current array to either scaled or original pixels
        /// </summary>
        /// <param name="isSave"></param>
        public void SetData(bool isSave) {
            // If save option is selected, then use original data
            if (isSave) {
                // Do this only once per image
                if (!isOriginalPixelsPopulated) {
                    PopulateOriginalPixels();
                    isOriginalPixelsPopulated = true;
                }
                currentHeight = originalHeight;
                currentWidth = originalWidth;
                for (int count = 0; count < pixels8RedOriginal.Count; count++) {
                    pixels8RedResultOriginal.Add(0);
                    pixels8GreenResultOriginal.Add(0);
                    pixels8BlueResultOriginal.Add(0);
                }

                pixels8RedCurrent = pixels8RedOriginal;
                pixels8BlueCurrent = pixels8BlueOriginal;
                pixels8GreenCurrent = pixels8GreenOriginal;

                pixels8RedResult = pixels8RedResultOriginal;
                pixels8BlueResult = pixels8BlueResultOriginal;
                pixels8GreenResult = pixels8GreenResultOriginal;
            } else {
                currentHeight = scaledHeight;
                currentWidth = scaledWidth;

                for (int count = 0; count < pixels8RedScaled.Count; count++) {
                    pixels8RedResultScaled.Add(0);
                    pixels8BlueResultScaled.Add(0);
                    pixels8GreenResultScaled.Add(0);
                }

                pixels8RedCurrent = pixels8RedScaled;
                pixels8BlueCurrent = pixels8BlueScaled;
                pixels8GreenCurrent = pixels8GreenScaled;

                pixels8RedResult = pixels8RedResultScaled;
                pixels8BlueResult = pixels8BlueResultScaled;
                pixels8GreenResult = pixels8GreenResultScaled;
            }
        }

        /// <summary>
        /// Load image and scale it to size provided
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public TransformedBitmap LoadImage(string fileName, out string errorMessage) {
            errorMessage = string.Empty;
            if (fileName != null) {
                isOriginalPixelsPopulated = false;

                pixels8RedOriginal.Clear();
                pixels8GreenOriginal.Clear();
                pixels8BlueOriginal.Clear();

                this.fileName = fileName;
                if (ReadImage(fileName) == true) {
                    // For display, and saving of scaled image
                    ComputeScaledWidthAndHeight();
                    ScaleImage(fileName);
                    PopulatePixelsScaled();
                } else {
                    errorMessage = "Image format not supported";
                }
            }
            return scaledImage;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Method to scale the original image to 600 x 600.
        /// This should preserve the aspect ratio of the original image.
        /// </summary>
        /// <param name="fileName"></param>
        void ScaleImage(string fileName) {
            double fac1 = Convert.ToDouble(scaledWidth / (1.0 * originalWidth));
            double fac2 = Convert.ToDouble(scaledHeight / (1.0 * originalHeight));
            var scale = new ScaleTransform(fac1, fac2);
            scaledImage = new TransformedBitmap(originalImage, scale);

            int stride = (scaledImage.PixelWidth * scaledImage.Format.BitsPerPixel + 7) / 8;

            if (scaledPixels != null)
                scaledPixels = null;
            scaledPixels = new byte[stride * scaledHeight];

            // Update the array scaledPixels from the scaled image
            scaledImage.CopyPixels(Int32Rect.Empty, scaledPixels, stride, 0);
        }

        /// <summary>
        /// Computes the scaled width and height of the image, so as to 
        /// maintain the aspect ratio of original image.
        /// </summary>
        void ComputeScaledWidthAndHeight(int size = 600) {
            if (originalWidth > originalHeight) {
                scaledWidth = size;
                scaledHeight = originalHeight * size / originalWidth;
            } else {
                scaledHeight = size;
                scaledWidth = originalWidth * size / originalHeight;
            }
        }

        /// <summary>
        /// Reads the Image and populates the fields
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        bool ReadImage(string fn) {
            bool retVal = false;
            // Open the image
            Uri imageUri = new Uri(fn, UriKind.RelativeOrAbsolute);
            originalImage = new BitmapImage(imageUri);

            int stride = (originalImage.PixelWidth * originalImage.Format.BitsPerPixel + 7) / 8;
            int origStride = stride;

            originalWidth = originalImage.PixelWidth;
            originalHeight = originalImage.PixelHeight;
            int iNumberOfPixels = originalWidth * originalHeight;

            if ((originalImage.Format == PixelFormats.Bgra32) ||
                (originalImage.Format == PixelFormats.Bgr32)) {
                retVal = true;
            }
            return retVal;
        }

        /// <summary>
        /// Populate the scaled pixels
        /// </summary>
        void PopulatePixelsScaled() {
            var capacity = originalHeight * originalWidth;
            int bitsPerPixel = originalImage.Format.BitsPerPixel;

            if (bitsPerPixel == 24 || bitsPerPixel == 32) {
                byte red, green, blue;

                pixels8RedScaled.Clear();
                pixels8GreenScaled.Clear();
                pixels8BlueScaled.Clear();

                pixels8RedResultScaled.Clear();
                pixels8GreenResultScaled.Clear();
                pixels8BlueResultScaled.Clear();

                // Populate the Red, Green and Blue lists.
                if (bitsPerPixel == 24) // 24 bits per pixel
                {
                    for (int i = 0; i < scaledPixels.Count(); i += 3) {
                        // In a 24-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red order
                        blue = (byte)(scaledPixels[i]);
                        green = (byte)(scaledPixels[i + 1]);
                        red = (byte)(scaledPixels[i + 2]);

                        pixels8RedScaled.Add(red);
                        pixels8GreenScaled.Add(green);
                        pixels8BlueScaled.Add(blue);
                    }
                }
                if (bitsPerPixel == 32) // 32 bits per pixel
                {
                    for (int i = 0; i < scaledPixels.Count(); i += 4) {
                        // In a 24-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red order
                        blue = (byte)(scaledPixels[i]);
                        green = (byte)(scaledPixels[i + 1]);
                        red = (byte)(scaledPixels[i + 2]);

                        pixels8RedScaled.Add(red);
                        pixels8GreenScaled.Add(green);
                        pixels8BlueScaled.Add(blue);
                    }
                }
            }
        }

        /// <summary>
        /// Populate original pixels
        /// </summary>
        void PopulateOriginalPixels() {
            int stride = (originalWidth * originalImage.Format.BitsPerPixel + 7) / 8;

            if (originalPixels != null)
                originalPixels = null;

            originalPixels = new byte[stride * originalHeight];
            originalImage.CopyPixels(Int32Rect.Empty, originalPixels, stride, 0);

            var capacity = originalHeight * originalWidth;
            int bitsPerPixel = originalImage.Format.BitsPerPixel;

            if (bitsPerPixel == 24 || bitsPerPixel == 32) {
                byte red, green, blue;

                pixels8RedOriginal = new List<byte>();
                pixels8GreenOriginal = new List<byte>();
                pixels8BlueOriginal = new List<byte>();

                pixels8RedResultOriginal = new List<byte>();
                pixels8GreenResultOriginal = new List<byte>();
                pixels8BlueResultOriginal = new List<byte>();

                // Populate the Red, Green and Blue lists.
                if (bitsPerPixel == 24) // 24 bits per pixel
                {
                    for (int i = 0; i < originalPixels.Count(); i += 3) {
                        // In a 24-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red order
                        blue = (byte)(originalPixels[i]);
                        green = (byte)(originalPixels[i + 1]);
                        red = (byte)(originalPixels[i + 2]);

                        pixels8RedOriginal.Add(red);
                        pixels8GreenOriginal.Add(green);
                        pixels8BlueOriginal.Add(blue);
                    }
                }
                if (bitsPerPixel == 32) // 32 bits per pixel
                {
                    for (int i = 0; i < originalPixels.Count(); i += 4) {
                        // In a 24-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red order
                        blue = (byte)(originalPixels[i]);
                        green = (byte)(originalPixels[i + 1]);
                        red = (byte)(originalPixels[i + 2]);

                        pixels8RedOriginal.Add(red);
                        pixels8GreenOriginal.Add(green);
                        pixels8BlueOriginal.Add(blue);
                    }
                }
            }
        }
        #endregion
    }
}