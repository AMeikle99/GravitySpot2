using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestSuite
{
    /// <summary>
    /// Handles logging the positional data for a single participant for each experimental condition they experience
    /// </summary>
    internal class ParticipantLogger : ExperimentLogger
    {
        private static readonly string FILENAME_FORMAT = "P_{0, 0:D3}";
        private static readonly string FILE_PATH = "ParticipantLocations";
        private static readonly List<object> FILE_HEADERS = new List<object> { "Participant_ID", "Experiment_ID", "Condition_ID", 
                                                                                "Guiding_Method", "Representation_Type", "Curr_X", "Curr_Y", "Target_X", "Target_Y", "Adjusted_X", "Adjusted_Y", "Timestamp" };

        private readonly int participantID;
        private readonly int experimentID;
        
        private int conditionID;
        private GuidingMethod guidingMethod;
        private RepresentationType representationType;

        private Point targetPoint;

        // Timestamps are recorded for each position, the difference between starting logging and the current time is used
        private DateTime timeAtLoggingStart;

        /// <summary>
        /// Creates a new Participant Logger, to log positional data for a given participant and experiment (through each condition)
        /// </summary>
        /// <param name="participantID">ID of the participant</param>
        /// <param name="experimentID">ID of the experiment</param>
        public ParticipantLogger(int participantID, int experimentID): base(60)
        {
            this.participantID = participantID;
            this.experimentID = experimentID;
            timeAtLoggingStart = DateTime.UtcNow;
            ChangeFile(string.Format(FILENAME_FORMAT, participantID), FILE_PATH, FILE_HEADERS);
        }

        /// <summary>
        /// Updates the Condition detials for the current experiemnt run
        /// </summary>
        /// <param name="conditionOffset">The offset (i.e which condition index) of the condition in this experiment</param>
        /// <param name="targetPoint">The target point the Participant needs to move towards</param>
        public void SetExperimentCondition(Point targetPoint, int conditionOffset)
        {
            this.targetPoint = targetPoint;

            conditionID = MainWindow.experimentIDToConditionsMap[experimentID][conditionOffset];
            representationType = MainWindow.idToConditionMap[conditionID].Item1;
            guidingMethod = MainWindow.idToConditionMap[conditionID].Item2;
        }

        /// <summary>
        /// Logs the positional data for a specific participant, experiment and condition
        /// </summary>
        /// <param name="currentPosition">The current (x,y) position of the participant</param>
        public void LogParticipantPosition(Point currentPosition)
        {
            Point adjustedPosition = new Point(currentPosition.X - targetPoint.X, currentPosition.Y - targetPoint.Y);
            DateTime now = DateTime.UtcNow;
            double timestamp = now.Subtract(timeAtLoggingStart).TotalMilliseconds;
            List<object> dataToLog = new List<object> { participantID, experimentID, conditionID, guidingMethod, representationType,
                                                        currentPosition.X, currentPosition.Y, targetPoint.X, targetPoint.Y, adjustedPosition.X, adjustedPosition.Y, timestamp};
            LogData(dataToLog);
        }
    }
}
