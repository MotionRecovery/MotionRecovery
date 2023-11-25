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


        // Dans votre code principal
        List<Position> positionRules = new List<Position>();
        Test TestLevel = new Test();
        ExercisesReaderXML ExercisesReaderXML = new ExercisesReaderXML();

      


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


        private void TestPosition(HandState handState, Point handPosition, Point ElbowPosition, Point WirstPosition,string filepath)
        {

            if (handState == HandState.Open)
            {
                this.TestLevel.getHand = handPosition;
                this.TestLevel.getElbow = ElbowPosition;
                this.TestLevel.getWrist = WirstPosition;


            }

            //devise the elements per two (but how ?)

            
            //Write the info into the XML
            ExercisesReaderXML.WriteNewAttributes(filepath,"hand",handPosition.ToString());
            ExercisesReaderXML.WriteNewAttributes(filepath,"elbow",ElbowPosition.ToString());
            ExercisesReaderXML.WriteNewAttributes(filepath, "wrist", WirstPosition.ToString());






  
        }


    }
}
