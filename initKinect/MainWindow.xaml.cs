namespace motionRecovery
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Microsoft.Win32;
    using System.Xml;


    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
        }

        private void mainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
        }

        // Used to load the page ExerciseList when MainWindows is loaded.
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ExerciseList myExerciseList = new ExerciseList();
            mainFrame.NavigationService.Navigate(myExerciseList);
        }



    }
}
