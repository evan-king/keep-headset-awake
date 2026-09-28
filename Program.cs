using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;


namespace keep_headset_awake
{
    public sealed class Program
    {
        [DllImport("winmm.dll")]
        private static extern int waveOutOpen(
            out IntPtr hWaveOut,
            int uDeviceID,
            ref WAVEFORMATEX lpFormat,
            WaveOutProc dwCallback,
            IntPtr dwInstance,
            int fdwOpen);

        [DllImport("winmm.dll")]
        private static extern int waveOutPrepareHeader(
            IntPtr hWaveOut,
            ref WAVEHDR lpWaveOutHdr,
            int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutWrite(
            IntPtr hWaveOut,
            ref WAVEHDR lpWaveOutHdr,
            int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutUnprepareHeader(
            IntPtr hWaveOut,
            ref WAVEHDR lpWaveOutHdr,
            int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        private static extern int waveOutReset(IntPtr hWaveOut);

        private const int WAVE_MAPPER = -1;
        private const int CALLBACK_FUNCTION = 0x00030000;
        private const int WOM_DONE = 0x3BD;

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        private delegate void WaveOutProc(
            IntPtr hWaveOut,
            uint uMsg,
            IntPtr dwInstance,
            IntPtr dwParam1,
            IntPtr dwParam2);
        
        async static Task Main(string[] args)
        {
            var configFilePath = Path.Join(Directory.GetParent(AppContext.BaseDirectory).FullName, "appsettings.json");
            var config = new BeepConfig();
            try
            {
                var configContent = File.ReadAllText(configFilePath, Encoding.UTF8);
                config = JsonSerializer.Deserialize<BeepConfig>(configContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Could not load appsettings.json!");
            }
            await new Program { myConfig = config }.RunForever();
        }

        private async Task RunForever()
        {
            var beepFrequency = myConfig.BeepFrequencyHz;
            var beepLength = TimeSpan.FromSeconds(myConfig.BeepDurationSeconds);
            var beepInterval = TimeSpan.FromSeconds(myConfig.BeepIntervalSeconds);

            Console.WriteLine($"Started: {DateTime.Now}");
            Console.WriteLine($"Frequency: {beepFrequency} Hz, Duration: {beepLength}, Interval: {beepInterval}");
            Console.WriteLine();

            while (true)
            {
                WriteStatus($"Currently beeping until {DateTime.Now + beepLength:HH:mm:ss}.");
                PlayTone(beepFrequency, (int)beepLength.TotalMilliseconds);
                WriteStatus($"Currently not beeping until {DateTime.Now + beepInterval:HH:mm:ss}.");
                await Task.Delay(beepInterval);
            }
        }

        private void WriteStatus(string text)
        {
            Console.Write($"\r{new string(' ', myPrevStatusWidth)}");
            Console.Write($"\r{text}");
            myPrevStatusWidth = text.Length;
        }

        private BeepConfig myConfig;
        private int myPrevStatusWidth = 0;
        
        private void PlayTone(int frequency, int duration)
        {
            const int sampleRate = 44100;
            const short amplitude = 3276;

            var samples = sampleRate * duration / 1000;
            var data = new short[samples];

            for (var i = 0; i < samples; i++)
                data[i] = (short)(amplitude * Math.Sin(2 * Math.PI * frequency * i / sampleRate));

            var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                var format = new WAVEFORMATEX
                {
                    wFormatTag = 1,
                    nChannels = 1,
                    nSamplesPerSec = sampleRate,
                    wBitsPerSample = 16,
                    nBlockAlign = 2,
                    nAvgBytesPerSec = sampleRate * 2
                };

                using var completed = new ManualResetEvent(false);

                WaveOutProc callback = (device, message, instance, param1, param2) =>
                {
                    if (message == WOM_DONE)
                        completed.Set();
                };

                var result = waveOutOpen(
                    out var device,
                    WAVE_MAPPER,
                    ref format,
                    callback,
                    IntPtr.Zero,
                    CALLBACK_FUNCTION);

                if (result != 0)
                    throw new InvalidOperationException($"waveOutOpen failed: {result}");

                try
                {
                    var header = new WAVEHDR
                    {
                        lpData = dataHandle.AddrOfPinnedObject(),
                        dwBufferLength = (uint)(data.Length * sizeof(short))
                    };

                    result = waveOutPrepareHeader(
                        device,
                        ref header,
                        Marshal.SizeOf<WAVEHDR>());

                    if (result != 0)
                        throw new InvalidOperationException($"waveOutPrepareHeader failed: {result}");

                    try
                    {
                        result = waveOutWrite(
                            device,
                            ref header,
                            Marshal.SizeOf<WAVEHDR>());

                        if (result != 0)
                            throw new InvalidOperationException($"waveOutWrite failed: {result}");

                        completed.WaitOne();
                    }
                    finally
                    {
                        waveOutUnprepareHeader(
                            device,
                            ref header,
                            Marshal.SizeOf<WAVEHDR>());
                    }
                }
                finally
                {
                    waveOutReset(device);
                    waveOutClose(device);
                }
            }
            finally
            {
                dataHandle.Free();
            }
        }
    }

    public sealed class BeepConfig
    {
        public int BeepFrequencyHz { get; set; } = 18000;
        public int BeepDurationSeconds { get; set; } = 60;
        public int BeepIntervalSeconds { get; set; } = 240;
    }
}
