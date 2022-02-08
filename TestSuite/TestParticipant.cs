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
        private Point targetPoint;

        public TestParticipant(int participantId)
        {
            this.participantId = participantId;
        }

        public void SetBodyIndex(int bodyIndex)
        {
            this.bodyIndex = bodyIndex;
        }

        public void SetTargetPoint(Point targetPoint)
        {
            this.targetPoint = targetPoint;
        }

        public int GetBodyIndex()
        {
            return this.bodyIndex;
        }

        public Point GetTargetPoint()
        {
            return this.targetPoint;
        }
    }
}
