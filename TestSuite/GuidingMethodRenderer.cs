using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TestSuite
{
    /// <summary>
    /// The Different Guiding Methods that can be used
    /// </summary>
    public enum GuidingMethod
    {
        TextBox,
        Arrows,
        Framing,
        Ellipse,
        VisualEffect
    };

    /// <summary>
    /// The direction that a user needs to move in towards the target
    /// </summary>
    public enum MovementDirection
    {
        Left,
        Right,
        Forward,
        Back
    }

    internal class GuidingMethodRenderer
    {
        // Default Guiding Method to instantiate with, can be overriden and changed
        public const GuidingMethod DEFAULT_GUIDING_METHOD = GuidingMethod.TextBox;

        private KinectSensor kinectSensor;
        private GuidingMethod currentGuidingMethod;
        private Canvas canvas;

        // Renderable Objects for the Text Box Guiding Method
        private IDictionary<int, Tuple<Border, TextBlock>> textMethodRenderable;

        /// <summary>
        /// Calculates the distance between 2 points
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>Distance: The distance between 2 points (metres)</returns>
        public static double DistanceBetweenPoints(Point p1, Point p2)
        {
            return Point.Subtract(p1, p2).Length;
        }

        /// <summary>
        /// Instantiate a GuidingMethodRenderer object which controls the graphical representation for each 
        /// Guiding Method
        /// </summary>
        /// <param name="kinectSensor">The Sensor for a Kinect Device</param>
        /// <param name="drawableCanvas">The canvas to draw the Guiding Methods On</param>
        /// <param name="initialGuidingMethod">The Guiding Method to initialise with (Default: TextBox)</param>
        public GuidingMethodRenderer(KinectSensor kinectSensor, Canvas drawableCanvas, GuidingMethod initialGuidingMethod = DEFAULT_GUIDING_METHOD)
        {
            this.kinectSensor = kinectSensor;
            this.canvas = drawableCanvas;
            this.currentGuidingMethod = initialGuidingMethod;

            InitialiseMethodRenderables();
        }

        /// <summary>
        /// Update the Guiding Method to be used and clear the current one
        /// </summary>
        /// <param name="guidingMethod">The Guiding Method to be changed to</param>
        public void SetGuidingMethod(GuidingMethod guidingMethod)
        {
            ClearGuidingMethod(currentGuidingMethod);
            currentGuidingMethod = guidingMethod;
        }

        /// <summary>
        /// Render the current Guiding Method on the canvas, can be called on each frame received
        /// </summary>
        /// <param name="bodies">An array of Body objects, populated with GetAndRefreshBodyData()</param>
        /// <param name="targetPoints">The SweetSpot points for each Body</param>
        public void RenderGuidingMethod(Body[] bodies, Point[] targetPoints)
        {
            switch (currentGuidingMethod)
            {
                case GuidingMethod.TextBox:
                    for (int i = 0; i < bodies.Length; i++)
                    {
                        Body body = bodies[i];
                        Point targetPoint = targetPoints[i];
                        Tuple<Border, TextBlock> textRenderable = textMethodRenderable[i];

                        // If Body is not tracked, hide the TextBox
                        if (!body.IsTracked)
                        {
                            textRenderable.Item1.Visibility = Visibility.Collapsed;
                            textRenderable.Item2.Visibility = Visibility.Collapsed;
                            continue;
                        }

                        // Update visibility
                        textRenderable.Item1.Visibility = Visibility.Visible;
                        textRenderable.Item2.Visibility = Visibility.Visible;

                        // Get a Joint (any down the spine) to be used as a reference point for this body
                        Joint? _trackingJoint = GetTrackingJoint(body);
                        if (_trackingJoint.HasValue)
                        {
                            Joint trackingJoint = _trackingJoint.Value;

                            Point bodyPoint = new Point { X = trackingJoint.Position.X, Y = trackingJoint.Position.Z };
                            ColorSpacePoint trackingColorPoint = kinectSensor.CoordinateMapper
                                .MapCameraPointToColorSpace(trackingJoint.Position);

                            Vector distanceVector = Point.Subtract(targetPoint, bodyPoint);

                            // Get distance to Target in centimetres
                            int horizontalDistanceCM = (int)Math.Round(Math.Abs(distanceVector.X * 100), 0);
                            int depthDistanceCM = (int)Math.Round(Math.Abs(distanceVector.Y * 100), 0);

                            // Decide the directions (X/Z) the person needs to move to the Target
                            MovementDirection horizontalDirection = distanceVector.X > 0
                                                                        ? MovementDirection.Right : MovementDirection.Left;
                            MovementDirection depthDirection = distanceVector.Y > 0
                                                                    ? MovementDirection.Back : MovementDirection.Forward;

                            string instructionLabel = $"Body: {i}\n{horizontalDirection}: {horizontalDistanceCM}cm\n" +
                                $"{depthDirection}: {depthDistanceCM}cm";

                            // Update the TextBox to new size for updated string
                            TextBlock instrTextBlock = textRenderable.Item2;
                            instrTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                            instrTextBlock.Arrange(new Rect(instrTextBlock.DesiredSize));

                            instrTextBlock.Text = instructionLabel;
                            Canvas.SetLeft(textRenderable.Item1, trackingColorPoint.X - instrTextBlock.ActualWidth / 2);
                            Canvas.SetTop(textRenderable.Item1, trackingColorPoint.Y - instrTextBlock.ActualHeight / 2);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void ClearGuidingMethod(GuidingMethod guidingMethod)
        {
            switch (guidingMethod)
            {
                case GuidingMethod.TextBox:
                    foreach (var bodyMethodRender in textMethodRenderable)
                    {
                        Tuple<Border, TextBlock> textMethodObject = bodyMethodRender.Value;
                        textMethodObject.Item1.Visibility = Visibility.Collapsed;
                        textMethodObject.Item2.Visibility = Visibility.Collapsed;
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Initialise each Guiding Methods required Canvas Objects
        /// </summary>
        private void InitialiseMethodRenderables()
        {
            int bodyCount = kinectSensor.BodyFrameSource.BodyCount;

            textMethodRenderable = new Dictionary<int, Tuple<Border, TextBlock>>();
            for (int i = 0; i < bodyCount; i++)
            {
                // --- Text Box Method ---
                TextBlock textInstruction = new TextBlock
                {
                    FontSize = 48,
                    Foreground = Brushes.Black,
                    Text = "Left: 100cm\nBack: 120cm",
                    Visibility = Visibility.Collapsed,
                };

                Border textContainer = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = Brushes.Red,
                    BorderThickness = new Thickness(2),
                    Child = textInstruction,
                    Visibility = Visibility.Collapsed
                };

                canvas.Children.Add(textContainer);
                textMethodRenderable.Add(i, new Tuple<Border, TextBlock>(textContainer, textInstruction));
            }
        }

        /// <summary>
        /// Tries to get a Joint (down the spine) that can be used as a central reference point for a skeleton,
        /// for tracking purposes
        /// </summary>
        /// <param name="body">The body object to extract the tracing joint from</param>
        /// <returns>An Optional Joint: The Joint (down the spine) to track the Skeleton Position</returns>
        private Joint? GetTrackingJoint(Body body)
        {
            // Joints down the spine to be used to track the skeleton position
            // Multiple to act as failsafes if one is out of view
            JointType[] possibleTrackingJoints = { JointType.SpineBase, JointType.SpineMid, JointType.SpineShoulder, 
                                                    JointType.Neck, JointType.Head};

            foreach (JointType jointType in possibleTrackingJoints)
            {
                if (body.Joints.ContainsKey(jointType))
                {
                    Joint trackingJoint = body.Joints[jointType];
                    CameraSpacePoint jointPosition = trackingJoint.Position;

                    if (trackingJoint.TrackingState != TrackingState.NotTracked && 
                        !double.IsInfinity(jointPosition.X) && !double.IsInfinity(jointPosition.Z) && jointPosition.Z >= 0)
                    {
                        return trackingJoint;
                    }
                }
            }

            return null;
        }
    }
}
