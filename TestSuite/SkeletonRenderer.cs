using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

// Inspired by Skeleton Representation Code by: https://github.com/Kinect/tutorial
// Adapted for this use case
namespace TestSuite
{
    /// <summary>
    /// Manages the rendering of each skeleton body
    /// </summary>
    internal class SkeletonRenderer
    {

        public static Color[] BodyColor =
        {
            Colors.Red,
            Colors.Green,
            Colors.DarkMagenta,
            Colors.Blue,
            Colors.Purple,
            Colors.Orange
        };

        private KinectSensor kinectSensor;
        private Canvas skeletonGrid;

        private KinectSkeleton[] skeletons;


        /// <summary>
        /// Instantiates a SkeletonRenderer, in charge of rendering correctly each skeleton's 
        /// joints and bones
        /// </summary>
        /// <param name="kinectSensor">A Kinect Sensor Object, to access data about the world</param>
        /// <param name="skeletonGrid">The Canvas where each</param>
        public SkeletonRenderer(KinectSensor kinectSensor, Canvas skeletonGrid)
        {

            this.kinectSensor = kinectSensor;
            this.skeletonGrid = skeletonGrid;

            skeletons = new KinectSkeleton[kinectSensor.BodyFrameSource.BodyCount];

            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; i++)
            {
                skeletons[i] = new KinectSkeleton(BodyColor[i], Colors.White);
            }

            SetupSkeletons();
        }

        // Adds all the joints and bones to the canvas
        private void SetupSkeletons()
        {
            foreach (KinectSkeleton skeleton in skeletons)
            {
                // Add Bones to Canvas
                foreach (var boneJointPair in skeleton.skeletonBones)
                {
                    skeletonGrid.Children.Add(boneJointPair.Value);
                }

                // Add Jones to Canvas
                foreach (var jointPair in skeleton.skeletonJoints)
                {
                    skeletonGrid.Children.Add(jointPair.Value);
                }
            }
        }

        /// <summary>
        /// Updates the rendering for each Skeleton. Adding new ones and removing ones out of view
        /// </summary>
        /// <param name="bodies">An array of Body objects, ideally from GetAndRefreshBodyData()</param>
        public void UpdateAllSkeletons(Body[] bodies)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                Body body = bodies[i];

                // Check if the current body is being tracked, update or clear as appropriate
                if (body.IsTracked)
                {
                    UpdateSkeleton(body, i);
                }
                else
                {
                    ClearSkeleton(i);
                }
            }
        }

        /// <summary>
        /// Removes all skeletons from the Canvas
        /// </summary>
        public void ClearAllSkeletons()
        {
            for (int i = 0; i < skeletons.Length; i++)
            {
                ClearSkeleton(i);
            }
        }

        /// <summary>
        /// Updates the rednering for one particular bodies' skeleton
        /// </summary>
        /// <param name="body">Body data for this skeleton, containing all the joints and their positions</param>
        /// <param name="skeletonIndex">The index of the skeleton being updated</param>
        private void UpdateSkeleton(Body body, int skeletonIndex)
        {
            // Map the joint to the 2-D canvas point to render it at
            IDictionary<JointType, Point> jointRenderPoints = new Dictionary<JointType, Point>();
            KinectSkeleton skeleton = skeletons[skeletonIndex];

            // Iterate through each available joint in the body, then render the visible/tracked ones
            foreach (var jointPair in body.Joints)
            {
                JointType jointType = jointPair.Key;
                Joint joint = jointPair.Value;

                // Stop Z (depth) being negative to prevent +/- inf cases
                joint.Position.Z = joint.Position.Z < 0 ? .1f : joint.Position.Z;

                // Render the Points in a 2-D 1920x1080 plane
                ColorSpacePoint jointColorPoint = kinectSensor.CoordinateMapper
                                                    .MapCameraPointToColorSpace(joint.Position);
                jointRenderPoints[jointType] = new Point { X = jointColorPoint.X, Y = jointColorPoint.Y };

                RenderJoint(skeleton.skeletonJoints[jointType], jointRenderPoints[jointType]);
            }

            // Iterate through each Bone (2 Joint Pair) and render the fully visible ones
            foreach (var boneJointPair in skeleton.BoneJointPairs)
            {
                RenderBone(skeleton.skeletonBones[boneJointPair], body.Joints[boneJointPair.Item1], body.Joints[boneJointPair.Item2],
                            jointRenderPoints[boneJointPair.Item1], jointRenderPoints[boneJointPair.Item2]);
            }
        }

        /// <summary>
        /// Remove the specified skeleton from the canvas
        /// </summary>
        /// <param name="skeletonIndex">The index of the skeleton that should be cleared</param>
        private void ClearSkeleton(int skeletonIndex)
        {
            KinectSkeleton skeleton = skeletons[skeletonIndex];

            // Remove each bone from the canvas
            foreach (var bonePair in skeleton.BoneJointPairs)
            {
                skeleton.skeletonBones[bonePair].Visibility = Visibility.Collapsed;
            }

            // Remove each joint from the canvas
            foreach (var joint in skeleton.skeletonJoints)
            {
                joint.Value.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Updates the Ellipse which represents a specific joint (Visibility and Position)
        /// </summary>
        /// <param name="jointEllipse">The Ellipse shape that represents the joint</param>
        /// <param name="jointPoint">The point (in 2-D) to update the Ellipse with</param>
        private void RenderJoint(Ellipse jointEllipse, Point jointPoint)
        {
            if (double.IsInfinity(jointPoint.X) || double.IsInfinity(jointPoint.Y))
            {
                jointEllipse.Visibility = Visibility.Collapsed;
                return;
            }

            jointEllipse.Visibility = Visibility.Visible;
            Canvas.SetLeft(jointEllipse, jointPoint.X - jointEllipse.Width / 2);
            Canvas.SetTop(jointEllipse, jointPoint.Y - jointEllipse.Height / 2);
        }

        /// <summary>
        /// Updates the Line which represents a specific bone
        /// </summary>
        /// <param name="boneLine">The Line shape which represents a bone</param>
        /// <param name="start">The Joint where the bone originates</param>
        /// <param name="end">The Joint where the bone terminates</param>
        /// <param name="startPoint">Point for Joint 1 in the bone (where it originates)</param>
        /// <param name="endPoint">Point for Joint 2 in the bone (where it terminates)</param>
        private void RenderBone(Line boneLine, Joint start, Joint end, Point startPoint, Point endPoint)
        {
            if (double.IsInfinity(startPoint.X) || double.IsInfinity(startPoint.Y) 
                || double.IsInfinity(endPoint.X) || double.IsInfinity(endPoint.Y))
            {
                boneLine.Visibility = Visibility.Collapsed;
                return;
            }

            boneLine.Visibility = Visibility.Visible;

            boneLine.X1 = startPoint.X;
            boneLine.X2 = endPoint.X;

            boneLine.Y1 = startPoint.Y;
            boneLine.Y2 = endPoint.Y;
        }

    }
}
