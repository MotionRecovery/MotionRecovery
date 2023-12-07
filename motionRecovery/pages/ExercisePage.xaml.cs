using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;
using static System.Net.Mime.MediaTypeNames;

namespace motionRecovery
{
    /// <summary>
    /// Receives an exercise as input, and asks the user to perform it via Kinect motion recognition.
    /// The user will have to do this series of positions, and ExercisePage will analyze if the user does this series of positions.
    /// </summary>
    public partial class ExercisePage : Page, INotifyPropertyChanged
    {
        // Used to print a message in the frontend
        private string userPositionStatus = null;
        private string exerciseNumber = null;
        private string exerciseDescription = null;
        private string statusText = null;

        // Kinect variables
        private KinectSensor kinectSensor = null; // Used to interact with the Kinect sensor throughout the application

        // Drawing variables
        private SkeletonGraphicInterface skeletonGraphicInterface; // Used to draw the skeleton
        private DrawingGroup drawingGroup; // Drawing group for body rendering output
        private DrawingImage imageSource; // Drawing image that we will display
        private CoordinateMapper coordinateMapper = null; // Coordinate mapper to map one type of point to another, such as mapping depth points to color points or skeletal joint positions to depth points.

        // Constants
        private const float InferredZPositionClamp = 0.1f; // Constant for clamping Z values of camera space points from being negative

        // Body tracking variables
        private BodyFrameReader bodyFrameReader = null; // Reader for body frames
        private Body[] bodies = null; // Array for the bodies

        // Display properties
        private int displayWidth; // Width of display (depth space)
        private int displayHeight; // Height of display (depth space)
        private List<Pen> bodyColors; // List of colors for each body tracked

        // Exercise tracking variables
        private ExerciseMultiPosition exerciseMultiPosition = new ExerciseMultiPosition(); // Keeps track of exercise positions
        private int IndexPosition = 0; // Index to identify the current exercise position

        // Timer variables
        private System.Timers.Timer ruleTimer = new System.Timers.Timer(); // Timer for controlling exercise rules
        private DateTime ruleTimerStartTime; // Start time for the rule timer



        // Constructor for ExercisePage that takes an ExerciseMultiPosition parameter
        public ExercisePage(ExerciseMultiPosition exerciseMultiPosition)
        {
            this.exerciseMultiPosition = exerciseMultiPosition;

            this.kinectSensor = KinectSensor.GetDefault(); // get the default Kinect sensor
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged; // Attach an event handler for changes in sensor availability
            this.kinectSensor.Open();
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText // Set the initial status text based on sensor availability
                                                            : Properties.Resources.NoSensorStatusText;

            // Initialize the SkeletonGraphicInterface for drawing skeletons
            this.skeletonGraphicInterface = new SkeletonGraphicInterface();

            // Skeleton Camera
            this.coordinateMapper = this.kinectSensor.CoordinateMapper; // Retrieve the coordinate mapper
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription; // Retrieve the depth frame source

            // Retrieve the size of the joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // Open the reader for body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // Populate body colors, one for each BodyIndex. Each color corresponds to a different tracked body
            this.bodyColors = new List<Pen>
            {
                new Pen(Brushes.Red, 6),
                new Pen(Brushes.Orange, 6),
                new Pen(Brushes.Green, 6),
                new Pen(Brushes.Blue, 6),
                new Pen(Brushes.Indigo, 6),
                new Pen(Brushes.Violet, 6)
            };

            this.drawingGroup = new DrawingGroup(); // Create the drawing group used for rendering
            this.imageSource = new DrawingImage(this.drawingGroup); // Create an image source for use in the image control

            this.DataContext = this; // Set the window object as the view model
            this.InitializeComponent(); // Initialize the window components (controls)
        }

        // INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        public event PropertyChangedEventHandler PropertyChanged;

        // Gets the bitmap to display
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ExercisePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
            ExerciseNumber = $"Exercise {IndexPosition + 1}/{exerciseMultiPosition.Rules.Count}";
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ExercisePage_Unloaded(object sender, RoutedEventArgs e)
        {

            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;

                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.skeletonGraphicInterface.DrawClippedEdges(body, dc, this.displayWidth, this.displayHeight);
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.skeletonGraphicInterface.DrawBody(joints, jointPoints, drawPen, dc);
                            this.skeletonGraphicInterface.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.skeletonGraphicInterface.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);

                            foreach (SimplePosition Positions in exerciseMultiPosition.Rules[IndexPosition].Positions)
                            {
                                this.skeletonGraphicInterface.SelectJointGraphical(Positions.Joint1, jointPoints, dc);
                                this.skeletonGraphicInterface.SelectJointGraphical(Positions.Joint2, jointPoints, dc);
                            }


