using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace motionRecovery
{

    public partial class CreateRuleXMLPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        private string errorPosition = null; // To display an error when we entre a new position
        private List<SimplePosition> positions = new List<SimplePosition>();
        public event EventHandler<ExerciseRule> CreatedRule;
        private int positionNumber = 0;
        private bool isPositionSelected = false;
        private int selectedPositionIndex = -1;

        public static readonly DependencyProperty IsDeleteButtonEnabledProperty = DependencyProperty.Register("IsDeleteButtonEnabled", typeof(bool), typeof(CreateRuleXMLPage), new PropertyMetadata(false));
        public CreateRuleXMLPage(ExerciseRule existingRule = null)
        {
            InitializeComponent();
            this.DataContext = this;

            // Attach the event handler for the selection changed event
            listBoxPositionList.SelectionChanged += listBoxPositionList_SelectionChanged;

            if (existingRule != null)
            {
                LoadRule(existingRule);
            }
        }

        // Method to load an existing rule into the page controls
        public void LoadRule(ExerciseRule rule)
        {
            if (rule != null)
            {
                textBoxTime.Text = rule.PositionTime.ToString();
                textBoxDescription.Text = rule.Description;

                listBoxPositionList.Items.Clear();
                positions.Clear();
                positionNumber = 0;

                foreach (var position in rule.Positions)
                {
                    positionNumber++;
                    listBoxPositionList.Items.Add($"{positionNumber}) {position.Joint1} -> {position.Joint2}: {position.AngleMin}°-{position.AngleMax}°");
                    positions.Add(position);
                }
            }
        }

        private void Button_GoCreationPage(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        // Collects all the elements necessary to write a rule, and returns the rule to the previous page.
        private void Button_Click_SaveRule(object sender, RoutedEventArgs e)
        {

            string timeText = textBoxTime.Text;
            string description = textBoxDescription.Text;

            if (!IsValidTime(timeText))
            {
                DisplayError("Time must be a positive number.");
                return;
            }
            double time = double.Parse(timeText);
            if (time > 1024)
            {
                DisplayError("The time should not exceed 1024 seconds.");
                return;
            }
            if (description.Length > 512)
            {
                DisplayError("The description must not exceed 512 characters.");
                return;
            }
            if (positions.Count == 0)
            {
                DisplayError("Add at least one position to the rule.");
                return;
            }

            ExerciseRule newRule = new ExerciseRule
            {
                PositionTime = time,
                Description = description,
                Positions = positions
            };

            CreatedRule?.Invoke(this, newRule);

            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private bool IsValidTime(string timeText)
        {
            double time;
            if (!double.TryParse(timeText, out time))
            {
                return false;
            }
            return time >= 0;
        }


        private void Button_Click_AddANewPosition(object sender, RoutedEventArgs e)
        {
            ParseJointType parseJointType = new ParseJointType();

            string joint1 = ((ListBoxItem)listBoxJoint1.SelectedItem)?.Content.ToString();
            string joint2 = ((ListBoxItem)listBoxJoint2.SelectedItem)?.Content.ToString();
            string minAngleText = textBoxMinAngle.Text;
            string maxAngleText = textBoxMaxAngle.Text;

            if (string.IsNullOrEmpty(joint1) || string.IsNullOrEmpty(joint2) || string.IsNullOrEmpty(minAngleText) || string.IsNullOrEmpty(maxAngleText))
            {
                DisplayError("All fields must be completed.");
                return;
            }

            if (joint1 == joint2)
            {
                DisplayError("The joints must be different.");
                return;
            }

            if (!IsValidAngle(minAngleText) || !IsValidAngle(maxAngleText))
            {
                DisplayError("Angles must be between 0 and 360.");
                return;
            }

            double minAngle = double.Parse(minAngleText);
            double maxAngle = double.Parse(maxAngleText);


            SimplePosition newPosition = new SimplePosition
            {
                Joint1 = parseJointType.ParseToJoint(joint1),
                Joint2 = parseJointType.ParseToJoint(joint2),
                AngleMin = minAngle,
                AngleMax = maxAngle
            };

            // If a position is selected, then update the existing position, else add a new position
            if (isPositionSelected)
            {
                positions[selectedPositionIndex] = newPosition;
                listBoxPositionList.Items[selectedPositionIndex] = $"{selectedPositionIndex + 1}) {joint1} -> {joint2}: {minAngle}°-{maxAngle}°";
                isPositionSelected = false;
                selectedPositionIndex = -1;
            }
            else
            {
                positionNumber++;
                listBoxPositionList.Items.Add($"{positionNumber}) {joint1} -> {joint2}: {minAngle}°-{maxAngle}°");
                positions.Add(newPosition);
            }

            // Clear fields
            ClearFields();
        }


        // Method to display errors
        private void DisplayError(string error)
        {
            ErrorPosition = error;
        }

        // Method to clear fields
        private void ClearFields()
        {
            listBoxJoint1.SelectedItem = null;
            listBoxJoint2.SelectedItem = null;
            textBoxMinAngle.Text = "";
            textBoxMaxAngle.Text = "";
        }

        // Method to validate an angle
        private bool IsValidAngle(string angleText)
        {
            double angle;
            return double.TryParse(angleText, out angle) && angle >= 0 && angle <= 360;
        }

        // From the item selected in listBoxPositionList, we retrieve the number put at the beginning of it
        private int GetNumberOfThePositionSelected()
        {
            if (listBoxPositionList.SelectedItem != null)
            {
                string selectedPositionDescription = listBoxPositionList.SelectedItem.ToString();
                string[] numberPart = selectedPositionDescription.Split(')');
                return int.Parse(numberPart[0]);
            }

            return 0;
        }

        // Activated when a item in the listBoxPositionList is selected. 
        private void listBoxPositionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxPositionList.SelectedItem != null)
            {
                int positionSelected = GetNumberOfThePositionSelected()-1;

                if (positionSelected >= 0 && positionSelected < positions.Count)
                {
                    SimplePosition selectedPosition = positions[positionSelected];

                    listBoxJoint1.SelectedItem = listBoxJoint1.Items.OfType<ListBoxItem>().FirstOrDefault(item => item.Content.ToString() == selectedPosition.Joint1.ToString());
                    listBoxJoint2.SelectedItem = listBoxJoint2.Items.OfType<ListBoxItem>().FirstOrDefault(item => item.Content.ToString() == selectedPosition.Joint2.ToString());
                    textBoxMinAngle.Text = selectedPosition.AngleMin.ToString();
                    textBoxMaxAngle.Text = selectedPosition.AngleMax.ToString();

                    isPositionSelected = true;
                    selectedPositionIndex = positionSelected;
                }

                UpdateDeleteButtonState();
            }
            else
            {
                isPositionSelected = false;
                selectedPositionIndex = -1;
                UpdateDeleteButtonState();
            }
        }

        public string ErrorPosition
        {
            get
            {
                return this.errorPosition;
            }

            set
            {
                if (this.errorPosition != value)
                {
                    this.errorPosition = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("ErrorPosition"));
                    }
                }
            }
        }

        // Used to delete a selected position in the listbox
        private void Button_Click_DeletePosition(object sender, RoutedEventArgs e)
        {
            if (listBoxPositionList.SelectedItem != null)
            {
                int positionSelected = GetNumberOfThePositionSelected()-1;

                if (positionSelected >= 0 && positionSelected < positions.Count)
                {
                    positions.RemoveAt(positionSelected);
                    listBoxPositionList.Items.RemoveAt(positionSelected);

                    ClearFields();
                    UpdateDeleteButtonState();
                }
            }
        }

        // used to define or not the state "enabled" of the btn, only if one item is selected in the listBoxPositionList
        private void UpdateDeleteButtonState()
        {
            IsDeleteButtonEnabled = listBoxPositionList.SelectedItem != null;
        }

        // used to enabled or not a btn, only if one item is selected in the listBoxPositionList
        public bool IsDeleteButtonEnabled
        {
            get { return (bool)GetValue(IsDeleteButtonEnabledProperty); }
            set { SetValue(IsDeleteButtonEnabledProperty, value); }
        }

        private void Button_UpdateSkeleton(object sender, RoutedEventArgs e)
        {
            ResetSkeleton();
            UpdateSkeleton();
        }

        /// <summary>
        /// Reset the skeleton by resetting joint colors to gray and clearing angle graphics (lines and arcs).
        /// </summary>
        private void ResetSkeleton()
        {
            // Reset joint colors
            foreach (var child in skeletonCanvas.Children)
            {
                if (child is Ellipse ellipse)
                {
                    ellipse.Fill = Brushes.Gray;
                }
            }

            // Clear lines and arcs
            // Remove all green lines and  green arcs from skeletonCanvas
            List<UIElement> elementsToRemove = new List<UIElement>();
            foreach (var child in skeletonCanvas.Children)
            {
                if (child is Line line)
                {
                    if (line.Stroke is SolidColorBrush solidColorBrush && solidColorBrush.Color == Colors.Green)
                    {
                        elementsToRemove.Add(line);
                    }
                }
                else if (child is Path path)
                {
                    if (path.Stroke is SolidColorBrush solidColorBrush && solidColorBrush.Color == Colors.Green)
                    {
                        elementsToRemove.Add(path);
                    }
                }
            }

            foreach (var element in elementsToRemove)
            {
                skeletonCanvas.Children.Remove(element);
            }
        }

        /// <summary>
        /// Update the skeleton by highlighting selected joints in green and displaying the angle range between them.
        /// </summary>
        private void UpdateSkeleton()
        {
            string joint1Name = ((ListBoxItem)listBoxJoint1.SelectedItem)?.Content.ToString();
            string joint2Name = ((ListBoxItem)listBoxJoint2.SelectedItem)?.Content.ToString();

            if (joint1Name == null || joint2Name == null)
            {
                ErrorPosition = "Please select both joints.";
                return;
            }

            if (!double.TryParse(textBoxMinAngle.Text, out double minAngle) ||
                !double.TryParse(textBoxMaxAngle.Text, out double maxAngle))
            {
                ErrorPosition = "Please enter valid angles.";
                return;
            }

            // Update joint colors
            UpdateJointColor(joint1Name, Brushes.Green);
            UpdateJointColor(joint2Name, Brushes.Green);

            // Display angle range
            DisplayAngleRange(joint1Name, joint2Name, minAngle, maxAngle);
        }

        /// <summary>
        /// Update the color of the joint by the color input.
        /// </summary>
        /// <param name="jointName">The name of the joint to update.</param>
        /// <param name="color">The color to set for the joint.</param>
        private void UpdateJointColor(string jointName, Brush color)
        {
            Ellipse joint = (Ellipse)FindName(jointName);
            if (joint != null)
            {
                joint.Fill = color;
            }
        }

        /// <summary>
        /// Display the angle range between two joints by drawing lines and an arc on the canvas.
        /// </summary>
        /// <param name="joint1Name">The name of the first joint.</param>
        /// <param name="joint2Name">The name of the second joint.</param>
        /// <param name="minAngle">The minimum angle of the angle range to display.</param>
        /// <param name="maxAngle">The maximum angle of the angle range to display.</param>
        private void DisplayAngleRange(string joint1Name, string joint2Name, double minAngle, double maxAngle)
        {
            int lineLength = 60;

            // Find the Ellipse elements based on their names
            Ellipse joint1 = (Ellipse)FindName(joint1Name);
            Ellipse joint2 = (Ellipse)FindName(joint2Name);

            if (joint1 != null && joint2 != null)
            {
                // Get the positions of the ellipses (joint points)
                Point joint1Position = new Point(Canvas.GetLeft(joint1) + joint1.Width / 2, Canvas.GetTop(joint1) + joint1.Height / 2);

                // Calculate positions based on angles
                Point startAnglePoint = CalculatePointFromAngle(joint1Position, minAngle, lineLength);
                Point endAnglePoint = CalculatePointFromAngle(joint1Position, maxAngle, lineLength);

                // Draw lines representing the angle range
                DrawLine(joint1Position, startAnglePoint);
                DrawLine(joint1Position, endAnglePoint);

                // Calculate midpoint between startAnglePoint and endAnglePoint
                int midlineLength = lineLength / 2;
                Point startMidPoint = CalculatePointFromAngle(joint1Position, minAngle, midlineLength);
                Point endMidPoint = CalculatePointFromAngle(joint1Position, maxAngle, midlineLength);

                // Determine the correct order for drawing the arc
                bool isClockwise = maxAngle > minAngle;

                // Create the arc path
                StreamGeometry arcGeometry = new StreamGeometry();
                using (StreamGeometryContext ctx = arcGeometry.Open())
                {
                    ctx.BeginFigure(startMidPoint, false, false);
                    double step = (maxAngle - minAngle) / 10; // Number of points on the arc

                    if (isClockwise)
                    {
                        for (double angle = minAngle + step; angle < maxAngle; angle += step)
                        {
                            Point arcPoint = CalculatePointFromAngle(joint1Position, angle, midlineLength);
                            ctx.LineTo(arcPoint, true, true);
                        }
                    }
                    else
                    {
                        // If minAngle > maxAngle, it means the arc goes counter-clockwise through 0 degrees
                        double angleMinTranslation = -(360 - minAngle); // Transform a positive angle to a negative one to obtain angleMin < angleMax
                        step = (maxAngle - angleMinTranslation) / 10; // Number of points on the arc

                        for (double angle = angleMinTranslation + step; angle < maxAngle; angle += step)
                        {
                            Point arcPoint = CalculatePointFromAngle(joint1Position, angle, midlineLength);
                            ctx.LineTo(arcPoint, true, true);
                        }
                    }

                    ctx.LineTo(endMidPoint, true, true);
                }

                // Draw the arc
                DrawArc(new Pen(Brushes.Green, 1), arcGeometry);
            }
            else
            {
                ErrorPosition = "One or both joints not found.";
            }
        }

        /// <summary>
        /// Draw a line between two specified points on the canvas.
        /// </summary>
        /// <param name="startPoint">The starting point of the line.</param>
        /// <param name="endPoint">The ending point of the line.</param>
        private void DrawLine(Point startPoint, Point endPoint)
        {
            Line line = new Line();
            line.X1 = startPoint.X;
            line.Y1 = startPoint.Y;
            line.X2 = endPoint.X;
            line.Y2 = endPoint.Y;
            line.Stroke = Brushes.Green;
            line.StrokeThickness = 2;

            skeletonCanvas.Children.Add(line);
        }

        /// <summary>
        /// Draw an arc on the canvas using the specified pen and geometry.
        /// </summary>
        /// <param name="pen">The pen used to draw the arc.</param>
        /// <param name="geometry">The geometry representing the arc to draw.</param>
        private void DrawArc(Pen pen, StreamGeometry geometry)
        {
            Path path = new Path();
            path.Stroke = pen.Brush;
            path.StrokeThickness = pen.Thickness;
            path.Data = geometry;

            skeletonCanvas.Children.Add(path);
        }

        /// <summary>
        /// Calculate the coordinates of a point based on an angle and distance from a specified origin point.
        /// </summary>
        /// <param name="origin">The origin point from which to calculate the new point.</param>
        /// <param name="angleDegrees">The angle in degrees from the origin point.</param>
        /// <param name="distance">The distance from the origin to the new point.</param>
        /// <returns>The calculated point coordinates.</returns>
        private Point CalculatePointFromAngle(Point origin, double angleDegrees, double distance)
        {
            double angleRadians = angleDegrees * (Math.PI / 180);
            double x = origin.X + distance * Math.Cos(angleRadians);
            double y = origin.Y - distance * Math.Sin(angleRadians); // Invert Y because canvas coordinates are upside down
            return new Point(x, y);
        }
    }
}
