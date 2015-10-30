﻿//------------------------------------------------------------------------------
// Let's touch some boxes, shall we?
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using System.Runtime.Serialization.Formatters.Binary;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 5;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private Pen skeletonPen = new Pen(Brushes.Yellow, 6);

        /// <summary>
        /// Pens used for drawing bones that are currently tracked in a wrong position
        /// </summary>
        private Pen wrongPositionPen = new Pen(Brushes.Red, 6);

        /// <summary>
        /// Pens used for drawing bones that are currently in a right position
        /// </summary>
        private Pen rightPositionPen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Static Skeleton to compare
        /// </summary>
        private Skeleton staticSkeleton;

        /// <summary>
        /// Is button pressed to capture current Position?
        /// </summary>
        private bool buttonPressed;

        /// <summary>
        /// Draw a pretty face of Master Bruce Lee
        /// </summary>
        private bool BruceLee = false;

        /// <summary>
        /// Picture info status
        /// </summary>
        private int feedbackStatus = 0;

        /// <summary>
        /// Time variables to measure elapsed time in a pose
        /// </summary>
        private int startTime;
        private int endTime;

        /// <summary>
        /// Is in the right pose?
        /// </summary>
        private bool inPose;

        /// <summary>
        /// Distance of the pose
        /// </summary>
        private double error;

        /// <summary>
        /// Time a pose must be held
        /// </summary>
        private const int holdPoseTime = 6;

        bool estasOK = false;

        bool seleccionado1 = false;

        bool modificado = false;

        int contadorObjetivo = 0;

        bool cambio = true;

        bool mostrarEsqueleto = true;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Button is not pressed and the user is not in any pose
            buttonPressed = false;
            inPose = false;
            staticSkeleton = null;

        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;

            // Create the drawing group to use
            this.drawingGroup = new DrawingGroup();

            // Create the image source
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the image source
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                //this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }

            alturaslider.Value = sensor.ElevationAngle;

        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            // Initialize skeletons
            Skeleton[] skeletons = new Skeleton[0];

            // Create our drawing context
            DrawingContext info = this.drawingGroup.Open();

            // Give feedback
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    Skeleton user = new Skeleton();
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
             
                }
            }
            info.Close();


            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawImage(this.colorBitmap, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                // If skeletons, draw
                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            skeletonPen = new Pen(VerificarDistanciaPosicion(skel), 6);
                            if (mostrarEsqueleto) this.DrawBonesAndJoints(skel, dc);

                            this.VerificarPosicionDistancia(skel,dc);
                            break;
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }
 
                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.HipCenter);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            
            // Render Joints
