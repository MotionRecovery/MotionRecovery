
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;


namespace motionRecovery
{

    public partial class CreateRuleXMLPage : Page, INotifyPropertyChanged
    {
        private ParseJointType ParseJointType; // Used to parse a string to a type Joint
        public event PropertyChangedEventHandler PropertyChanged;// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        private string errorPosition = null; // To display an error when we entre a new position
        private List<SimplePosition> positions = new List<SimplePosition>();
        public event EventHandler<ExerciseRule> CreatedRule;
        public CreateRuleXMLPage()
        {
            InitializeComponent();
            this.DataContext = this;
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

            if (minAngle > maxAngle)
            {
                DisplayError("\r\nThe MinAngle must be less than or equal to the MaxAngle.");
                return;
            }

            // Create an new position
            SimplePosition newPosition = new SimplePosition
            {
                Joint1 = parseJointType.ParseToJoint(joint1),
                Joint2 = parseJointType.ParseToJoint(joint2),
                AngleMin = minAngle,
                AngleMax = maxAngle
            };

            // add a new position to the list
            listBoxPositionList.Items.Add($"{joint1}, {joint2}; with the MinAngle: {minAngle}, and the MaxAngle: {maxAngle}");
            positions.Add(newPosition);


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
    }
}
