﻿using Gma.System.MouseKeyHook;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace stickme
{
    public partial class stickForm : Form
    {
        // images
        Image[] faces = new Image[6];

        // audio data
        WaveInEvent audioIn = new WaveInEvent();
        byte[] levels = new byte[6];
        List<double> samples = new List<double>();
        double maxDB = 0.0;
        double min = 70.0;

        // keyboard/mouse events
        IKeyboardMouseEvents me;
        MouseButtons mousePTT;
        Keys keyboardPTT;

        public stickForm()
        {
            InitializeComponent();

            // load faces
            faces[0] = resize(Image.FromFile(Properties.Settings.Default.closedImage), 640, 360);
            faces[1] = resize(Image.FromFile(Properties.Settings.Default.twentyImage), 640, 360);
            faces[2] = resize(Image.FromFile(Properties.Settings.Default.fourtyImage), 640, 360);
            faces[3] = resize(Image.FromFile(Properties.Settings.Default.sixtyImage), 640, 360);
            faces[4] = resize(Image.FromFile(Properties.Settings.Default.eightyImage), 640, 360);
            faces[5] = resize(Image.FromFile(Properties.Settings.Default.openImage), 640, 360);
            pbOne.Image = faces[0];

            // numbers chosen arbitrarily based on testing
            // once in place recalculated on the fly
            levels[0] = 70;
            levels[1] = 78;
            levels[2] = 84;
            levels[3] = 90;
            levels[4] = 96;
            levels[5] = 102;

            // subscribe to device
            audioIn.DataAvailable += AudioIn_DataAvailable;
            audioIn.WaveFormat = new WaveFormat(48000, 1);
            audioIn.BufferMilliseconds = 25;

            // setup PTT
            if(Properties.Settings.Default.enablePtt)
            {
                me = Hook.GlobalEvents();
                if (Enum.TryParse<MouseButtons>(Properties.Settings.Default.pttButton, out mousePTT))
                {
                    me.MouseDown += Me_MouseDown;
                    me.MouseUp += Me_MouseUp;
                }
                if(Enum.TryParse<Keys>(Properties.Settings.Default.pttButton, out keyboardPTT))
                {
                    me.KeyDown += Me_KeyDown;
                    me.KeyUp += Me_KeyUp;
                }
            }

        }

        #region event handling
        private void Me_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyData == keyboardPTT)
            {
                startListening();
            }
        }

        private void Me_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == keyboardPTT)
            {
                stopListening();
            }
        }

        private void Me_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == mousePTT)
            {
                stopListening();
            }
        }

        private void Me_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == mousePTT)
            {
                startListening();
            }
        }

        private void AudioIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            double average = 0.0;
            int y = 0;
            for(int x = 0; x < e.BytesRecorded; x = x + 2)
            {
                // get word
                var word = e.Buffer[x] + (e.Buffer[x + 1] << 8);

                // do the math
                var db = 20 * Math.Log(word / 32768.0);

                // only include useful samples
                if(!Double.IsInfinity(db) && !Double.IsNaN(db))
                {
                    average += db;
                    y++;
                }
            }
            average = average / Convert.ToDouble(y);

            // average samples over time to smooth animation (hopefully)
            // note: laziness prompts use of List<>
            samples.Add(average);
            if(samples.Count() > 6)
            {
                // pop the oldest sample out of the list
                samples.RemoveAt(0);
            }
            var adjustedDB = Convert.ToInt32(100 + samples.Average());
            maxDB = adjustedDB > maxDB ? adjustedDB : maxDB;

            try
            {
                if (adjustedDB <= levels[0])
                {
                    pbOne.Image = faces[0];
                }
                for (int x = 1; x < levels.Length; x++)
                {
                    if (adjustedDB <= levels[x] && adjustedDB > levels[x - 1])
                    {
                        pbOne.Image = faces[x];
                    }
                }
            }
            catch { }
        }

        private void pbOne_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion

        private void startListening()
        {
            audioIn.StartRecording();
        }

        private void stopListening()
        {
            audioIn.StopRecording();
            pbOne.Image = faces[0];

            // recalculate levels based on highest previous recorded DB
            // should help with variance due to things like mic position
            var range = maxDB - min;
            var step = range / 5.0;
            for (int x = 1; x < 6; x++)
            {
                levels[x] = Convert.ToByte(Math.Floor(min + (step * x)));
            }
            maxDB = 0.0;
        }

        // shamelessly stolen from SO
        private Image resize(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}