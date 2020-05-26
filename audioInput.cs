using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stickme
{
    class audioInput
    {
        WaveInEvent audioIn = new WaveInEvent();
        
        public delegate void AudioDataEvent(object sender, IEnumerable<int> samples, IEnumerable<double> levels);

        public AudioDataEvent AudioReceived;

        public audioInput(int bufferMS, int hertz, int channels, string deviceName)
        {
            var ct = WaveIn.DeviceCount;
            var map = new Dictionary<int, string>();
            for (int x = 0; x < ct; x++)
            {
                var dev = WaveIn.GetCapabilities(x);
                map.Add(x, dev.ProductName);
            }

            // subscribe to device
            audioIn.DataAvailable += AudioIn_DataAvailable; ;
            audioIn.WaveFormat = new WaveFormat(hertz, channels);
            audioIn.BufferMilliseconds = bufferMS;  // approx 30 fps
            audioIn.DeviceNumber = map.First(x => x.Value.Contains(deviceName)).Key;
        }

        private void AudioIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            var buff = new List<int>();
            var db = new List<double>();

            for (int x = 0; x < e.BytesRecorded; x = x + 2)
            {
                // get word
                var word = e.Buffer[x] + (e.Buffer[x + 1] << 8);
                buff.Add(word);
                db.Add(20.0 * Math.Log(word / 32768.0));
            }

            AudioReceived?.Invoke(sender, buff, db);
        }

        public void Start()
        {
            audioIn.StartRecording();
        }

        public void Stop()
        {
            audioIn.StopRecording();
        }
    }
}
