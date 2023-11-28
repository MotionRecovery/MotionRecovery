using System;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace motionRecovery
{
    /// <summary>
    /// Class responsible for writing ExerciseMultiPositon objects to XML files.
    /// </summary>
    internal class ExerciseWriterXML
    {
        /// <summary>
        /// Writes the ExerciseMultiPositon object to an XML file.
        /// </summary>
        /// <param name="exercise">The ExerciseMultiPositon object to be written to the file.</param>
        /// <param name="filePath">The path where we wante to write the XML file.</param>
        public static void WriteExerciseToFile(ExerciseMultiPositon exercise, string filePath)
        {
            // Check if the exercise or file path is null
            if (exercise == null || string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("Exercise and file path cannot be null or empty.");
            }

            try
            {
                // Use XmlSerializer to serialize the ExerciseMultiPositon object to XML
                XmlSerializer serializer = new XmlSerializer(typeof(ExerciseMultiPositon));

                // Configure settings to include an XML header
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = false, // Include XML declaration
                    Encoding = Encoding.UTF8
                };

                using (XmlWriter writer = XmlWriter.Create(filePath, settings))
                {
                    // Write the XML declaration manually
                    writer.WriteStartDocument();

                    writer.WriteStartElement("ExerciseRule");

                    // Write the 'name' and 'description' elements
                    writer.WriteElementString("name", exercise.Name);
                    writer.WriteElementString("description", exercise.Description);

                    // Write each rule of the exercise
                    foreach (ExerciseRule rule in exercise.Rules)
                    {
                        WriteExerciseRule(writer, rule);
                    }

                    // End the root element
                    writer.WriteEndElement();

                    // End the document
                    writer.WriteEndDocument();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error writing exercise to XML file.", ex);
            }
        }

        /// <summary>
        /// Writes the ExerciseRule object to the XML writer, including each position, the PositionTime, and the Description.
        /// </summary>
        /// <param name="writer">The XmlWriter instance to write XML content.</param>
        /// <param name="rule">The ExerciseRule object to be written.</param>
        private static void WriteExerciseRule(XmlWriter writer, ExerciseRule rule)
        {
            writer.WriteStartElement("rule");

            // Write each position of the rule
            foreach (SimplePosition position in rule.Positions)
            {
                WritePosition(writer, position);
            }

            // Write the 'PositionTime' and 'Description' elements
            writer.WriteElementString("PositionTime", rule.PositionTime.ToString());
            writer.WriteElementString("Description", rule.Description);

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the SimplePosition object to the XML writer, including Membre1 (==Joint1), Membre2(==Joint2), AngleMin, and AngleMax.
        /// </summary>
        /// <param name="writer">The XmlWriter instance to write XML content.</param>
        /// <param name="position">The SimplePosition object to be written.</param>
        private static void WritePosition(XmlWriter writer, SimplePosition position)
        {
            writer.WriteStartElement("Position");

            // Write the 'Membre1', 'Membre2', 'AngleMin', and 'AngleMax' elements
            writer.WriteElementString("Membre1", position.Joint1.ToString());
            writer.WriteElementString("Membre2", position.Joint2.ToString());
            writer.WriteElementString("AngleMin", position.AngleMin.ToString());
            writer.WriteElementString("AngleMax", position.AngleMax.ToString());

            writer.WriteEndElement();
        }


        public void WriteAttributes(string filepath, string attributes, string value)
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


    }






}
