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

        private List<(JointType, JointType)> JointBonePairs = new List<(JointType, JointType)>
        {
            (JointType.Head, JointType.Neck),
            (JointType.Neck, JointType.SpineShoulder),
            (JointType.SpineShoulder, JointType.ShoulderLeft),
            (JointType.SpineShoulder, JointType.ShoulderRight),
            
            (JointType.ShoulderLeft, JointType.ElbowLeft),
            (JointType.ElbowLeft, JointType.WristLeft),
            (JointType.WristLeft, JointType.HandLeft),
            (JointType.WristLeft, JointType.ThumbLeft),
            (JointType.HandLeft, JointType.HandTipLeft),

            (JointType.ShoulderRight, JointType.ElbowRight),
            (JointType.ElbowRight, JointType.WristRight),
            (JointType.WristRight, JointType.HandRight),
            (JointType.WristRight, JointType.ThumbRight),
            (JointType.HandRight, JointType.HandTipRight),

            (JointType.SpineShoulder, JointType.SpineMid),
            (JointType.SpineMid, JointType.SpineBase),
            (JointType.SpineBase, JointType.HipLeft),
            (JointType.SpineBase, JointType.HipRight),

            (JointType.HipLeft, JointType.KneeLeft),
            (JointType.KneeLeft, JointType.AnkleLeft),
            (JointType.AnkleLeft, JointType.FootLeft),

            (JointType.HipRight, JointType.KneeRight),
            (JointType.KneeRight, JointType.AnkleRight),
            (JointType.AnkleRight, JointType.FootRight)
        };


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
        private IDictionary<int, IDictionary<(JointType, JointType), Line>> bodyBones;

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
        }

        #region WindowLifecycleEvents
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetupSkeletonJoints();
            SetupSkeletonBones();
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
                RenderSkeleton(bodyFrame);
            }
        }
        #endregion

        #region SkeletonRendering

        private void SetupSkeletonJoints()
        {
            bodyJoints = new Dictionary<int, IDictionary<JointType, Ellipse>>();

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

                    jointEllipseDict.Add(joint, jointEllipse);
                    SkeletonGrid.Children.Add(jointEllipse);
                }

                bodyJoints.Add(i, jointEllipseDict);
            }
        }
        private void SetupSkeletonBones()
        {
            bodyBones = new Dictionary<int, IDictionary<(JointType, JointType), Line>>();

            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; i++)
            {
                IDictionary<(JointType, JointType), Line> jointBoneLines = new Dictionary<(JointType, JointType), Line>();
                foreach(var jointPair in JointBonePairs)
                {
                    Line boneLine = new Line()
                    {
                        Stroke = Brushes.White,
                        StrokeThickness = 4,
                        Visibility = Visibility.Collapsed
                    };

                    jointBoneLines.Add(jointPair, boneLine);
                    SkeletonGrid.Children.Add(boneLine);
                }
                bodyBones.Add(i, jointBoneLines);
            }
        }

        private void RenderSkeleton(BodyFrame bodyFrame)
        {
            if (bodyFrame == null)
            {
                return;
            }

            Body[] bodies = new Body[kinectSensor.BodyFrameSource.BodyCount];

            bodyFrame.GetAndRefreshBodyData(bodies);

            RenderSkeletonJoints(bodies);
            RenderSkeletonBones(bodies);

        }
        private void RenderSkeletonJoints(Body[] bodies)
        {
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

        private void RenderSkeletonBones(Body[] bodies)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                Body body = bodies[i];
                IDictionary<(JointType, JointType), Line> currentBodyBones = bodyBones[i];

                foreach (var jointPair in JointBonePairs)
                {
                    Line boneLine = currentBodyBones[jointPair];

                    Joint start = body.Joints[jointPair.Item1];
                    Joint end = body.Joints[jointPair.Item2];

                    if (start.TrackingState == TrackingState.NotTracked || end.TrackingState == TrackingState.NotTracked)
                    {
                        boneLine.Visibility = Visibility.Collapsed;
                        continue;
                    }

                    boneLine.Visibility = Visibility.Visible;

                    start.Position.Z = start.Position.Z < 0 ? .1f : start.Position.Z;
                    end.Position.Z = end.Position.Z < 0 ? .1f : end.Position.Z;

                    DepthSpacePoint startPosition = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(start.Position);
                    DepthSpacePoint endPosition = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(end.Position);

                    boneLine.X1 = startPosition.X;
                    boneLine.X2 = endPosition.X;
                    boneLine.Y1 = startPosition.Y;
                    boneLine.Y2 = endPosition.Y;
                }
            }
        }

        #endregion
    }
}
