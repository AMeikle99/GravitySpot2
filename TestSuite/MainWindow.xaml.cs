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
        // Represents the Kinect Device, used to gather data on Persons in View
        private KinectSensor kinectSensor;
        private MultiSourceFrameReader multiSourceFrameReader;

        private FrameSourceTypes enabledFramesSources =
            FrameSourceTypes.Body
            | FrameSourceTypes.Color
            | FrameSourceTypes.Depth
            | FrameSourceTypes.Infrared
            | FrameSourceTypes.BodyIndex;

        private SkeletonRenderer skeletonRenderer;

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
            skeletonRenderer = new SkeletonRenderer(kinectSensor, SkeletonGrid);
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
        private void RenderSkeleton(BodyFrame bodyFrame)
        {
            if (bodyFrame == null)
            {
                return;
            }

            Body[] bodies = new Body[kinectSensor.BodyFrameSource.BodyCount];

            bodyFrame.GetAndRefreshBodyData(bodies);

            skeletonRenderer.UpdateAllSkeletons(bodies);
        }
        #endregion
    }
}
