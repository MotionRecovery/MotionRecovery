namespace motionRecovery
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Microsoft.Win32;


    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Variables
        private KinectSensor kinectSensor = null; // Variable is used to interact with the Kinect sensor throughout the application lifecycle
        private string statusText = null; // Used to notice if the Kinect is running or not
        private string userPositionStatus = "No rules detected";

        // Design parameters
        private const double HandSize = 30; //Radius of drawn hand circles
        private const double JointThickness = 3; //Thickness of drawn joint lines
        private const double ClipBoundsThickness = 10; //Thickness of clip edge rectangles (edges which tell us if we are leaving the camera)

        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)); //Brush used for drawing hands that are currently tracked as closed
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0)); //Brush used for drawing hands that are currently tracked as opened
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255)); // Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68)); // Brush used for drawing joints that are currently tracked
        private readonly Brush inferredJointBrush = Brushes.Yellow; // Brush used for drawing joints that are currently inferred
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1); // Pen used for drawing bones that are currently inferred

        private DrawingGroup drawingGroup; // Drawing group for body rendering output
        private DrawingImage imageSource; // Drawing image that we will display
        private CoordinateMapper coordinateMapper = null; // Coordinate mapper to map one type of point to another

        private const float InferredZPositionClamp = 0.1f; //Constant for clamping Z values of camera space points from being negative

        private BodyFrameReader bodyFrameReader = null; // Reader for body frames
        private Body[] bodies = null; // Array for the bodies

        private List<Tuple<JointType, JointType>> bones; // definition of bones

        private int displayWidth; // Width of display (depth space)
        private int displayHeight; // Height of display (depth space)

        private List<Pen> bodyColors; // List of colors for each body tracked


        // Dans votre code principal
        List<Position> positionRules = new List<Position>();
        int IndexPosition = 0;

        


        public MainWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault(); // get the kinectSensor object
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged; // set IsAvailableChanged event notifier
            this.kinectSensor.Open();
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
            ExercicesReaderXML exerciseReader = new ExercicesReaderXML();
            // Utilisez OpenFileDialog pour permettre à l'utilisateur de sélectionner un fichier
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichiers XML (*.xml)|*.xml|Tous les fichiers (*.*)|*.*";

            bool? result = openFileDialog.ShowDialog();

            string filePath = null;

            if (result == true)
            {
                // L'utilisateur a sélectionné un fichier, obtenez le chemin du fichier
                filePath = openFileDialog.FileName;
            }



            positionRules = exerciseReader.ReadExerciseFile(filePath);



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




        // Gets or sets the current status text to display
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



        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }


        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
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
                            this.DrawClippedEdges(body, dc);
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

                            this.DrawBody(joints, jointPoints, dc, drawPen);
                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);

                            if (body != null & positionRules.Count !=0) // Check if a body is detectected and if there are some rules (positionRules)
                            {
                                Joint Joint1 = body.Joints[positionRules[IndexPosition].Joint1];
                                Joint Joint2 = body.Joints[positionRules[IndexPosition].Joint2];
                                Double AngleMin = positionRules[IndexPosition].AngleMin;
                                Double AngleMax = positionRules[IndexPosition].AngleMax;
                                String Description = positionRules[IndexPosition].Description;

                                this.CheckUserPosition(Joint1, Joint2, AngleMin, AngleMax, Description);
                            }
                           
                        }
                    }

                    // Prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }


        private void CheckUserPosition(Joint Joint1, Joint Joint2, Double AngleMin, Double AngleMax, String Description)
        {
            // Calculate the angle between the head and neck using a custom function
            double angle = CalculateAngle(Joint1, Joint2);

            if (Math.Abs(angle) > AngleMin & Math.Abs(angle) < AngleMax)
            {
                this.UserPositionStatus = $"OK => angle: {Math.Abs(angle):F2}, {Description}";
                if (IndexPosition < positionRules.Count-1)
                {
                    IndexPosition++;
                }
            }
            else
            {
                this.UserPositionStatus = $"KO => angle: {Math.Abs(angle):F2}, {Description}";
            }
        }

        // Calculate the angle between two points
        private double CalculateAngle(Joint joint1, Joint joint2)
        {
            double deltaY = joint2.Position.Y - joint1.Position.Y;
            double deltaX = joint2.Position.X - joint1.Position.X;
            double angleRad = Math.Atan2(deltaY, deltaX); // Use arc tangent to calculate angle
            double angleDegrees = angleRad * (180.0 / Math.PI); // Translated from radian to degree

            return angleDegrees;
        }


        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }


    }
}
