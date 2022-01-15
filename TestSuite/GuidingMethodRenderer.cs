using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Vector = System.Windows.Vector;
using Vector4 = Microsoft.Kinect.Vector4;

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
        VisualEffect,
        None
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

    /// <summary>
    /// The corner of the frame, for the frame guiding method
    /// </summary>
    public enum FramePosition
    {
        Top,
        Right,
        Bottom,
        Left,
    }

    public enum FrameCorner
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }

    internal class GuidingMethodRenderer
    {
        // Default Guiding Method to instantiate with, can be overriden and changed
        public const GuidingMethod DEFAULT_GUIDING_METHOD = GuidingMethod.None;

        private KinectSensor kinectSensor;
        private GuidingMethod currentGuidingMethod;
        private Canvas overlayCanvas;
        private Canvas underlayCanvas;

        private Tuple<Point, bool>[] currentBodyPositions;

        // Renderable Objects for the Text Box Guiding Method
        private IDictionary<int, Tuple<Border, TextBlock>> textMethodRenderable;

        // Renderable Objects for the Arrows Guiding Method
        private IDictionary<int, Tuple<Border, Image>> arrowMethodRenderable;
        private BitmapImage arrowOriginalImage;

        // Renderable Objects for the Ellipse Guiding Method
        private IDictionary<int, Ellipse> ellipseMethodRenderable;

        // The Frame Guiding Method
        private IDictionary<int, IDictionary<FramePosition, Line>> frameMethodRenderable;
        private IDictionary<int, IDictionary<FramePosition, IDictionary<FrameCorner, Vector>>> frameMethodOriginalRenderPositions;
        private IDictionary<int, IDictionary<FrameCorner, CameraSpacePoint>> frameMethodOriginalCameraPositions;
        private IDictionary<int, CameraSpacePoint> bodyTrackingPoint;
        private IDictionary<int, bool> isShowingBodyFrame;
        private bool updateBodyFrames = false;

        public double[] rotateAngles;
        public Vector4 cameraFloorClipPlane;

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
        /// <param name="overlayCanvas">The canvas to draw the Guiding Methods On</param>
        /// <param name="initialGuidingMethod">The Guiding Method to initialise with (Default: TextBox)</param>
        public GuidingMethodRenderer(KinectSensor kinectSensor, Canvas overlayCanvas, Canvas underlayCanvas, GuidingMethod initialGuidingMethod = DEFAULT_GUIDING_METHOD)
        {
            this.kinectSensor = kinectSensor;
            this.overlayCanvas = overlayCanvas;
            this.underlayCanvas = underlayCanvas;
            this.currentGuidingMethod = initialGuidingMethod;

            InitialiseMethodRenderables();
        }

        /// <summary>
        /// Update the Guiding Method to be used and clear the current one
        /// </summary>
        /// <param name="guidingMethod">The Guiding Method to be changed to</param>
        public void SetGuidingMethod(GuidingMethod guidingMethod)
        {
            GuidingMethod prevGuidingMethod = currentGuidingMethod;
            currentGuidingMethod = GuidingMethod.None;
            HideAllGuidingMethods(prevGuidingMethod);
            currentGuidingMethod = guidingMethod;
        }

        /// <summary>
        /// Render the current Guiding Method on the canvas, can be called on each frame received
        /// </summary>
        /// <param name="bodies">An array of Body objects, populated with GetAndRefreshBodyData()</param>
        /// <param name="targetPoints">The SweetSpot points for each Body</param>
        public void RenderGuidingMethod(Body[] bodies, Point[] targetPoints)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                Body body = bodies[i];
                Point targetPoint = targetPoints[i];

                // The possible joints to use for body tracking and rendering the guiding method object
                JointType[] bodyPositionPossibleJoints = { JointType.SpineBase, JointType.SpineMid, JointType.SpineShoulder,
                                                    JointType.Neck, JointType.Head };
                JointType[] renderGuidePossibleJoints = { JointType.SpineShoulder, JointType.SpineMid };

                // If not being tracked then hide that body's guiding object
                if (!body.IsTracked)
                {
                    HideGuidingMethod(currentGuidingMethod, i);
                    currentBodyPositions[i] = new Tuple<Point, bool>(new Point(0, 0), false);
                    continue;
                }

                // Get a Joint (any down the spine) to be used as a reference point for this body
                Joint? _trackingJoint = GetTrackingJoint(body, bodyPositionPossibleJoints);
                Joint? _renderGuideJoint = GetTrackingJoint(body, renderGuidePossibleJoints);
                if (!_trackingJoint.HasValue) continue;
                
                Joint trackingJoint = _trackingJoint.Value;
                Joint renderGuideJoint = _renderGuideJoint ?? trackingJoint;

                // Convert the joint camera position to one that can be rendered in 2D canvas
                ColorSpacePoint trackingJointColorPoint = kinectSensor.CoordinateMapper
                                            .MapCameraPointToColorSpace(trackingJoint.Position);
                ColorSpacePoint guideJointColorPoint = kinectSensor.CoordinateMapper
                                            .MapCameraPointToColorSpace(renderGuideJoint.Position);

                Point bodyPoint = new Point { X = trackingJoint.Position.X, Y = trackingJoint.Position.Z };
                currentBodyPositions[i] = new Tuple<Point, bool>(bodyPoint, body.IsTracked);

                // Calculate the Vector from current position to our target
                Vector moveToTargetVector = Point.Subtract(targetPoint, bodyPoint);

                // Get distance to Target in centimetres
                int horizontalDistanceCM = (int)Math.Round(Math.Abs(moveToTargetVector.X * 100), 0);
                int depthDistanceCM = (int)Math.Round(Math.Abs(moveToTargetVector.Y * 100), 0);

                double scaleFactor = GuideScaleFactor(moveToTargetVector.Length);

                switch (currentGuidingMethod)
                {
                    // --- Render Text Box Method ---
                    case GuidingMethod.TextBox:
                        Tuple<Border, TextBlock> textRenderable = textMethodRenderable[i];

                        // Update visibility
                        textRenderable.Item1.Visibility = Visibility.Visible;
                        textRenderable.Item2.Visibility = Visibility.Visible;

                        // Decide the directions (X/Z) the person needs to move to the Target
                        MovementDirection horizontalDirection = moveToTargetVector.X > 0
                                                                    ? MovementDirection.Right : MovementDirection.Left;
                        MovementDirection depthDirection = moveToTargetVector.Y > 0
                                                                ? MovementDirection.Back : MovementDirection.Forward;

                        // Format Instruction Label for User
                        string instructionLabel = $"Body: {i}\n{horizontalDirection}: {horizontalDistanceCM}cm\n" +
                            $"{depthDirection}: {depthDistanceCM}cm";

                        // Update the TextBox to new size for updated string
                        TextBlock instrTextBlock = textRenderable.Item2;
                        instrTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        instrTextBlock.Arrange(new Rect(instrTextBlock.DesiredSize));

                        // Position the Text Box
                        instrTextBlock.Text = instructionLabel;
                        Canvas.SetLeft(textRenderable.Item1, guideJointColorPoint.X - instrTextBlock.ActualWidth / 2);
                        Canvas.SetTop(textRenderable.Item1, guideJointColorPoint.Y - instrTextBlock.ActualHeight / 2);
                        break;

                    // --- Render Arrow Method ---
                    case GuidingMethod.Arrows:
                        Border arrowBorder = arrowMethodRenderable[i].Item1;
                        Image arrowRenderable = arrowMethodRenderable[i].Item2;

                        // Update the Visibility
                        arrowRenderable.Visibility = Visibility.Visible;
                        arrowBorder.Visibility = Visibility.Visible;

                        // Create vector with direction forwards/towards kinect
                        // This is because the Arrow begins pointing up (forward)
                        // Any deviance from this is how we should rotate the arrow
                        Vector baseDirection = new Vector(0, -1);
                        double rotateAngle = Vector.AngleBetween(baseDirection, moveToTargetVector);
                        rotateAngles[i] = rotateAngle;

                        // Scale First
                        // Begins shrinking when 1m left and less
                       
                        ScaleTransform arrowScaleTransform = new ScaleTransform(scaleFactor, scaleFactor,
                                                                    arrowRenderable.Width / 2, arrowRenderable.Height / 2);

                        // Then Rotate
                        RotateTransform arrowRotateTransform = new RotateTransform(rotateAngle, arrowRenderable.Width / 2,
                                                                                                arrowRenderable.Height / 2);

                        // Apply the transform to the arrow
                        TransformGroup arrowTransform = new TransformGroup();
                        arrowTransform.Children.Add(arrowScaleTransform);
                        arrowTransform.Children.Add(arrowRotateTransform);
                        arrowRenderable.RenderTransform = arrowTransform;

                        // Position the arrow to follow the User
                        Canvas.SetLeft(arrowBorder, guideJointColorPoint.X - arrowBorder.Width / 2);
                        Canvas.SetTop(arrowBorder, guideJointColorPoint.Y - arrowBorder.Height / 2);
                        break;

                    /// --- Render Ellipse Method ---
                    case GuidingMethod.Ellipse:
                        Ellipse ellipseRenderable = ellipseMethodRenderable[i];
                        ellipseRenderable.Visibility = Visibility.Visible;

                        JointType[] leftFootJointType = { JointType.FootLeft };
                        JointType[] rightFootJointType = { JointType.FootRight };

                        Joint? _leftFootJoint = GetTrackingJoint(body, leftFootJointType);
                        Joint? _rightFootJoint = GetTrackingJoint(body, rightFootJointType);

                        if (!_leftFootJoint.HasValue || !_rightFootJoint.HasValue) continue;

                        // Extract the Joint for both feet, if one is null then rely on a single foot
                        Joint leftFootJoint = _leftFootJoint.Value;
                        Joint rightFootJoint = _rightFootJoint.Value;

                        // Generate ColorSpace Poitn for both feet
                        ColorSpacePoint leftFootColorPoint = kinectSensor.CoordinateMapper
                                                                .MapCameraPointToColorSpace(leftFootJoint.Position);
                        ColorSpacePoint rightFootColorPoint = kinectSensor.CoordinateMapper
                                                                .MapCameraPointToColorSpace(rightFootJoint.Position);

                        // Ellipse Center is Centered around the horixontal distance of spine (mid point roughly) and Height of Lowest Foot (incase one is being lifted)
                        Point ellipseColorPoint = new Point { X = trackingJointColorPoint.X, Y = Math.Max(leftFootColorPoint.Y, rightFootColorPoint.Y) };

                        if (double.IsInfinity(ellipseColorPoint.X) || double.IsInfinity(ellipseColorPoint.Y))
                        {
                            continue;
                        }

                        // Scale the ellipse to match the distance from target (no difference > 1m, smallest scale is 0.2)
                        ScaleTransform ellipseScaleTransform = new ScaleTransform(scaleFactor, scaleFactor, ellipseRenderable.Width / 2, ellipseRenderable.Height / 2);
                        ellipseRenderable.RenderTransform = ellipseScaleTransform;

                        Canvas.SetLeft(ellipseRenderable, ellipseColorPoint.X - ellipseRenderable.Width / 2);
                        Canvas.SetTop(ellipseRenderable, ellipseColorPoint.Y - ellipseRenderable.Height / 2);
                        break;

                    // --- Render Frame Method ---
                    case GuidingMethod.Framing:

                        if (!isShowingBodyFrame[i])
                        {
                            InitialiseFrame(body, i, trackingJoint);
                            updateBodyFrames = true;
                        }
                        break;
                    default:
                        break;

                }
            }

            if (updateBodyFrames && currentGuidingMethod == GuidingMethod.Framing)
            {
                updateBodyFrames = false;
                UpdateFrameToTargetPosition(targetPoints);
            }
        }

        private void InitialiseFrame(Body body, int bodyIndex, Joint trackingJoint)
        {
            float leftXPosition = float.MaxValue, rightXPosition = float.MinValue, topYPosition = float.MinValue, bottomYPosition = float.MaxValue;
            JointType leftXJoint = JointType.SpineBase, rightXJoint = JointType.SpineBase, topYJoint = JointType.SpineBase, bottomYJoint = JointType.SpineBase;
            IDictionary<FramePosition, Line> frameLines = frameMethodRenderable[bodyIndex];

            foreach (var jointTypeJointPair in body.Joints)
            {
                JointType jointType = jointTypeJointPair.Key;
                Joint joint = jointTypeJointPair.Value;

                CameraSpacePoint jointPosition = joint.Position;
                if (joint.TrackingState != TrackingState.NotTracked && !double.IsInfinity(jointPosition.X) && !double.IsInfinity(jointPosition.Y) && !double.IsInfinity(jointPosition.Z))
                {
                    if (jointPosition.X < leftXPosition)
                    {
                        leftXPosition = jointPosition.X;
                        leftXJoint = jointType;
                    }
                    if (jointPosition.X > rightXPosition)
                    {
                        rightXPosition = jointPosition.X;
                        rightXJoint = jointType;
                    }
                    if (jointPosition.Y > topYPosition)
                    {
                        topYPosition = jointPosition.Y;
                        topYJoint = jointType;
                    }
                    if (jointPosition.Y < bottomYPosition)
                    {
                        bottomYPosition = jointPosition.Y;
                        bottomYJoint = jointType;
                    }
                }
            }

            leftXPosition = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[leftXJoint].Position).X;
            rightXPosition = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[rightXJoint].Position).X;
            topYPosition = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[topYJoint].Position).Y;
            bottomYPosition = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[bottomYJoint].Position).Y;

            if (double.IsInfinity(leftXPosition) || double.IsInfinity(rightXPosition) || double.IsInfinity(topYPosition) || double.IsInfinity(bottomYPosition))
            {
                return;
            }

            Vector topLeftCorner = new Vector(leftXPosition, topYPosition);
            Vector topRightCorner = new Vector(rightXPosition, topYPosition);
            Vector bottomRightCorner = new Vector(rightXPosition, bottomYPosition);
            Vector bottomLeftCorner = new Vector(leftXPosition, bottomYPosition);

            Joint leftJoint = body.Joints[leftXJoint], rightJoint = body.Joints[rightXJoint], topJoint = body.Joints[topYJoint], bottomJoint = body.Joints[bottomYJoint];

            CameraSpacePoint topLeftCameraPoint = new CameraSpacePoint { X = leftJoint.Position.X, Y = topJoint.Position.Y, Z = (topJoint.Position.Z + leftJoint.Position.Z) / 2 };
            CameraSpacePoint topRightCameraPoint = new CameraSpacePoint { X = rightJoint.Position.X, Y = topJoint.Position.Y, Z = (topJoint.Position.Z + rightJoint.Position.Z) / 2 };
            CameraSpacePoint bottomRightCameraPoint = new CameraSpacePoint { X = rightJoint.Position.X, Y = bottomJoint.Position.Y, Z = (bottomJoint.Position.Z + rightJoint.Position.Z) / 2 };
            CameraSpacePoint bottomLeftCameraPoint = new CameraSpacePoint { X = leftJoint.Position.X, Y = bottomJoint.Position.Y, Z = (bottomJoint.Position.Z + leftJoint.Position.Z) / 2 };

            frameMethodOriginalRenderPositions[bodyIndex] = new Dictionary<FramePosition, IDictionary<FrameCorner, Vector>>
            {
                { FramePosition.Top, new Dictionary<FrameCorner, Vector>{ { FrameCorner.TopLeft, topLeftCorner }, { FrameCorner.TopRight, topRightCorner }} },
                { FramePosition.Right,  new Dictionary<FrameCorner, Vector>{ { FrameCorner.TopRight, topRightCorner }, { FrameCorner.BottomRight, bottomRightCorner }} },
                { FramePosition.Bottom,  new Dictionary<FrameCorner, Vector>{ { FrameCorner.BottomRight, bottomRightCorner }, { FrameCorner.BottomLeft, bottomLeftCorner }} },
                { FramePosition.Left,  new Dictionary<FrameCorner, Vector>{ { FrameCorner.BottomLeft, bottomLeftCorner }, { FrameCorner.TopLeft, topLeftCorner }} }
            };

            frameMethodOriginalCameraPositions[bodyIndex] = new Dictionary<FrameCorner, CameraSpacePoint>
            {
                { FrameCorner.TopLeft,topLeftCameraPoint },
                { FrameCorner.TopRight, topRightCameraPoint },
                { FrameCorner.BottomRight, bottomRightCameraPoint },
                { FrameCorner.BottomLeft, bottomLeftCameraPoint },
            };

            bodyTrackingPoint[bodyIndex] = trackingJoint.Position;
            isShowingBodyFrame[bodyIndex] = true;

            UpdateFrameLines(frameLines, frameMethodOriginalRenderPositions[bodyIndex], Visibility.Visible, false, 0);
        }

        /// <summary>
        /// Debugging: Get the position of the body with the lowest index and is also tracked
        /// </summary>
        /// <returns>Tuple (index, point): the body index and tracking point</returns>
        public Tuple<int, Point> PositionOfFirstTrackedBody()
        {
            Point trackedPosition = new Point(0,0);
            int positionIndex = -1;

            for (int i = 0; i < currentBodyPositions.Length; i++)
            {
                if (currentBodyPositions[i].Item2)
                {
                    trackedPosition = currentBodyPositions[i].Item1;
                    positionIndex = i;
                    break;
                }
            }

            return new Tuple<int, Point>(positionIndex, trackedPosition);
        }

        /// <summary>
        /// Hides the currently showing Guiding Method for the specified <paramref name="bodyIndex"/>
        /// </summary>
        /// <param name="bodyIndex">The index of the body to hide the guiding object</param>
        private void HideGuidingMethod(GuidingMethod guidingMethod, int bodyIndex)
        {
            switch (guidingMethod)
            {
                case GuidingMethod.TextBox:
                    Tuple<Border, TextBlock> textRenderable = textMethodRenderable[bodyIndex];
                    textRenderable.Item1.Visibility = Visibility.Collapsed;
                    textRenderable.Item2.Visibility = Visibility.Collapsed;
                    break;
                case GuidingMethod.Arrows:
                    Border arrowBorder = arrowMethodRenderable[bodyIndex].Item1;
                    Image arrowRenderable = arrowMethodRenderable[bodyIndex].Item2;
                    arrowRenderable.Visibility = Visibility.Collapsed;
                    arrowBorder.Visibility = Visibility.Collapsed;
                    break;
                case GuidingMethod.Ellipse:
                    Ellipse ellipseRenderable = ellipseMethodRenderable[bodyIndex];
                    ellipseRenderable.Visibility = Visibility.Collapsed;
                    break;
                case GuidingMethod.Framing:
                    foreach (Line frameLine in frameMethodRenderable[bodyIndex].Values)
                    {
                        frameLine.Visibility = Visibility.Collapsed;
                    }
                    isShowingBodyFrame[bodyIndex] = false;
                    frameMethodOriginalCameraPositions.Remove(bodyIndex);
                    frameMethodOriginalRenderPositions.Remove(bodyIndex);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Clear the specified guiding method's renderables from the screen
        /// </summary>
        /// <param name="guidingMethod">The Guiding Method to be cleared</param>
        private void HideAllGuidingMethods(GuidingMethod guidingMethod)
        {
            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; i++)
            {
                HideGuidingMethod(guidingMethod, i);
            }
            if (guidingMethod == GuidingMethod.Framing)
            {
                updateBodyFrames = false;
            }
        }

        #region InitialiseGuidingMethodObjects
        /// <summary>
        /// Initialise each Guiding Methods required Canvas Objects
        /// </summary>
        private void InitialiseMethodRenderables()
        {
            int bodyCount = kinectSensor.BodyFrameSource.BodyCount;

            currentBodyPositions = new Tuple<Point, bool>[bodyCount];
            rotateAngles = new double[bodyCount];
            textMethodRenderable = new Dictionary<int, Tuple<Border, TextBlock>>();
            arrowMethodRenderable = new Dictionary<int, Tuple<Border, Image>>();
            arrowOriginalImage = new BitmapImage(new Uri("Assets/arrow_guide.png", UriKind.Relative));
            ellipseMethodRenderable = new Dictionary<int, Ellipse>();
            frameMethodRenderable = new Dictionary<int, IDictionary<FramePosition, Line>>();
            frameMethodOriginalCameraPositions = new Dictionary<int, IDictionary<FrameCorner, CameraSpacePoint>>();
            frameMethodOriginalRenderPositions = new Dictionary<int, IDictionary<FramePosition, IDictionary<FrameCorner, Vector>>>();
            bodyTrackingPoint = new Dictionary<int, CameraSpacePoint>();
            isShowingBodyFrame = new Dictionary<int, bool>();

            for (int i = 0; i < bodyCount; i++)
            {
                currentBodyPositions[i] = new Tuple<Point, bool>(new Point(0, 0), false);
                rotateAngles[i] = 0;
                // --- Text Box Method ---
                CreateTextMethodRenderable(i);

                // --- Arrow Method ---
                CreateArrowMethodRenderable(i);

                // --- Ellipse Method ---
                CreateEllipseMethodRenderable(i);

                // --- Frame Method ---
                CreateFrameMethodRenderable(i);

                isShowingBodyFrame.Add(i, false);
            }
        }

        /// <summary>
        /// Create a Text Instruction Object for a specified <paramref name="bodyIndex"/>
        /// </summary>
        /// <param name="bodyIndex">The index of the body</param>
        private void CreateTextMethodRenderable(int bodyIndex)
        {
            TextBlock textInstruction = new TextBlock
            {
                FontSize = 48,
                Foreground = Brushes.Black,
                Visibility = Visibility.Collapsed,
            };

            Border textContainer = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(SkeletonRenderer.BodyColor[bodyIndex]),
                BorderThickness = new Thickness(2),
                Child = textInstruction,
                Visibility = Visibility.Collapsed
            };

            overlayCanvas.Children.Add(textContainer);
            textMethodRenderable.Add(bodyIndex, new Tuple<Border, TextBlock>(textContainer, textInstruction));
        }
        
        /// <summary>
        /// Creates a Arrow Guide Object for a specified <paramref name="bodyIndex"/>
        /// </summary>
        /// <param name="bodyIndex">The index of the specified body</param>
        private void CreateArrowMethodRenderable(int bodyIndex)
        {
            // Create the arrow image from the original source, make it a square
            Image bodyArrowImage = new Image
            {
                Source = arrowOriginalImage,
                Width = 100,
                Height = 100,
                MaxHeight = 100,
                MaxWidth = 100,
                Stretch = Stretch.Fill,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Put a border, slightly larger, around the arrow to frame it
            Border bodyArrowBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(SkeletonRenderer.BodyColor[bodyIndex]),
                BorderThickness = new Thickness(2),
                Child = bodyArrowImage,
                Width = bodyArrowImage.MaxWidth * 1.2,
                Height = bodyArrowImage.MaxHeight * 1.2,
                MinWidth = bodyArrowImage.MaxWidth * 1.2,
                MinHeight = bodyArrowImage.MaxHeight * 1.2,
                Visibility = Visibility.Collapsed
            };

            overlayCanvas.Children.Add(bodyArrowBorder);
            arrowMethodRenderable.Add(bodyIndex, 
                new Tuple<Border, Image>(bodyArrowBorder, bodyArrowImage));
        }

        /// <summary>
        /// Creates an Ellipse Guide Object for a specified <paramref name="bodyIndex"/>
        /// </summary>
        /// <param name="bodyIndex">The index of the specified body</param>
        private void CreateEllipseMethodRenderable(int bodyIndex)
        {
            Ellipse ellipseGuide = new Ellipse
            {
                Fill = Brushes.WhiteSmoke,
                Stroke = new SolidColorBrush(SkeletonRenderer.BodyColor[bodyIndex]),
                Opacity = 0.4,
                Width = 300,
                Height = 200,
                MaxWidth = 300,
                MaxHeight = 200,
                Visibility = Visibility.Collapsed
            };

            underlayCanvas.Children.Add(ellipseGuide);
            ellipseMethodRenderable.Add(bodyIndex, ellipseGuide);
        }

        private void CreateFrameMethodRenderable(int bodyIndex)
        {
            IDictionary<FramePosition, Line> frameLines = new Dictionary<FramePosition, Line>();

            foreach (FramePosition side in Enum.GetValues(typeof(FramePosition)))
            {
                Line frameLine = CreateFrameLine(SkeletonRenderer.BodyColor[bodyIndex]);
                frameLines.Add(side, frameLine);
                overlayCanvas.Children.Add(frameLine);
            }

            frameMethodRenderable.Add(bodyIndex, frameLines);
        }

        private Line CreateFrameLine(Color lineColor)
        {
            Line line = new Line
            {
                Fill = new SolidColorBrush(lineColor),
                Stroke = new SolidColorBrush(lineColor),
                StrokeThickness = 4,
                Visibility = Visibility.Collapsed
            };

            return line;
        }
        #endregion

        private void UpdateFrameLines(IDictionary<FramePosition, Line> frameLines, IDictionary<FramePosition, IDictionary<FrameCorner, Vector>> frameCorners, Visibility visibility, bool animated, double animationDelay)
        {
            foreach (var frameKVPair in frameLines)
            {

                UpdateFrameLine(frameKVPair.Value, frameCorners[frameKVPair.Key].ElementAt(0).Value, frameCorners[frameKVPair.Key].ElementAt(1).Value, animated, animationDelay, visibility);
            }
        }

        private void UpdateFrameLine(Line frameLine, Vector start, Vector end, bool animated, double animationDelay, Visibility visibility = Visibility.Visible)
        {
            frameLine.Visibility = visibility;

            if (animated)
            {
                double animationTime = 1000;

                Storyboard sb = new Storyboard();

                var daX1 = new DoubleAnimation(start.X, new Duration(TimeSpan.FromMilliseconds(animationTime)));
                var daX2 = new DoubleAnimation(end.X, new Duration(TimeSpan.FromMilliseconds(animationTime)));
                var daY1 = new DoubleAnimation(start.Y, new Duration(TimeSpan.FromMilliseconds(animationTime)));
                var daY2 = new DoubleAnimation(end.Y, new Duration(TimeSpan.FromMilliseconds(animationTime)));

                daX1.BeginTime = TimeSpan.FromMilliseconds(animationDelay);
                daX2.BeginTime = TimeSpan.FromMilliseconds(animationDelay);
                daY1.BeginTime = TimeSpan.FromMilliseconds(animationDelay);
                daY2.BeginTime = TimeSpan.FromMilliseconds(animationDelay);

                sb.Children.Add(daX1);
                sb.Children.Add(daX2);
                sb.Children.Add(daY1);
                sb.Children.Add(daY2);

                Storyboard.SetTarget(daX1, frameLine);
                Storyboard.SetTarget(daX2, frameLine);
                Storyboard.SetTarget(daY1, frameLine);
                Storyboard.SetTarget(daY2, frameLine);

                Storyboard.SetTargetProperty(daX1, new PropertyPath(Line.X1Property));
                Storyboard.SetTargetProperty(daX2, new PropertyPath(Line.X2Property));
                Storyboard.SetTargetProperty(daY1, new PropertyPath(Line.Y1Property));
                Storyboard.SetTargetProperty(daY2, new PropertyPath(Line.Y2Property));

                sb.Begin(overlayCanvas);
            }
            else
            {
                frameLine.X1 = start.X;
                frameLine.X2 = end.X;
                frameLine.Y1 = start.Y;
                frameLine.Y2 = end.Y;
            }

        }

        private void UpdateFrameToTargetPosition(Point[] targetPoints)
        {
            foreach (var originalCameraPointsKV in frameMethodOriginalCameraPositions)
            {
                int bodyIndex = originalCameraPointsKV.Key;
                IDictionary<FrameCorner, CameraSpacePoint> originalCameraPoints = originalCameraPointsKV.Value;
                IDictionary<FrameCorner, Vector> adjustedCornerPoints = new Dictionary<FrameCorner, Vector>();
                IDictionary<FramePosition, IDictionary<FrameCorner, Vector>> targetFrameLinePoints = new Dictionary<FramePosition, IDictionary<FrameCorner, Vector>>();
                Point targetPoint = targetPoints[bodyIndex];

                CameraSpacePoint trackingPointInCameraSpace = bodyTrackingPoint[bodyIndex];
                CameraSpacePoint targetPointInCameraSpace = new CameraSpacePoint { X = (float)targetPoint.X, Y = trackingPointInCameraSpace.Y, Z = (float)targetPoint.Y };

                Vector3 trackingPointVector = new Vector3 { X = trackingPointInCameraSpace.X, Y = trackingPointInCameraSpace.Y, Z = trackingPointInCameraSpace.Z };               

                Vector3 yNew = new Vector3(cameraFloorClipPlane.X, cameraFloorClipPlane.Y, cameraFloorClipPlane.Z);
                Vector3 zNew = new Vector3(0, 1, -cameraFloorClipPlane.Y / cameraFloorClipPlane.Z);
                zNew = Vector3.Normalize(zNew);

                Vector3 xNew = Vector3.Cross(yNew, zNew);

                Matrix4x4 rotation = new Matrix4x4(
                    xNew.X, xNew.Y, xNew.Z, 0,
                    yNew.X, yNew.Y, yNew.Z, 0,
                    zNew.X, zNew.Y, zNew.Z, 0,
                    0, 0, 0, 1);

                trackingPointVector = Vector3.Transform(trackingPointVector, rotation);

                Vector3 targetPointVector = new Vector3 { X = targetPointInCameraSpace.X, Y = targetPointInCameraSpace.Y, Z = targetPointInCameraSpace.Z };
                Vector3 moveToTargetVectorCameraSpace = Vector3.Subtract(targetPointVector, trackingPointVector);

                IDictionary<FrameCorner, Vector> tempAdjustedCornerPoints = new Dictionary<FrameCorner, Vector>();
                // Calculate adjusted corner points
                foreach (FrameCorner frameCorner in Enum.GetValues(typeof(FrameCorner)))
                {
                    CameraSpacePoint originalCornerPoint = originalCameraPoints[frameCorner];
                    Vector3 originalCornerVector = new Vector3 { X = originalCornerPoint.X, Y = originalCornerPoint.Y, Z = originalCornerPoint.Z };
                    originalCornerVector = Vector3.Transform(originalCornerVector, rotation);

                    Vector3 adjustedCornerVector = originalCornerVector + moveToTargetVectorCameraSpace;
                    CameraSpacePoint adjustedCornerPoint = new CameraSpacePoint { X = adjustedCornerVector.X, Y = adjustedCornerVector.Y, Z = adjustedCornerVector.Z };

                    ColorSpacePoint adjusterCornerPointColorSpace = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(adjustedCornerPoint);

                    Vector adjustedCornerPointVector = new Vector { X = adjusterCornerPointColorSpace.X, Y = adjusterCornerPointColorSpace.Y };

                    tempAdjustedCornerPoints[frameCorner] = adjustedCornerPointVector;
                }

                // Average out corner X/Y coordinates
                double avgTopY = (tempAdjustedCornerPoints[FrameCorner.TopLeft].Y + tempAdjustedCornerPoints[FrameCorner.TopRight].Y) / 2;
                double avgBottomY = (tempAdjustedCornerPoints[FrameCorner.BottomLeft].Y + tempAdjustedCornerPoints[FrameCorner.BottomRight].Y) / 2;
                double avgLeftX = (tempAdjustedCornerPoints[FrameCorner.TopLeft].X + tempAdjustedCornerPoints[FrameCorner.BottomLeft].X) / 2;
                double avgRightX = (tempAdjustedCornerPoints[FrameCorner.TopRight].X + tempAdjustedCornerPoints[FrameCorner.BottomRight].X) / 2;

                adjustedCornerPoints[FrameCorner.TopLeft] = new Vector(avgLeftX, avgTopY);
                adjustedCornerPoints[FrameCorner.TopRight] = new Vector(avgRightX, avgTopY);
                adjustedCornerPoints[FrameCorner.BottomRight] = new Vector(avgRightX, avgBottomY);
                adjustedCornerPoints[FrameCorner.BottomLeft] = new Vector(avgLeftX, avgBottomY);

                foreach (var frameToCornerKV in frameMethodOriginalRenderPositions[bodyIndex])
                {
                    IDictionary<FrameCorner, Vector> lineCornerPoints = new Dictionary<FrameCorner, Vector>();
                    foreach (var cornerToPointKV in frameToCornerKV.Value)
                    {
                        lineCornerPoints[cornerToPointKV.Key] = adjustedCornerPoints[cornerToPointKV.Key];
                    }

                    targetFrameLinePoints[frameToCornerKV.Key] = lineCornerPoints;
                }

                IDictionary<FramePosition, Line> frameLines = frameMethodRenderable[bodyIndex];
                foreach (var frameCornerKV in targetFrameLinePoints)
                {
                    Line frameLine = frameLines[frameCornerKV.Key];
                    Vector start = frameCornerKV.Value.ElementAt(0).Value;
                    Vector end = frameCornerKV.Value.ElementAt(1).Value;

                    UpdateFrameLine(frameLine, start, end, true, 1000, Visibility.Visible);
                }
            }
        }

        /// <summary>
        /// Tries to get a Joint (down the spine) that can be used as a central reference point for a skeleton,
        /// for tracking purposes
        /// </summary>
        /// <param name="body">The body object to extract the tracing joint from</param>
        /// <returns>An Optional Joint: The Joint (down the spine) to track the Skeleton Position</returns>
        private Joint? GetTrackingJoint(Body body, JointType[] possibleJointTypes)
        {
            foreach (JointType jointType in possibleJointTypes)
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

        /// <summary>
        /// Using a scaling function, calculate the factor to scale a guide object by (based on distance to target)
        /// </summary>
        /// <param name="distanceToTarget">Distance of Current Point to Target Point (metres)</param>
        /// <returns>ScaleFactor: 0.0 to 1.0</returns>
        private double GuideScaleFactor(double distanceToTarget)
        {
            // S-Curve Function: From GravitySpot Paper (significantly most accurate)
            // URL: https://doi.org/10.1145/2807442.2807490
            double scaleFactor = (Math.Pow(2 * (distanceToTarget - 0.5), 7) + 2 * (distanceToTarget - 0.5) + 3.2) / 4;

            // Clamp to 0,1 range
            scaleFactor = Math.Max(0.0, Math.Min(scaleFactor, 1.0));

            return scaleFactor;
        }
    }
}
