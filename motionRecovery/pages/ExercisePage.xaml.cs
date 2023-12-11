using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;
using System.Text;
using System.Windows.Threading;

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
        private string exerciseValidation = null;    
        private Boolean exerciseValid = true;


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
        private int indexRule = 0; // Index to identify the current exercise position
        private int maxRule = 0;

        // Timer variables
        private System.Timers.Timer ruleTimer = new System.Timers.Timer(); // Timer for controlling exercise rules
        private DateTime ruleTimerStartTime; // Start time for the rule timer

        // POPUP Timer
        private DispatcherTimer popupTimer = new DispatcherTimer();
        private int popupTime = 6;
        private int countdownSecondsPopup = 6;

        // Config Varaible
        Boolean DisplayGraphicalHelp = false;


        // Constructor for ExercisePage that takes an ExerciseMultiPosition parameter
        public ExercisePage(ExerciseMultiPosition exerciseMultiPosition)
        {
            this.exerciseMultiPosition = exerciseMultiPosition;

            maxRule = exerciseMultiPosition.Rules.Count;

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
            ExerciseNumber = $"Exercise {indexRule + 1}/{exerciseMultiPosition.Rules.Count}";
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
        /// Event handler called when a body frame is received from the sensor.
        /// Processes the body frame data, draws the skeletal tracking data, and checks exercise rules.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">Event arguments containing the body frame data.</param>
        /// <remarks>
        /// This method processes the body frame data, including drawing skeletal tracking data on the display.
        /// It checks for the presence of tracked bodies, extracts joint positions, and renders the bodies and hands.
        /// Additionally, it invokes the <see cref="CheckMultiplePosition"/> method to validate the user's position against
        /// multiple joint positions and angles specified in the exercise rules.
        /// 
        /// Environment:
        /// - <see cref="bodies"/> (global Body[]): Represents an array of tracked bodies in the current frame.
        /// - <see cref="drawingGroup"/> (global DrawingGroup): Represents the drawing group used for rendering.
        /// - <see cref="bodyColors"/> (global Pen[]): Represents an array of pens used for drawing body outlines.
        /// - <see cref="displayWidth"/> (global double): Represents the width of the display.
        /// - <see cref="displayHeight"/> (global double): Represents the height of the display.
        /// - <see cref="coordinateMapper"/> (global CoordinateMapper): Represents the coordinate mapper for mapping joint positions.
        /// - <see cref="skeletonGraphicInterface"/> (global SkeletonGraphicInterface): Represents the graphic interface for skeleton rendering.
        /// - <see cref="exerciseMultiPosition"/> (global ExerciseMultiPosition): Represents the data structure of the exercise.
        /// - <see cref="checkMultiplePosition"/> (global method): Represents the method to check multiple joint positions and angles.
        /// </remarks>
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


                            // CHECK RULES
                            if (body != null && exerciseMultiPosition.Rules.Count != 0) // Check if a body is detected and if some rules exists
                            {
                                if (popupContainer.Visibility == Visibility.Collapsed) // Check if the success popup is visible, if not we can analyse the rule(s)
                                {
                                    checkMultiplePosition(body, jointPoints, dc);
                                }
                            }
                        }
                    }

                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// Checks if the user's body complies to different joint positions and angles for a specified period.
        /// Upadate the userPositionStatus.
        /// It visually displays the joint positions, wanted angles, and current angles.
        /// </summary>
        /// <param name="body">The body data representing the user's skeletal information.</param>
        /// <param name="jointPoints">A dictionary containing joint points for graphical rendering.</param>
        /// <param name="dc">The DrawingContext used for graphical rendering.</param>
        /// <remarks>
        /// This method iterates over multiple joint positions and associated angle constraints specified in the
        /// current exercise rule. It visually displays the joint positions, wanted angles, and current angles.
        /// The method checks if the user's current angles for each position are within the specified range.
        /// If all positions are respected, it initiates a timer for the next rule, and the user position status
        /// is updated accordingly. If the positions are not respected, the timer is stopped, and the user position
        /// status reflects a failure.
        /// 
        /// Environment:
        /// - <see cref="indexRule"/> (global int): Represents the index of the current exercise rule.
        /// - <see cref="exerciseMultiPosition"/> (global ExerciseMultiPosition): Represents the data structure of the exercise.
        /// - <see cref="ExerciseNumber"/> (global string): Displays to the user the number of the Exercise/Rule.
        /// - <see cref="ExerciseDescription"/> (global string): Displays to the user the description of the Exercise/Rule.
        /// - <see cref="UserPositionStatus"/> (global string): Represents the status of the user's position.
        /// - <see cref="ruleTimer"/> (global System.Timers.Timer): Represents the timer used for rule transition.
        /// - <see cref="ruleTimerStartTime"/> (global DateTime): Represents the start time of the rule timer.
        /// </remarks>
        private void checkMultiplePosition(Body body, IDictionary<JointType, Point> jointPoints, DrawingContext dc)
        {
            bool CheckPos = true; // Used to check if all the positions are respected

            StringBuilder positionDetails = new StringBuilder();

            foreach (SimplePosition Positions in exerciseMultiPosition.Rules[indexRule].Positions)
            {
                Joint Joint1 = body.Joints[Positions.Joint1];
                Joint Joint2 = body.Joints[Positions.Joint2];
                Double AngleMin = Positions.AngleMin;
                Double AngleMax = Positions.AngleMax;
                Double PositionTime = exerciseMultiPosition.Rules[indexRule].PositionTime;
                String Description = exerciseMultiPosition.Rules[indexRule].Description;
                double currentAngle = CalculateAngle(body.Joints[Positions.Joint1], body.Joints[Positions.Joint2]);

                // Check if this position respect the rule.
                Boolean AngleAccepted = CheckAngle(AngleMin, AngleMax, currentAngle);

                positionDetails.AppendLine($" + {Positions.Joint1} - {Positions.Joint2} = {currentAngle:F1}°");
                positionDetails.AppendLine($"       Wanted Angle: {AngleMin:F1}° - {AngleMax:F2}°");


                // Display the graphical assistance
                if (DisplayGraphicalHelp)
                {
                    Color positionColor;
                    if (AngleAccepted)
                    {
                        positionColor = Colors.Green;
                    } else
                    {
                        positionColor = Colors.Red;
                    }

                    this.skeletonGraphicInterface.SelectJointGraphical(Positions.Joint1, jointPoints, dc, 6, new SolidColorBrush(positionColor));
                    this.skeletonGraphicInterface.SelectJointGraphical(Positions.Joint2, jointPoints, dc, 6, new SolidColorBrush(positionColor));
                    this.skeletonGraphicInterface.DisplayWantedAngle(Positions.Joint1, Positions.AngleMin, Positions.AngleMax, jointPoints, dc, positionColor);
                    this.skeletonGraphicInterface.DisplayCurrentAngle(Positions.Joint1, currentAngle, jointPoints, dc, positionColor);
                }

                if (AngleAccepted == false)
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

                    this.ExerciseValidation = $"OK: time remaining = {remaining.TotalSeconds:F1} seconds";

                    this.ExerciseValid = true; // used in the frontend
                }
                else
                {
                    this.ExerciseValidation = $"KO";

                    this.ExerciseValid = false; // used in the frontend

                    if (ruleTimer != null)
                    {
                        ruleTimer.Stop();
                        ruleTimer.Dispose();
                        ruleTimer = null;
                    }
                }
            }

            this.UserPositionStatus = positionDetails.ToString();
        }




        /// <summary>
        /// Checks if the provided angle falls within a specified range, considering cases where the range crosses 360 degrees.
        /// </summary>
        /// <param name="angleMin">The minimum angle of the specified range.</param>
        /// <param name="angleMax">The maximum angle of the specified range.</param>
        /// <param name="angle">The angle to be checked.</param>
        /// <returns>
        /// True if the angle is within the specified range; otherwise, false.
        /// </returns>
        /// <remarks>
        ///  Check if the current angle is within the specified range, considering cases where the angle range crosses 360 degrees.
        /// If AngleMax is greater than AngleMin, verify if angle is between AngleMin and AngleMax.
        /// If AngleMin is greater than AngleMax, consider two sub-cases:
        ///   - If angle is greater than AngleMin and less than or equal to 360.
        ///   - If angle is less than or equal to AngleMax.
        /// </remarks>
        private bool CheckAngle(Double AngleMin, Double AngleMax, double angle)
        {
            if ((AngleMax > AngleMin && angle >= AngleMin && angle <= AngleMax) || (AngleMin > AngleMax && ((angle > AngleMin && angle <= 360) || (angle <= AngleMax))))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Event handler called when the rule timer elapses. Pass to the next Rule/exercise and delete the timer
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">Event arguments containing information about the elapsed time.</param>
        /// <remarks>
        /// Environment:
        /// - <see cref="exerciseMultiPosition"/> (global ExerciseMultiPosition): Represents the data structure of the exercise.
        /// - <see cref="ExerciseNumber"/> (global string): Displays to the user the number of the Exercise/Rule.
        /// - <see cref="ExerciseDescription"/> (global string): Displays to the user the description of the Exercise/Rule.
        /// </remarks>
        private void RuleTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ruleTimer.Stop();
            ruleTimer.Dispose();
            ruleTimer = null;

            Show_RuleSuccessPopup();
        }


        /// <summary>
        /// Moves to the next rule in a sequence of exercises, if available.
        /// </summary>
        /// <remarks>
        /// Environment:
        /// - <see cref="indexRule"/> (global int): Represents the index of the current rule.
        /// - <see cref="exerciseMultiPosition"/> (global ExerciseMultiPosition): Represents the data structure of the exercise.
        /// - <see cref="ExerciseNumber"/> (global string): Displays to the user the number of the Exercise/Rule.
        /// - <see cref="ExerciseDescription"/> (global string): Displays to the user the description of the Exercise/Rule.
        /// </remarks>
        private void PassToNextRule()
        {
            if (indexRule < exerciseMultiPosition.Rules.Count - 1)   
            {
                indexRule++;
                this.ExerciseNumber = $"Exercise {indexRule + 1}/{exerciseMultiPosition.Rules.Count}";
                this.ExerciseDescription = $"{exerciseMultiPosition.Rules[indexRule].Description}";
            }

            this.IsFirstRule = (indexRule == 0); // used in the front
            this.IsMaxRule = (maxRule == (indexRule + 1)); // used in the front
        }

        /// <summary>
        /// Moves to the previous rule in a sequence of exercises, if available.
        /// <remarks>
        /// environment :
        /// <see cref="indexRule"/> global int: the index of the current rule
        /// <see cref="exerciseMultiPosition"/> global ExerciseMultiPosition: the data structure of the exercise
        /// <see cref="ExerciseNumber"/> global string: display to the user the number of the Exercise/Rule
        /// <see cref="ExerciseDescription"/> global string: display to the user the description of the Exercise/Rule
        /// </remarks>
        /// </summary>
        private void PassToPreviousRule()
        {
            if (indexRule > 0)
            {
                indexRule--;
                this.ExerciseNumber = $"Exercise {indexRule + 1}/{exerciseMultiPosition.Rules.Count}";
                this.ExerciseDescription = $"{exerciseMultiPosition.Rules[indexRule].Description}";
            }

            this.IsFirstRule = (indexRule == 0); // used in the front
            this.IsMaxRule = (maxRule == (indexRule + 1)); // used in the front
        }

        /// <summary>
        /// Calculates the angle in degrees between two joints in a 2D space.
        /// </summary>
        /// <param name="joint1">The first joint</param>
        /// <param name="joint2">The second joint</param>
        /// <returns>The angle in degrees between the two joints</returns>
        public double CalculateAngle(Joint joint1, Joint joint2)
        {
            double deltaY = joint2.Position.Y - joint1.Position.Y;
            double deltaX = joint2.Position.X - joint1.Position.X;

            double angleRad = Math.Atan2(deltaY, deltaX); // Use arc tangent to calculate angle
            double angleDegrees = angleRad * (180.0 / Math.PI); // Translated from radian to degree

            // To ensure the angle is in the range [0, 360].
            if (angleDegrees < 0)
            {
                angleDegrees += 360;
            }

            return angleDegrees;
        }

        private void Show_RuleSuccessPopup()
        {
            Dispatcher.Invoke(() =>
            {
                popupContainer.Visibility = Visibility.Visible;

                if (popupTimer != null)
                {
                    popupTimer.Stop();
                    popupTimer.Tick -= Timer_Tick; 
                    popupTimer = null;
                }

                popupTimer = new DispatcherTimer();
                popupTimer.Interval = TimeSpan.FromSeconds(1);
                popupTimer.Tick += Timer_Tick;
                popupTimer.Start();
            });
        }


        private void Hide_RuleSuccessPopup()
        {
            Dispatcher.Invoke(() =>
            {
                popupContainer.Visibility = Visibility.Collapsed;
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            countdownSecondsPopup--;

            if (countdownSecondsPopup <= 0)
            {
                popupTimer.Stop();
                countdownSecondsPopup = popupTime;

                Hide_RuleSuccessPopup();
                PassToNextRule();
            }
            else
            {
                countdownLabel.Text = $"Next rule in {countdownSecondsPopup} seconds";
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

        private void Button_Click_previousRule(object sender, RoutedEventArgs e)
        {
            if (ruleTimer != null)
            {
                ruleTimer.Stop();
                ruleTimer.Dispose();
                ruleTimer = null;
            }
            PassToPreviousRule();
        }

        private void Button_Click_skipRule(object sender, RoutedEventArgs e)
        {
            if (ruleTimer != null)
            {
                ruleTimer.Stop();
                ruleTimer.Dispose();
                ruleTimer = null;
            }
            PassToNextRule();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DisplayGraphicalHelp = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DisplayGraphicalHelp = false;
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

        public Boolean IsFirstRule
        {
            get
            {
                return (indexRule == 0);

            }
            set
            {
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("IsFirstRule"));
                }
            }
        }

        public Boolean IsMaxRule
        {
            get
            {
                return (maxRule == (indexRule + 1));

            }
            set
            {
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("IsMaxRule"));
                }
            }
        }

        public string ExerciseValidation
        {
            get { return this.exerciseValidation; }
            set
            {
                if (this.exerciseValidation != value)
                {
                    this.exerciseValidation = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("ExerciseValidation"));
                    }
                }
            }
        }

        public Boolean ExerciseValid
        {
            get { return exerciseValid; }
            set
            {
                if (exerciseValid != value)
                {
                    this.exerciseValid = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("ExerciseValid"));
                    }
                }
            }
        }

        public string ExerciseNumber
        {
            get { return $"Exercise {indexRule + 1}/{exerciseMultiPosition.Rules.Count}"; }
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
                    return $"{exerciseMultiPosition.Rules[indexRule].Description}";
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