// -----------------------------------------------------------
            System.Windows.Media.SolidColorBrush tmp = VerificarDistanciaPosicion(skeleton);

            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {   
// -------------------------------------------------------------
                    //drawingContext.DrawEllipse(Brushes.Yellow, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                    drawingContext.DrawEllipse(tmp, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            // Move a little because yes
            return new Point(depthPoint.X, depthPoint.Y + 40);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;

            skeletonPen.Thickness = 10;
            drawPen = this.skeletonPen;
            
            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        
        private double getError(Skeleton person, Skeleton model)
        {
            double total = 0;
            if (model == null || person == null)
                return 1;
            System.Collections.IEnumerator userJoints = person.Joints.GetEnumerator();
            System.Collections.IEnumerator savedModelJoints = model.Joints.GetEnumerator();
            userJoints.MoveNext();
            savedModelJoints.MoveNext();
            bool flag = false;
            for (Joint jointUser = (Joint)userJoints.Current; !flag; flag = userJoints.MoveNext(), jointUser = (Joint)userJoints.Current)
            {
                for (Joint jointSavedModel = (Joint)savedModelJoints.Current; !flag; flag = savedModelJoints.MoveNext(), jointSavedModel = (Joint)savedModelJoints.Current)
                {
                    if (jointSavedModel.GetType().Equals(jointUser.GetType()))
                    {
                        total += System.Math.Sqrt(System.Math.Pow(jointSavedModel.Position.X - jointUser.Position.X, 2) + System.Math.Pow(jointSavedModel.Position.Y - jointUser.Position.Y, 2));
                    }
                }
                savedModelJoints.Reset();
                savedModelJoints.MoveNext();
            }
            return total;
        }


        private bool ComprobarInicial(Skeleton skel)
        {
            if (skel.Joints[JointType.Head].Position.X < skel.Joints[JointType.HandRight].Position.X)
            {
                if (skel.Joints[JointType.HandRight].Position.Y > (skel.Joints[JointType.Head].Position.Y - 0.5) && 
                    (skel.Joints[JointType.HandRight].Position.Y < (skel.Joints[JointType.Head].Position.Y +1)) ){
                    if((skel.Joints[JointType.Head].Position.X + 0.15) < skel.Joints[JointType.HandRight].Position.X){
                        return true;
                    }
                }
            }
            return false;
        }

        private System.Windows.Media.SolidColorBrush VerificarDistanciaPosicion(Skeleton skel)
        {
            bool correcto=ComprobarInicial(skel);

            if (skel.Joints[JointType.ShoulderCenter].Position.Z >= 2.0 && skel.Joints[JointType.ShoulderCenter].Position.Z <= 2.5)
            { // Distancia ok
                if(correcto) // Posición mano ok
                    return Brushes.Green;
                return Brushes.Yellow;
            }
            else
            {
                return Brushes.Red;
            }
        }
        
        bool seleccionar1(Skeleton skel, double x, double y, double z) { 
        
            bool seleccion=seleccionado1;
               
            if((SkeletonPointToScreen(skel.Joints[JointType.HandRight].Position).X>x)&&
                (SkeletonPointToScreen(skel.Joints[JointType.HandRight].Position).X<(x+50))&&
                (SkeletonPointToScreen(skel.Joints[JointType.HandRight].Position).Y>y)&&
                (SkeletonPointToScreen(skel.Joints[JointType.HandRight].Position).Y<(y+50)))
            {
                
                if (skel.Joints[JointType.HandRight].Position.Z < (skel.Joints[JointType.ShoulderCenter].Position.Z - 0.5) && !modificado)
                {
                    modificado = true;
                    seleccion = seleccion ? false : true;
                }
            }

            if (skel.Joints[JointType.HandRight].Position.Z > skel.Joints[JointType.ShoulderCenter].Position.Z - 0.4)
                modificado = false;
            return seleccion;
        
        }
        
        private void VerificarPosicionDistancia(Skeleton skel, DrawingContext drawingContext)
        {

            HacerTodo(skel, drawingContext);

            txtbox1.Text = SkeletonPointToScreen(skel.Joints[JointType.HandRight].Position).X.ToString() + "\n";
            txtbox1.Text += SkeletonPointToScreen(skel.Joints[JointType.HandRight].Position).Y.ToString() + "\n";
            txtbox1.Text += skel.Joints[JointType.HandRight].Position.Z.ToString();

            cajamano1.Text = skel.Joints[JointType.Head].Position.X.ToString() + "\n";
            cajamano1.Text += skel.Joints[JointType.Head].Position.Y.ToString() + "\n";
            cajamano1.Text += skel.Joints[JointType.Head].Position.Z.ToString();
        }

        private void HacerTodo(Skeleton skel, DrawingContext drawingContext)
        {
            double posicionZMano = 0;
            double posicionXCaja = 0;
            double posicionYCaja = 0;

            if (!(skel.Joints[JointType.ShoulderCenter].Position.Z >= 2.0 && skel.Joints[JointType.ShoulderCenter].Position.Z <= 2.5)) 
            { // Si no está en la distancia correcta
                Informar("Ponte a la distancia correcta");
                PintarRecuadroPies(skel, drawingContext, Brushes.White);
                PintarFlechas(skel, drawingContext);
            } 
            else
            { // Si sí está en la distancia correcta
                caja1.Text = "Estas bien";
                PintarRecuadroPies(skel, drawingContext, Brushes.LightGreen);
                if (!estasOK)
                { // Si no está en la posición inicial (cuidado)
                    Informar("");
                    estasOK = ComprobarInicial(skel); // Verificar si está en la posición inicial
                    posicionZMano = skel.Joints[JointType.HandRight].Position.Z; // Guardar la posición de la mano
                }
                if (estasOK)
                { // Si sí está en la posición inicial (mano levantada)
                    posicionXCaja = SkeletonPointToScreen(skel.Joints[JointType.ShoulderCenter].Position).X + 100;
                    posicionYCaja = SkeletonPointToScreen(skel.Joints[JointType.ShoulderCenter].Position).Y - 50;

                    seleccionado1 = seleccionar1(skel, posicionXCaja, posicionYCaja, posicionZMano);
                    if (seleccionado1)
                    {
                        if (cambio)
                            contadorObjetivo++;
                        Informar("Intenta deseleccionar la caja verde");
                        drawingContext.DrawRectangle(Brushes.Green, null, new Rect(SkeletonPointToScreen(skel.Joints[JointType.ShoulderCenter].Position).X + 100, SkeletonPointToScreen(skel.Joints[JointType.ShoulderCenter].Position).Y - 50, 50, 50));
                        cambio = false;
                    }                    
                    else
                    {
                        if (!cambio)
                            contadorObjetivo++;
                        Informar("Intenta seleccionar la caja roja");
                        drawingContext.DrawRectangle(Brushes.Red, null, new Rect(SkeletonPointToScreen(skel.Joints[JointType.ShoulderCenter].Position).X + 100, SkeletonPointToScreen(skel.Joints[JointType.ShoulderCenter].Position).Y - 50, 50, 50));
                        cambio = true;
                    }
                }
                else
                { // Si no está en la posición inicial
                    Informar("Levanta la mano derecha");
                }

            }
            if (contadorObjetivo >= 2)
                Informar("OBJETIVO CONSEGUIDO!!!!");
        }

        private void PintarRecuadroPies(Skeleton skel, DrawingContext drawingContext, Brush brush)
        {

            //drawingContext.DrawRectangle(Brushes.White, null, new Rect(SkeletonPointToScreen(skel.Joints[JointType.Spine].Position).X - 25, SkeletonPointToScreen(skel.Joints[JointType.FootLeft].Position).Y - 25, 50, 50));

            SolidColorBrush myBrush;
            if (brush == Brushes.LightGreen)
                myBrush = new SolidColorBrush(Color.FromArgb(127, 127, 255, 127));
            else // blanco
                myBrush = new SolidColorBrush(Color.FromArgb(127, 255, 255, 255));
            
            //EllipseGeometry gm = new EllipseGeometry(new Rect(SkeletonPointToScreen(skel.Joints[JointType.Spine].Position).X - 25, SkeletonPointToScreen(skel.Joints[JointType.FootLeft].Position).Y - 25, 50, 50));
            EllipseGeometry gm = new EllipseGeometry(new Rect(SkeletonPointToScreen(skel.Joints[JointType.Spine].Position).X - 50, SkeletonPointToScreen(skel.Joints[JointType.FootLeft].Position).Y - 25, 100, 50));
            drawingContext.DrawGeometry(myBrush, null, gm);

        }

        private void Informar(String inf)
        {
            mensajelbl.Visibility = System.Windows.Visibility.Visible ;
            mensajelbl.Content = inf;
        }

        private void PintarFlechas(Skeleton skel, DrawingContext drawingContext)
        {
            Point a = new Point( SkeletonPointToScreen(skel.Joints[JointType.Spine].Position).X     , SkeletonPointToScreen(skel.Joints[JointType.FootLeft].Position).Y - 15);
            Point b = new Point( SkeletonPointToScreen(skel.Joints[JointType.Spine].Position).X     , SkeletonPointToScreen(skel.Joints[JointType.FootLeft].Position).Y + 10);
            Point c = new Point( SkeletonPointToScreen(skel.Joints[JointType.Spine].Position).X - 10, SkeletonPointToScreen(skel.Joints[JointType.FootLeft].Position).Y - 5);
            Point d = new Point( SkeletonPointToScreen(skel.Joints[JointType.Spine].Position).X + 10, SkeletonPointToScreen(skel.Joints[JointType.FootLeft].Position).Y - 5);
            Point e = new Point( SkeletonPointToScreen(skel.Joints[JointType.Spine].Position).X - 10, SkeletonPointToScreen(skel.Joints[JointType.FootLeft].Position).Y + 5);
            Point f = new Point( SkeletonPointToScreen(skel.Joints[JointType.Spine].Position).X + 10, SkeletonPointToScreen(skel.Joints[JointType.FootLeft].Position).Y + 5);

            Pen flechaPen = new Pen(Brushes.Red, 4);

            if (skel.Joints[JointType.ShoulderCenter].Position.Z < 2.0)
            {
                caja1.Text = "Aléjate un poco";
                drawingContext.DrawLine(flechaPen, a, b);
                drawingContext.DrawLine(flechaPen, c, a);
                drawingContext.DrawLine(flechaPen, a, d);
            }
            else
            {
                caja1.Text = "Acércate un poco";
                drawingContext.DrawLine(flechaPen, a, b);
                drawingContext.DrawLine(flechaPen, e, b);
                drawingContext.DrawLine(flechaPen, b, f);
            }
        }

        private void esqueletobtn_Click(object sender, RoutedEventArgs e)
        {
            if (mostrarEsqueleto)
            {
                esqueletobtn.Content = "Activar esqueleto";
                mostrarEsqueleto = false;
            }
            else
            {
                esqueletobtn.Content = "Desactivar esqueleto";
                mostrarEsqueleto = true;
            }
        }


        private void modificar_altura(object sender, MouseButtonEventArgs e)
        {
            try
            {
                sensor.ElevationAngle = (int)alturaslider.Value;
            }
            catch { }

        }

        

    }
}