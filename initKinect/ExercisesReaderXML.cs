using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Xml;

namespace motionRecovery
{
    // Class responsible for reading exercise data from an XML file
    internal class ExercisesReaderXML
    {
        private ParseJointType ParseJointType; // Used to parse a string to a type Joint

        // Used to read exercise data from an XML file and return an ExerciseMultiPositon object
        public ExerciseMultiPositon ReadExerciseFile(string filePath)
        {
            ExerciseMultiPositon exerciseMultiPositon = new ExerciseMultiPositon();
            ParseJointType parseJointType = new ParseJointType();

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                XmlNodeList exerciseNodes = xmlDoc.SelectNodes("//ExerciseMultiPositon");

                foreach (XmlNode exerciseNode in exerciseNodes)
                {
                    exerciseMultiPositon.Name = exerciseNode.SelectSingleNode("Name").InnerText.Trim();
                    exerciseMultiPositon.Description = exerciseNode.SelectSingleNode("Description").InnerText.Trim();

                    XmlNodeList ruleNodes = exerciseNode.SelectNodes("Rules/ExerciseRule");
                    foreach (XmlNode ruleNode in ruleNodes)
                    {
                        ExerciseRule exerciseRule = new ExerciseRule
                        {
                            PositionTime = Convert.ToDouble(ruleNode.SelectSingleNode("PositionTime").InnerText.Trim()),
                            Description = ruleNode.SelectSingleNode("Description").InnerText.Trim()
                        };

                        XmlNodeList positionNodes = ruleNode.SelectNodes("Positions/SimplePosition");
                        foreach (XmlNode positionNode in positionNodes)
                        {
                            SimplePosition simplePosition = new SimplePosition
                            {
                                Joint1 = parseJointType.ParseToJoint(positionNode.SelectSingleNode("Joint1").InnerText.Trim()),
                                Joint2 = parseJointType.ParseToJoint(positionNode.SelectSingleNode("Joint2").InnerText.Trim()),
                                AngleMin = Convert.ToDouble(positionNode.SelectSingleNode("AngleMin").InnerText.Trim()),
                                AngleMax = Convert.ToDouble(positionNode.SelectSingleNode("AngleMax").InnerText.Trim())
                            };

                            exerciseRule.Positions.Add(simplePosition);
                        }

                        exerciseMultiPositon.Rules.Add(exerciseRule);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reading the exercise file: {ex.Message}");
            }

            return exerciseMultiPositon;
        }
    }
}
