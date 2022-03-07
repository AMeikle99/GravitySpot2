using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSuite
{
    class LastConditionLogger: ExperimentLogger
    {
        private static readonly string FILENAME_FORMAT = "LastConditions";
        private static readonly string FILE_PATH = "";
        private static readonly List<object> FILE_HEADERS = new List<object> { "Experiment_ID", "Condition_Offset", "Next_Participant_ID", "Guiding_Method", "Representation_Type" };

        public LastConditionLogger()
        {
            ChangeFile(FILENAME_FORMAT, FILE_PATH, FILE_HEADERS);
        }

        public void LogCondition(int experimentID, int conditionOffset, int nextParticipantID)
        {
            int experimentsCount = MainWindow.experimentIDToConditionsMap.Keys.Count;
            int conditionID = MainWindow.experimentIDToConditionsMap[experimentID % experimentsCount][conditionOffset];
            RepresentationType representationType = MainWindow.idToConditionMap[conditionID].Item1;
            GuidingMethod guidingMethod = MainWindow.idToConditionMap[conditionID].Item2;

            List<object> dataToLog = new List<object> { experimentID, conditionOffset, nextParticipantID, guidingMethod, representationType };

            PrepareToLogData(dataToLog);
            FlushData();
        }
    }
}
