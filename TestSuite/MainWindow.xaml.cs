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
using SharpDX.XInput;

namespace TestSuite
{
    /// <summary>
    /// The Type of Representation for the User
    /// </summary>
    public enum RepresentationType
    {
        Skeleton,
        MirrorImage,
        None
    }

    public enum ExperimentState
    {
        WaitingToBegin,
        InitialControllerLink,
        RedoControllerLink,
        WaitingToStartCondition,
        ConditionInProgress,
        ExperimentComplete
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IUserControllerDelegate
    {
        // Constants related to Skeleton Tracking
        // Will also be used in calculating sensible random points
        private const double SensorFOV = 70.6 * Math.PI / 180;
        private const double MinSkeletonDepth = 1.75;
        private const double MaxSkeletonDepth = 4;

        private const RepresentationType DEFAULT_REPRESENTATION = RepresentationType.None;

        // Mapping from Condition ID to Representation/Guiding Method Pair
        private IDictionary<int, Tuple<RepresentationType, GuidingMethod>> idToConditionMap = new Dictionary<int, Tuple<RepresentationType, GuidingMethod>>()
        {
            {0, new Tuple<RepresentationType, GuidingMethod>(RepresentationType.Skeleton, GuidingMethod.Framing) },
            {1, new Tuple<RepresentationType, GuidingMethod>(RepresentationType.Skeleton, GuidingMethod.Ellipse) },
            {2, new Tuple<RepresentationType, GuidingMethod>(RepresentationType.MirrorImage, GuidingMethod.Framing) },
            {3, new Tuple<RepresentationType, GuidingMethod>(RepresentationType.MirrorImage, GuidingMethod.TextBox) },
            {4, new Tuple<RepresentationType, GuidingMethod>(RepresentationType.Skeleton, GuidingMethod.TextBox) },
            {5, new Tuple<RepresentationType, GuidingMethod>(RepresentationType.Skeleton, GuidingMethod.VisualEffect) },
            {6, new Tuple<RepresentationType, GuidingMethod>(RepresentationType.MirrorImage, GuidingMethod.Ellipse) },
            {7, new Tuple<RepresentationType, GuidingMethod>(RepresentationType.MirrorImage, GuidingMethod.VisualEffect) },
            {8, new Tuple<RepresentationType, GuidingMethod>(RepresentationType.MirrorImage, GuidingMethod.Arrows) },
            {9, new Tuple<RepresentationType, GuidingMethod>(RepresentationType.Skeleton, GuidingMethod.Arrows) },
        };

        // Mapping an experiment to the ordering of conditions to show
        private IDictionary<int, int[]> experimentIDToConditionsMap = new Dictionary<int, int[]>()
        {
            {0, new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9} },
            {1, new int[] {1, 3, 0, 5, 2, 7, 4, 9, 6, 8} },
            {2, new int[] {3, 5, 1, 7, 0, 9, 2, 8, 4, 6} },
            {3, new int[] {5, 7, 3, 9, 1, 8, 0, 6, 2, 4} },
            {4, new int[] {7, 9, 5, 8, 3, 6, 1, 4, 0, 2} },
            {5, new int[] {9, 8, 7, 6, 5, 4, 3, 2, 1, 0} },
            {6, new int[] {8, 6, 9, 4, 7, 2, 5, 0, 3, 1} },
            {7, new int[] {6, 4, 8, 2, 9, 0, 7, 1, 5, 3} },
            {8, new int[] {4, 2, 6, 0, 8, 1, 9, 3, 7, 5} },
            {9, new int[] {2, 0, 4, 1, 6, 3, 8, 5, 9, 7} },
        };

        private int currentExperimentID = 1;
        private int nextParticipantID = 1;
        private int currentParticipantLinked = 0;
        private int userCountForExperiment = 0;
        private int currentConditionOffset = 0;
        private ExperimentState currentExperimentState = ExperimentState.WaitingToBegin;
        private List<UserIndex> linkedControllers;

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
        // Controls the rendering of the mirror image representation
        private MirrorImageRenderer mirrorImageRenderer;
        private List<int> bodyIndexesToShow;
        // Controls rendering the different guiding techniques 
        private GuidingMethodRenderer guidingMethodRenderer;