                                // CHECK RULES
                                if (body != null && exerciseMultiPosition.Rules.Count != 0)
                            {
                                // If there are one position in the rule or not
                                if (exerciseMultiPosition.Rules[IndexPosition].Positions.Count > 1)
                                {
                                    checkMultiplePosition(body);
                                } else
                                {
                                    CheckOnePosition(body);
                                }
                            }
                                    


                        }
                    }

                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private void checkMultiplePosition(Body body)
        {
            bool CheckPos = true; // Used to check if all the positions are respected

            foreach (SimplePosition Positions in exerciseMultiPosition.Rules[IndexPosition].Positions)
            {
                Joint Joint1 = body.Joints[Positions.Joint1];
                Joint Joint2 = body.Joints[Positions.Joint2];
                Double AngleMin = Positions.AngleMin;
                Double AngleMax = Positions.AngleMax;
                Double PositionTime = exerciseMultiPosition.Rules[IndexPosition].PositionTime;
                String Description = exerciseMultiPosition.Rules[IndexPosition].Description;

                if (CheckPosition(Joint1, Joint2, AngleMin, AngleMax, Description, PositionTime) == false)
                {
                    CheckPos = false;
                }

                if (CheckPos)
                {
                    // check if ruleTime exist
                    if (ruleTimer == null)
                    {
                        // If rulerTimer doesn't exist, create it
                        ruleTimer = new System.Timers.Timer();
                        ruleTimer.Elapsed += RuleTimerElapsed;
                        ruleTimer.AutoReset = false; // timer does not repeat automatically
                        ruleTimer.Interval = PositionTime * 1000;
                        ruleTimer.Start();
                        ruleTimerStartTime = DateTime.Now;
                    }

                    // Calculate the time remaining before the end of the exercise
                    TimeSpan elapsed = DateTime.Now - ruleTimerStartTime;
                    TimeSpan remaining = TimeSpan.FromMilliseconds(ruleTimer.Interval) - elapsed;

                    this.UserPositionStatus = $"OK => MultiPosition, time remaining = {remaining.TotalSeconds:F1} seconds";
                    this.ExerciseDescription = $"{exerciseMultiPosition.Rules[IndexPosition].Description}";
                }
                else
                {
                    this.UserPositionStatus = $"KO => MultiPosition,";

                    if (ruleTimer != null)
                    {
                        ruleTimer.Stop();
                        ruleTimer.Dispose();
                        ruleTimer = null;
                    }
                }
            }
        }

        private void CheckOnePosition(Body body)
        {
            Joint Joint1 = body.Joints[exerciseMultiPosition.Rules[IndexPosition].Positions[0].Joint1];
            Joint Joint2 = body.Joints[exerciseMultiPosition.Rules[IndexPosition].Positions[0].Joint2];
            Double AngleMin = exerciseMultiPosition.Rules[IndexPosition].Positions[0].AngleMin;
            Double AngleMax = exerciseMultiPosition.Rules[IndexPosition].Positions[0].AngleMax;
            Double PositionTime = exerciseMultiPosition.Rules[IndexPosition].PositionTime;
            String Description = exerciseMultiPosition.Rules[IndexPosition].Description;

            // Calculate the angle between the head and neck using a custom function
            double angle = CalculateAngle(Joint1, Joint2);
            if (Math.Abs(angle) > AngleMin && Math.Abs(angle) < AngleMax)
            {

                // check if ruleTime exist
                if (ruleTimer == null)
                {
                    // If rulerTimer doesn't exist, create it
                    ruleTimer = new System.Timers.Timer();
                    ruleTimer.Elapsed += RuleTimerElapsed;
                    ruleTimer.AutoReset = false; // timer does not repeat automatically
                    ruleTimer.Interval = PositionTime * 1000;
                    ruleTimer.Start();
                    ruleTimerStartTime = DateTime.Now;
                }
                // Calculate the time remaining before the end of the exercise
                TimeSpan elapsed = DateTime.Now - ruleTimerStartTime;
                TimeSpan remaining = TimeSpan.FromMilliseconds(ruleTimer.Interval) - elapsed;
                this.UserPositionStatus = $"OK => angle: {Math.Abs(angle):F1}, time remaining = {remaining.TotalSeconds:F1} seconds";
                this.ExerciseDescription = $"{exerciseMultiPosition.Rules[IndexPosition].Description}";
            }
            else
            {
                this.UserPositionStatus = $"KO => angle: {Math.Abs(angle):F1}";
                if (ruleTimer != null)
                {
                    ruleTimer.Stop();
                    ruleTimer.Dispose();
                    ruleTimer = null;
                }
            }
        }

        

        // Check the user position, compare with the rules
        private bool CheckPosition(Joint Joint1, Joint Joint2, Double AngleMin, Double AngleMax, String Description, double PositionTime)
        {
            // Calculate the angle between the head and neck using a custom function
            double angle = CalculateAngle(Joint1, Joint2);

            if (Math.Abs(angle) > AngleMin && Math.Abs(angle) < AngleMax)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void RuleTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            PassToNextRule();
            ruleTimer.Stop();
            ruleTimer.Dispose();
            ruleTimer = null;
        }

        private void PassToNextRule()
        {
            if (IndexPosition < exerciseMultiPosition.Rules.Count - 1)   
            {
                IndexPosition++;
                this.ExerciseNumber = $"Exercise {IndexPosition + 1}/{exerciseMultiPosition.Rules.Count}";
                this.ExerciseDescription = $"{exerciseMultiPosition.Rules[IndexPosition].Description}";
            }
        }

        private void PassToPreviousRule()
        {
            if (IndexPosition>0)
            {
                IndexPosition--;
                this.ExerciseNumber = $"Exercise {IndexPosition + 1}/{exerciseMultiPosition.Rules.Count}";
                this.ExerciseDescription = $"{exerciseMultiPosition.Rules[IndexPosition].Description}";
            }
        }

        // Calculate the angle between two points
        public double CalculateAngle(Joint joint1, Joint joint2)
        {
            double deltaY = joint2.Position.Y - joint1.Position.Y;
            double deltaX = joint2.Position.X - joint1.Position.X;
            double angleRad = Math.Atan2(deltaY, deltaX); // Use arc tangent to calculate angle
            double angleDegrees = angleRad * (180.0 / Math.PI); // Translated from radian to degree

            return angleDegrees;
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (this.kinectSensor != null)
            {
                this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                                : Properties.Resources.SensorNotAvailableStatusText;
            }
            else
            {
                // Gérer le cas où le capteur Kinect n'est pas disponible
                this.StatusText = "Kinect sensor not available";
            }
        }

        private void Button_Click_stopExercise(object sender, RoutedEventArgs e)
        {
            // If a timer exist we delete it before to go back to the menu
            if (ruleTimer != null)
            {
                ruleTimer.Stop();
                ruleTimer.Dispose();
                ruleTimer = null;
            }

            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void Button_Click_previousExercise(object sender, RoutedEventArgs e)
        {
            if (ruleTimer != null)
            {
                ruleTimer.Stop();
                ruleTimer.Dispose();
                ruleTimer = null;
            }
            PassToPreviousRule();
        }

        private void Button_Click_skipExercise(object sender, RoutedEventArgs e)
        {
            if (ruleTimer != null)
            {
                ruleTimer.Stop();
                ruleTimer.Dispose();
                ruleTimer = null;
            }
            PassToNextRule();
        }



        // SECTION TO PRINT TEXT IN THE GRAPHICAL INTERFACE //
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        public string UserPositionStatus
        {
            get { return this.userPositionStatus; }
            set
            {
                if (this.userPositionStatus != value)
                {
                    this.userPositionStatus = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("UserPositionStatus"));
                    }
                }
            }
        }

        public string ExerciseNumber
        {
            get { return $"Exercise {IndexPosition + 1}/{exerciseMultiPosition.Rules.Count}"; }
            set
            {
                if (exerciseNumber != value)
                {
                    exerciseNumber = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("ExerciseNumber"));
                    }
                }
            }
        }

        public string ExerciseDescription
        {
            get
            {
                if (exerciseMultiPosition.Rules.Count != 0)
                {
                    return $"{exerciseMultiPosition.Rules[IndexPosition].Description}";
                }
                else
                {
                    return "...";
                }
            }
            set
            {
                if (exerciseDescription != value)
                {
                    exerciseDescription = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("ExerciseDescription"));
                    }
                }
            }
        }

        public string ExerciseMainDescription
        {
            get { return exerciseMultiPosition.Description; }
            set
            {
                if (this.exerciseMultiPosition.Description != value)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ExerciseMainDescription"));
                }
            }
        }

        public string ExerciseName
        {
            get { return exerciseMultiPosition.Name; }
            set
            {
                if (this.exerciseMultiPosition.Name != value)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ExerciseName"));
                }
            }
        }
    }
}

