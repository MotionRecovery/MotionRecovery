using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;

namespace motionRecovery
{
    public partial class ExercisePage : Page, INotifyPropertyChanged
    {
        // Variables
        private KinectSensor kinectSensor = null; // Variable is used to interact with the Kinect sensor throughout the application 

        private SkeletonGraphicInterface skeletonGraphicInterface; //Used to draw the skeleton

        private DrawingGroup drawingGroup; // Drawing group for body rendering output
        private DrawingImage imageSource; // Drawing image that we will display
        private CoordinateMapper coordinateMapper = null; // Coordinate mapper to map one type of point to another

        private const float InferredZPositionClamp = 0.1f; //Constant for clamping Z values of camera space points from being negative

        private BodyFrameReader bodyFrameReader = null; // Reader for body frames
        private Body[] bodies = null; // Array for the bodies
        private readonly List<Tuple<JointType, JointType>> bones;


        private int displayWidth; // Width of display (depth space)
        private int displayHeight; // Height of display (depth space)
        private List<Pen> bodyColors; // List of colors for each body tracked

        private ExerciseWriterXML writerXML = new ExerciseWriterXML();




        ExerciseMultiPosition exerciseMultiPosition = new ExerciseMultiPosition();
        int IndexPosition = 0;

        private System.Timers.Timer ruleTimer = new System.Timers.Timer();
        private DateTime ruleTimerStartTime;

       

        // Used to print a message in the frontend
        private string userPositionStatus;
        private string exerciseNumber;
        private string exerciseDescription;
        private string statusText = null;


        public ExercisePage(String filePath)
        {
            this.kinectSensor = KinectSensor.GetDefault(); // get the kinectSensor object
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged; // set IsAvailableChanged event notifier
            this.kinectSensor.Open();
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
            ExercisesReaderXML exerciseReader = new ExercisesReaderXML();

            exerciseMultiPosition = exerciseReader.ReadExerciseFile(filePath);

            this.skeletonGraphicInterface = new SkeletonGraphicInterface();

            // Skeleton Camera
            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);


            this.DataContext = this; // use the window object as the view model in this simple example
            this.InitializeComponent();  // initialize the components (controls) of the window
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



        /// <summary>
        /// Handles the body frame data arriving from the sensor.
        /// Processes body data, updates joint positions, and draws the skeleton of tracked users.
        /// Also visualizes the state of left and right hands.
        /// </summary>
        /// <param name="sender">Object sending the event (Kinect sensor)</param>
        /// <param name="e">Event arguments containing the body frame data</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;  // "Flag"indicating whether valid body frame data has been received

            // Using statement ensures proper disposal of resources after the block
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                // Check if the body frame is not null
                if (bodyFrame != null)
                {
                    // Initialize or resize the array to hold body data
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // Get and refresh body data
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            // If valid body frame data has been received, proceed with visualization
            if (dataReceived)
            {
                // Using statement ensures proper disposal of resources after the block
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;

                    // Iterate through each tracked body
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        // Check if the body is tracked
                        if (body.IsTracked)
                        {
                            // Draw clipped edges and obtain joint positions in depth space
                            this.skeletonGraphicInterface.DrawClippedEdges(body, dc, this.displayWidth, this.displayHeight);
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // Convert joint positions to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // Ensure joint depth is non-negative
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                // Map camera point to depth space
                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.skeletonGraphicInterface.DrawBody(joints, jointPoints, drawPen, bones, dc);
                            this.skeletonGraphicInterface.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.skeletonGraphicInterface.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);



                            // CHECK rules
                            if (body != null & exerciseMultiPosition.Rules.Count != 0) // Check if a body is detectected and if there are some rules (positionRules)
                            {
                                Joint Joint1 = body.Joints[exerciseMultiPosition.Rules[IndexPosition].Positions[0].Joint1];
                                Joint Joint2 = body.Joints[exerciseMultiPosition.Rules[IndexPosition].Positions[0].Joint2];
                                Double AngleMin = exerciseMultiPosition.Rules[IndexPosition].Positions[0].AngleMin;
                                Double AngleMax = exerciseMultiPosition.Rules[IndexPosition].Positions[0].AngleMax;
                                Double PositionTime = exerciseMultiPosition.Rules[IndexPosition].PositionTime;
                                String Description = exerciseMultiPosition.Rules[IndexPosition].Description;

                                this.CheckUserPosition(Joint1, Joint2, AngleMin, AngleMax, Description, PositionTime);

                            }

                        }
                    }

                    // Prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }


        // Check the user position, compare with the rules
        private void CheckUserPosition(Joint Joint1, Joint Joint2, Double AngleMin, Double AngleMax, String Description, double PositionTime)
        {
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

        private double CalculateAngleWithDouble(double Point1, double Point2, double Point3, double Point4)
        {
            double deltaY = Point2 - Point3;
            double deltaX = Point2 - Point4;
            double angleRad = Math.Atan2(deltaY, deltaX); // Use arc tangent to calculate angle
            double angleDegrees = angleRad * (180.0 / Math.PI); // Translated from radian to degree
            angleDegrees = angleDegrees * 1.5;

            return angleDegrees;
        }



        // SECTION TO PRINT TEST IN THE GRAPHICAL INTERFACE //
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

