using System;
using NationalInstruments;
using NationalInstruments.DAQmx;

namespace DAQ
{
    public class Device:IDisposable
    {

        private AnalogWaveform<double>[] data;
        private returnPtr returStr;

        private double  _samplingRate = 0;
        private int     _samplesPerChannel = 0;
        private bool    _stop = true;
        private bool    _DAQErrorOccured = false;

        public bool     IsStopped { get { return _stop; } }
        public double   SamplingRate { get { return _samplingRate; } }
        public int      samplesPerChannel { get { return _samplesPerChannel; } }
        public bool     DAQErrorOccured { get { return _DAQErrorOccured; } }

        public Device()
        {
            // Create a new task
            returStr.Task = new NationalInstruments.DAQmx.Task();
        }

        /* this class is modified for a single channel only, must be updated in order to work in multiple channels simultaneously*/
        public delegate void Callback(IAsyncResult result);
        public returnPtr Connect(string AnalogInChannel,string ExtClockChannel,double minVoltage,double maxVoltage, SamplingMode mode,double samplingRate,int sampleCount, Callback callbackFunc)
        {
            _samplingRate = samplingRate;
            _samplesPerChannel = sampleCount;

                returStr.Task.AIChannels.CreateVoltageChannel(AnalogInChannel, "", (AITerminalConfiguration)(-1), minVoltage, maxVoltage, AIVoltageUnits.Volts);

                // Configure timing specs    
                SampleQuantityMode _mode = (SampleQuantityMode)mode;
                returStr.Task.Timing.ConfigureSampleClock(ExtClockChannel, samplingRate, SampleClockActiveEdge.Rising, _mode, sampleCount);

            // Verify the task
            returStr.Task.Control(TaskAction.Verify);

            returStr.channelReader = new AnalogMultiChannelReader(returStr.Task.Stream);
            returStr.callBackPtr = new AsyncCallback(callbackFunc);

            returStr.channelReader.SynchronizeCallbacks = true;

            return returStr;
        }

        public void Dispose()
        {
            returStr.Task.Dispose();
        }

        public void Start()
        {
            _stop = false;
            this._DAQErrorOccured = false;

            if (this.returStr.Task == null)
                return;

            this.returStr.channelReader.BeginReadWaveform(this._samplesPerChannel, this.returStr.callBackPtr, this.returStr.Task);
        }

        public void Restart()
        {
            _stop = false;
            this._DAQErrorOccured = false;


            if (this.returStr.Task == null)
                return;

            this.returStr.channelReader.BeginMemoryOptimizedReadWaveform(this._samplesPerChannel, this.returStr.callBackPtr, this.returStr.Task, this.data);
        }

        public void Stop()
        {
            _stop = true;
            this._DAQErrorOccured = false;
        }

        /* called in the callback function */
        public double[] GetData(IAsyncResult ar)
        {
            try
            {
                this.data = this.returStr.channelReader.EndReadWaveform(ar);

                if (!this._stop)
                    Restart();
            }
            catch (Exception e)
            {
                this._DAQErrorOccured = true;
                Console.WriteLine(e.Message);
            }

            return data[0].GetRawData(0, data[0].SampleCount);
        }

    }

    public static class Channels
    {
        public static string[] ReturnChannels()
        {
            return DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External);
        }
    }

    public struct returnPtr
    {
        public AnalogMultiChannelReader channelReader;
        public AsyncCallback callBackPtr;
        public NationalInstruments.DAQmx.Task Task;
    }

    public enum SamplingMode
    {
        Continuous = SampleQuantityMode.ContinuousSamples,
        Finite = SampleQuantityMode.FiniteSamples,
        HardwareTimedSinglePoint = SampleQuantityMode.HardwareTimedSinglePoint
    }

}
