using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace motionRecovery
{
    // Page used to write exercise files without writing in XML
    public partial class CreateExerciseXMLPage : Page, INotifyPropertyChanged
    {
        ExerciseMultiPosition newExercise;
        public event PropertyChangedEventHandler PropertyChanged; // INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        private string errorXMLCreation = null;
        private ExerciseRule selectedRule = null; // Property to hold the selected rule

        public CreateExerciseXMLPage()
        {
            newExercise = new ExerciseMultiPosition();
            this.DataContext = this; // Set the data context to this class for data binding
            InitializeComponent();
        }

        private void Button_GoMenu(object sender, RoutedEventArgs e)
        {
            // Navigate back to the previous page if possible
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void Button_Click_SaveXML(object sender, RoutedEventArgs e)
        {
            // Validate input before saving XML
            if (string.IsNullOrEmpty(textBoxName.Text) || textBoxName.Text.Length > 128)
            {
                ErrorXMLCreation = "Name must not be empty and should be at most 128 characters.";
                return;
            }
            if (textBoxDescription.Text.Length > 1028)
            {
                ErrorXMLCreation = "Description must not exceed 1028 characters.";
                return;
            }
            if (newExercise.Rules == null || newExercise.Rules.Count == 0)
            {
                ErrorXMLCreation = "Add at least one rule to the exercise.";
                return;
            }

            try
            {
                newExercise.Name = textBoxName.Text;
                newExercise.Description = textBoxDescription.Text;
                // Use SaveFileDialog to let the user choose the file location and name
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

                    // Use the ExerciseWriterXML class to write the exercise to the XML file
                    ExerciseWriterXML.WriteExerciseToFile(newExercise, filePath);

                    // Reset errors if there are none
                    ErrorXMLCreation = null;

                    // Navigate back to the previous page
                    if (NavigationService.CanGoBack)
                    {
                        NavigationService.GoBack();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any exception related to writing the XML file
                ErrorXMLCreation = $"Error writing XML file: {ex.Message}";
            }
        }

        private CreateRuleXMLPage createRulePage;
        private void Button_Click_AddANewRule(object sender, RoutedEventArgs e)
        {
            var createRulePage = new CreateRuleXMLPage();
            NavigationService.Navigate(createRulePage);
            createRulePage.CreatedRule += CreateRuleXMLPage_CreatedRule;
        }

        private void Button_Click_ModifyRule(object sender, RoutedEventArgs e)
        {
            if (selectedRule != null)
            {
                var createRulePage = new CreateRuleXMLPage(selectedRule);
                NavigationService.Navigate(createRulePage);
                createRulePage.CreatedRule += CreateRuleXMLPage_ModifiedRule;
            }
        }

        private void Button_Click_DeleteRule(object sender, RoutedEventArgs e)
        {
            if (selectedRule != null)
            {
                int index = newExercise.Rules.IndexOf(selectedRule);
                newExercise.Rules.RemoveAt(index);
                listBoxRules.Items.RemoveAt(index);
                selectedRule = null;
                btnModifyRule.IsEnabled = false;
                btnDeleteRule.IsEnabled = false;
            }
        }

        private void Button_Click_Reset(object sender, RoutedEventArgs e)
        {
            newExercise = new ExerciseMultiPosition();
            textBoxName.Text = string.Empty;
            textBoxDescription.Text = string.Empty;
            listBoxRules.Items.Clear();
            ErrorXMLCreation = null;
            selectedRule = null;
            btnModifyRule.IsEnabled = false;
            btnDeleteRule.IsEnabled = false;
        }

        private void CreateRuleXMLPage_CreatedRule(object sender, ExerciseRule newRule)
        {
            newExercise.Rules.Add(newRule);
            string RuleNumber = $"Rule number {newExercise.Rules.Count}";
            listBoxRules.Items.Add(RuleNumber);
        }

        private void CreateRuleXMLPage_ModifiedRule(object sender, ExerciseRule modifiedRule)
        {
            if (selectedRule != null)
            {
                int index = newExercise.Rules.IndexOf(selectedRule);
                newExercise.Rules[index] = modifiedRule;
                listBoxRules.Items[index] = $"Rule number {index + 1}";
            }

            selectedRule = null;
            btnModifyRule.IsEnabled = false;
        }

        private void ListBoxRules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxRules.SelectedIndex >= 0)
            {
                selectedRule = newExercise.Rules[listBoxRules.SelectedIndex];
                btnModifyRule.IsEnabled = true;
                btnDeleteRule.IsEnabled = true;
            }
            else
            {
                selectedRule = null;
                btnModifyRule.IsEnabled = false;
                btnDeleteRule.IsEnabled = false;
            }
        }

        // Property for the error message, implementing INotifyPropertyChanged
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
    }
}