        private RepresentationType currentUserRepresentation = DEFAULT_REPRESENTATION;
        private GuidingMethod currentGuidingMethod = GuidingMethod.None;

        private Random rand = new Random();

        private UserController[] controllers;

        private Body[] bodies = new Body[0];
        private Body[] allBodies
        {
            get => bodies;
            set
            {
                if (!bodies.Equals(value))
                {
                    bodies = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TrackedUserCount"));
                }
            }
        }
        private int[] currentTrackedBodyIDs;
        private Body[] trackedBodies
        {
            get
            {
                if (allBodies.Where(body => body == null).Any()) return new Body[0];

                return allBodies.Where(body => body.IsTracked).ToArray();
            }
        }

        private const string EXP_START_MESSAGE = "Waiting to Start...";
        private const string EXP_END_MESSAGE = "All Tasks Complete\nThank You";
        private const string EXP_COND_WAIT_MESSAGE = "Waiting for Next Task...";
        private string userLabelMessage = EXP_START_MESSAGE;

        // Debugging
        public bool DebugMode
        {
            get => debugMode;
            set
            {
                if (debugMode != value)
                {
                    debugMode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DebugMode"));
                }
            }
        }
        private bool debugMode;
        private Point[] randomPoints;
        private Point randomPoint;
        private Point bodyPoint;
        private double bodyPointY;
        private UserIndex controllerIndex = UserIndex.Any;
        private double controllerTime;
        private double bodyFinalDistance;
        private double tiltAngle;

        public ExperimentState CurrentExperimentState
        {
            get => currentExperimentState;
            set
            {
                if (currentExperimentState != value)
                {
                    currentExperimentState = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentExperimentState"));
                }
            }
        }
        public int TrackedUserCount
        {
            get => trackedBodies.Length;
        }
        public string UserLabelMessage
        {
            get => userLabelMessage;
            set
            {
                if (userLabelMessage != value)
                {
                    userLabelMessage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UserLabelMessage"));
                }
            }
        }
        public double TiltAngle
        {
            get => Math.Round(tiltAngle, 2);
            set
            {
                if (tiltAngle != value)
                {
                    tiltAngle = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TiltAngle"));
                }
            }
        }

        public double RotateAngle
        {
            get 
            {
                if (guidingMethodRenderer == null) return 0;
                int bodyIndex = guidingMethodRenderer.PositionOfFirstTrackedBody().Item1;
                return bodyIndex != -1 ? guidingMethodRenderer.rotateAngles[bodyIndex] : 0;
            }
        }
        public double BodyFinalDistance
        {
            get => bodyFinalDistance;
            set
            {
                if (bodyFinalDistance != value)
                {
                    bodyFinalDistance = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BodyFinalDistance"));
                }
            }
        }
        public UserIndex ControllerIndex
        {
            get => controllerIndex;
            set
            {
                if (controllerIndex != value)
                {
                    controllerIndex = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ControllerIndex"));
                }
            }
        }

        public double ControllerTime
        {
            get => controllerTime;
            set
            {
                if (controllerTime != value)
                {
                    controllerTime = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ControllerTime"));
                }
            }
        }

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
                    guidingMethodRenderer.currentRepresentationType = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentUserRepresentation"));

                    switch (value)
                    {
                        case RepresentationType.Skeleton:
                            mirrorImageRenderer.ClearAllMirrorImages();
                            break;
                        case RepresentationType.MirrorImage:
                            skeletonRenderer.ClearAllSkeletons();
                            break;
                        case RepresentationType.None:
                            mirrorImageRenderer.ClearAllMirrorImages();
                            skeletonRenderer.ClearAllSkeletons();
                            break;
                    }
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
            get
            {
                if (guidingMethodRenderer == null) return new Point(0, 0);
                int bodyIndex = guidingMethodRenderer.PositionOfFirstTrackedBody().Item1;
                return bodyIndex == -1 ? new Point(0, 0) : randomPoints[bodyIndex];
            }
            set
            {
                if (!randomPoint.Equals(value))
                {
                    randomPoint = value;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RandomPoint"));
                }
            }
        }

