using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Vector = System.Windows.Vector;
using Vector4 = Microsoft.Kinect.Vector4;
using Bitmap = System.Drawing.Bitmap;

using System.Windows.Interop;

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
        Pixelation,
        Distortion,
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
    /// The edge of the frame, for the frame guiding method
    /// </summary>
    public enum FramePosition
    {
        Top,
        Right,
        Bottom,
        Left,
    }

    /// <summary>
    /// The corner of the frame, for the frame guiding method
    /// </summary>
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

        private IDictionary<FramePosition, Tuple<FrameCorner, FrameCorner>> frameSideCornersMap = new Dictionary<FramePosition, Tuple<FrameCorner, FrameCorner>>
        {
            { FramePosition.Top, new Tuple<FrameCorner, FrameCorner>(FrameCorner.TopLeft, FrameCorner.TopRight) },
            { FramePosition.Right, new Tuple<FrameCorner, FrameCorner>(FrameCorner.TopRight, FrameCorner.BottomRight) },
            { FramePosition.Bottom, new Tuple<FrameCorner, FrameCorner>(FrameCorner.BottomRight, FrameCorner.BottomLeft) },
            { FramePosition.Left, new Tuple<FrameCorner, FrameCorner>(FrameCorner.BottomLeft, FrameCorner.TopLeft) }
        };


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
        private IDictionary<int, IDictionary<FrameCorner, Vector3>> frameMethodCornerOffsets;
        private IDictionary<int, CameraSpacePoint> bodyTrackingPoint;
        private IDictionary<int, bool> isShowingBodyFrame;
        private bool updateBodyFrames = false;
        private IDictionary<Line, Storyboard> frameStoryboards;

        // Pixelation & Distortion Guiding Method
        private IDictionary<int, Tuple<Border, Image>> imageEffectMethodRenderable;
        private Bitmap imageEffectOriginalBitmap;

        private float FrameMargin
        {
            get => (currentRepresentationType == RepresentationType.MirrorImage ||
                    currentRepresentationType == RepresentationType.Silhouette) ? 70 : 0;
        }
        private float BottomFrameMargin
        {
            get => (currentRepresentationType == RepresentationType.MirrorImage ||
                    currentRepresentationType == RepresentationType.Silhouette) ? 50 : 0;
        }

        // The 3D rotation matrix, to adjust points for camera tilt
        private Matrix4x4 pointRotationMatrix = new Matrix4x4();
        private double cameraTiltAngle;
        private float cameraHeightOffset = 0.5f;

        public float CameraHeightOffset
        {
            get => cameraHeightOffset;
            set
            {
                if (cameraHeightOffset != value)
                {
                    cameraHeightOffset = value;
                    pointRotationMatrix.Translation = new Vector3(0, value, 0);
                }
            }
        }

        public bool isDebugMode = false;
        public double[] rotateAngles;
        // The Plane Representing what the Camera thinks is the floor
        public Vector4 cameraFloorPlane;

        public RepresentationType currentRepresentationType = RepresentationType.None;
        public double CameraTiltAngle
        {
            get => cameraTiltAngle;
            set
            {
                if (value != cameraTiltAngle)
                {
                    cameraTiltAngle = value;

                    Matrix4x4 rotationMatrix = new Matrix4x4(
                        1, 0, 0, 0,
                        0, (float)Math.Cos(CameraTiltAngle), -(float)Math.Sin(CameraTiltAngle), 0,
                        0, (float)Math.Sin(CameraTiltAngle), (float)Math.Cos(CameraTiltAngle), 0,
                        0, 0, 0, 1)
                    {
                        Translation = new Vector3(0, cameraHeightOffset, 0)
                    };

                    pointRotationMatrix =  rotationMatrix;
                }
            }
        }

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
        public void RenderGuidingMethod(Body[] bodies, Point[] targetPoints, List<int> indexesToShow)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                Body body = bodies[i];
                Point targetPoint = targetPoints[i];

                // The possible joints to use for body tracking and rendering the guiding method object
                JointType[] bodyPositionPossibleJoints = { JointType.SpineBase, JointType.SpineMid, JointType.SpineShoulder,
                                                    JointType.Neck, JointType.Head };
                JointType[] renderGuidePossibleJoints = { JointType.SpineShoulder, JointType.SpineMid };

                // If not being tracked or it is not in the specified valid indexes then hide that body's guiding object
                if (!body.IsTracked || !indexesToShow.Contains(i))
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

                CameraSpacePoint trackingJointCameraPoint = rotateCameraPointForTilt(trackingJoint.Position, true);
                CameraSpacePoint guideJointCameraPoint = rotateCameraPointForTilt(renderGuideJoint.Position);

                // Convert the joint camera position to one that can be rendered in 2D canvas
                ColorSpacePoint trackingJointColorPoint = kinectSensor.CoordinateMapper
                                            .MapCameraPointToColorSpace(trackingJointCameraPoint);
                ColorSpacePoint guideJointColorPoint = kinectSensor.CoordinateMapper
                                            .MapCameraPointToColorSpace(guideJointCameraPoint);

                if (double.IsInfinity(guideJointColorPoint.X) || double.IsInfinity(guideJointColorPoint.Y) ||
                    double.IsInfinity(trackingJointColorPoint.X) || double.IsInfinity(trackingJointColorPoint.Y))
                {
                    continue;
                }

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
                        string instructionLabel = "";
                        if (isDebugMode) instructionLabel += $"Body: {i}\n";
                        
                        instructionLabel += $"{horizontalDirection}: {horizontalDistanceCM}cm\n" +
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

                        CameraSpacePoint leftFootCameraPoint = rotateCameraPointForTilt(leftFootJoint.Position);
                        CameraSpacePoint rightFootCameraPoint = rotateCameraPointForTilt(rightFootJoint.Position);

                        // Generate ColorSpace Poitn for both feet
                        ColorSpacePoint leftFootColorPoint = kinectSensor.CoordinateMapper
                                                                .MapCameraPointToColorSpace(leftFootCameraPoint);
                        ColorSpacePoint rightFootColorPoint = kinectSensor.CoordinateMapper
                                                                .MapCameraPointToColorSpace(rightFootCameraPoint);

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
                        // If this body isn't currently being tracked, initialise to its current position
                        if (!isShowingBodyFrame[i])
                        {
                            InitialiseFrame(body, i, trackingJoint);
                            updateBodyFrames = true;
                        }
                        break;

                    // --- Render Pixelate Method
                    case GuidingMethod.Pixelation:
                        Border imageEffectBorder = imageEffectMethodRenderable[i].Item1;
                        Image imageEffectRenderable = imageEffectMethodRenderable[i].Item2;

                        imageEffectBorder.Visibility = Visibility.Visible;
                        imageEffectRenderable.Visibility = Visibility.Visible;

                        // Copy Original Bitmap and apply pixelation
                        // Effect Clamps to 1 when effect size is 1/25 OR 0.04
                        Bitmap originalTmp = (Bitmap)imageEffectOriginalBitmap.Clone();
                        int effectSize = Math.Max((int)(25 * scaleFactor), 1);
                        ImageEffectRenderer.ApplyNormalPixelate(ref originalTmp, new System.Drawing.Size(effectSize, effectSize));
                        
                        // Create a BitmapSource from our pixelated bitmap
                        // Since Pixelation creates larger block size, there is an unpixelated border
                        // Crop this from width and height (remainder of effect size from Width/Height) I.e. 10 px wide with effect size of 3 has 3 pixels and 1 remainder in border
                        BitmapSource pixelatedBitmap = Imaging.CreateBitmapSourceFromHBitmap(originalTmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        pixelatedBitmap = new CroppedBitmap(pixelatedBitmap, new Int32Rect(0, 0, originalTmp.Width - originalTmp.Width % effectSize, originalTmp.Height - originalTmp.Height % effectSize));

                        imageEffectRenderable.Source = pixelatedBitmap;

                        Canvas.SetLeft(imageEffectBorder, guideJointColorPoint.X - imageEffectBorder.Width / 2);
                        Canvas.SetTop(imageEffectBorder, guideJointColorPoint.Y - imageEffectBorder.Height / 2);
                        break;
                    case GuidingMethod.Distortion:
                        Border imageDistortionEffectBorder = imageEffectMethodRenderable[i].Item1;
                        Image imageDistortionEffectRenderable = imageEffectMethodRenderable[i].Item2;

                        imageDistortionEffectBorder.Visibility = Visibility.Visible;
                        imageDistortionEffectRenderable.Visibility = Visibility.Visible;

                        BitmapSource distoretBitmap = ImageEffectRenderer.ApplyDistortion("Assets/kempegowda.png", scaleFactor);

                        imageDistortionEffectRenderable.Source = distoretBitmap;

                        Canvas.SetLeft(imageDistortionEffectBorder, guideJointColorPoint.X - imageDistortionEffectBorder.Width / 2);
                        Canvas.SetTop(imageDistortionEffectBorder, guideJointColorPoint.Y - imageDistortionEffectBorder.Height / 2);
                        break;
                    default:
                        break;

                }
            }

            // If we have body frames we need to update and currently on the Framing Method
            if (updateBodyFrames && currentGuidingMethod == GuidingMethod.Framing)
            {
                updateBodyFrames = false;
                UpdateFrameToTargetPosition(targetPoints);
            }
        }

        

        #region UpdateFramePosition
        /// <summary>
        /// Sets the Framing method's Frame to encompass the User Body
        /// </summary>
        /// <param name="body">The object containing the details about a particular user's body</param>
        /// <param name="bodyIndex">The index of the body</param>
        /// <param name="trackingJoint">A joint that can be used as a single reference for the body position</param>
        private void InitialiseFrame(Body body, int bodyIndex, Joint trackingJoint)
        {
            float leftXPosition = float.MaxValue, rightXPosition = float.MinValue, topYPosition = float.MinValue, bottomYPosition = float.MaxValue;
            JointType leftXJoint = JointType.SpineBase, rightXJoint = JointType.SpineBase, topYJoint = JointType.SpineBase, bottomYJoint = JointType.SpineBase;
            IDictionary<FramePosition, Line> frameLines = frameMethodRenderable[bodyIndex];

            // Iterate through each joint and find the X/Y coordinates that bound the body (Max/Min X/Y)
            // i.e The Points of the horizontal/vertical joints
            foreach (var jointTypeJointPair in body.Joints)
            {
                JointType jointType = jointTypeJointPair.Key;
                Joint joint = jointTypeJointPair.Value;

                CameraSpacePoint jointPosition = rotateCameraPointForTilt(joint.Position);
                if (joint.TrackingState != TrackingState.NotTracked && !double.IsInfinity(jointPosition.X) && !double.IsInfinity(jointPosition.Y) && !double.IsInfinity(jointPosition.Z))
                {
                    // Check if the X coordinate is farther to the left than the current
                    if (jointPosition.X < leftXPosition)
                    {
                        leftXPosition = jointPosition.X;
                        leftXJoint = jointType;
                    }
                    // Check if X Coordinate is farther to the right than the current
                    if (jointPosition.X > rightXPosition)
                    {
                        rightXPosition = jointPosition.X;
                        rightXJoint = jointType;
                    }
                    // Check if the Y Coordinate is farther up than the current
                    if (jointPosition.Y > topYPosition)
                    {
                        topYPosition = jointPosition.Y;
                        topYJoint = jointType;
                    }
                    // Check if the Y Coordinate is farther down then the current
                    if (jointPosition.Y < bottomYPosition)
                    {
                        bottomYPosition = jointPosition.Y;
                        bottomYJoint = jointType;
                    }
                }
            }

            // Extract and rotate the Point for each bounding joint
            CameraSpacePoint leftJointPoint = rotateCameraPointForTilt(body.Joints[leftXJoint].Position);
            CameraSpacePoint rightJointPoint = rotateCameraPointForTilt(body.Joints[rightXJoint].Position);
            CameraSpacePoint topJointPoint = rotateCameraPointForTilt(body.Joints[topYJoint].Position);
            CameraSpacePoint bottomJointPoint = rotateCameraPointForTilt(body.Joints[bottomYJoint].Position);
            CameraSpacePoint trackingJointPoint = rotateCameraPointForTilt(trackingJoint.Position);

            // Map each to the color space (for rendering)
            leftXPosition = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(leftJointPoint).X;
            rightXPosition = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(rightJointPoint).X;
            topYPosition = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(topJointPoint).Y;
            bottomYPosition = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(bottomJointPoint).Y;

            if (double.IsInfinity(leftXPosition) || double.IsInfinity(rightXPosition) || double.IsInfinity(topYPosition) || double.IsInfinity(bottomYPosition))
            {
                return;
            }

            // Add Padding (for Mirror Image)
            leftXPosition -= FrameMargin;
            rightXPosition += FrameMargin;
            topYPosition -= FrameMargin;
            bottomYPosition += BottomFrameMargin;

            // Create Vector Points that represent each corner of the frame
            Vector topLeftCorner = new Vector(leftXPosition, topYPosition);
            Vector topRightCorner = new Vector(rightXPosition, topYPosition);
            Vector bottomRightCorner = new Vector(rightXPosition, bottomYPosition);
            Vector bottomLeftCorner = new Vector(leftXPosition, bottomYPosition);

            // Create Camera Points, using tracking joint depth to represent whole body
            CameraSpacePoint topLeftCameraPoint = new CameraSpacePoint { X = leftJointPoint.X, Y = topJointPoint.Y, Z = trackingJointPoint.Z };
            CameraSpacePoint topRightCameraPoint = new CameraSpacePoint { X = rightJointPoint.X, Y = topJointPoint.Y, Z = trackingJointPoint.Z };
            CameraSpacePoint bottomRightCameraPoint = new CameraSpacePoint { X = rightJointPoint.X, Y = bottomJointPoint.Y, Z = trackingJointPoint.Z };
            CameraSpacePoint bottomLeftCameraPoint = new CameraSpacePoint { X = leftJointPoint.X, Y = bottomJointPoint.Y, Z = trackingJointPoint.Z };

            // Convert Corner Camera Points into Vectors
            Vector3 topLeftVectorOrig = new Vector3(topLeftCameraPoint.X, topLeftCameraPoint.Y, topLeftCameraPoint.Z);
            Vector3 topRightVectorOrig = new Vector3(topRightCameraPoint.X, topRightCameraPoint.Y, topRightCameraPoint.Z);
            Vector3 bottomRightVectorOrig = new Vector3(bottomRightCameraPoint.X, bottomRightCameraPoint.Y, bottomRightCameraPoint.Z);
            Vector3 bottomLeftVectorOrig = new Vector3(bottomLeftCameraPoint.X, bottomLeftCameraPoint.Y, bottomLeftCameraPoint.Z);
            Vector3 trackingPointVector = new Vector3 { X = trackingJointPoint.X, Y = trackingJointPoint.Y, Z = trackingJointPoint.Z };

            // Store the mapping from Frame Side to the 2 Corners Points that form it
            var frameMethodOriginalRenderPositions = new Dictionary<FramePosition, IDictionary<FrameCorner, Vector>>
            {
                { FramePosition.Top, new Dictionary<FrameCorner, Vector>{ { FrameCorner.TopLeft, topLeftCorner }, { FrameCorner.TopRight, topRightCorner }} },
                { FramePosition.Right,  new Dictionary<FrameCorner, Vector>{ { FrameCorner.TopRight, topRightCorner }, { FrameCorner.BottomRight, bottomRightCorner }} },
                { FramePosition.Bottom,  new Dictionary<FrameCorner, Vector>{ { FrameCorner.BottomRight, bottomRightCorner }, { FrameCorner.BottomLeft, bottomLeftCorner }} },
                { FramePosition.Left,  new Dictionary<FrameCorner, Vector>{ { FrameCorner.BottomLeft, bottomLeftCorner }, { FrameCorner.TopLeft, topLeftCorner }} }
            };

            // Store the offsets in 3D space to move from tracking joint to each corner
            frameMethodCornerOffsets[bodyIndex] = new Dictionary<FrameCorner, Vector3>
            {
                { FrameCorner.TopLeft,  topLeftVectorOrig - trackingPointVector },
                { FrameCorner.TopRight,  topRightVectorOrig - trackingPointVector },
                { FrameCorner.BottomRight,  bottomRightVectorOrig - trackingPointVector },
                { FrameCorner.BottomLeft,  bottomLeftVectorOrig - trackingPointVector }
            };

            // Store the Reference/Tracking point for the body
            bodyTrackingPoint[bodyIndex] = rotateCameraPointForTilt(trackingJoint.Position);
            isShowingBodyFrame[bodyIndex] = true;

            // Update the Rendering of the Frame Lines
            UpdateFrameLines(frameLines, frameMethodOriginalRenderPositions, Visibility.Visible, false);
        }

        /// <summary>
        /// Update the position of the Framing Method's Frames to the location of the Sweet Spot Target
        /// </summary>
        /// <param name="targetPoints">An array of Target Points, one for each user body</param>
        private void UpdateFrameToTargetPosition(Point[] targetPoints)
        {
            // Iterate through Each body and the associated Frame Corner Offsets
            foreach (var frameCornerOffsetsKV in frameMethodCornerOffsets)
            {
                int bodyIndex = frameCornerOffsetsKV.Key;
                IDictionary<FrameCorner, Vector3> frameCornerOffsets = frameCornerOffsetsKV.Value;

                IDictionary<FrameCorner, Vector> adjustedCornerPoints = new Dictionary<FrameCorner, Vector>();
                IDictionary<FramePosition, IDictionary<FrameCorner, Vector>> targetFrameLinePoints = new Dictionary<FramePosition, IDictionary<FrameCorner, Vector>>();
                Point targetPoint = targetPoints[bodyIndex];

                CameraSpacePoint trackingPointInCameraSpace = bodyTrackingPoint[bodyIndex];
                CameraSpacePoint targetPointInCameraSpace = new CameraSpacePoint { X = (float)targetPoint.X, Y = trackingPointInCameraSpace.Y, Z = (float)targetPoint.Y };

                // Calculate the vector to adjust points using to move them towards the target position
                Vector3 targetPointVector = new Vector3 { X = targetPointInCameraSpace.X, Y = targetPointInCameraSpace.Y, Z = targetPointInCameraSpace.Z };


                IDictionary<FrameCorner, Vector> tempAdjustedCornerPoints = new Dictionary<FrameCorner, Vector>();
                // Calculate adjusted corner points, around the target point
                foreach (var frameCornerOffsetKV in frameCornerOffsets)
                {
                    // Vector representing one of the frame corners, around tghe target point, in camera space
                    Vector3 adjustedCornerVector = targetPointVector + frameCornerOffsetKV.Value;

                    CameraSpacePoint adjustedCornerPoint = new CameraSpacePoint { X = adjustedCornerVector.X, Y = adjustedCornerVector.Y, Z = adjustedCornerVector.Z };

                    // Convert corner point into color space (for rendering)
                    ColorSpacePoint adjustedCornerPointColorSpace = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(adjustedCornerPoint);

                    Vector adjustedCornerPointVector = new Vector { X = adjustedCornerPointColorSpace.X, Y = adjustedCornerPointColorSpace.Y };

                    tempAdjustedCornerPoints[frameCornerOffsetKV.Key] = adjustedCornerPointVector;
                }

                // Average out corner X/Y coordinates
                double avgTopY = (tempAdjustedCornerPoints[FrameCorner.TopLeft].Y + tempAdjustedCornerPoints[FrameCorner.TopRight].Y) / 2;
                double avgBottomY = (tempAdjustedCornerPoints[FrameCorner.BottomLeft].Y + tempAdjustedCornerPoints[FrameCorner.BottomRight].Y) / 2;
                double avgLeftX = (tempAdjustedCornerPoints[FrameCorner.TopLeft].X + tempAdjustedCornerPoints[FrameCorner.BottomLeft].X) / 2;
                double avgRightX = (tempAdjustedCornerPoints[FrameCorner.TopRight].X + tempAdjustedCornerPoints[FrameCorner.BottomRight].X) / 2;

                // Add Padding to the frame (for the Mirror Image)
                avgTopY -= FrameMargin;
                avgBottomY += BottomFrameMargin;
                avgLeftX -= FrameMargin;
                avgRightX += FrameMargin;

                adjustedCornerPoints[FrameCorner.TopLeft] = new Vector(avgLeftX, avgTopY);
                adjustedCornerPoints[FrameCorner.TopRight] = new Vector(avgRightX, avgTopY);
                adjustedCornerPoints[FrameCorner.BottomRight] = new Vector(avgRightX, avgBottomY);
                adjustedCornerPoints[FrameCorner.BottomLeft] = new Vector(avgLeftX, avgBottomY);

                // Iterate through each frame side and the associated corners
                foreach (var frameToCornerKV in frameSideCornersMap)
                {
                    FramePosition frameSide = frameToCornerKV.Key;
                    Tuple<FrameCorner, FrameCorner> corners = frameToCornerKV.Value;

                    // For each frame side line, get the points for start/end position
                    IDictionary<FrameCorner, Vector> lineCornerPoints = new Dictionary<FrameCorner, Vector>();
                    
                    lineCornerPoints[corners.Item1] = adjustedCornerPoints[corners.Item1];
                    lineCornerPoints[corners.Item2] = adjustedCornerPoints[corners.Item2];

                    // Store that lines corner points
                    targetFrameLinePoints[frameSide] = lineCornerPoints;
                }

                // Update the rendering of each frame line
                IDictionary<FramePosition, Line> frameLines = frameMethodRenderable[bodyIndex];
                UpdateFrameLines(frameLines, targetFrameLinePoints, Visibility.Visible, true);
            }
        }
        #endregion

        #region HideGuidingMethods
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
                    break;
                case GuidingMethod.Pixelation:
                case GuidingMethod.Distortion:
                    Border imageEffectBorder = imageEffectMethodRenderable[bodyIndex].Item1;
                    Image imageEffectRenderable = imageEffectMethodRenderable[bodyIndex].Item2;
                    imageEffectBorder.Visibility = Visibility.Collapsed;
                    imageEffectRenderable.Visibility = Visibility.Collapsed;
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
            if (guidingMethod == GuidingMethod.Framing)
            {
                updateBodyFrames = false;
                bodyTrackingPoint.Clear();
                frameMethodCornerOffsets.Clear();

                foreach (Storyboard sb in frameStoryboards.Values)
                {
                    sb.Remove(overlayCanvas);
                    sb.Children.Clear();
                }
                frameStoryboards.Clear();
            }

            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; i++)
            {
                HideGuidingMethod(guidingMethod, i);
            }
        }
        #endregion

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
            frameMethodCornerOffsets = new Dictionary<int, IDictionary<FrameCorner, Vector3>>();
            frameStoryboards = new Dictionary<Line, Storyboard>();
            imageEffectMethodRenderable = new Dictionary<int, Tuple<Border, Image>>();
            imageEffectOriginalBitmap = new Bitmap("Assets/kempegowda.png");
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

                // --- Image Effect Methods (Pixelation & Distortion) ---
                CreateImageEffectMethodRenderable(i);

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

        /// <summary>
        /// Creates the Image Effect Objects for a specified <paramref name="bodyIndex"/>
        /// </summary>
        /// <param name="bodyIndex">The index of the specified body</param>
        private void CreateImageEffectMethodRenderable(int bodyIndex)
        {
            Image imageEffectImage = new Image
            {
                Source = Imaging.CreateBitmapSourceFromHBitmap(imageEffectOriginalBitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, null),
                Width = 160 * 1.5,
                Height = 135 * 1.5,
                MaxWidth = 160 * 1.5,
                MaxHeight = 135 * 1.5,
                Stretch = Stretch.UniformToFill,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Border imageEffectImageBorder = new Border
            {
                BorderBrush = new SolidColorBrush(SkeletonRenderer.BodyColor[bodyIndex]),
                BorderThickness = new Thickness(2),
                Child = imageEffectImage,
                Width = imageEffectImage.MaxWidth,
                Height = imageEffectImage.MaxHeight,
                Visibility = Visibility.Collapsed
            };

            overlayCanvas.Children.Add(imageEffectImageBorder);
            imageEffectMethodRenderable.Add(bodyIndex, new Tuple<Border, Image>(imageEffectImageBorder, imageEffectImage));
        }
        #endregion

        #region FrameMethodRenderables
        /// <summary>
        /// Creates the 4 Frame Lines for a specified <paramref name="bodyIndex"/>
        /// </summary>
        /// <param name="bodyIndex"></param>
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

        /// <summary>
        /// Creates a Frame Line in a specified colour
        /// </summary>
        /// <param name="lineColor">The colour the line should be</param>
        /// <returns>A bound line that will be used in the Framing Method </returns>
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

        /// <summary>
        /// Updatess multiple lines in a Bounding Frame
        /// </summary>
        /// <param name="frameLines">Dictionary mapping Frame Side to the Line object</param>
        /// <param name="frameCorners">Dictionary mapping the Frame Side to the Corners and Points</param>
        /// <param name="visibility">Whether the Lines should be visible or not</param>
        /// <param name="animated">True if the update should happen in an animated fashion. False, otherwise</param>
        private void UpdateFrameLines(IDictionary<FramePosition, Line> frameLines, IDictionary<FramePosition, IDictionary<FrameCorner, Vector>> frameCorners, Visibility visibility, bool animated)
        {
            foreach (var frameKVPair in frameLines)
            {
                UpdateFrameLine(frameKVPair.Value, frameCorners[frameKVPair.Key].ElementAt(0).Value, frameCorners[frameKVPair.Key].ElementAt(1).Value, animated, visibility);
            }
        }

        /// <summary>
        /// Update a single Frame Bounding Line Rendering
        /// </summary>
        /// <param name="frameLine">The frame line to be updated</param>
        /// <param name="start">The new Start Position</param>
        /// <param name="end">The new End Position</param>
        /// <param name="animated">Whether the Line should be visible or not</param>
        /// <param name="visibility">True if the update should happen in an animated fashion. False, otherwise</param>
        private void UpdateFrameLine(Line frameLine, Vector start, Vector end, bool animated, Visibility visibility = Visibility.Visible)
        {
            frameLine.Visibility = visibility;

            if (animated)
            {
                // How long the animation time should be
                double animationTime = 1000;
                // How long to delay the start of the animation
                double animationStartDelay = 1000;

                Storyboard sb = new Storyboard();

                // Animation for each of the Start/End's X/Y Coordinates
                var daX1 = new DoubleAnimation(start.X, new Duration(TimeSpan.FromMilliseconds(animationTime)));
                var daX2 = new DoubleAnimation(end.X, new Duration(TimeSpan.FromMilliseconds(animationTime)));
                var daY1 = new DoubleAnimation(start.Y, new Duration(TimeSpan.FromMilliseconds(animationTime)));
                var daY2 = new DoubleAnimation(end.Y, new Duration(TimeSpan.FromMilliseconds(animationTime)));

                // Set the Begin Delay time
                daX1.BeginTime = TimeSpan.FromMilliseconds(animationStartDelay);
                daX2.BeginTime = TimeSpan.FromMilliseconds(animationStartDelay);
                daY1.BeginTime = TimeSpan.FromMilliseconds(animationStartDelay);
                daY2.BeginTime = TimeSpan.FromMilliseconds(animationStartDelay);

                sb.Children.Add(daX1);
                sb.Children.Add(daX2);
                sb.Children.Add(daY1);
                sb.Children.Add(daY2);

                // Assign the animation targets to the frame line
                Storyboard.SetTarget(daX1, frameLine);
                Storyboard.SetTarget(daX2, frameLine);
                Storyboard.SetTarget(daY1, frameLine);
                Storyboard.SetTarget(daY2, frameLine);

                // Set which Named Property the animation belongs to
                Storyboard.SetTargetProperty(daX1, new PropertyPath(Line.X1Property));
                Storyboard.SetTargetProperty(daX2, new PropertyPath(Line.X2Property));
                Storyboard.SetTargetProperty(daY1, new PropertyPath(Line.Y1Property));
                Storyboard.SetTargetProperty(daY2, new PropertyPath(Line.Y2Property));

                sb.Begin(overlayCanvas, true);
                frameStoryboards[frameLine] = sb;
            }
            else
            {
                frameLine.X1 = start.X;
                frameLine.X2 = end.X;
                frameLine.Y1 = start.Y;
                frameLine.Y2 = end.Y;
            }
        }
        #endregion

        #region HelperMethods
        /// <summary>
        /// Debugging: Get the position of the body with the lowest index and is also tracked
        /// </summary>
        /// <returns>Tuple (index, point): the body index and tracking point</returns>
        public Tuple<int, Point> PositionOfFirstTrackedBody()
        {
            Point trackedPosition = new Point(0, 0);
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

        /// <summary>
        /// Rotates a Camera Space Point, adjusting for the Tilt of the Camera
        /// </summary>
        /// <param name="pointToRotate">The Camera Point (X/Y/Z) to be rotated</param>
        /// <param name="forceRotate">Should the rotation be forced regardless of representation type</param>
        /// <returns>A Camera Space Point, rotated adjusting for camera tilt</returns>
        private CameraSpacePoint rotateCameraPointForTilt(CameraSpacePoint pointToRotate, bool forceRotate = false)
        {
            return forceRotate || (currentRepresentationType != RepresentationType.MirrorImage && currentRepresentationType != RepresentationType.Silhouette)
                ? RotateCameraPointForTilt(pointToRotate)
                : pointToRotate;
        }

        /// <summary>
        /// Rotates a Camera Space Point, adjusting for the Tilt of the Camera
        /// </summary>
        /// <param name="pointToRotate">The Camera Point (X/Y/Z) to be rotated</param>
        /// <returns>A Camera Space Point, rotated adjusting for camera tilt</returns>
        public CameraSpacePoint RotateCameraPointForTilt(CameraSpacePoint pointToRotate)
        {
            Vector3 pointToRotateVector = new Vector3(pointToRotate.X, pointToRotate.Y, pointToRotate.Z);
            Vector3 rotatedPointVector = Vector3.Transform(pointToRotateVector, pointRotationMatrix);

            //return pointToRotate;
            return new CameraSpacePoint { X = rotatedPointVector.X, Y = rotatedPointVector.Y, Z = rotatedPointVector.Z };
        }
        #endregion
    }
}
