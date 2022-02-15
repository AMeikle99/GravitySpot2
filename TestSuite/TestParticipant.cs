using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TestSuite
{
    /// <summary>
    /// Represents a single Participant in the Experiment
    /// Encapsulates data about which controller they are using and their known points and targets
    /// </summary>
    class TestParticipant
    {
        ParticipantLogger logger;
        private int participantId;
        private int bodyIndex = -1;
        private UserIndex controllerIndex;
        private Point targetPoint;
        private Point lastKnownPoint;

        private bool conditionActive = false;
        private int experimentID;

        /// <summary>
        /// Initialises a TestParticipant object, for use during the experiment
        /// </summary>
        /// <param name="participantId">The ID of the Participant, in the experiment</param>
        /// <param name="controllerIndex">The index of the controller associated with that Participant</param>
        public TestParticipant(int participantId, int experimentID, UserIndex controllerIndex)
        {
            this.participantId = participantId;
            this.experimentID = experimentID;
            this.controllerIndex = controllerIndex;
        }

        /// <summary>
        /// Updates the Body index for a Participant
        /// </summary>
        /// <param name="bodyIndex">The index of the body relating to this participant</param>
        public void SetBodyIndex(int bodyIndex)
        {
            this.bodyIndex = bodyIndex;
        }

        /// <summary>
        /// Updates the experiment condition data, when the condition advances, for logging purposes
        /// </summary>
        /// <param name="conditionOffset">The offset (i.e which condition index) of the condition in this experiment</param>
        /// <param name="targetPoint">The target point the participant has to move to</param>
        public void UpdateExperimentCondition(int conditionOffset, Point targetPoint)
        {
            if (logger == null)
            {
                logger = new ParticipantLogger(participantId, experimentID);
            }
            
            this.targetPoint = targetPoint;

            logger.SetExperimentCondition(targetPoint, conditionOffset);
        }

        /// <summary>
        /// Stops the Logging for this participant, closes the file and flushes data
        /// </summary>
        public void FinishLogging()
        {
            conditionActive = false;
            logger.CloseLog();
        }

        /// <summary>
        /// Sets whether both a condition is active and the participant is "in play" for this condition, false if the user has submitted even if the condition is active for others
        /// </summary>
        /// <param name="isActive"></param>
        public void SetConditionIsActive(bool isActive)
        {
            conditionActive = isActive;
        }

        /// <summary>
        /// Updates the last point the user was tracked and is confident of
        /// </summary>
        /// <param name="point"></param>
        public void UpdateLastKnownPoint(Point point)
        {
            lastKnownPoint = point;

            if (conditionActive)
            {
                logger.LogParticipantPosition(point);
            }
        }

        /// <summary>
        /// Returns the Body Index of this Participant
        /// </summary>
        /// <returns>Body Index (0...5)</returns>
        public int GetBodyIndex()
        {
            return this.bodyIndex;
        }

        /// <summary>
        /// Returns the Target Point for this Participant
        /// </summary>
        /// <returns>Point (x,y) - Target for Participant</returns>
        public Point GetTargetPoint()
        {
            return this.targetPoint;
        }

        /// <summary>
        /// Returns the Controller Index for this Participant
        /// </summary>
        /// <returns>Controller Index - User{One, Two, Three, Four}</returns>
        public UserIndex GetControllerIndex()
        {
            return this.controllerIndex;
        }

        /// <summary>
        /// Returns the Last Point the Participant was confidently tracked to
        /// </summary>
        /// <returns>Point (x,y) - Last Tracked Position</returns>
        public Point GetLastKnownPoint()
        {
            return this.lastKnownPoint;
        }

        /// <summary>
        /// Returns the Participant ID
        /// </summary>
        /// <returns>Participant ID</returns>
        public int GetParticipantID()
        {
            return this.participantId;
        }
    }
}
