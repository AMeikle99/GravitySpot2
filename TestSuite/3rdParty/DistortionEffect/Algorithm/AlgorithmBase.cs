// Cool Image Effects

using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Algorithm {
    /// <summary>
    /// Base class for all image processing algorithms
    /// </summary>
    abstract public class AlgorithmBase {
        protected Color[] colourArray = {
                Colors.Red, Colors.Green, Colors.Blue, 
                Colors.Gray, Colors.Black,
                Colors.White };

        protected string FileName {
            get {
                return currentLoadedImage.FileName;
            }
        }

        protected int CurrentHeight {
            get {
                return currentLoadedImage.CurrentHeight;
            }
        }

        protected int CurrentWidth {
            get {
                return currentLoadedImage.CurrentWidth;
            }
        }

        protected List<byte> PixGray {
            get {
                return currentLoadedImage.PixGray;
            }
            set {
                currentLoadedImage.PixGray = value;
            }
        }

        protected List<byte> Pixels8RedResult {
            get {
                return currentLoadedImage.Pixels8RedResult;
            }
        }

        protected List<byte> Pixels8GreenResult {
            get {
                return currentLoadedImage.Pixels8GreenResult;
            }
        }

        protected List<byte> Pixels8BlueResult {
            get {
                return currentLoadedImage.Pixels8BlueResult;
            }
        }

        public List<byte> Pixels8RedCurrent {
            get {
                return currentLoadedImage.Pixels8RedCurrent;
            }
        }

        public List<byte> Pixels8GreenCurrent {
            get {
                return currentLoadedImage.Pixels8GreenCurrent;
            }
        }

        public List<byte> Pixels8BlueCurrent {
            get {
                return currentLoadedImage.Pixels8BlueCurrent;
            }
        }

        public AlgorithmBase() {
            Options = new List<AlgorithmOption>();
        }

        public void SetImageData(ImageData image) {
            this.currentLoadedImage = image;

        }

        public BitmapSource OrignalImage {
            get {
                return this.currentLoadedImage.OriginalImage;
            }
        }

        public TransformedBitmap ScaledImage {

            get {
                return this.currentLoadedImage.ScaledImage;
            }
        }

        protected IList<AlgorithmOption> Options;
        ImageData currentLoadedImage;

        #region Abstract and Virtual Methods
        public abstract BitmapSource ApplyEffect(List<AlgorithmParameter> algorithmParameter, double effectSize, bool isSave = false);
        public abstract IList<AlgorithmOption> GetOptions();
        public abstract string GetDisplayInfo();
        #endregion

        #region Public Methods
        public AlgorithmBase(ImageData imageData) {
            Options = new List<AlgorithmOption>();
            currentLoadedImage = imageData;
        }
        #endregion

        #region Protected Methods
        protected void SetData(bool isSave) {
            currentLoadedImage.SetData(isSave);
        }

        protected BitmapSource UpdateImage() {
            return currentLoadedImage.UpdateImage();
        }

        protected void SetBackgroundColour(AlgorithmParameter colour, int i1, byte value) {
            currentLoadedImage.SetBackgroundColour(colour, i1, value);
        }

        protected void ComputeGrayscaleImage() {
            currentLoadedImage.ComputeGrayscaleImage();
        }
        #endregion
    }
}