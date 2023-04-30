# Unity VFX Kinect Player
A working Kinect XEF file player based on KinectXEFTools and Unity VFX Graph.
In the KinectPlayer_DemoScene, unfold the KinectPlayer prefab and there you can set the path to a XEF file on your device.

Then you can play it in Unity:
![Playback preview in Unity](PreviewScreenshot.png)

## Everywhen
This Kinect XEF player is a cleaned up version of the XEF player created for the project [Everywhen](https://www.mariajudova.net/project/everywhen/):
A 360° video adaptation of an intermedia performance that deals with the topic of historic recurrence through movement, 3d visuals, and sound spatialization

[![Everywhen Part 1 on Vimeo](https://i.vimeocdn.com/video/1162945670-be16094e7246c0961b8b4ee84df98df68b6c2eae8a45af6cb079e16c44211f5e-d?mw=1500&mh=643&q=70=)](https://vimeo.com/562519947)

## Visual Effect Features
- **Clipping:** Clipping a certain range of the image
- **Body Index Masking:** Mask by a human body the Kinect detected (usually very good results)
- **Joint Distance Masking:** Mask elements that exceed a certain distance to a joint of the person recorded
- **Keying:** A classic color key / green screen
