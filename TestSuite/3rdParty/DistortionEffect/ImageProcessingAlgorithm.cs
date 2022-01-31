// Cool Image Effects

using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Algorithm {
    /// <summary>
    /// Delegates work to respective algorithm
    /// </summary>
    public class ImageProcessingAlgorithm : IImageProcessingAlgorithm {
        #region Private
        /// <summary>
        /// Current set algorithm
        /// </summary>
        AlgorithmBase currentAlgorithm;
        /// <summary>
        /// Algorithms supported by applications
        /// </summary>
        static IDictionary<Effects, AlgorithmBase> availableAlgorithms = new Dictionary<Effects, AlgorithmBase>();
        private static ImageData imageData = new ImageData();
        private static string lastLoadedImagePath = string.Empty;

        private AlgorithmBase GetAlgorithm(Effects effect) {
            return availableAlgorithms[effect];
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Constructor
        /// </summary>
        public ImageProcessingAlgorithm() {
            if (availableAlgorithms.Count == 0) {
                availableAlgorithms = new Dictionary<Effects, AlgorithmBase>();
                availableAlgorithms.Add(Effects.Wave, new WaveAlgorithm());
            }
        }

        /// <summary>
        /// Get the Algorithm specific options
        /// </summary>
        /// <param name="effect"></param>
        /// <returns></returns>
        public IList<AlgorithmOption> GetOptions(Effects effect) {
            var currentAlgorithm = GetAlgorithm(effect);
            if (currentAlgorithm != null) {
                return currentAlgorithm.GetOptions();
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="algorithmParameter"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public BitmapSource ApplyEffect(List<AlgorithmParameter> algorithmParameter, double effectSize, out string errorMessage) {
            BitmapSource result = null;
            errorMessage = string.Empty;
            if (currentAlgorithm != null) {
                try {
                    result = currentAlgorithm.ApplyEffect(algorithmParameter, effectSize);
                } catch (Exception ex) {

                    errorMessage = ex.Message;
                }
            } else {
                errorMessage = "Please select an Effect";
            }
            return result;
        }

        /// <summary>
        /// Sets the effect
        /// </summary>
        /// <param name="effect"></param>
        /// <returns></returns>
        public bool SetEffects(Effects effect) {
            currentAlgorithm = GetAlgorithm(effect);
            return true;
        }

        /// <summary>
        /// Loads the input image
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public TransformedBitmap LoadInputImage(string filename, out string errorMessage) {
            if (string.Compare(filename, lastLoadedImagePath, true) != 0) {
                imageData.LoadImage(filename, out errorMessage);
                lastLoadedImagePath = filename;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            TransformedBitmap result = null;
            if (currentAlgorithm != null) {
                result = imageData.ScaledImage;
                errorMessage = string.Empty;
                currentAlgorithm.SetImageData(imageData);
            } else {
                errorMessage = "Please select an Effect";
            }
            Mouse.OverrideCursor = null;
            return result;
        }

        /// <summary>
        /// Gets information about the algorithm
        /// </summary>
        /// <returns></returns>
        public string GetDisplayInfo() {
            return currentAlgorithm.GetDisplayInfo();
        }

        /// <summary>
        /// Apply the effect on the original image
        /// </summary>
        /// <param name="algorithmParameter"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public BitmapSource ApplyEffectOnOriginalDimensions(List<AlgorithmParameter> algorithmParameter, double effectSize, out string errorMessage) {
            errorMessage = string.Empty;
            BitmapSource result = null;
            try {
                result = currentAlgorithm.ApplyEffect(algorithmParameter, effectSize, true);
            } catch (Exception ex) {
                errorMessage = ex.Message;
            }
            return result;
        }
        #endregion
    }
}