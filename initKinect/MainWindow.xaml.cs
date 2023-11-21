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
            if (mainFrame.Content is ExerciseList exerciseList)
            {
                // The exercise page is already loaded, so we change the text of the button and close the page
                mainFrame.NavigationService.Navigate(null); // Navigate to null to unload current page
            }
            else
            {
                // The exercise page is not loaded, so we open it
                ExerciseList myExerciseList = new ExerciseList();
                mainFrame.NavigationService.Navigate(myExerciseList);
            }
        }

        private void mainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {

        }
    }
}
