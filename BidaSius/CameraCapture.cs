//----------------------------------------------------------------------------
//  Copyright (C) 2004-2016 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Linq;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using BidaSius;

namespace tarcza
{
    public partial class CameraCapture : Form
    {
        #region props

        public Form MainF { get; set; }
        private Capture _capture = null;
        private bool _captureInProgress;
        delegate void SkonczonyPrzepierdalanie(ProcessFrameResult result);
        public int threshhhhh { get; set; }
        private TargetDetails _useThisTarget = new TargetDetails();
        public TargetDetails useThisTarget { get { return _useThisTarget; } set { _useThisTarget = value; } }
        bool captureRectFlag = false;
        public int pauseTimer = 0;
        public bool useManualShotPositiong = false;
        public bool alreadyManual = false;

        #endregion

        #region public 

        public CameraCapture()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;
            ReadSettings();
            FillComPorts();

        }

        #endregion

        #region private

        private void ProcessFrame(object sender, EventArgs arg)
        {
            Mat frame = new Mat();
            _capture.Retrieve(frame);



            //   imageBox4.Image = frame;//preview

            int threshOne = 0;
            int threstwo = 0;
            int threshone1 = 0;
            int threshtwo1 = 0;


            if (this.trackThreshOne.InvokeRequired)
            {
                threshOne = (int)this.Invoke(new Func<int>(() => trackThreshOne.Value));
                threstwo = (int)this.Invoke(new Func<int>(() => trackthreshTwo.Value));
                threshone1 = (int)this.Invoke(new Func<int>(() => trackthresh3.Value));
                threshtwo1 = (int)this.Invoke(new Func<int>(() => trackthresh4.Value));

            }
            else
            {
                threshOne = trackThreshOne.Value;
                threstwo = trackthreshTwo.Value;
                threshone1 = trackthresh3.Value;
                threshtwo1 = trackthresh4.Value;
            }

            ProcessFrameResult result;
            if (!useManualShotPositiong)
                result = CaptureHelper.ProcessFrame(frame, threshOne, threstwo, threshone1, threshtwo1, useThisTarget);
            else if (useManualShotPositiong && !alreadyManual)
            {
                alreadyManual = true;
                result = CaptureHelper.ManualProcessFrame(frame, threshOne, threstwo, threshone1, threshtwo1, useThisTarget);
            }
            else
                return;

            UstawRezultat(result);
        }

        private void ProcessFromFile()
        {
            Mat frame = new Mat();

            //Load the image from file and resize it for display
            Image<Bgr, Byte> ff = new Image<Bgr, byte>(fileNameTextBox.Text);//.Resize(400, 400, Emgu.CV.CvEnum.Inter.Linear, true);
            frame = ff.Mat;


            int threshOne = 0;
            int threstwo = 0;
            int threshone1 = 0;
            int threshtwo1 = 0;



            if (this.trackThreshOne.InvokeRequired)
            {
                threshOne = (int)this.Invoke(new Func<int>(() => trackThreshOne.Value));
                threstwo = (int)this.Invoke(new Func<int>(() => trackthreshTwo.Value));
                threshone1 = (int)this.Invoke(new Func<int>(() => trackthresh3.Value));
                threshtwo1 = (int)this.Invoke(new Func<int>(() => trackthresh4.Value));
            }
            else
            {
                threshOne = trackThreshOne.Value;
                threstwo = trackthreshTwo.Value;
                threshone1 = trackthresh3.Value;
                threshtwo1 = trackthresh4.Value;
            }




            var result = CaptureHelper.ProcessFromFile(frame, threshOne, threstwo, threshone1, threshtwo1, useThisTarget);
            UstawRezultat(result);
            imageBox1.Image = result.TargetScanWithResult;
            imageBox2.Image = result.TargetMarked;

            imageBox3.Image = result.GrSmootWarped;


            imageBox4.Image = result.SmOryCanny;
            //imageBox1.Image = result.TargetMarked;

        }

        private void ScrollPaper()
        {
            if (comboComPorts.Items.Count == 0)
                return;

            System.IO.Ports.SerialPort myPort = new System.IO.Ports.SerialPort(comboComPorts.SelectedValue.ToString(), 9600);
            if (myPort.IsOpen == false) //if not open, open the port
                myPort.Open();
            myPort.Write("on");
            myPort.Close();
        }