        public double BodyPointY
        {
            get => bodyPointY;
            set
            {
                if (value != bodyPointY)
                {
                    bodyPointY = value;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BodyPointY"));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RandomPoint"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RotateAngle"));
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
            int bodyCount = kinectSensor.BodyFrameSource.BodyCount;

            allBodies = new Body[bodyCount];
            currentTrackedBodyIDs = new int[bodyCount];
            int[] bodyRange = Enumerable.Range(0, bodyCount).ToArray();
            randomPoints = bodyRange.Select(i => Random2DPointInCameraSpace()).ToArray();

            UserIndex[] controllerRange = { UserIndex.One, UserIndex.Two, UserIndex.Three, UserIndex.Four};
            controllers = controllerRange.Select(i => new UserController(i, this)).ToArray();
        }

        #region WindowEventHandlers
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            skeletonRenderer = new SkeletonRenderer(kinectSensor, SkeletonGrid);
            mirrorImageRenderer = new MirrorImageRenderer(MirrorImage, kinectSensor);
            guidingMethodRenderer = new GuidingMethodRenderer(kinectSensor, SkeletonGrid, EllipseGrid);
            skeletonRenderer.guiding = guidingMethodRenderer;

            KeyDown += MainWindow_KeyDown;
        }

        // Handles Key Presses to control the state of the experiment
        // Can handle changing the representation and guiding method
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
                case Key.D6:
                    CurrentGuidingMethod = GuidingMethod.None;
                    break;

                // Manually Switch User Representation
                case Key.D8:
                    CurrentUserRepresentation = RepresentationType.Skeleton;
                    break;
                case Key.D9:
                    CurrentUserRepresentation = RepresentationType.MirrorImage;
                    break;
                case Key.D0:
                    CurrentUserRepresentation = RepresentationType.None;
                    break;

                // Debugging Adjustments
                // Rotate Height Offset
                case Key.H:
                    string newHeightOffsetStr = (string)PromptDialog.Dialog.Prompt("Enter Height Offset: ", "New Height Offset");
                    float newHeightOffset;
                    if (float.TryParse(newHeightOffsetStr, out newHeightOffset)) guidingMethodRenderer.CameraHeightOffset = newHeightOffset;
                    break;

                // Enable/Disable Debug
                case Key.D:
                    DebugMode = !DebugMode;
                    break;

                // Experiment Control
                // Begin Experiment
                case Key.Space:
                    if (CurrentExperimentState == ExperimentState.WaitingToBegin && trackedBodies.Length > 0)
                    {
                        BeginExperiment();
                    }
                    break;
                // Advance To Next Condition
                case Key.A:
                    if (CurrentExperimentState != ExperimentState.WaitingToBegin && CurrentExperimentState != ExperimentState.InitialControllerLink && CurrentExperimentState != ExperimentState.RedoControllerLink)
                    {
                        AdvanceState();
                    }
                    break;

                // Exit Application
                case Key.Q:
                    Application.Current.Shutdown();
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
                if (bodyFrame == null) return;

                Vector4 floorPlane = bodyFrame.FloorClipPlane;
                guidingMethodRenderer.cameraFloorPlane = floorPlane;
                guidingMethodRenderer.CameraTiltAngle = Math.Atan2(floorPlane.Z, floorPlane.Y);
                TiltAngle = Math.Atan2(floorPlane.Z, floorPlane.Y) * 180 / Math.PI;

                Body[] tempBodies = new Body[bodyFrame.BodyCount];
                bodyFrame.GetAndRefreshBodyData(tempBodies);

                allBodies = tempBodies;
            }

