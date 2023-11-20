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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
        }



        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (mainFrame.Content is ExerciseList exerciceList)
            {
                // La page d'exercice est déjà chargée, donc on change le texte du bouton et ferme la page
                mainFrame.NavigationService.Navigate(null); // Navigue vers null pour décharger la page actuelle
            }
            else
            {
                // La page d'exercice n'est pas chargée, alors on l'ouvre
                ExerciseList myExerciceList = new ExerciseList();
                mainFrame.NavigationService.Navigate(myExerciceList);
            }
        }

        private void mainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {

        }
    }
}
