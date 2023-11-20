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


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mainFrame.Content is ExercisePage exercicePage)
            {
                // La page d'exercice est déjà chargée, donc on change le texte du bouton et ferme la page
                mainFrame.NavigationService.Navigate(null); // Navigue vers null pour décharger la page actuelle
                btnGoToExercices.Content = "Go to exercices";
            }
            else
            {
                // La page d'exercice n'est pas chargée, alors on l'ouvre
                ExercisePage myExercicePage = new ExercisePage();
                mainFrame.NavigationService.Navigate(myExercicePage);
                btnGoToExercices.Content = "Stop the exerice";
            }
        }
    }
}
