using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        List<Tuple<Keys, Image>> animations = new List<Tuple<Keys, Image>>();
        Timer animation = new Timer();
        Timer t;

        // audio data
        int[] levels = new int[6];
        double[] modifiers = new double[6];
        List<double> samples = new List<double>();
        double dynamicMaxDB = double.NegativeInfinity;
        double dynamicMinDB = double.PositiveInfinity;
        audioInput listener;

        // keyboard/mouse events
        IKeyboardMouseEvents me;
        MouseButtons mousePTT;
        Keys keyboardPTT;
        long frames = 0;
        DateTime start;
        

        public stickForm()
        {
            InitializeComponent();

            start = DateTime.Now;
            t = new Timer
            {
                Interval = 5000
            };
            t.Tick += T_Tick;
            t.Start();

            // load faces
            faces[0] = resize(Image.FromFile(Properties.Settings.Default.closedImage), 640, 360);
            faces[1] = resize(Image.FromFile(Properties.Settings.Default.twentyImage), 640, 360);
            faces[2] = resize(Image.FromFile(Properties.Settings.Default.fourtyImage), 640, 360);
            faces[3] = resize(Image.FromFile(Properties.Settings.Default.sixtyImage), 640, 360);
            faces[4] = resize(Image.FromFile(Properties.Settings.Default.eightyImage), 640, 360);
            faces[5] = resize(Image.FromFile(Properties.Settings.Default.openImage), 640, 360);
            
            // load "animations"
            // really just custom frames intended to be triggered at appropriate moments
            var coffee = resize(Image.FromFile("coffee.png"), 640, 360);
            animations.Add(new Tuple<Keys, Image>(Keys.F4, coffee));

            pbOne.Image = faces[0];

            calcRange(Properties.Settings.Default.dbFloor, Properties.Settings.Default.dbMax);

            listener = new audioInput(17, 48000, 1, Properties.Settings.Default.deviceName);
            listener.AudioReceived += listener_AudioReceived;

            // setup PTT
            if (Properties.Settings.Default.enablePtt)
            {
                me = Hook.GlobalEvents();
                if (Enum.TryParse<MouseButtons>(Properties.Settings.Default.pttButton, out mousePTT))
                {
                    me.MouseDown += Me_MouseDown;
                    me.MouseUp += Me_MouseUp;
                }
            } else
            {
                listener.Start();
            }

            me = Hook.GlobalEvents();
            me.KeyDown += Me_KeyDown;
            me.KeyUp += Me_KeyUp;

            // setup initial levels
            levels[0] = -200;
            levels[1] = -150;
            levels[2] = -100;
            levels[3] = -50;
            levels[4] = 0;
            levels[5] = 50;
            
            // values chosen aribtrarily
            modifiers[0] = 0.72;
            modifiers[1] = 0.79;
            modifiers[2] = 0.85;
            modifiers[3] = 0.90;
            modifiers[4] = 0.94;
            modifiers[5] = 1.0;

            animation.Interval = 5000;
            animation.Tick += Animation_Tick;

            if(!Properties.Settings.Default.enablePtt)
            {
                // if not ptt, listen until stopped
                startListening();
            }
        }

        private void T_Tick(object sender, EventArgs e)
        {
            var fps = frames / DateTime.Now.Subtract(start).TotalSeconds;
            Debug.Write(string.Format("frames per sec: {0}\r\n", fps));
        }

        private void setFace(int index)
        {
            if(!animation.Enabled)
            {
                pbOne.Image = faces[index];
            }
        }

        private void setFace(Tuple<Keys, Image> face)
        {
            pbOne.Image = face.Item2;
            animation.Start();
        }

        #region event handling
        private void Animation_Tick(object sender, EventArgs e)
        {
            animation.Stop();
            setFace(0);
        }

        private void Me_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyData == keyboardPTT)
            {
                startListening();
            }
            if(animations.Any(ax => ax.Item1 == e.KeyData))
            {
                setFace(animations.First(ax => ax.Item1 == e.KeyData));
            }
        }

        private void Me_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.KeyData & keyboardPTT) == keyboardPTT)
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

        private void listener_AudioReceived(object sender, IEnumerable<int> buffer, IEnumerable<double> dbs)
        {
            frames++;

            var average = dbs.Where(db => !Double.IsInfinity(db) && !Double.IsNaN(db)).Average();

            // average samples over time to smooth animation (hopefully)
            // note: laziness prompts use of List<>
            samples.Add(average);
            if(samples.Count() > 6)
            {
                // pop the oldest sample out of the list
                samples.RemoveAt(0);
            }

            try
            {
                var adjustedDB = Convert.ToInt32(samples.Average());

                if (!Properties.Settings.Default.enablePtt)
                {
                    // adjust min/max here, don't wait for end of a sample group
                    var localMin = samples.Min();
                    dynamicMinDB = localMin < dynamicMinDB ? localMin : dynamicMinDB;
                    dynamicMaxDB = adjustedDB > dynamicMaxDB ? adjustedDB : dynamicMaxDB;
                    calcRange(dynamicMinDB, dynamicMaxDB);
                }
                else
                {
                    dynamicMinDB = samples.Min();
                    dynamicMaxDB = adjustedDB > dynamicMaxDB ? adjustedDB : dynamicMaxDB;
                }

                if (adjustedDB <= levels[0])
                {
                    this.setFace(0);
                }
                for (int x = 1; x < levels.Length; x++)
                {
                    if (adjustedDB <= levels[x] && adjustedDB > levels[x - 1])
                    {
                        this.setFace(x);
                    }
                }
            }
            // should probably be smarter about this, but for now I don't care
            // so far var adjustedDB = Convert.ToInt32(samples.Average()); has generated exceptions twice
            // always at the wrong moment so I haven't looked into why or how yet
            catch (Exception ex) { Debug.Write(ex.ToString()); }
        }

        private void pbOne_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion

        private void calcRange(double floor, double ceil)
        {
            var range = ceil - floor;// dynamicMaxDB - Properties.Settings.Default.dbFloor;
            var step = Math.Abs(range / 5.0);
            for (int x = 0; x < 6; x++)
            {
                levels[x] = Convert.ToInt32(Math.Floor(floor + (range * modifiers[x])));
            }
        }

        private void startListening()
        {
            listener.Start();
        }

        private void stopListening()
        {
            listener.Stop();
            setFace(0);
            
            if(Properties.Settings.Default.dynamicMax)
            {
                // recalculate levels based on previous recorded DB values
                calcRange(dynamicMinDB, dynamicMaxDB);
                dynamicMaxDB = 0.0;
            }
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
