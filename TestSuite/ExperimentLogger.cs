using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSuite
{
    /// <summary>
    /// Base class for creating other Loggers for Experimental Data
    /// </summary>
    internal abstract class ExperimentLogger
    {

        private StreamWriter streamWriter;
        private int colCount;
        private bool fileOpen = false;
        private List<List<object>> dataToLog;

        /// <summary>
        /// Creates a Base Experiment logger
        /// </summary>
        public ExperimentLogger()
        {
            dataToLog = new List<List<object>>();
        }

        /// <summary>
        /// Logs the given data in a comma separated manner to the associated file
        /// </summary>
        /// <param name="logData">List of data items to be logged, these will be joined by commas and converted to commas</param>
        private void LogData(List<object> logData)
        {
            if (fileOpen && logData.Count == colCount)
            {
                string logString = string.Join(",", logData);
                streamWriter.WriteLine(logString);
            }
        }

        protected void PrepareToLogData(List<object> logData)
        {
            if (logData.Count == colCount)
            {
                dataToLog.Add(logData);
            }
        }

        /// <summary>
        /// Changes the file the logger is pointing to. Closing and flushing inbetweeen switching
        /// </summary>
        /// <param name="filename">Name of the file to write to</param>
        /// <param name="path">The folder path for where to place the log file, relative to build directory</param>
        /// <param name="headers">List of headers for the CSV file, will be comma separated</param>
        protected void ChangeFile(string filename, string path, List<object> headers)
        {
            if (fileOpen)
            {
                CloseLog();
            }

            string filepath = new StringBuilder("Logs/").Append(path).Append("/").Append(filename).Append(".csv").ToString();
            colCount = headers.Count;

            bool printHeaders = !File.Exists(filepath);

            FileStream fs = File.Open(filepath, FileMode.Append);
            streamWriter = new StreamWriter(fs);
            fileOpen = true;

            if(printHeaders)
            {
                PrepareToLogData(headers);
                FlushData();
            }
        }

        /// <summary>
        /// Closes the Logger's log file, flushes data and cleans up memory
        /// </summary>
        public void CloseLog()
        {
            fileOpen = false;
            FlushData();

            streamWriter.Close();
            streamWriter.Dispose();
        }

        /// <summary>
        /// Flush data to the file after this experimental run, to be safe
        /// </summary>
        public void ExperimentRunFinished()
        {
            FlushData();
        }

        /// <summary>
        /// Force a flush of the data from buffer to the file
        /// </summary>
        protected void FlushData()
        {
            foreach(List<object> dataLine in dataToLog)
            {
                LogData(dataLine);
            }
            streamWriter.Flush();

            dataToLog.Clear();
        }
    }
}