            if (CurrentExperimentState != ExperimentState.WaitingToBegin && CurrentExperimentState != ExperimentState.WaitingToStartCondition && CurrentExperimentState != ExperimentState.ExperimentComplete)
            switch (CurrentUserRepresentation)
            {
                case RepresentationType.Skeleton:
                    // Renders the Skeleton Representation 
                    RenderSkeleton();
                    break;
                case RepresentationType.MirrorImage:
                    // Renders the Mirror Image Representation
                    RenderMirrorImage(multiSourceFrame, bodyIndexesToShow);
                    break;
                case RepresentationType.None:
                    break;
            }

            // Render the Guiding Method currently selected
            RenderGuidingMethod(allBodies);
        }
        #endregion

        #region SkeletonRendering
        /// <summary>
        /// Renders the users as a Skeleton Representation
        /// </summary>
        /// <param name="bodies">An array of Body objects, populated from GetAndRefreshBodyData()</param>
        private void RenderSkeleton()
        {

            if (trackedBodies.Any())
            {
                Body firstBody = trackedBodies.First();
                CameraSpacePoint bodyCameraPoint = firstBody.Joints[JointType.SpineBase].Position;
                bodyCameraPoint = guidingMethodRenderer.RotateCameraPointForTilt(bodyCameraPoint);
                BodyPoint = new Point(bodyCameraPoint.X, bodyCameraPoint.Z);
                BodyPointY = bodyCameraPoint.Y;
            }

            skeletonRenderer.UpdateAllSkeletons(allBodies, bodyIndexesToShow);
        }
        #endregion

        #region MirrorImageRendering
        /// <summary>
        /// Renders the users as a Mirror Image Representation
        /// </summary>
        /// <param name="multiSourceFrame">The Multi Source Frame containing the frame data for color, infrared and body index</param>
        private void RenderMirrorImage(MultiSourceFrame multiSourceFrame, List<int> bodyIndexesToShow)
        {
            mirrorImageRenderer.UpdateAllMirrorImages(multiSourceFrame, bodyIndexesToShow);
        }
        #endregion

        #region IUserControllerDelegate
        /// <summary>
        /// Delegate Method to handle a button press for the user/controller
        /// </summary>
        /// <param name="controllerIndex">The index of the controller that the user interacted with</param>
        void IUserControllerDelegate.LetterButtonPressed(UserIndex controllerIndex)
        {
            Dispatcher.Invoke(() =>
            {
                if ((CurrentExperimentState == ExperimentState.InitialControllerLink || CurrentExperimentState == ExperimentState.RedoControllerLink) && !linkedControllers.Contains(controllerIndex))
                {
                    linkedControllers.Add(controllerIndex);
                    AdvanceState();
                }
                else if (CurrentExperimentState == ExperimentState.ConditionInProgress)
                {
                    controllers[(int)controllerIndex].StopTiming();
                }
            });
        }

        /// <summary>
        /// Delegate Method to provide updated elapsed time for a specified controller index
        /// </summary>
        /// <param name="controllerIndex">The index of the controller</param>
        /// <param name="elapsedTime">The current time elapsed</param>
        void IUserControllerDelegate.UpdateTimeElapsed(UserIndex controllerIndex, long elapsedTime)
        {
            ControllerIndex = controllerIndex;
            ControllerTime = Math.Round(elapsedTime / 1000.0, 2);
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

        #region TimerMethods
        /// <summary>
        /// For every controller, begin their timer
        /// </summary>
        private void StartTimers()
        {
            for (int i = 0; i < userCountForExperiment; i++)
            {
                controllers[i].StartTiming();
            }
        }

        private void StopTimers()
        {
            for (int i = 0; i < userCountForExperiment; i++)
            {
                controllers[i].StopTiming();
            }
        }
        #endregion

        #region ExperimentStateMethods
        private void AdvanceState()
        {
            switch (CurrentExperimentState)
            {
                case ExperimentState.WaitingToBegin:
                    CurrentExperimentState = ExperimentState.InitialControllerLink;
                    UserLabelMessage = "";
                    ShowNextUserForLink();
                    break;
                case ExperimentState.WaitingToStartCondition:
                    StartNextCondition();
                    UserLabelMessage = "";
                    break;
                case ExperimentState.ConditionInProgress:
                    ResetForNextCondition();

                    if (currentConditionOffset == experimentIDToConditionsMap[currentExperimentID].Length)
                    {
                        EndExperiment();
                    } else
                    {
                        UserLabelMessage = EXP_COND_WAIT_MESSAGE;
                    }
                    break;
                case ExperimentState.InitialControllerLink:
                    if (currentParticipantLinked == userCountForExperiment)
                    {
                        UserLabelMessage = EXP_COND_WAIT_MESSAGE;
                        ResetForNextCondition();
                    }
                    else
                    {
                        ShowNextUserForLink();
                    }
                    break;
                case ExperimentState.RedoControllerLink:
                    if (currentParticipantLinked == userCountForExperiment)
                    {
                        UserLabelMessage = EXP_COND_WAIT_MESSAGE;
                        ResetForNextCondition();
                        if (currentConditionOffset == experimentIDToConditionsMap[currentExperimentID].Length)
                        {
                            EndExperiment();
                        }
                    } 
                    else
                    {
                        ShowNextUserForLink();
                    }
                    break;
                case ExperimentState.ExperimentComplete:
                    UserLabelMessage = EXP_START_MESSAGE;
                    CurrentExperimentState = ExperimentState.WaitingToBegin;
                    break;
            }
        }

        private void BeginExperiment()
        {
            userCountForExperiment = trackedBodies.Length;
            List<Body> allBodiesList = allBodies.ToList();
            currentTrackedBodyIDs = trackedBodies.Select(body => allBodiesList.IndexOf(body)).ToArray();
            linkedControllers = new List<UserIndex>(userCountForExperiment);
            AdvanceState();
        }

        private void EndExperiment()
        {
            currentExperimentID++;
            currentConditionOffset = 0;
            currentParticipantLinked = 0;
            UserLabelMessage = EXP_END_MESSAGE;
            CurrentExperimentState = ExperimentState.ExperimentComplete;
        }

        private void StartNextCondition()
        {
            int nextConditionID = experimentIDToConditionsMap[currentExperimentID][currentConditionOffset++];
            Tuple<RepresentationType, GuidingMethod> conditionVariables = idToConditionMap[nextConditionID];

            CurrentUserRepresentation = conditionVariables.Item1;
            CurrentGuidingMethod = conditionVariables.Item2;

            CurrentExperimentState = ExperimentState.ConditionInProgress;
            StartTimers();
        }

        private void ResetForNextCondition()
        {
            StopTimers();
            CurrentUserRepresentation = RepresentationType.None;
            CurrentGuidingMethod = GuidingMethod.None;
            CurrentExperimentState = ExperimentState.WaitingToStartCondition;
        }

        private void ShowNextUserForLink()
        {
            CurrentUserRepresentation = RepresentationType.MirrorImage;
            bodyIndexesToShow = new List<int> { currentTrackedBodyIDs[currentParticipantLinked++] };
        }
        #endregion

        #region KinectHelperMethods
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
            double sensorWidth = 0.9 * MaxSensorWidth(depth);

            return (-sensorWidth / 2, sensorWidth / 2);
        }

        /// <summary>
        /// Generates a random 2d point in the skeleton recognizing range of the sensor
        /// </summary>
        /// <returns>Point: Random 2D point in range of visible camera params (X, Z, both metres)</returns>
        private Point Random2DPointInCameraSpace()
        {
            double depthMinMaxDifference = MaxSkeletonDepth - MinSkeletonDepth;
            double Z = (rand.NextDouble() * depthMinMaxDifference) + MinSkeletonDepth;
            (double, double) widthRange = SensorWidthRange(Z);

            double X = (rand.NextDouble() * widthRange.Item2 * 2) + widthRange.Item1;
            // X (Horizontal), Y (Vertical), Z (Depth) Point => X, Z Point
            // The 3-D Plane becomes a 2-D plane Horizontally (X) and Deep (Z)
            return new Point(X, Z);
        }
        #endregion

    }
}
