using Coding4Fun.Kinect.Wpf.Controls;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace KinectedLearning
{
    public partial class MainWindow : Window
    {
        #region Animation properties
        private Rect screenRect;
        private const int TimerResolution = 2;
        private bool runningGameThread;
        private const double MaxFramerate = 70;
        private const double MinFramerate = 15;
        private double targetFramerate = MaxFramerate;
        private DateTime lastFrameDrawn = DateTime.MinValue;
        private DateTime predNextFrame = DateTime.MinValue;
        private double actualFrameTime;
        private int frameCount;
        #endregion

        #region Kinect buttons source flag
        private bool isHeadSourceClicked = false;
        private bool isShoulderSourceClicked = false;
        private bool isElbowSourceClicked = false;
        private bool isStomachSourceClicked = false;
        private bool isThighSourceClicked = false;
        private bool isKneeSourceClicked = false;
        private bool isFootSourceClicked = false;
        #endregion

        #region Kinect buttons targets flag
        private bool isHeadTargetClicked = false;
        private bool isShoulderTargetClicked = false;
        private bool isElbowTargetClicked = false;
        private bool isStomachTargetClicked = false;
        private bool isThighTargetClicked = false;
        private bool isKneeTargetClicked = false;
        private bool isFootTargetClicked = false;
        #endregion

        #region Kinect buttons boundries
        private static double _topBoundary;
        private static double _bottomBoundary;
        private static double _leftBoundary;
        private static double _rightBoundary;
        private static double _itemLeft;
        private static double _itemTop;
        #endregion

        #region Kinect device
        private KinectSensor _sensor;
        private Skeleton[] allSkeletons = new Skeleton[6];
        private List<HoverButton> kinectButtons;
        private bool isAnyDragged = false;
        private SoundPlayer player = new SoundPlayer();
        #endregion

        #region Ctor
        public MainWindow()
        {
            InitializeComponent();

            kinectButtons = new List<HoverButton>()
            {
                khbHeadSource,
                khbHeadTarget,
                khbShoulderSource,
                khbShoulderTarget,
                khbElbowSource,
                khbElbowTarget,
                khbStomachSource,
                khbStomachTarget,
                khbThighSource,
                khbThighTarget,
                khbKneeSource,
                khbKneeTarget,
                khbFootSource,
                khbFootTarget
            };
        }
        #endregion

        #region Window Loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                _sensor = KinectSensor.KinectSensors[0];
                if (_sensor.Status == KinectStatus.Connected)
                {
                    _sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    _sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(_sensor_AllFramesReady);

                    var parameters = new TransformSmoothParameters
                    {
                        Smoothing = 0.3f,
                        Correction = 0.0f,
                        Prediction = 0.0f,
                        JitterRadius = 1.0f,
                        MaxDeviationRadius = 0.5f
                    };
                    _sensor.SkeletonStream.Enable(parameters);

                    _sensor.Start();
                }
            }

            this.screenRect.X = 0;
            this.screenRect.Y = 0;
            this.screenRect.Width = this.TextCanvas.ActualWidth;
            this.screenRect.Height = this.TextCanvas.ActualHeight;

            TimeBeginPeriod(TimerResolution);
            var myGameThread = new Thread(this.GameThread);
            myGameThread.SetApartmentState(ApartmentState.STA);
            myGameThread.Start();

            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Lets's begin!");
        }
        #endregion

        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData != null)
                {
                    skeletonFrameData.CopySkeletonDataTo(allSkeletons);
                    Skeleton skeleton = GetPrimarySkeleton(allSkeletons);

                    if (skeleton == null)
                    {

                    }
                    else
                    {
                        Joint cursor = skeleton.Joints[JointType.HandRight];

                        TrackJoint(cursor, handCursor, LayoutRoot);

                        if (!IsEnd())
                        {
                            DragGesture(cursor, iHead, LayoutRoot, isHeadSourceClicked, isHeadTargetClicked);
                            DragGesture(cursor, iShoulder, LayoutRoot, isShoulderSourceClicked, isShoulderTargetClicked);
                            DragGesture(cursor, iElbow, LayoutRoot, isElbowSourceClicked, isElbowTargetClicked);
                            DragGesture(cursor, iStomach, LayoutRoot, isStomachSourceClicked, isStomachTargetClicked);
                            DragGesture(cursor, iThigh, LayoutRoot, isThighSourceClicked, isThighTargetClicked);
                            DragGesture(cursor, iKnee, LayoutRoot, isKneeSourceClicked, isKneeTargetClicked);
                            DragGesture(cursor, iFoot, LayoutRoot, isFootSourceClicked, isFootTargetClicked);

                            ActivateButtons(kinectButtons);
                        }
                        else
                        {
                            tbGameOverText.Visibility = System.Windows.Visibility.Visible;
                        }
                    }
                }
            }
        }

        private void DragGesture(Joint joint, FrameworkElement trackingElement, FrameworkElement container, bool isSourceClicked, bool isTargetClicked)
        {
            if (ElementCanFollow(isSourceClicked, isTargetClicked))
            {
                Point jointPoint = GetJointPoint(_sensor, joint, container.RenderSize, new Point
                    (trackingElement.ActualWidth / 2.0, trackingElement.ActualHeight / 2.0));

                Canvas.SetLeft(trackingElement, jointPoint.X);
                Canvas.SetTop(trackingElement, jointPoint.Y);
            }
        }

        private void TrackJoint(Joint joint, FrameworkElement trackingElement, FrameworkElement container)
        {
            if (joint.TrackingState == JointTrackingState.NotTracked)
            {
                trackingElement.Visibility = Visibility.Collapsed;
            }
            else
            {
                trackingElement.Visibility = Visibility.Visible;
                Point jointPoint = GetJointPoint(_sensor, joint, container.RenderSize, new Point
                    (trackingElement.ActualWidth / 2.0, trackingElement.ActualHeight / 2.0));
                Canvas.SetLeft(trackingElement, jointPoint.X);
                Canvas.SetTop(trackingElement, jointPoint.Y);
            }
        }

        private static Point GetJointPoint(KinectSensor kinectDevice, Joint joint, Size containerSize, Point offset)
        {
            DepthImagePoint point = kinectDevice.MapSkeletonPointToDepth(joint.Position, kinectDevice.DepthStream.Format);
            point.X = (int)((point.X * containerSize.Width / kinectDevice.DepthStream.FrameWidth) - offset.X);
            point.Y = (int)((point.Y * containerSize.Height / kinectDevice.DepthStream.FrameHeight) - offset.Y);

            return new Point(point.X, point.Y);
        }

        private Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;

            if (skeletons != null)
            {
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = skeletons[i];
                        }
                        else
                        {
                            if (skeleton.Position.Z > skeletons[i].Position.Z)
                            {
                                skeleton = skeletons[i];
                            }
                        }
                    }
                }
            }

            return skeleton;
        }

        private bool IsEnd()
        {
            if((isHeadSourceClicked && isHeadTargetClicked) &&
                (isShoulderSourceClicked && isShoulderTargetClicked) &&
                (isElbowSourceClicked && isElbowTargetClicked) &&
                (isStomachSourceClicked && isShoulderTargetClicked) &&
                (isThighSourceClicked && isThighTargetClicked) &&
                (isKneeSourceClicked && isKneeTargetClicked) &&
                (isFootSourceClicked && isFootTargetClicked)
                )
            {
                return true;
            }
            return false;
        }

        #region Kinect buttons helper methods
        private static void CheckButton(HoverButton button, Image thumbStick)
        {
            if (IsItemMidpointInContainer(button, thumbStick))
            {
                button.Hovering();
            }
            else
            {
                button.Release();
            }
        }

        public static bool IsItemMidpointInContainer(FrameworkElement container, FrameworkElement target)
        {
            FindValues(container, target);

            if (_itemTop < _topBoundary || _bottomBoundary < _itemTop)
            {
                return false;
            }

            if (_itemLeft < _leftBoundary || _rightBoundary < _itemLeft)
            {
                return false;
            }

            return true;
        }

        private static void FindValues(FrameworkElement container, FrameworkElement target)
        {
            var containerTopLeft = container.PointToScreen(new Point());
            var itemTopLeft = target.PointToScreen(new Point());

            _topBoundary = containerTopLeft.Y;
            _bottomBoundary = _topBoundary + container.ActualHeight;
            _leftBoundary = containerTopLeft.X;
            _rightBoundary = _leftBoundary + container.ActualWidth;

            _itemLeft = itemTopLeft.X + (target.ActualWidth / 2);
            _itemTop = itemTopLeft.Y + (target.ActualHeight / 2);
        }
        #endregion

        #region Kinect source buttons click events
        private void khbHeadSource_Click(object sender, RoutedEventArgs e)
        {
            if (!isAnyDragged)
            {
                HideButtonShowImage(khbHeadSource, iHead);
                isHeadSourceClicked = true;
                isAnyDragged = true;
                PlaySound("click");
            }
        }

        private void khbShoulderSource_Click(object sender, RoutedEventArgs e)
        {
            if (!isAnyDragged)
            {
                HideButtonShowImage(khbShoulderSource, iShoulder);
                isShoulderSourceClicked = true;
                isAnyDragged = true;
                PlaySound("click");
            }
        }

        private void khbElbowSource_Click(object sender, RoutedEventArgs e)
        {
            if (!isAnyDragged)
            {
                HideButtonShowImage(khbElbowSource, iElbow);
                isElbowSourceClicked = true;
                isAnyDragged = true;
                PlaySound("click");
            }
        }

        private void khbStomachSource_Click(object sender, RoutedEventArgs e)
        {
            if (!isAnyDragged)
            {
                HideButtonShowImage(khbStomachSource, iStomach);
                isStomachSourceClicked = true;
                isAnyDragged = true;
                PlaySound("click");
            }
        }

        private void khbThighSource_Click(object sender, RoutedEventArgs e)
        {
            if (!isAnyDragged)
            {
                HideButtonShowImage(khbThighSource, iThigh);
                isThighSourceClicked = true;
                isAnyDragged = true;
                PlaySound("click");
            }
        }

        private void khbKneeSource_Click(object sender, RoutedEventArgs e)
        {
            if (!isAnyDragged)
            {
                HideButtonShowImage(khbKneeSource, iKnee);
                isKneeSourceClicked = true;
                isAnyDragged = true;
                PlaySound("click");
            }
        }

        private void khbFootSource_Click(object sender, RoutedEventArgs e)
        {
            if (!isAnyDragged)
            {
                HideButtonShowImage(khbFootSource, iFoot);
                isFootSourceClicked = true;
                isAnyDragged = true;
                PlaySound("click");
            }
        }
        #endregion

        #region Kinect target buttons click events
        private void khbHeadTarget_Click(object sender, RoutedEventArgs e)
        {
            if (isAnyDragged)
            {
                if (isHeadSourceClicked)
                {
                    if (!isHeadTargetClicked)
                    {
                        if (HasChild(GameCanvas, iHead))
                        {
                            SnapToTarget(GameCanvas, iHead, PointersGrid, khbHeadTarget);
                            isHeadTargetClicked = true;
                            isAnyDragged = false;
                            PlaySound("good");
                            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Great!");
                        }
                    }
                }
            }
            if (!isHeadTargetClicked)
            {
                PlaySound("wrong");
            }
        }

        private void khbShoulderTarget_Click(object sender, RoutedEventArgs e)
        {
            if (isAnyDragged)
            {
                if (isShoulderSourceClicked)
                {
                    if (!isShoulderTargetClicked)
                    {
                        if (HasChild(GameCanvas, iShoulder))
                        {
                            SnapToTarget(GameCanvas, iShoulder, PointersGrid, khbShoulderTarget);
                            isShoulderTargetClicked = true;
                            isAnyDragged = false;
                            PlaySound("good");
                            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Brilliant!");
                        }
                    }
                }
            }
            if (!isShoulderTargetClicked)
            {
                PlaySound("wrong");
            }
        }

        private void khbElbowTarget_Click(object sender, RoutedEventArgs e)
        {
            if (isAnyDragged)
            {
                if (isElbowSourceClicked)
                {
                    if (!isElbowTargetClicked)
                    {
                        if (HasChild(GameCanvas, iElbow))
                        {
                            SnapToTarget(GameCanvas, iElbow, PointersGrid, khbElbowTarget);
                            isElbowTargetClicked = true;
                            isAnyDragged = false;
                            PlaySound("good");
                            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Amazing!");
                        }
                    }
                }
            }
            if (!isElbowTargetClicked)
            {
                PlaySound("wrong");
            }
        }

        private void khbStomachTarget_Click(object sender, RoutedEventArgs e)
        {
            if (isAnyDragged)
            {
                if (isStomachSourceClicked)
                {
                    if(!isStomachTargetClicked)
                    {
                        if (HasChild(GameCanvas, iStomach))
                        {
                            SnapToTarget(GameCanvas, iStomach, PointersGrid, khbStomachTarget);
                            isStomachTargetClicked = true;
                            isAnyDragged = false;
                            PlaySound("good");
                            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Great!");
                        }                    
                    }
                }
            }
            if (!isStomachTargetClicked)
            {
                PlaySound("wrong");
            }
        }

        private void khbThighTarget_Click(object sender, RoutedEventArgs e)
        {
            if (isAnyDragged)
            {
                if (isThighSourceClicked)
                {
                    if (!isThighTargetClicked)
                    {
                        if (HasChild(GameCanvas, iThigh))
                        {
                            SnapToTarget(GameCanvas, iThigh, PointersGrid, khbThighTarget);
                            isThighTargetClicked = true;
                            isAnyDragged = false;
                            PlaySound("good");
                            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Nice!");
                        }
                    }
                }
            }
            if (!isThighTargetClicked)
            {
                PlaySound("wrong");
            }
        }

        private void khbKneeTarget_Click(object sender, RoutedEventArgs e)
        {
            if (isAnyDragged)
            {
                if (isKneeSourceClicked)
                {
                    if(!isKneeTargetClicked)
                    {
                        if (HasChild(GameCanvas, iKnee))
                        {
                            SnapToTarget(GameCanvas, iKnee, PointersGrid, khbKneeTarget);
                            isKneeTargetClicked = true;
                            isAnyDragged = false;
                            PlaySound("good");
                            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Very Good!");
                        }                    
                    }
                }
            }
            if (!isKneeTargetClicked)
            {
                PlaySound("wrong");
            }
        }

        private void khbFootTarget_Click(object sender, RoutedEventArgs e)
        {
            if (isAnyDragged)
            {
                if (isFootSourceClicked)
                {
                    if (!isFootTargetClicked)
                    {
                        if (HasChild(GameCanvas, iFoot))
                        {
                            SnapToTarget(GameCanvas, iFoot, PointersGrid, khbFootTarget);
                            isFootTargetClicked = true;
                            isAnyDragged = false;
                            PlaySound("good");
                            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Fantastic!");
                        }
                    }
                }
            }
            if (!isFootTargetClicked)
            {
                PlaySound("wrong");
            }
        }
        #endregion

        #region UIelement methods
        private bool HasChild(Canvas tree, Image im)
        {
            foreach (UIElement el in tree.Children)
            {
                Image image = el as Image;
                if (image.Name == im.Name)
                    return true;
            }
            return false;
        }

        private void SnapToTarget(Canvas source, Image obj, Grid targetContainer, HoverButton targetObject)
        {
            int rowIndex = Grid.GetRow(targetObject);
            int colIndex = Grid.GetColumn(targetObject);

            source.Children.Remove(obj);
            Grid.SetRow(obj, rowIndex);
            Grid.SetColumn(obj, colIndex);
            targetContainer.Children.Add(obj);
        }

        private void ActivateButtons(List<HoverButton> buttonsList)
        {
            foreach (HoverButton hb in buttonsList)
            {
                CheckButton(hb, handCursor);
            }
        }

        private void HideButtonShowImage(HoverButton buttonSource, Image trackingImage)
        {
            buttonSource.Visibility = System.Windows.Visibility.Collapsed;
            trackingImage.Visibility = System.Windows.Visibility.Visible;
        }

        private bool ElementCanFollow(bool isSourceClicked, bool isTargetClicked)
        {
            if ((isSourceClicked && !isTargetClicked))
                return true;
            else return false;
        }
        #endregion

        #region Music
        private void PlaySound(string fileName)
        {
            player.SoundLocation = "Music\\" + fileName + ".wav";
            try
            {
                player.Load();
                player.Play();
            }
            catch (System.IO.FileNotFoundException ex)
            {
                MessageBox.Show("Brak pliku dźwiękowego: \n" + ex.Message);
            }
        }
        #endregion

        #region Window Closed
        private void Window_Closed(object sender, EventArgs e)
        {
            if (_sensor != null)
            {
                _sensor.Stop();
            }
            this.runningGameThread = false;
        }
        #endregion

        #region Extern methods
        [DllImport("Winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern int TimeBeginPeriod(uint period);
        #endregion

        #region GameThred methods
        private void GameThread()
        {
            this.runningGameThread = true;
            this.predNextFrame = DateTime.Now;
            this.actualFrameTime = 1000.0 / this.targetFramerate;

            // Try to dispatch at as constant of a framerate as possible by sleeping just enough since
            // the last time we dispatched.
            while (this.runningGameThread)
            {
                // Calculate average framerate.  
                DateTime now = DateTime.Now;
                if (this.lastFrameDrawn == DateTime.MinValue)
                {
                    this.lastFrameDrawn = now;
                }

                double ms = now.Subtract(this.lastFrameDrawn).TotalMilliseconds;
                this.actualFrameTime = (this.actualFrameTime * 0.95) + (0.05 * ms);
                this.lastFrameDrawn = now;

                // Adjust target framerate down if we're not achieving that rate
                this.frameCount++;
                if ((this.frameCount % 100 == 0) && (1000.0 / this.actualFrameTime < this.targetFramerate * 0.92))
                {
                    this.targetFramerate = Math.Max(MinFramerate, (this.targetFramerate + (1000.0 / this.actualFrameTime)) / 2);
                }

                if (now > this.predNextFrame)
                {
                    this.predNextFrame = now;
                }
                else
                {
                    double milliseconds = this.predNextFrame.Subtract(now).TotalMilliseconds;
                    if (milliseconds >= TimerResolution)
                    {
                        Thread.Sleep((int)(milliseconds + 0.5));
                    }
                }

                this.predNextFrame += TimeSpan.FromMilliseconds(1000.0 / this.targetFramerate);

                this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<int>(this.HandleGameTimer), 0);
            }
        }

        private void HandleGameTimer(int param)
        {
            TextCanvas.Children.Clear();

            FlyingText.Draw(TextCanvas.Children);
        }
        #endregion
    }
}