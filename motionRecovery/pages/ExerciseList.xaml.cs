using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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

        // Asks the user to choose an exo file, if done launches the exo page, otherwise stays on this page
        private void Button_Click_ExercisePage(object sender, RoutedEventArgs e)
        {
            // Create an instance of ExercisesReaderXML to handle reading exercises
            ExercisesReaderXML exerciseReader = new ExercisesReaderXML();

            // OpenFileDialog allows the user to select a file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";

            // Show the dialog and get the result
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

            // Navigate to the ExercisePage with the selected file path
            NavigationService.Navigate(new ExercisePage(filePath));
        }


        private void Button_Click_CreateExercise(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CreateExerciseXMLPage());

        }
    }
}
