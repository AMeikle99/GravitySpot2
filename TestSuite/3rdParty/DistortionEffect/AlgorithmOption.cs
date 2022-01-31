// Cool Image Effects

using System.Collections.Generic;

namespace Algorithm {
    /// <summary>
    /// Data related to input and output from an algorithm
    /// </summary>
    public class AlgorithmOption {
        /// <summary>
        /// Type of input whether input has predefined options
        /// </summary>
        public InputType InputType {
            get;
            set;
        }

        /// <summary>
        /// Options Available
        /// </summary>
        public Dictionary<string, AlgorithmParameter> Options {
            get;
            set;
        }

        public string ParameterName {
            get;
            set;
        }

        public AlgorithmOption(InputType inputType, Dictionary<string, AlgorithmParameter> options) {
            this.InputType = inputType;
            this.Options = options;
        }
    }

    /// <summary>
    /// Type of input accepted by algorithm
    /// </summary>
    public enum InputType {
        /// <summary>
        /// Can be a radio button or a combobox
        /// </summary>
        MultipleChoice,

        //Can be a text box or a slider
        SingleInput,

        /// <summary>
        /// Takes no input 
        /// </summary> 
        NoInput
    }
}