        private void GetTargetRect()
        {
            TargetDetails td = new TargetDetails();
            td.TL = new Point((int)textBoxTLX.Value, (int)textBoxTL.Value);
            td.TR = new Point((int)textBoxTRX.Value, (int)textBoxTR.Value);
            td.BR = new Point((int)textBoxBRX.Value, (int)textBoxBR.Value);
            td.BL = new Point((int)textBoxBLX.Value, (int)textBoxBL.Value);

            td.TargetRect = new PointF[] {
                td.TL,
                td.TR,
                td.BR,
                td.BL
            };

            td.BlackR = (int)numBLCKradius.Value;
            td.BlackCenter = new Point((int)numBLCx.Value, (int)numBLCy.Value);
            useThisTarget = td;


            SaveSettings(td);
        }

        private static void SaveSettings(TargetDetails td)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("AppSettings.bin",
                FileMode.OpenOrCreate,
                FileAccess.Write, FileShare.Read);
            formatter.Serialize(stream, td);
            stream.Close();
        }

        private void FillComPorts()
        {

            List<String> allPorts = new List<String>();
            foreach (String portName in System.IO.Ports.SerialPort.GetPortNames())
            {
                allPorts.Add(portName);
            }
            comboComPorts.DataSource = allPorts;

        }

        private void ReadSettings()
        {
            TargetDetails td = new TargetDetails();
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("AppSettings.bin",
                                      FileMode.OpenOrCreate,
                                      FileAccess.Read,
                                      FileShare.Read);
            if (stream.Length == 0)
            {
                td.BL = new Point(283, 671);
                td.BR = new Point(1038, 675);
                td.TL = new Point(350, 28);
                td.TR = new Point(975, 39);
                td.BlackCenter = new Point(374, 374);
                td.BlackR = 133;
                td.TargetRect = new PointF[4];
                td.TargetRect[0] = new PointF(350, 28);
                td.TargetRect[1] = new PointF(975, 39);
                td.TargetRect[2] = new PointF(1038, 675);
                td.TargetRect[3] = new PointF(283, 671);

            }
            else
            {
                td = (TargetDetails)formatter.Deserialize(stream);
            }
            stream.Close();
            SaveSettings(td);
            useThisTarget = td;
            FillSettingsToGui(td);

        }

        private void FillSettingsToGui(TargetDetails td)
        {
            textBoxTLX.Value = td.TL.X;
            textBoxTL.Value = td.TL.Y;

            textBoxTRX.Value = td.TR.X;
            textBoxTR.Value = td.TR.Y;

            textBoxBRX.Value = td.BR.X;
            textBoxBR.Value = td.BR.Y;

            textBoxBLX.Value = td.BL.X;
            textBoxBL.Value = td.BL.Y;

            numBLCKradius.Value = (int)td.BlackR;
            numBLCx.Value = td.BlackCenter.X;
            numBLCy.Value = td.BlackCenter.Y;
        }

        private void InitCamera()
        {
            try
            {
                _capture = new Capture((int)numericCameraNo.Value);

                // _capture.SetCaptureProperty(CapProp.FrameHeight, 768);
                //_capture.SetCaptureProperty(CapProp.FrameWidth, 1024);

                _capture.SetCaptureProperty(CapProp.FrameHeight, 1080);
                _capture.SetCaptureProperty(CapProp.FrameWidth, 1920);

                _capture.ImageGrabbed += ProcessFrame;

                // LeftCameraImageCapture = new Capture(CaptureType.DShow);

                //// Get Frames from Left Camera...
                //var mm = LeftCameraImageCapture.QueryFrame();

                //// Set the Resolution of the Camera...
                //LeftCameraImageCapture.SetCaptureProperty(CapProp.FrameWidth, 1024);
                //LeftCameraImageCapture.SetCaptureProperty(CapProp.FrameHeight, 768);

                //// Initialize the Left Camera Frame Capture Timer...
                //Application.Idle += new EventHandler(ProcessFrame);
                // var mm = LeftCameraImageCapture.QueryFrame();


            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }

        private void UstawRezultat(ProcessFrameResult result)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.imageBox1.InvokeRequired)
            {
                SkonczonyPrzepierdalanie d = new SkonczonyPrzepierdalanie(UstawRezultat);
                this.Invoke(d, new object[] { result });
            }
            else
            {
                if (captureRectFlag)
                {
                    FillSettingsToGui(result.Target);
                }

                captureImageBox.Image = result.Oryginal;
                grayscaleImageBox.Image = result.SmoothedOryginal;
                cannyImageBox.Image = result.SmOryCanny;
                smoothedGrayscaleImageBox.Image = result.FoundKontur;

                imageBox1.Image = result.TargetMarked;
                imageBox2.Image = result.Warped;
                imageBox3.Image = result.WarpedTargetCanny;
                imageBox4.Image = result.TargetScanWithResult;



                MainForm mf = (MainForm)MainF;


                if (mf!= null && result.Shot != null)
                {
                    var lastshot = mf.Shots.LastOrDefault();
                    alreadyManual = false;
                    useManualShotPositiong = false;
                    buttonPauseAndSelect.Enabled = true;
                    if (lastshot != null && (result.Shot.Time - lastshot.Time) < (TimeSpan.TicksPerSecond * 4))
                        return;
                    mf.Shots.Add(result.Shot);
                    result.TargetScanWithResult?.Save("C:\\Users\\mjordanek\\Desktop\\imagesSius\\" + DateTime.Now.Ticks.ToString() + ".jpg");
                    result.Warped?.Save("C:\\Users\\mjordanek\\Desktop\\imagesSius\\" + DateTime.Now.Ticks.ToString() + "_oryg.jpg");
                    //MessageBox.Show("zarejestrowane " + result.shot.Value.ToString());
                    //  DialogResult result1 = MessageBox.Show("zarejestrowane " + result.shot.Value.ToString(), "czekaj", MessageBoxButtons.YesNo);
                    ScrollPaper();
                    mf.RefreshTarget();
                   
                }
             

            }
        }

        private void ReleaseData()
        {
            _capture?.Dispose();
        }

        #endregion

        #region events

        private void CaptureButtonClick(object sender, EventArgs e)
        {
            if (_capture == null)
                InitCamera();

            if (_captureInProgress)
            {  //stop the capture
                captureButton.Text = "Start Capture";
                _capture.Pause();
                _capture.Stop();
                _capture.Dispose();
                _capture = null;
            }
            else
            {
                //start the capture
                captureButton.Text = "Stop";
                _capture.Start();
            }

            _captureInProgress = !_captureInProgress;

        }

        private void trackThreshOne_ValueChanged(object sender, EventArgs e)
        {
            label12.Text = trackThreshOne.Value.ToString();
            threshhhhh = trackThreshOne.Value;
            //   ProcessFrame(null, null);
        }

        private void trackthreshTwo_ValueChanged(object sender, EventArgs e)
        {
            label11.Text = trackthreshTwo.Value.ToString();
            //   ProcessFrame(null, null);
        }

        private void trackthresh4_ValueChanged(object sender, EventArgs e)
        {
            label10.Text = trackthresh4.Value.ToString();
            //     ProcessFrame(null, null);
        }

        private void trackthresh3_ValueChanged(object sender, EventArgs e)
        {
            label9.Text = trackthresh3.Value.ToString();
            //     ProcessFrame(null, null);
        }

        private void textBoxTLX_ValueChanged(object sender, EventArgs e)
        {
            GetTargetRect();
        }

        private void buttonTestCom_Click(object sender, EventArgs e)
        {
            ScrollPaper();

        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void loadImageButton_Click_1(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
            {
                fileNameTextBox.Text = openFileDialog1.FileName;
            }
        }

        private void fileNameTextBox_TextChanged(object sender, EventArgs e)
        {
            ProcessFromFile();
        }

        private void buttonPauseAndSelect_Click(object sender, EventArgs e)
        {
            useManualShotPositiong = true;
            buttonPauseAndSelect.Enabled = false;
        }

        #endregion

    }



}
