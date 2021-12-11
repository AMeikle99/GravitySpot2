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

        private KinectSensor kinectSensor;
        private Canvas skeletonGrid;

        private KinectSkeleton[] skeletons;


        /// <summary>
        /// Instantiates a SkeletonRenderer, incharge of rendering correctly each skeleton's 
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
                skeletons[i] = new KinectSkeleton(Colors.Red, Colors.White);
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

        public void UpdateAllSkeletons(Body[] bodies)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                Body body = bodies[i];

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

        private void UpdateSkeleton(Body body, int skeltonIndex)
        {
            IDictionary<JointType, Point> jointRenderPoints = new Dictionary<JointType, Point>();
            KinectSkeleton skeleton = skeletons[skeltonIndex];

            foreach (var jointPair in body.Joints)
            {
                JointType jointType = jointPair.Key;
                Joint joint = jointPair.Value;

                // Stop Z (depth) being negative to prevent +/- inf cases
                joint.Position.Z = joint.Position.Z < 0 ? .1f : joint.Position.Z;

                DepthSpacePoint jointDepthPoint = kinectSensor.CoordinateMapper
                                                    .MapCameraPointToDepthSpace(joint.Position);
                jointRenderPoints[jointType] = new Point { X = jointDepthPoint.X, Y = jointDepthPoint.Y };

                RenderJoint(skeleton.skeletonJoints[jointType], jointRenderPoints[jointType], joint);
            }

            foreach (var boneJointPair in skeleton.BoneJointPairs)
            {
                RenderBone(skeleton.skeletonBones[boneJointPair], body.Joints[boneJointPair.Item1], body.Joints[boneJointPair.Item2],
                            jointRenderPoints[boneJointPair.Item1], jointRenderPoints[boneJointPair.Item2]);
            }
        }

        private void ClearSkeleton(int skeletonIndex)
        {
            KinectSkeleton skeleton = skeletons[skeletonIndex];
            foreach (var bonePair in skeleton.BoneJointPairs)
            {
                skeleton.skeletonBones[bonePair].Visibility = Visibility.Collapsed;
            }

            foreach (var joint in skeleton.skeletonJoints)
            {
                joint.Value.Visibility = Visibility.Collapsed;
            }
        }

        private void RenderJoint(Ellipse jointEllipse, Point jointPoint, Joint jointInfo)
        {
            if (jointInfo.TrackingState == TrackingState.NotTracked)
            {
                jointEllipse.Visibility = Visibility.Collapsed;
                return;
            }

            jointEllipse.Visibility = Visibility.Visible;
            Canvas.SetLeft(jointEllipse, jointPoint.X - jointEllipse.Width / 2);
            Canvas.SetTop(jointEllipse, jointPoint.Y - jointEllipse.Height / 2);
        }

        private void RenderBone(Line boneLine, Joint start, Joint end, Point startPoint, Point endPoint)
        {
            if (start.TrackingState == TrackingState.NotTracked || end.TrackingState == TrackingState.NotTracked)
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
