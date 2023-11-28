using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace motionRecovery
{
    // Page used to write exercise files without writing in XML
    public partial class CreateExerciseXMLPage : Page, INotifyPropertyChanged
    {
        // The ExerciseMultiPosition instance for creating the exercise
        private ExerciseMultiPosition newExercise;

        // Event to notify changes in data for binding
        public event PropertyChangedEventHandler PropertyChanged;

        // Error message property for XML creation
        private string errorXMLCreation = null;

        // Constructor
        public CreateExerciseXMLPage()
        {
            // Initialize the ExerciseMultiPosition instance
            newExercise = new ExerciseMultiPosition();

            // Set the data context for data binding
            this.DataContext = this;

            // Initialize the UI components
            InitializeComponent();
        }

        #region Button Click Handlers

        // Handles the "Go Back" button click
        private void Button_GoMenu(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        // Handles the "Save as XML" button click
        private void Button_Click_SaveXML(object sender, RoutedEventArgs e)
        {
            // Validate input data before saving XML
            if (ValidateInput())
            {
                try
                {
                    // Use SaveFileDialog to allow the user to choose the file location and name
                    Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                    saveFileDialog.Filter = "XML files (*.xml)|*.xml";
                    saveFileDialog.DefaultExt = ".xml";
                    saveFileDialog.FileName = textBoxName.Text; // Use the entered name as the default file name

                    // Show the dialog and wait for the user's choice
                    bool? result = saveFileDialog.ShowDialog();

                    // If the user chose a location and clicked Save
                    if (result == true)
                    {
                        string filePath = saveFileDialog.FileName;

                        // Use the XmlExerciseWriter class to write the exercise to the XML file
                        ExerciseWriterXML.WriteExerciseToFile(newExercise, filePath);

                        // Reset errors if there are none
                        ErrorXMLCreation = null;

                        // Go back to the previous page
                        if (NavigationService.CanGoBack)
                        {
                            NavigationService.GoBack();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions related to writing the XML file
                    ErrorXMLCreation = $"Error writing XML file: {ex.Message}";
                }
            }
        }

        // Handles the "Add a new Rule" button click
        private void Button_Click_AddANewRule(object sender, RoutedEventArgs e)
        {
            createRulePage = new CreateRuleXMLPage();
            NavigationService.Navigate(createRulePage);
            createRulePage.CreatedRule += CreateRuleXMLPage_CreatedRule;
        }

        #endregion

        #region Rule Creation

        // Handles the creation of a new rule
        private void CreateRuleXMLPage_CreatedRule(object sender, ExerciseRule newRule)
        {
            newExercise.Rules.Add(newRule);
            String RuleNumber = $"Rule number {newExercise.Rules.Count()}";
            listBoxRules.Items.Add(RuleNumber);
        }

        #endregion

        #region Validation

        // Validates input data before saving XML
        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(textBoxName.Text) || textBoxName.Text.Length > 128)
            {
                ErrorXMLCreation = "Name must not be empty and should be at most 128 characters.";
                return false;
            }
            if (textBoxDescription.Text.Length > 1028)
            {
                ErrorXMLCreation = "Description must not exceed 1028 characters.";
                return false;
            }
            if (newExercise.Rules == null || newExercise.Rules.Count == 0)
            {
                ErrorXMLCreation = "Add at least one rule to the exercise.";
                return false;
            }

            // Reset errors if there are no validation issues
            ErrorXMLCreation = null;
            return true;
        }

        #endregion

        #region Properties

        // Property for the XML creation error message
        public string ErrorXMLCreation
        {
            get
            {
                return this.errorXMLCreation;
            }

            set
            {
                if (this.errorXMLCreation != value)
                {
                    this.errorXMLCreation = value;

                    // Notify any bound elements that the text has changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ErrorXMLCreation"));
                }
            }
        }

        #endregion
    }
}
