using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Xml;

namespace motionRecovery
{
    // Class responsible for reading exercise data from an XML file
    internal class ExercisesReaderXML
    {

        // Used to read exercise data from an XML file and return an ExerciseMultiPositon object
        public ExerciseMultiPosition ReadExerciseFile(string filePath)
        {
            ExerciseMultiPosition exerciseMultiPosition = new ExerciseMultiPosition();
            ParseJointType parseJointType = new ParseJointType();

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                XmlNodeList exerciseNodes = xmlDoc.SelectNodes("//ExerciseRule");

                foreach (XmlNode exerciseNode in exerciseNodes)
                {
                    exerciseMultiPosition.Name = exerciseNode.SelectSingleNode("name").InnerText.Trim();
                    exerciseMultiPosition.Description = exerciseNode.SelectSingleNode("description").InnerText.Trim();

                    XmlNodeList ruleNodes = exerciseNode.SelectNodes("//rule");
                    foreach (XmlNode ruleNode in ruleNodes)
                    {
                        ExerciseRule exerciseRule = new ExerciseRule
                        {
                            PositionTime = Convert.ToDouble(ruleNode.SelectSingleNode("PositionTime").InnerText.Trim()),
                            Description = ruleNode.SelectSingleNode("Description").InnerText.Trim()
                        };

                        XmlNodeList positionNodes = ruleNode.SelectNodes("//Position");
                        foreach (XmlNode positionNode in positionNodes)
                        {
                            SimplePosition simplePosition = new SimplePosition
                            {
                                Joint1 = parseJointType.ParseToJoint(positionNode.SelectSingleNode("Membre1").InnerText.Trim()),
                                Joint2 = parseJointType.ParseToJoint(positionNode.SelectSingleNode("Membre2").InnerText.Trim()),
                                AngleMin = Convert.ToDouble(positionNode.SelectSingleNode("AngleMin").InnerText.Trim()),
                                AngleMax = Convert.ToDouble(positionNode.SelectSingleNode("AngleMax").InnerText.Trim())
                            };

                            exerciseRule.Positions.Add(simplePosition);
                        }

                        exerciseMultiPosition.Rules.Add(exerciseRule);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reading the exercise file: {ex.Message}");
            }

            return exerciseMultiPosition;
        }
    }
}
