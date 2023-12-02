using Microsoft.Kinect;
using System;

namespace motionRecovery
{
    internal class ParseJointType
    {
        // Method to parse a string representation of JointType
        public JointType ParseToJoint(string jointTypeName)
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
