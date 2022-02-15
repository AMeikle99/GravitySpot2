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
        private int writeCountSinceFlush = 0;
        private int writesBeforeFlush;

        /// <summary>
        /// Creates a Base Experiment logger
        /// </summary>
        /// <param name="writesBeforeFlush">How many lines of data are written to buffer between flushes. Default: 1 (aka AutoFlush)</param>
        public ExperimentLogger(int writesBeforeFlush = 1)
        {
            this.writesBeforeFlush = writesBeforeFlush;
        }

        /// <summary>
        /// Logs the given data in a comma separated manner to the associated file
        /// </summary>
        /// <param name="logData">List of data items to be logged, these will be joined by commas and converted to commas</param>
        protected void LogData(List<object> logData)
        {
            if (fileOpen && logData.Count == colCount)
            {
                string logString = string.Join(",", logData);
                streamWriter.WriteLine(logString);
                writeCountSinceFlush++;

                if (writeCountSinceFlush == writesBeforeFlush)
                {
                    FlushData();
                }
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

            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }

            FileStream fs = File.OpenWrite(filepath);
            streamWriter = new StreamWriter(fs);
            fileOpen = true;

            LogData(headers);
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
        /// Force a flush of the data from buffer to the file
        /// </summary>
        protected void FlushData()
        {
            streamWriter.Flush();
            writeCountSinceFlush = 0;
        }
    }
}
