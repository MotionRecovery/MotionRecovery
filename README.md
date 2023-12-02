# MotionRecovery
Sports rehabilitation project based on Kinect for the Embodied and Augmented Interfaces course (<https://directory.unamur.be/teaching/courses/INFOM435/2023>), present in our software engineering master's degree.

## Contributors

- <https://github.com/creuther01>
- <https://github.com/djedje-cyber>
- <https://github.com/kouanga>

## Thanks

Many thanks to Professor Bruno Dumas for his course on embodied and augmented interfaces, it allowed us to learn a lot about this area. Thanks also to Maxime André, who was our assistant for this course, who gave us valuable advice for the development of this project.

## Troubleshooting

- **The kinect doesn't recognize me:** Check that the kinect is turned on and connected, normally if the kinect is not connected a message will appear on the exercise page to tell you this. It is also possible that your exercise environment is not suitable, if for example you are dressed in black and the background is also black. If you wear loose clothing, the Kinect may recognize you but less well.

- **Angle problem:** When the exercise requires you to make an angle of 90°, the movement analyzer may accept angles of 90° but also -90°. In summary, for an angle of X°, the motion analyzer will also accept -X°. For example, if you are asked to raise your arm, the program may accept the exercise if your arm is down.