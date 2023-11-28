using Microsoft.Kinect;
using System.Collections.Generic;
using System.Data;

// Data Class. Used by the program to recognize positions via Kinect skeleton
namespace motionRecovery
{
    public class ExerciseMultiPositon
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ExerciseRule> Rules { get; set; }

        public ExerciseMultiPositon()
        {
            Rules = new List<ExerciseRule>();
        }
    }

    public class ExerciseRule
    {
        public double PositionTime { get; set; }
        public string Description { get; set; }
        public List<SimplePosition> Positions { get; set; }

        public ExerciseRule()
        {
            Positions = new List<SimplePosition>();
        }
    }

    public class SimplePosition
    {
        public JointType Joint1 { get; set; }
        public JointType Joint2 { get; set; }
        public double AngleMin { get; set; }
        public double AngleMax { get; set; }
    }
}
