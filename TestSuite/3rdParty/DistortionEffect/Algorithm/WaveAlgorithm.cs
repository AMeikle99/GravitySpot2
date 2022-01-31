// Cool Image Effects

using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace Algorithm {
    /// <summary>
    /// Wave Algorithm
    /// </summary>
    public class WaveAlgorithm : AlgorithmBase {
        #region Public Methods

        /// <summary>
        /// Wave effect
        /// Based on the Lecture Notes by Prof. Onur Guleryuz of Polytechnic University, New York. 
        /// </summary>
        /// <param name="algorithmParameter"></param>
        /// <param name="isSave"></param>
        /// <returns></returns>
        public override BitmapSource ApplyEffect(List<AlgorithmParameter> algorithmParameter, double effectSize, bool isSave = false) {
            SetData(isSave);
            int x, y, k, el, w1, w2;
            byte r, g, b;
            int currentSelection = algorithmParameter[0].Value;

            for (el = 0; el < CurrentHeight; ++el) {
                y = el;
                if (currentSelection == 4) {
                    y = Convert.ToInt32(el + (10 * effectSize * Math.Sin(2.0 * Math.PI * el / 30.0)));
                }
                w2 = CurrentWidth * y;
                for (k = 0; k < CurrentWidth; ++k) {
                    x = k;
                    if (currentSelection == 1) {
                        y = Convert.ToInt32(el + 4 * effectSize * Math.Sin(4.0 * Math.PI * k / 128.0));
                    } else if (currentSelection == 2) { // eff == Effects.Wave2H
                        x = Convert.ToInt32(k + 10.0 * effectSize * Math.Sin(2.0 * Math.PI * k / 30.0));
                    } else if (currentSelection == 3) {
                        x = Convert.ToInt32(k + 10.0 * effectSize * Math.Sin(2.0 * Math.PI * el / 128.0));
                    }

                    // Clamp the values
                    if (x < 0)
                        x = 0;
                    if (x >= CurrentWidth)
                        x = CurrentWidth - 1;

                    if (y < 0)
                        y = 0;
                    if (y >= CurrentHeight)
                        y = CurrentHeight - 1;

                    // Source pixel
                    w1 = CurrentWidth * y + x;
                    r = Pixels8RedCurrent[w1];
                    g = Pixels8GreenCurrent[w1];
                    b = Pixels8BlueCurrent[w1];

                    // Target pixel
                    w1 = CurrentWidth * el + k;
                    Pixels8RedResult[w1] = r;
                    Pixels8GreenResult[w1] = g;
                    Pixels8BlueResult[w1] = b;
                }
            }
            return UpdateImage();
        }

        /// <summary>
        ///Get the options suported by this algorithm
        /// </summary>
        /// <returns></returns>
        public override IList<AlgorithmOption> GetOptions() {
            Dictionary<string, AlgorithmParameter> option = GetOptionsForMethod();
            Options.Add(new AlgorithmOption(InputType.MultipleChoice, option));
            return Options;
        }

        /// <summary>
        /// Gets the display information for this algorithm
        /// </summary>
        /// <returns></returns>
        public override string GetDisplayInfo() {
            return "Wave Effect for your image";
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Gets option supported by this algorithm
        /// </summary>
        /// <returns></returns>
        Dictionary<string, AlgorithmParameter> GetOptionsForMethod() {
            Dictionary<string, AlgorithmParameter> option = new Dictionary<string, AlgorithmParameter>();
            option.Add("Wave 1",
                new AlgorithmParameter()
                {
                    Value = 1
                });
            option.Add("Wave 2",
                new AlgorithmParameter()
                {
                    Value = 2
                });
            option.Add("Wave 3",
                new AlgorithmParameter()
                {
                    Value = 3
                });
            option.Add("Wave 4",
                new AlgorithmParameter()
                {
                    Value = 4
                });
            return option;
        }
        #endregion
    }
}