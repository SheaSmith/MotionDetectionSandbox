using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace MotionDetectionSandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            var afWindow = new Window("Annotated Frame");
            var cdWindow = new Window("Contour Delta");

            VideoCapture capture = new VideoCapture("rtsp://10.0.0.104:554/1/h264major");

            int frameIndex = 0;
            Mat lastFrame = new Mat();
            VideoWriter writer = null;

            while (capture.IsOpened())
            {
                Mat frame = new Mat();

                if (!capture.Read(frame))
                    break;

                Mat grayFrame, dilatedFrame, edges, deltaCopyFrame = new Mat();
                Mat deltaFrame = new Mat();

                try
                {
                    frame = frame.Resize(new Size(0, 0), 0.33, 0.33);
                }
                catch (Exception e)
                {

                }
                grayFrame = frame.CvtColor(ColorConversionCodes.BGR2GRAY);
                grayFrame = grayFrame.GaussianBlur(new Size(21, 21), 0);

                if (frameIndex == 0)
                {
                    frameIndex++;

                    afWindow.Move(0, 0);
                    cdWindow.Move(0, grayFrame.Size().Height);

                    string fileName = "C:\\temp\\capture.avi";

                    string fcc = capture.FourCC;
                    double fps = capture.Get(CaptureProperty.Fps);

                    Size frameSize = new Size(grayFrame.Size().Width, grayFrame.Size().Height);

                    writer = new VideoWriter(fileName, fcc, fps, frameSize);
                    Console.Out.WriteLine("Frame Size = " + grayFrame.Size().Width + " x " + grayFrame.Size().Height);

                    if (!writer.IsOpened())
                    {
                        Console.Out.WriteLine("Error Opening Video File For Write");
                        return;
                    }

                    lastFrame = grayFrame;
                    continue;
                }
                else if (frameIndex % 50 == 0)
                {
                    frameIndex = 0;
                    lastFrame = grayFrame;
                }

                frameIndex++;

                Cv2.Absdiff(lastFrame, grayFrame, deltaFrame);
                Cv2.Threshold(deltaFrame, deltaFrame, 50, 255, ThresholdTypes.Binary);

                int iterations = 2;
                Cv2.Dilate(deltaFrame, deltaFrame, new Mat(), new Point(), iterations);

                Point[][] contours;
                HierarchyIndex[] hierarchy;

                Cv2.FindContours(deltaFrame, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new Point(0, 0));

                var countorsPoly = new Point[contours.Length][];
                List<Rect> boundRect = new List<Rect>();
                List<Point2f> center = new List<Point2f>();
                List<float> radius = new List<float>();

                for (int i = 0; i < contours.Length; i++)
                {
                    countorsPoly[i] = Cv2.ApproxPolyDP(contours[i], 3, true);
                    if (countorsPoly.Length != 0)
                    {
                        boundRect.Insert(i, Cv2.BoundingRect(countorsPoly[i]));
                        Cv2.MinEnclosingCircle(countorsPoly[i], out Point2f centerObj, out float radiusObj);
                        center.Insert(i, centerObj);
                        radius.Insert(i, radiusObj);
                    }
                }

                for (int i = 0; i < contours.Length; i++)
                {
                    if (countorsPoly.Length != 0)
                    {
                        Scalar color = new Scalar(54, 67, 244);
                        //Cv2.DrawContours(frame, countorsPoly, i, color, 1, LineTypes.Link8, new HierarchyIndex[] { }, 0, new Point());
                        Cv2.Rectangle(frame, boundRect[i].TopLeft, boundRect[i].BottomRight, color, 2, LineTypes.Link8, 0);
                        //Cv2.Circle(frame, (int)center[i].X, (int)center[i].Y, (int)radius[i], color, 2, LineTypes.Link8, 0);
                    }
                }

                afWindow.ShowImage(frame);
                cdWindow.ShowImage(deltaFrame);

                writer.Write(frame);

                switch(Cv2.WaitKey(1))
                {
                    case 27:
                        capture.Release();
                        writer.Release();
                        return;
                }
            }
        }
    }
}
