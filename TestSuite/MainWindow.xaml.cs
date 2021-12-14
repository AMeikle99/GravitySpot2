using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;

namespace TestSuite
{
    /// <summary>
    /// 
    /// </summary>
    public enum RepresentationType
    {
        Skeleton,
        MirrorImage
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Constants related to Skeleton Tracking
        // Will also be used in calculating sensible random points
        private const double SensorFOV = 70.6 * Math.PI / 180;
        private const double MinSkeletonDepth = 1.5;
        private const double MaxSkeletonDepth = 4;

        private const RepresentationType DEFAULT_REPRESENTATION = RepresentationType.Skeleton;

        // Represents the Kinect Device, used to gather data on Persons in View
        private KinectSensor kinectSensor;
        private MultiSourceFrameReader multiSourceFrameReader;

        // The Frames that are needed to be received from the Sensor
        private FrameSourceTypes enabledFramesSources =
            FrameSourceTypes.Body
            | FrameSourceTypes.Color
            | FrameSourceTypes.Depth
            | FrameSourceTypes.Infrared
            | FrameSourceTypes.BodyIndex;

        // Controls the rendering of the skeleton representation
        private SkeletonRenderer skeletonRenderer;
        // Controls rendering the different guiding techniques 
        private GuidingMethodRenderer guidingMethodRenderer;

        private RepresentationType currentUserRepresentation = DEFAULT_REPRESENTATION;
        private GuidingMethod currentGuidingMethod;
        private Point[] randomPoints;
        private Point randomPoint;
        private Point bodyPoint;

