using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestSuite
{
    /// <summary>
    /// Represents a single Participant in the Experiment
    /// Encapsulates data about which controller they are using and their known points and targets
    /// </summary>
    class TestParticipant
    {

        private int participantId;
        private int bodyIndex = -1;
        private UserIndex controllerIndex;
        private Point targetPoint;
        private Point lastKnownPoint;

        /// <summary>
        /// Initialises a TestParticipant object, for use during the experiment
        /// </summary>
        /// <param name="participantId">The ID of the Participant, in the experiment</param>
        /// <param name="controllerIndex">The index of the controller associated with that Participant</param>
        public TestParticipant(int participantId, UserIndex controllerIndex)
        {
            this.participantId = participantId;
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
        /// Updates the Target the User needs to reach
        /// </summary>
        /// <param name="targetPoint"></param>
        public void SetTargetPoint(Point targetPoint)
        {
            this.targetPoint = targetPoint;
        }

        /// <summary>
        /// Updates the last point the user was tracked and is confident of
        /// </summary>
        /// <param name="point"></param>
        public void UpdateLastKnownPoint(Point point)
        {
            this.lastKnownPoint = point;
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
    }
}
