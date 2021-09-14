using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {

        DAQ.Device NI6361;

        public Form1()
        {
            InitializeComponent();

            if (NI6361 == null)
                NI6361 = new DAQ.Device();      //build it with x86 conf.
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ;
        }

        int dataCount = 0;
        void readDataCallBack(IAsyncResult result)
        {
            if (NI6361 == null)
                return;

            string s = string.Empty;
            double[] DAQArray = NI6361.GetData(result);

            Invoke(new Action(() => { Text = "Data Received " + (dataCount++).ToString(); }));
            for (int h = 0; h < DAQArray.Length; h++)
            {
                s = DAQArray[h].ToString() + ";" + s;
            }
            Invoke(new Action(() => { textBox1.Text = s; }));

            DAQArray = null;
        }

        bool firstStart = true;
        private void button1_Click(object sender, EventArgs e)
        {
            if (firstStart)
            {
                string phyChnl = "Dev1/ai0";
                string extClockChnl = "/Dev1/PFI0";  /*  or string.Empty to asynchronous sampling    */;
                double minVoltage = -4.0F;
                double maxVoltage = 4.0F;
                double samplingRate = 100;
                int sampleCount = 10;               // (int)(0.25 /*second*/ * samplingRate);

                NI6361.Connect(phyChnl, extClockChnl, minVoltage, maxVoltage, DAQ.SamplingMode.Continuous, samplingRate, sampleCount, readDataCallBack);
                NI6361.Start();

                firstStart = false;
                button1.Text = "stop";

            }
            else
            {
                if (NI6361.IsStopped)
                {
                    NI6361.Restart();
                    button1.Text = "stop";
                }
                else
                {
                    NI6361.Stop();
                    button1.Text = "start";
                }
            }
        }
    }
}
