using System.Runtime.InteropServices;

namespace FreeFlowWindows;

internal sealed class WaveInRecorder : IDisposable
{
    private const uint WaveMapper = 0xFFFFFFFF;
    private const uint CallbackFunction = 0x00030000;
    private const uint MmWimData = 0x3C0;
    private const int BufferCount = 4;
    private const int BufferMilliseconds = 100;
    private const int SampleRate = 16000;
    private const short Channels = 1;
    private const short BitsPerSample = 16;

    private readonly object gate = new();
    private readonly List<byte> pcm = new();
    private readonly List<BufferState> buffers = new();
    private readonly NativeMethods.WaveInProc callback;
    private IntPtr handle;
    private bool recording;

    public event EventHandler<byte[]>? PcmChunkCaptured;

    public WaveInRecorder()
    {
        callback = WaveCallback;
    }

    public void Start()
    {
        lock (gate)
        {
            if (recording)
            {
                return;
            }

            pcm.Clear();
            OpenDevice();
            AllocateBuffers();
            Check(NativeMethods.waveInStart(handle), "waveInStart");
            recording = true;
        }
    }

    public byte[] Stop()
    {
        IntPtr handleToStop;
        lock (gate)
        {
            if (!recording && handle == IntPtr.Zero)
            {
                return Array.Empty<byte>();
            }

            recording = false;
            handleToStop = handle;
        }

        if (handleToStop != IntPtr.Zero)
        {
            NativeMethods.waveInStop(handleToStop);
            NativeMethods.waveInReset(handleToStop);
        }

        lock (gate)
        {
            var captured = pcm.ToArray();
            CloseDevice();
            return WavEncoder.Encode(captured, SampleRate, Channels, BitsPerSample);
        }
    }

    private void OpenDevice()
    {
        var format = new NativeMethods.WaveFormatEx
        {
            wFormatTag = 1,
            nChannels = (ushort)Channels,
            nSamplesPerSec = SampleRate,
            wBitsPerSample = (ushort)BitsPerSample,
            nBlockAlign = (ushort)(Channels * BitsPerSample / 8),
            cbSize = 0
        };
        format.nAvgBytesPerSec = format.nSamplesPerSec * format.nBlockAlign;

        Check(
            NativeMethods.waveInOpen(
                out handle,
                WaveMapper,
                ref format,
                callback,
                IntPtr.Zero,
                CallbackFunction),
            "waveInOpen");
    }

    private void AllocateBuffers()
    {
        var bytesPerBuffer = SampleRate * Channels * (BitsPerSample / 8) * BufferMilliseconds / 1000;
        for (var i = 0; i < BufferCount; i++)
        {
            var buffer = new BufferState(bytesPerBuffer);
            buffers.Add(buffer);
            Check(NativeMethods.waveInPrepareHeader(handle, buffer.HeaderPointer, (uint)Marshal.SizeOf<NativeMethods.WaveHdr>()), "waveInPrepareHeader");
            Check(NativeMethods.waveInAddBuffer(handle, buffer.HeaderPointer, (uint)Marshal.SizeOf<NativeMethods.WaveHdr>()), "waveInAddBuffer");
        }
    }

    private void WaveCallback(IntPtr hwi, uint message, IntPtr instance, IntPtr param1, IntPtr param2)
    {
        if (message != MmWimData)
        {
            return;
        }

        lock (gate)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var header = Marshal.PtrToStructure<NativeMethods.WaveHdr>(param1);
            if (recording && header.dwBytesRecorded > 0)
            {
                var bytes = new byte[header.dwBytesRecorded];
                Marshal.Copy(header.lpData, bytes, 0, bytes.Length);
                pcm.AddRange(bytes);
                PcmChunkCaptured?.Invoke(this, bytes);
                NativeMethods.waveInAddBuffer(handle, param1, (uint)Marshal.SizeOf<NativeMethods.WaveHdr>());
            }
        }
    }

    private void CloseDevice()
    {
        foreach (var buffer in buffers)
        {
            if (handle != IntPtr.Zero)
            {
                NativeMethods.waveInUnprepareHeader(handle, buffer.HeaderPointer, (uint)Marshal.SizeOf<NativeMethods.WaveHdr>());
            }
            buffer.Dispose();
        }
        buffers.Clear();

        if (handle != IntPtr.Zero)
        {
            NativeMethods.waveInClose(handle);
            handle = IntPtr.Zero;
        }
    }

    private static void Check(uint result, string operation)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{operation} failed with MMRESULT {result}.");
        }
    }

    public void Dispose()
    {
        IntPtr handleToStop;
        lock (gate)
        {
            recording = false;
            handleToStop = handle;
        }

        if (handleToStop != IntPtr.Zero)
        {
            NativeMethods.waveInStop(handleToStop);
            NativeMethods.waveInReset(handleToStop);
        }

        lock (gate)
        {
            CloseDevice();
        }
    }

    private sealed class BufferState : IDisposable
    {
        public IntPtr DataPointer { get; }
        public IntPtr HeaderPointer { get; }

        public BufferState(int byteLength)
        {
            DataPointer = Marshal.AllocHGlobal(byteLength);
            var header = new NativeMethods.WaveHdr
            {
                lpData = DataPointer,
                dwBufferLength = (uint)byteLength
            };
            HeaderPointer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.WaveHdr>());
            Marshal.StructureToPtr(header, HeaderPointer, false);
        }

        public void Dispose()
        {
            if (HeaderPointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(HeaderPointer);
            }
            if (DataPointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(DataPointer);
            }
        }
    }
}
