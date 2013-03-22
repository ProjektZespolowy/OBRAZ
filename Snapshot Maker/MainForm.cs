using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Imaging.Textures;
using AForge.Imaging.ComplexFilters;

namespace Snapshot_Maker
{
    public partial class MainForm : Form
    {
        private FilterInfoCollection urzadzenia;
        private VideoCaptureDevice kamera;
        private VideoCapabilities[] parametry_video;
        private VideoCapabilities[] parametry_zdjecia;

        private SnapshotForm snapshotForm = null;

        private Bitmap zdjecie;
        private Bitmap zdjecie_binarne;


        public MainForm( )
        {
            InitializeComponent( );
        }

        // Main form is loaded
        private void MainForm_Load( object sender, EventArgs e )
        {
            // enumerate video devices
            urzadzenia = new FilterInfoCollection( FilterCategory.VideoInputDevice );

            if ( urzadzenia.Count != 0 )
            {
                // add all devices to combo
                foreach ( FilterInfo device in urzadzenia )
                {
                    devicesCombo.Items.Add( device.Name );
                }
            }
            else
            {
                devicesCombo.Items.Add( "No DirectShow devices found" );
            }

            devicesCombo.SelectedIndex = 0;

            EnableConnectionControls( false ); // dostep do zmiany jakosci photo/video
            devicesCombo.Enabled = true;
            connectButton.Enabled = true;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Image = pictureBox1.InitialImage;

        }

        // Closing the main form
        private void MainForm_FormClosing( object sender, FormClosingEventArgs e )
        {
            Disconnect( );
        }

        // Enable/disable connection related controls
        private void EnableConnectionControls( bool enable )
        {
            devicesCombo.Enabled = enable;
            videoResolutionsCombo.Enabled = enable;
            snapshotResolutionsCombo.Enabled = enable;
            connectButton.Enabled = enable;
            disconnectButton.Enabled = !enable;
            triggerButton.Enabled = ( !enable ) && ( parametry_zdjecia.Length != 0 );
        }

        // New video device is selected
        private void devicesCombo_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( urzadzenia.Count != 0 )
            {
                kamera = new VideoCaptureDevice( urzadzenia[devicesCombo.SelectedIndex].MonikerString );
                EnumeratedSupportedFrameSizes( kamera );
            }
        }

