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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public string SpineBasePositionLabel
            => SpineBasePosition == null
                    ? "X: ? Y: ? Z: ?"
                    : string.Format("X: {0} Y: {1} Z: {2}",
                                    SpineBasePosition.X, SpineBasePosition.Y, SpineBasePosition.Z);
        public int ActiveSkeletonIndex
        {
            get => activeSkeletonIndex;
            set
            {
                if (activeSkeletonIndex != value)
                {
                    activeSkeletonIndex = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ActiveSkeletonIndex"));
                }
            }
        }

        private int activeSkeletonIndex = -1;

        private CameraSpacePoint spineBasePosition;
        private CameraSpacePoint SpineBasePosition
        {
            get => spineBasePosition;

            set
            {
                if (!spineBasePosition.Equals(value))
                {
                    spineBasePosition = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpineBasePositionLabel"));
                }
            }
        }

        // Represents the Kinect Device, used to gather data on Persons in View
        private KinectSensor kinectSensor;
        private MultiSourceFrameReader multiSourceFrameReader;

        private FrameSourceTypes enabledFramesSources =
            FrameSourceTypes.Body
            | FrameSourceTypes.Color
            | FrameSourceTypes.Depth
            | FrameSourceTypes.Infrared
            | FrameSourceTypes.BodyIndex;

        private IDictionary<int, IDictionary<JointType, Ellipse>> bodyJoints;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();

            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(enabledFramesSources);
            multiSourceFrameReader.MultiSourceFrameArrived += MainWindow_MultiSourceFrameArrived;

            Loaded += MainWindow_Loaded;

            bodyJoints = new Dictionary<int, IDictionary<JointType, Ellipse>>();

            kinectSensor.Open();

            InitializeComponent();

            DataContext = this;
        }

        #region WindowLifecycleEvents
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetupSkeletonJoints();
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
                RenderSkeletonJoints(bodyFrame);
            }
        }
        #endregion

        #region SkeletonRendering

        private void SetupSkeletonJoints()
        {
            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; i++)
            {
                Dictionary<JointType, Ellipse> jointEllipseDict = new Dictionary<JointType, Ellipse>();

                foreach (JointType joint in Enum.GetValues(typeof(JointType)))
                {
                    Ellipse jointEllipse = new Ellipse()
                    {
                        Fill = Brushes.White,
                        Height = 10,
                        Width = 10,
                        Visibility = Visibility.Collapsed
                    };

                    if (joint == JointType.HandLeft || joint == JointType.HandRight)
                    {
                        jointEllipse.Fill = Brushes.Yellow;
                        jointEllipse.Height = 20;
                        jointEllipse.Width = 20;
                    }

                    jointEllipseDict.Add(joint, jointEllipse);
                    SkeletonGrid.Children.Add(jointEllipse);
                }

                bodyJoints.Add(i, jointEllipseDict);
            }
        }

        private void RenderSkeletonJoints(BodyFrame bodyFrame)
        {
            if (bodyFrame == null)
            {
                return;
            }

            Body[] bodies = new Body[kinectSensor.BodyFrameSource.BodyCount];

            bodyFrame.GetAndRefreshBodyData(bodies);

            for (int i = 0; i < bodies.Length; i++)
            {
                Body body = bodies[i];
                IDictionary<JointType, Ellipse> currentBodyJoints = bodyJoints[i];

                foreach (var jointPair in body.Joints)
                {
                    JointType jointType = jointPair.Key;
                    Joint joint = jointPair.Value;

                    Ellipse jointEllipse = currentBodyJoints[jointType];

                    joint.Position.Z = joint.Position.Z < 0 ? .1f : joint.Position.Z;

                    DepthSpacePoint jointDepthPoint = kinectSensor.CoordinateMapper
                                                        .MapCameraPointToDepthSpace(joint.Position);

                    jointEllipse.Visibility = joint.TrackingState == TrackingState.NotTracked ?
                                                Visibility.Collapsed : Visibility.Visible;

                    if (joint.TrackingState != TrackingState.NotTracked)
                    {
                        Point jointPoint = new Point(jointDepthPoint.X, jointDepthPoint.Y);
                        Canvas.SetLeft(jointEllipse, jointPoint.X - jointEllipse.Width / 2);
                        Canvas.SetTop(jointEllipse, jointPoint.Y - jointEllipse.Height / 2);
                    }

                    if (body.IsTracked)
                    {
                        ActiveSkeletonIndex = i;

                        if (joint.JointType == JointType.SpineBase)
                        {
                            SpineBasePosition = joint.Position;
                        }
                    }
                    
                }
            }

        }

        #endregion
    }
}
