using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestSuite
{
    class TestParticipant
    {

        private int participantId;
        private int bodyIndex = -1;
        private UserIndex controllerIndex;
        private Point targetPoint;
        private Point lastKnownPoint;

        public TestParticipant(int participantId, UserIndex controllerIndex)
        {
            this.participantId = participantId;
            this.controllerIndex = controllerIndex;
        }

        public void SetBodyIndex(int bodyIndex)
        {
            this.bodyIndex = bodyIndex;
        }

        public void SetTargetPoint(Point targetPoint)
        {
            this.targetPoint = targetPoint;
        }

        public void UpdateLastKnownPoint(Point point)
        {
            this.lastKnownPoint = point;
        }

        public int GetBodyIndex()
        {
            return this.bodyIndex;
        }

        public Point GetTargetPoint()
        {
            return this.targetPoint;
        }

        public UserIndex GetControllerIndex()
        {
            return this.controllerIndex;
        }

        public Point GetLastKnownPoint()
        {
            return this.lastKnownPoint;
        }
    }
}
