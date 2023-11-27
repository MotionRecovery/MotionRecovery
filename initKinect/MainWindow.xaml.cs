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


        private void TestPositionForThreeMembers(Point handPosition, Point ElbowPosition, Point WirstPosition,string filepath)
        {


            double angleBetweenHandAndElbow;
            double angleBetweenElbowAndWrist;


            //get the new angle
            angleBetweenHandAndElbow = CalculateAngleWithDouble(handPosition.Y, handPosition.X,ElbowPosition.Y,ElbowPosition.X);
            angleBetweenElbowAndWrist = CalculateAngleWithDouble(ElbowPosition.Y, ElbowPosition.X, WirstPosition.Y, WirstPosition.X);


            

            ExercisesReaderXML.WriteAttributes(filepath, "AngleMinMember3Member2", angleBetweenElbowAndWrist.ToString());

            angleBetweenElbowAndWrist += 10;

            ExercisesReaderXML.WriteAttributes(filepath, "AngleMaxMember3Member2", angleBetweenElbowAndWrist.ToString());


            ExercisesReaderXML.WriteAttributes(filepath, "AngleMinMember2Member1", angleBetweenHandAndElbow.ToString());

            angleBetweenHandAndElbow += 10;

            ExercisesReaderXML.WriteAttributes(filepath, "AngleMaxMember2Member1", angleBetweenHandAndElbow.ToString());







        }

        private void TestPositionForTwoMembers(Point Member1,Point Member2, string filepath)
        {

            double AngleMember2AndMember1;


            AngleMember2AndMember1 = CalculateAngleWithDouble(Member2.Y, Member2.X, Member1.Y, Member1.X);


            ExercisesReaderXML.WriteAttributes(filepath, "AngleMin", AngleMember2AndMember1.ToString());
            
            AngleMember2AndMember1 += 10;

            ExercisesReaderXML.WriteAttributes(filepath, "AngleMax", AngleMember2AndMember1.ToString());



        }



        private double CalculateAngleWithDouble(double Point1,double Point2,double Point3,double Point4)
        {
            double deltaY = Point2 - Point3;    
            double deltaX = Point2 - Point4;
            double angleRad = Math.Atan2(deltaY, deltaX); // Use arc tangent to calculate angle
            double angleDegrees = angleRad * (180.0 / Math.PI); // Translated from radian to degree
            angleDegrees = angleDegrees * 1.5;

            return angleDegrees;
        }


    }
}
