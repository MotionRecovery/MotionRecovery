using Microsoft.Kinect;

// Used by the program to recognize positions via Kinect skeleton
namespace motionRecovery
{
    internal class Position
    {
        public JointType Joint1 { get; set; }
        public JointType Joint2 { get; set; }
        public double AngleMin { get; set; }
        public double AngleMax { get; set; }
        public string Description { get; set; }

    }
}
