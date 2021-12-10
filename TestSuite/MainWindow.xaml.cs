using System;
using System.Collections.Generic;
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
    public partial class MainWindow : Window
    {

        // Represents the Kinect Device, used to gather data on Persons in View
        private KinectSensor kinectSensor;
        private MultiSourceFrameReader multiSourceFrameReader;

        private FrameSourceTypes enabledFramesSources =
            FrameSourceTypes.Body
            | FrameSourceTypes.Color
            | FrameSourceTypes.Depth
            | FrameSourceTypes.Infrared
            | FrameSourceTypes.BodyIndex;

        private IDictionary<int, List<Ellipse>> bodyHands;

        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();

            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(enabledFramesSources);
            multiSourceFrameReader.MultiSourceFrameArrived += MainWindow_MultiSourceFrameArrived;

            Loaded += MainWindow_Loaded;

            bodyHands = new Dictionary<int, List<Ellipse>>();
            

            kinectSensor.Open();

            InitializeComponent();
        }

        #region WindowLifecycleEvents
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; i++)
            {
                Ellipse leftHand = new Ellipse()
                {
                    Fill = Brushes.Yellow,
                    Height = 30,
                    Width = 30,
                    Visibility = Visibility.Collapsed
                };
                Ellipse rightHand = new Ellipse()
                {
                    Fill = Brushes.Yellow,
                    Height = 30,
                    Width = 30,
                    Visibility = Visibility.Collapsed
                };

                List<Ellipse> hands = new List<Ellipse>();
                hands.Add(leftHand);
                hands.Add(rightHand);

                bodyHands.Add(i, hands);
                SkeletonGrid.Children.Add(leftHand);
                SkeletonGrid.Children.Add(rightHand);
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

                for (int i = 0; i < bodies.Length; i++)
                {
                    Body body = bodies[i];
                    Joint leftHand = body.Joints[JointType.HandLeft];
                    Joint rightHand = body.Joints[JointType.HandRight];

                    if (leftHand.Position.Z < 0)
                    {
                        leftHand.Position.Z = .1f;
                    }

                    if (rightHand.Position.Z < 0)
                    {
                        rightHand.Position.Z = .1f;
                    }

                    Ellipse leftHandEllipse = bodyHands[i][0];
                    Ellipse rightHandEllipse = bodyHands[i][1];

                    CameraSpacePoint[] handCameraPoints = { leftHand.Position, rightHand.Position };
                    DepthSpacePoint[] handDepthPoints = new DepthSpacePoint[2];

                    kinectSensor.CoordinateMapper.MapCameraPointsToDepthSpace(handCameraPoints, handDepthPoints);

                    leftHandEllipse.Visibility = leftHand.TrackingState == TrackingState.NotTracked ?
                                                        Visibility.Collapsed : Visibility.Visible;

                    rightHandEllipse.Visibility = rightHand.TrackingState == TrackingState.NotTracked ?
                                                        Visibility.Collapsed : Visibility.Visible;

                    if (leftHand.TrackingState != TrackingState.NotTracked)
                    {
                        Point leftHandPoint = new Point(handDepthPoints[0].X, handDepthPoints[0].Y);
                        Canvas.SetLeft(leftHandEllipse, leftHandPoint.X - leftHandEllipse.Width / 2);
                        Canvas.SetTop(leftHandEllipse, leftHandPoint.Y - leftHandEllipse.Height / 2);


                    }
                    if (rightHand.TrackingState != TrackingState.NotTracked)
                    {
                        Point rightHandPoint = new Point(handDepthPoints[1].X, handDepthPoints[1].Y);
                        Canvas.SetLeft(rightHandEllipse, rightHandPoint.X - rightHandEllipse.Width / 2);
                        Canvas.SetTop(rightHandEllipse, rightHandPoint.Y - rightHandEllipse.Height / 2);
                    }
                }
            }
        }
        #endregion
    }
}
