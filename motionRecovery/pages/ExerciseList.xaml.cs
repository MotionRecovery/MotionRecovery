using Microsoft.Win32;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace motionRecovery
{
    /// <summary>
    /// Logique d'interaction pour ExerciseList.xaml
    /// </summary>
    public partial class ExerciseList : Page
    {
        public ExerciseList()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Cache la page précédente en la retirant de l'historique de navigation
            if (NavigationService.CanGoBack)
            {
                NavigationService.RemoveBackEntry();
            }
        }

        // Asks the user to choose an exo file, if done launches the exo page with the exercise in parameter, otherwise stays on this page
        private void Button_Click_ExercisePage(object sender, RoutedEventArgs e)
        {

            // OpenFileDialog allows the user to select a file. Show the dialog and get the result
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            bool? result = openFileDialog.ShowDialog();

            string filePath = null; 

            // Check if the user selected a file,then get the selected file path, else stay on the current page
            if (result == true)
            {
                filePath = openFileDialog.FileName;
            }
            else
            {
                return;
            }
            ExercisesReaderXML exerciseReader = new ExercisesReaderXML(); // Class that reads the XML file to convert it into our data structure "exerciseMultiPosition"
            ExerciseMultiPosition exerciseMultiPosition = new ExerciseMultiPosition();

            exerciseMultiPosition = exerciseReader.ReadExerciseFile(filePath);

            // Navigate to the ExercisePage with the selected exerciseMultiPosition
            NavigationService.Navigate(new ExercisePage(exerciseMultiPosition));
        }


        private void Button_Click_CreateExercise(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CreateExerciseXMLPage());

        }
    }
}
