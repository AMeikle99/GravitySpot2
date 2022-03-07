using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestSuite
{
    /// <summary>
    /// Handles logging the data for each participant for each experimental condition
    /// </summary>
    class ExperimentConditionLogger : ExperimentLogger
    {
        private static readonly string FILENAME_FORMAT = "E_{0, 0:D3}";
        private static readonly string FILE_PATH = "ExperimentConditionResults";
        private static readonly List<object> FILE_HEADERS = new List<object> { "Experiment_ID", "Condition_ID", "Guiding_Method", "Representation_Type", "Participant_ID",
                                                                                "Time_ms", "Distance_cm", "Final_X", "Final_Y", "Target_X", "Target_Y" };

        private int experimentID;
        private int conditionID;
        private RepresentationType representationType;
        private GuidingMethod guidingMethod;

        /// <summary>
        /// Updates the Condition detials for the current experiemnt run
        /// </summary>
        /// <param name="conditionOffset">The offset (i.e which condition index) of the condition in this experiment</param>
        public void SetExperimentCondition(int conditionOffset)
        {
            int experimentsCount = MainWindow.experimentIDToConditionsMap.Keys.Count;
            conditionID = MainWindow.experimentIDToConditionsMap[experimentID % experimentsCount][conditionOffset];
            representationType = MainWindow.idToConditionMap[conditionID].Item1;
            guidingMethod = MainWindow.idToConditionMap[conditionID].Item2;
        }

        /// <summary>
        /// Updates the Experiment ID to be logged for. Each goes into their own file
        /// </summary>
        /// <param name="newExperimentID">ID for the Experiment</param>
        public void UpdateExperimentID(int newExperimentID)
        {
            experimentID = newExperimentID;
            ChangeFile(string.Format(FILENAME_FORMAT, experimentID), FILE_PATH, FILE_HEADERS);
        }

        /// <summary>
        /// Logs the condition result for a given participant, for the current experiment run
        /// </summary>
        /// <param name="participant">Test Participant we want to log for</param>
        /// <param name="time_ms">The elapsed time for this condition, in milliseconds</param>
        /// <param name="distance_cm">The distance from the Participant's last position to Target, in centimetres</param>
        /// <param name="finalPoint">The final point the participant reached when ending this condition</param>
        public void LogConditionResult(TestParticipant participant, long time_ms, double distance_cm, Point finalPoint)
        {
            Point target = participant.GetTargetPoint();
            List<object> dataToLog = new List<object> { experimentID, conditionID, guidingMethod, representationType,
                                                        participant.GetParticipantID(), time_ms, distance_cm, finalPoint.X, finalPoint.Y, target.X, target.Y };

            PrepareToLogData(dataToLog);
        }
    }
}