        // Collect supported video and snapshot sizes
        private void EnumeratedSupportedFrameSizes( VideoCaptureDevice kamera )
        {
            this.Cursor = Cursors.WaitCursor;

            videoResolutionsCombo.Items.Clear( );
            snapshotResolutionsCombo.Items.Clear( );

            try
            {
                parametry_video = kamera.VideoCapabilities;
                parametry_zdjecia = kamera.SnapshotCapabilities;
                kamera.DesiredFrameRate = 50;

                foreach ( VideoCapabilities capabilty in parametry_video )
                {
                    if ( !videoResolutionsCombo.Items.Contains( capabilty.FrameSize ) )
                    {
                        videoResolutionsCombo.Items.Add( capabilty.FrameSize );
                    }
                }

                foreach ( VideoCapabilities capabilty in parametry_zdjecia )
                {
                    if ( !snapshotResolutionsCombo.Items.Contains( capabilty.FrameSize ) )
                    {
                        snapshotResolutionsCombo.Items.Add( capabilty.FrameSize );
                    }
                }

                if ( parametry_video.Length == 0 )
                {
                    videoResolutionsCombo.Items.Add( "Not supported" );
                }
                if ( parametry_zdjecia.Length == 0 )
                {
                    snapshotResolutionsCombo.Items.Add( "Not supported" );
                }

                videoResolutionsCombo.SelectedIndex = 0;
                snapshotResolutionsCombo.SelectedIndex = 0;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        // On "Connect" button clicked
        private void connectButton_Click( object sender, EventArgs e )
        {
            if ( kamera != null )
            {
                if ( ( parametry_video != null ) && ( parametry_video.Length != 0 ) )
                {
                    kamera.DesiredFrameSize = (Size) videoResolutionsCombo.SelectedItem;
                }

                if ( ( parametry_zdjecia != null ) && ( parametry_zdjecia.Length != 0 ) )
                {
                    kamera.ProvideSnapshots = true;
                    kamera.DesiredSnapshotSize = (Size) snapshotResolutionsCombo.SelectedItem;
                    //kamera.SnapshotFrame += new NewFrameEventHandler( kamera_SnapshotFrame );
                }

                EnableConnectionControls( false );

                videoSourcePlayer.VideoSource = kamera;
                videoSourcePlayer.Start( );
            }
        }

        // On "Disconnect" button clicked
        private void disconnectButton_Click( object sender, EventArgs e )
        {
            Disconnect( );
        }

        // Disconnect from video device
        private void Disconnect( )
        {
            if ( videoSourcePlayer.VideoSource != null )
            {
                // stop video device
                videoSourcePlayer.SignalToStop( );
                videoSourcePlayer.WaitForStop( );
                videoSourcePlayer.VideoSource = null;

                if ( kamera.ProvideSnapshots )
                {
                    kamera.SnapshotFrame -= new NewFrameEventHandler( kamera_SnapshotFrame );
                }

                EnableConnectionControls( true );
            }
        }

        // Simulate snapshot trigger
        private void triggerButton_Click( object sender, EventArgs e )
        {
            if ( ( kamera != null ) && ( kamera.ProvideSnapshots ) )
            {
                kamera.SimulateTrigger( );
                kamera.SnapshotFrame += new NewFrameEventHandler(kamera_SnapshotFrame);
            }
        }

        // New snapshot frame is available
        private void kamera_SnapshotFrame( object sender, NewFrameEventArgs eventArgs )
        {
            // zrob zdjecie :
            Bitmap old = (Bitmap)pictureBox1.Image;
            zdjecie = (Bitmap)eventArgs.Frame.Clone();
            //pictureBox1 = new PictureBox();
            pictureBox1.Image = zdjecie;

            // greyscale :
            Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
            Bitmap temp = (Bitmap)pictureBox1.Image;
            zdjecie = (Bitmap)filter.Apply(temp);
            
            pictureBox1.Image = (Bitmap)zdjecie;
            pictureBox1.Update();
            // czyszczenie pamieci starego zdjecia
            /*
            if (old != null)
            {
                old.Dispose();
            }
             */
        }

        private void ShowSnapshot( Bitmap snapshot )
        {
            if ( InvokeRequired )
            {
                Invoke( new Action<Bitmap>( ShowSnapshot ), snapshot );
            }
            else
            {
                if ( snapshotForm == null )
                {
                    snapshotForm = new SnapshotForm( );
                    snapshotForm.FormClosed += new FormClosedEventHandler( snapshotForm_FormClosed );
                    snapshotForm.Show( );
                }

                snapshotForm.SetImage( snapshot );
            }
        }

        private void snapshotForm_FormClosed( object sender, FormClosedEventArgs e )
        {
            snapshotForm = null;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void button_hough_Click(object sender, EventArgs e)
        {
            Bitmap temp = (Bitmap)zdjecie_binarne;
            HoughCircleTransformation transformata_hougha = new HoughCircleTransformation(Convert.ToInt32(textBox1.Text));
            transformata_hougha.ProcessImage(temp);
            
            textBox3.Text = transformata_hougha.CirclesCount.ToString();
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            

            HoughCircle[] circles = transformata_hougha.GetCirclesByRelativeIntensity(0.2);
            foreach (HoughCircle circle in circles)
            {
                string s = string.Format("X = {0}, Y = {1}, I = {2} ({3})", circle.X, circle.Y, circle.Intensity, circle.RelativeIntensity);
                System.Diagnostics.Debug.WriteLine(s);
                
            }

            temp = transformata_hougha.ToBitmap();
            pictureBox2.Image = (Bitmap)temp;
        }

        private void button_progowanie_Click(object sender, EventArgs e)
        {
            Threshold progowanie = new Threshold();
            Bitmap obrazek = (Bitmap)zdjecie;
            progowanie.ThresholdValue = Convert.ToInt32(textBox2.Text);
            obrazek = progowanie.Apply(obrazek);
            pictureBox1.Image = obrazek;
            pictureBox1.Update();

            zdjecie_binarne = obrazek;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int temp = 0;
            textBox1.Text += Convert.ToString(temp++);
        }
    }
}
