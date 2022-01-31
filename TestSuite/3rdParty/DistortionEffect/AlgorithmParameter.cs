// Cool Image Effects

namespace Algorithm {
    /// <summary>
    /// Algorithm parameter
    /// </summary>
    public class AlgorithmParameter {
        public int Value {
            get;
            set;
        }
        public string ParameterName {
            get;
            set;
        }
    }

    /// <summary>
    /// Algorithm parameter for agorithms which accept a range of data
    /// </summary>
    public class RangeAlgorithmParameter : AlgorithmParameter {
        public int Maximum {
            get;
            set;
        }
        public int Minimum {
            get;
            set;
        }

        private int stepSize = 1;
        public int StepSize {
            get {
                return stepSize;
            }
            set {
                stepSize = value;
            }
        }
    }
}