        public GuidingMethod CurrentGuidingMethod
        {
            get => currentGuidingMethod;
            set
            {
                if (currentGuidingMethod != value)
                {
                    currentGuidingMethod = value;
                    guidingMethodRenderer.SetGuidingMethod(value);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentGuidingMethod"));
                }
            }
        }
        public RepresentationType CurrentUserRepresentation
        {
            get => currentUserRepresentation;
            set
            {
                if (value != currentUserRepresentation)
                {
                    currentUserRepresentation = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentUserRepresentation"));
                }
            }
        }
        public double BodyDistance
        {
            get
            {
                if (RandomPoint == null || BodyPoint == null)
                {
                    return 0;
                }

                return GuidingMethodRenderer.DistanceBetweenPoints(RandomPoint, BodyPoint);
            }
        }
        public Point RandomPoint
        {
            get => randomPoint;
            set
            {
                if (!randomPoint.Equals(value))
                {
                    randomPoint = value;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RandomPoint"));
                }
            }
        }
        public Point BodyPoint
        {
            get => bodyPoint;
            set
            {
                if (!bodyPoint.Equals(value))
                {
                    bodyPoint = value;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BodyPoint"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BodyDistance"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();

            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(enabledFramesSources);
            multiSourceFrameReader.MultiSourceFrameArrived += MainWindow_MultiSourceFrameArrived;

            Loaded += MainWindow_Loaded;

            kinectSensor.Open();

            InitializeComponent();

            DataContext = this;
            RandomPoint = Random2DPointInCameraSpace();

            int[] bodyRange = Enumerable.Range(0, kinectSensor.BodyFrameSource.BodyCount).ToArray();
            randomPoints = bodyRange.Select(i => Random2DPointInCameraSpace()).ToArray();
        }

        #region WindowEventHandlers
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            skeletonRenderer = new SkeletonRenderer(kinectSensor, SkeletonGrid);
            guidingMethodRenderer = new GuidingMethodRenderer(kinectSensor, SkeletonGrid);

            KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // For Manual Debugging, Switch the User Representation/Guiding Method
            switch(e.Key)
            {
                // Manually Switch Guiding Technique
                case Key.D1:
                    CurrentGuidingMethod = GuidingMethod.TextBox;
                    break;
                case Key.D2:
                    CurrentGuidingMethod = GuidingMethod.Arrows;
                    break;
                case Key.D3:
                    CurrentGuidingMethod = GuidingMethod.Ellipse;
                    break;
                case Key.D4:
                    CurrentGuidingMethod = GuidingMethod.Framing;
                    break;
                case Key.D5:
                    CurrentGuidingMethod = GuidingMethod.VisualEffect;
                    break;

                // Manually Switch User Representation
                case Key.D9:
                    CurrentUserRepresentation = RepresentationType.Skeleton;
                    break;
                case Key.D0:
                    CurrentUserRepresentation = RepresentationType.MirrorImage;
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region KinectEventCallback
        private void MainWindow_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            if (multiSourceFrame == null)
            {
                return;
            }

            using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
            {
                Body[] bodies = new Body[kinectSensor.BodyFrameSource.BodyCount];

                bodyFrame.GetAndRefreshBodyData(bodies);

                // Renders the Skeleton Representation and the Guiding Method currently selected
                RenderSkeleton(bodies);
                RenderGuidingMethod(bodies);
            }
        }
        #endregion

        #region SkeletonRendering
        /// <summary>
        /// Renders the users as a Skeleton Representation
        /// </summary>
        /// <param name="bodies">An array of Body objects, populated from GetAndRefreshBodyData()</param>
        private void RenderSkeleton(Body[] bodies)
        {
            Body[] trackedBodies = bodies.Where(body => body.IsTracked).ToArray();

            if (trackedBodies.Any())
            {
                Body firstBody = trackedBodies.First();
                CameraSpacePoint bodyCameraPoint = firstBody.Joints[JointType.SpineBase].Position;
                BodyPoint = new Point(bodyCameraPoint.X, bodyCameraPoint.Z);
            }

            skeletonRenderer.UpdateAllSkeletons(bodies);
        }
        #endregion

        #region GuidingMethodRendering
        /// <summary>
        /// Renders the currently selected Guiding Method
        /// </summary>
        /// <param name="bodies">An array of Body objects, populated from GetAndRefreshBodyData()</param>
        private void RenderGuidingMethod(Body[] bodies)
        {
            guidingMethodRenderer.RenderGuidingMethod(bodies, randomPoints);
        }
        #endregion

        /// <summary>
        /// Calculates the theoretical max sensor depth at a given depth
        /// </summary>
        /// <param name="depth">The depth of the point from the Sensor</param>
        /// <returns>SensorWidth: The reliable sensing width at a given depth</returns>
        private double MaxSensorWidth(double depth)
        {
            // Using Formula Here (based on trig):
            // https://social.msdn.microsoft.com/Forums/en-US/c95d3e40-6ed6-47a1-a206-5ff26c889c29/kinect-v2-maximum-range
            return 2 * depth * Math.Tan(SensorFOV / 2);
        }

        /// <summary>
        /// Gets the +/- Camera X Range
        /// </summary>
        /// <param name="depth">The depth of the point from the Sensor</param>
        /// <returns>(MinCameraX, MaxCameraX) - The +/- range of X values at a given depth</returns>
        private (double, double) SensorWidthRange(double depth)
        {
            double sensorWidth = MaxSensorWidth(depth);

            return (-sensorWidth / 2, sensorWidth / 2);
        }

        /// <summary>
        /// Generates a random 2d point in the skeleton recognizing range of the sensor
        /// </summary>
        /// <returns>Point: Random 2D point in range of visible camera params (X, Z, both metres)</returns>
        private Point Random2DPointInCameraSpace()
        {
            Random rand = new Random();

            double depthMinMaxDifference = MaxSkeletonDepth - MinSkeletonDepth;
            double Z = (rand.NextDouble() * depthMinMaxDifference) + MinSkeletonDepth;
            (double, double) widthRange = SensorWidthRange(Z);


            double X = (rand.NextDouble() * widthRange.Item2 * 2) + widthRange.Item1;
            // X (Horizontal), Y (Vertical), Z (Depth) Point => X, Z Point
            // The 3-D Plane becomes a 2-D plane Horizontally (X) and Deep (Z)
            return new Point(X, Z);
        }

        
    }
}
