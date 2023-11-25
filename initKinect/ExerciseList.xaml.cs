using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private void Button_Click_ExercisePage(object sender, RoutedEventArgs e)
        {
            ExercisesReaderXML exerciseReader = new ExercisesReaderXML();
            // OpenFileDialog allows to a user to select a file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichiers XML (*.xml)|*.xml|Tous les fichiers (*.*)|*.*";

            bool? result = openFileDialog.ShowDialog();

            string filePath = null;

            if (result == true)
            {
                // L'utilisateur a sélectionné un fichier, obtenez le chemin du fichier
                filePath = openFileDialog.FileName;
            }
            NavigationService.Navigate(new ExercisePage(filePath));
        }
    }
}
