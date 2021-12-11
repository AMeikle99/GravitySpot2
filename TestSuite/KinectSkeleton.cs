using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

// Inspired by Skeleton Representation Code by: https://github.com/Kinect/tutorial
// Adapted for this use case
namespace TestSuite
{
    /// <summary>
    /// Represents a Skeleton and encapsulates the Bones, Joints and their associated Canvas UI elements
    /// </summary>
    internal class KinectSkeleton
    {

        /// <summary>
        /// The Start and End joints that represent a Bone
        /// </summary>
        public List<(JointType, JointType)> BoneJointPairs = new List<(JointType, JointType)>
        {
            // Upper Body
            (JointType.Head, JointType.Neck),
            (JointType.Neck, JointType.SpineShoulder),
            (JointType.SpineShoulder, JointType.ShoulderLeft),
            (JointType.SpineShoulder, JointType.ShoulderRight),

            // Left Arm
            (JointType.ShoulderLeft, JointType.ElbowLeft),
            (JointType.ElbowLeft, JointType.WristLeft),
            (JointType.WristLeft, JointType.HandLeft),
            (JointType.WristLeft, JointType.ThumbLeft),
            (JointType.HandLeft, JointType.HandTipLeft),

            // Right Arm
            (JointType.ShoulderRight, JointType.ElbowRight),
            (JointType.ElbowRight, JointType.WristRight),
            (JointType.WristRight, JointType.HandRight),
            (JointType.WristRight, JointType.ThumbRight),
            (JointType.HandRight, JointType.HandTipRight),

            // Mid Body
            (JointType.SpineShoulder, JointType.SpineMid),
            (JointType.SpineMid, JointType.SpineBase),
            (JointType.SpineBase, JointType.HipLeft),
            (JointType.SpineBase, JointType.HipRight),

            // Left Leg
            (JointType.HipLeft, JointType.KneeLeft),
            (JointType.KneeLeft, JointType.AnkleLeft),
            (JointType.AnkleLeft, JointType.FootLeft),

            // Right Leg
            (JointType.HipRight, JointType.KneeRight),
            (JointType.KneeRight, JointType.AnkleRight),
            (JointType.AnkleRight, JointType.FootRight)
        };

        /// <summary>
        /// The Graphical Lines which represent a specific bone
        /// </summary>
        public IDictionary<(JointType, JointType), Line> skeletonBones;

        /// <summary>
        /// The Graphical Ellipse that represents a skeleton joint
        /// </summary>
        public IDictionary<JointType, Ellipse> skeletonJoints;

        private Color jointColor;
        private Color boneColor;


        public KinectSkeleton(Color jointColor, Color boneColor)
        {
            this.jointColor = jointColor;
            this.boneColor = boneColor;

            skeletonJoints = new Dictionary<JointType, Ellipse>();
            skeletonBones = new Dictionary<(JointType, JointType), Line>();

            AddJoints();
            AddBones();
            
        }

        /// <summary>
        /// Populate the Joints with Default Ellipses
        /// </summary>
        private void AddJoints()
        {
            foreach (JointType joint in Enum.GetValues(typeof(JointType)))
            {
                Ellipse jointEllipse = new Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = new SolidColorBrush(jointColor),
                    Visibility = System.Windows.Visibility.Collapsed
                };

                skeletonJoints.Add(joint, jointEllipse);
            }
        }

        /// <summary>
        /// Populate the Bones with Default Lines
        /// </summary>
        private void AddBones()
        {
             foreach (var jointPair in BoneJointPairs)
            {
                Line bone = new Line
                {
                    Stroke = new SolidColorBrush(boneColor),
                    StrokeThickness = 5,
                    Visibility = System.Windows.Visibility.Collapsed
                };

                skeletonBones.Add(jointPair, bone);
            }
        }
    }
}
