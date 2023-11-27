using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;

namespace motionRecovery
{
    // Class responsible for reading exercise data from an XML file
    internal class ExercisesReaderXML
    {
        // Used to read exercise data from an XML file and return a list of positions
        public List<Position> ReadExerciseFile(string filePath)
        {
            List<Position> positionList = new List<Position>();

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                XmlNodeList positionNodes = xmlDoc.SelectNodes("//Position");

                foreach (XmlNode positionNode in positionNodes)
                {
                    Position myPosition = new Position();

                    myPosition.Joint1 = ParseJointType(positionNode.SelectSingleNode("Membre1").InnerText.Trim());
                    myPosition.Joint2 = ParseJointType(positionNode.SelectSingleNode("Membre2").InnerText.Trim());
                    myPosition.AngleMin = Convert.ToDouble(positionNode.SelectSingleNode("AngleMin").InnerText.Trim());
                    myPosition.AngleMax = Convert.ToDouble(positionNode.SelectSingleNode("AngleMax").InnerText.Trim());
                    myPosition.PositionTime = Convert.ToDouble(positionNode.SelectSingleNode("PositionTime").InnerText.Trim());
                    myPosition.Description = positionNode.SelectSingleNode("Description").InnerText.Trim();

                    positionList.Add(myPosition);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reading the exercise file: {ex.Message}");
            }

            return positionList;
        }




        public void WriteNewAttributes(string filePath, string attributes, string value)
        {
            XDocument doc = XDocument.Load(filePath);
            XElement school = doc.Element("Position");
            school.Add(new XElement("Intermidiate",
                       new XElement(attributes, value)));
            doc.Save(filePath);

        }


        public void WriteAttributes(string filepath,string attributes, string value)
        {

            try
            {
                // Load the XML file
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filepath);

                // Select the node where you want to add/modify the attribute
                XmlNode node = xmlDoc.SelectSingleNode("/Position/"); // Update the XPath accordingly

                // Check if the node exists
                if (node != null)
                {
                    // Add or update the attribute
                    XmlAttribute attribute = node.Attributes[attributes];
                    if (attribute == null)
                    {
                        // If the attribute doesn't exist, create it
                        attribute = xmlDoc.CreateAttribute(attributes);
                        node.Attributes.Append(attribute);
                    }

                    // Set the attribute value
                    attribute.Value = value;
                }
                else
                {
                    throw new InvalidOperationException("Node not found");
                }

                // Save the modified XML document
                xmlDoc.Save(filepath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }


        }


        // Method to parse a string representation of JointType
        private JointType ParseJointType(string jointTypeName)
        {
            switch (jointTypeName)
            {
                case "SpineBase":
                    return JointType.SpineBase;
                case "SpineMid":
                    return JointType.SpineMid;
                case "SpineShoulder":
                    return JointType.SpineShoulder;
                case "Neck":
                    return JointType.Neck;
                case "Head":
                    return JointType.Head;
                case "ShoulderLeft":
                    return JointType.ShoulderLeft;
                case "ElbowLeft":
                    return JointType.ElbowLeft;
                case "WristLeft":
                    return JointType.WristLeft;
                case "HandLeft":
                    return JointType.HandLeft;
                case "ShoulderRight":
                    return JointType.ShoulderRight;
                case "ElbowRight":
                    return JointType.ElbowRight;
                case "WristRight":
                    return JointType.WristRight;
                case "HandRight":
                    return JointType.HandRight;
                case "HipLeft":
                    return JointType.HipLeft;
                case "KneeLeft":
                    return JointType.KneeLeft;
                case "AnkleLeft":
                    return JointType.AnkleLeft;
                case "FootLeft":
                    return JointType.FootLeft;
                case "HipRight":
                    return JointType.HipRight;
                case "KneeRight":
                    return JointType.KneeRight;
                case "AnkleRight":
                    return JointType.AnkleRight;
                case "FootRight":
                    return JointType.FootRight;

                // Add more cases as needed for other joints
                default:
                    throw new ArgumentException($"Unknown joint type: {jointTypeName}");
            }
        }
    }
}
