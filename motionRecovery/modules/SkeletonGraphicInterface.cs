using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace motionRecovery
{
    internal class SkeletonGraphicInterface
    {
        private const double HandSize = 30;
        private const double JointThickness = 3;
        private const double ClipBoundsThickness = 10;

        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        private List<Tuple<JointType, JointType>> bones; // List of bones for body rendering

        public SkeletonGraphicInterface()
        {

            // Define bones as lines between two joints using collection initializer syntax
            this.bones = new List<Tuple<JointType, JointType>>
            {
                // Torso
                Tuple.Create(JointType.Head, JointType.Neck),
                Tuple.Create(JointType.Neck, JointType.SpineShoulder),
                Tuple.Create(JointType.SpineShoulder, JointType.SpineMid),
                Tuple.Create(JointType.SpineMid, JointType.SpineBase),
                Tuple.Create(JointType.SpineShoulder, JointType.ShoulderRight),
                Tuple.Create(JointType.SpineShoulder, JointType.ShoulderLeft),
                Tuple.Create(JointType.SpineBase, JointType.HipRight),
                Tuple.Create(JointType.SpineBase, JointType.HipLeft),

                // Right Arm
                Tuple.Create(JointType.ShoulderRight, JointType.ElbowRight),
                Tuple.Create(JointType.ElbowRight, JointType.WristRight),
                Tuple.Create(JointType.WristRight, JointType.HandRight),
                Tuple.Create(JointType.HandRight, JointType.HandTipRight),
                Tuple.Create(JointType.WristRight, JointType.ThumbRight),

                // Left Arm
                Tuple.Create(JointType.ShoulderLeft, JointType.ElbowLeft),
                Tuple.Create(JointType.ElbowLeft, JointType.WristLeft),
                Tuple.Create(JointType.WristLeft, JointType.HandLeft),
                Tuple.Create(JointType.HandLeft, JointType.HandTipLeft),
                Tuple.Create(JointType.WristLeft, JointType.ThumbLeft),

                // Right Leg
                Tuple.Create(JointType.HipRight, JointType.KneeRight),
                Tuple.Create(JointType.KneeRight, JointType.AnkleRight),
                Tuple.Create(JointType.AnkleRight, JointType.FootRight),

                // Left Leg
                Tuple.Create(JointType.HipLeft, JointType.KneeLeft),
                Tuple.Create(JointType.KneeLeft, JointType.AnkleLeft),
                Tuple.Create(JointType.AnkleLeft, JointType.FootLeft)
            };
        }


        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        /// <param name="dc">drawing context to draw to</param>
        public void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, Pen drawingPen, DrawingContext dc)
        {
            // Draw the bones
            foreach (var bone in bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingPen, dc);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    dc.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific bone</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        public void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, Pen drawingPen, DrawingContext dc)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            dc.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }


        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="dc">drawing context to draw to</param>
        public void DrawHand(HandState handState, Point handPosition, DrawingContext dc)
        {
            switch (handState)
            {
                case HandState.Closed:
                    dc.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    dc.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    dc.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="dc">drawing context to draw to</param>
        public void DrawClippedEdges(Body body, DrawingContext dc, double displayWidth, double displayHeight)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                dc.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, displayHeight - ClipBoundsThickness, displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                dc.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                dc.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                dc.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, displayHeight));
            }
        }

        /// <summary>
        /// Enlarges the specified joint and changes its color.
        /// </summary>
        /// <param name="jointType">The type of joint to enlarge and change color.</param>
        /// <param name="dc">Drawing context to draw to</param>
        /// <param name="jointPoints">The different positions of joints</param>
        /// <param name="enlargedJointSize">The size of the enlarged joint.</param>
        /// <param name="enlargedJointColor">The color of the enlarged joint.</param>
        public void SelectJointGraphical(JointType jointType, IDictionary<JointType, Point> jointPoints, DrawingContext dc, double enlargedJointSize, Brush enlargedJointColor)
        {
            // Check if the jointType exists in the jointPoints dictionary
            if (jointPoints.TryGetValue(jointType, out Point jointPosition))
            {
                // Update the joint size and color of the specified joint
                dc.DrawEllipse(enlargedJointColor, null, jointPosition, enlargedJointSize, enlargedJointSize);
            }
            else
            {
                Console.WriteLine("Joint not found in the jointPoints dictionary.");
            }
        }

        /// <summary>
        /// Display lines representing the wanted angle range from joint.
        /// </summary>
        /// <param name="jointType">The type of joint to display the angle for.</param>
        /// <param name="angleMin">The minimum angle of the wanted range.</param>
        /// <param name="angleMax">The maximum angle of the wanted range.</param>
        /// <param name="jointPoints">Translated positions of joints to draw.</param>
        /// <param name="dc">Drawing context to draw to.</param>
        /// <param name="lineColor">The color of the lines representing the angle range.</param>
        public void DisplayWantedAngle(JointType jointType, double angleMin, double angleMax, IDictionary<JointType, Point> jointPoints, DrawingContext dc, Color lineColor)
        {
            int lineLength = 60;
            if (jointPoints.TryGetValue(jointType, out Point jointPosition))
            {
                // Calculate the positions of the end of the lines based on the angles
                Point startAnglePoint = CalculatePointFromAngle(jointPosition, angleMin, lineLength);
                Point endAnglePoint = CalculatePointFromAngle(jointPosition, angleMax, lineLength);

                // Draw lines representing the angle range
                dc.DrawLine(new Pen(new SolidColorBrush(lineColor), 2), jointPosition, startAnglePoint);
                dc.DrawLine(new Pen(new SolidColorBrush(lineColor), 2), jointPosition, endAnglePoint);

                // Calculate the midpoint between startAnglePoint and endAnglePoint
                int midlineLength = lineLength / 2;
                Point startmidPoint = CalculatePointFromAngle(jointPosition, angleMin, midlineLength);
                Point endmidPoint = CalculatePointFromAngle(jointPosition, angleMax, midlineLength);

                // Determine the correct order for drawing the arc
                bool isClockwise = angleMax > angleMin; // Check if angleMax is greater than angleMin

                // Create the arc path
                StreamGeometry arcGeometry = new StreamGeometry();
                using (StreamGeometryContext ctx = arcGeometry.Open())
                {
                    if (isClockwise)
                    {
                        ctx.BeginFigure(startmidPoint, false, false);
                        double step = (angleMax - angleMin) / 10; // Number of points on the arc
                        for (double angle = angleMin + step; angle < angleMax; angle += step)
                        {
                            Point arcPoint = CalculatePointFromAngle(jointPosition, angle, midlineLength);
                            ctx.LineTo(arcPoint, true, true);
                        }
                        ctx.LineTo(endmidPoint, true, true);
                    }
                    else // Counter-clockwise (angleMin>angleMax)
                    {
                        ctx.BeginFigure(startmidPoint, false, false);
                        double angleMinTranslation = -(360 - angleMin); // Transfrom an positive angle in a negative angle to obtain angleMin<angleMax
                        double step = (angleMax - angleMinTranslation) / 10; // Number of points on the arc

                        for (double angle = angleMinTranslation + step; angle < angleMax; angle += step)
                        {
                            Point arcPoint = CalculatePointFromAngle(jointPosition, angle, midlineLength);
                            ctx.LineTo(arcPoint, true, true);
                        }
                        ctx.LineTo(endmidPoint, true, true);
                    }
                }

                // Draw the arc
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(lineColor), 1), arcGeometry);
            }
            else
            {
                Console.WriteLine($"Joint not found in the jointPoints dictionary: {jointType}");
            }
        }

        /// <summary>
        /// Display the current angle value next to a joint.
        /// </summary>
        /// <param name="jointType">The type of joint to display the current angle for.</param>
        /// <param name="currentAngle">The current angle to display.</param>
        /// <param name="jointPoints">Translated positions of joints to draw.</param>
        /// <param name="dc">Drawing context to draw to.</param>
        /// <param name="textColor">The color of the text displaying the current angle.</param>
        public void DisplayCurrentAngle(JointType jointType, double currentAngle, IDictionary<JointType, Point> jointPoints, DrawingContext dc, Color textColor)
        {
            if (jointPoints.TryGetValue(jointType, out Point jointPosition))
            {
                // Draw text displaying the current angle next to the joint
                FormattedText formattedText = new FormattedText(
                    $"{currentAngle:F1}°",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    12,
                    new SolidColorBrush(textColor));

                dc.DrawText(formattedText, new Point(jointPosition.X + 20, jointPosition.Y));
            }
            else
            {
                Console.WriteLine($"Joint not found in the jointPoints dictionary: {jointType}");
            }
        }


        // With a point, a angle and a distance we calculate a new point.
        private Point CalculatePointFromAngle(Point origin, double angleDegrees, double distance)
        {
            // Ensure the angle is in the range [0, 360)
            double normalizedAngle = angleDegrees % 360;

            double angleRadians = normalizedAngle * (Math.PI / 180);
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);
            double x = origin.X + distance * cos;
            double y = origin.Y - distance * sin; // I don't know whyyy, but we must use - the distance and not + (otherwise it gives a point in the other direction from a Y axis point of view)
            return new Point(x, y);
        }

    }
}
