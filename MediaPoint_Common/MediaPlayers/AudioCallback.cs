using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MediaPoint.Common.DirectShow.MediaPlayers;
using MediaPoint.Common.Interfaces;

namespace MediaPoint.Common.MediaPlayers
{
    [ComVisible(true)]
    public class AudioCallback : IDCDSPFilterPCMCallBack
    {

        int _numSamples;
        int _channels;
        int _frequency;

        MediaPlayerBase _owner;

        public AudioCallback(MediaPlayerBase owner)
        {
            _owner = owner;
        }

        public int PCMDataCB(IntPtr Buffer, int Length, ref int NewSize, ref TDSStream Stream)
        {
            int numSamples = Length / (Stream.Bits / 8);

            if (_owner.NumSamples != numSamples)
            {
                _owner.NumSamples = numSamples;
            }

            int samplesPerChannel = numSamples / Stream.Channels;

            float[] samples = new float[numSamples];

            bool streamdirty = false;

            if (_numSamples != numSamples ||
                _channels != Stream.Channels ||
                _frequency != Stream.Frequency)
            {
                _channels = Stream.Channels;
                _frequency = Stream.Frequency;
                _numSamples = numSamples;
                streamdirty = true;
            }

            if (streamdirty)
            {
                _owner.AudioStreamInfo = Stream;
            }

            switch (Stream.Bits)
            {
                case 8:
                    byte[] buffer8 = new byte[numSamples];
                    Marshal.Copy(Buffer, buffer8, 0, numSamples);

                    for (int j = 0; j < numSamples; j++)
                    {
                        samples[j] = buffer8[j];
                    }

                    break;
                case 16:

                    byte[] buffer16 = new byte[numSamples * 2];
                    Marshal.Copy(Buffer, buffer16, 0, numSamples * 2);

                    var window16 = (float)(255 << 8 | 255);

                    for (int j = 0; j < buffer16.Length; j += 2)
                    {
                        samples[j / 2] = (buffer16[j] << 8 | buffer16[j + 1]) / window16;
                    }
                    //if (!samples.Any(s => s < 0))
                    //{
                    //    for (int i = 0; i < samples.Length; i++)
                    //        samples[i] = (samples[i]-0.5f)*2.0f;
                    //}
                    break;
                case 24:

                    byte[] buffer24 = new byte[numSamples * 3];
                    Marshal.Copy(Buffer, buffer24, 0, numSamples * 3);

                    var window24 = (float)(255 << 16 | 255 << 8 | 255);

                    for (int j = 0; j < buffer24.Length; j += 3)
                    {
                        samples[j / 3] = (buffer24[j] << 16 | buffer24[j + 1] << 8 | buffer24[j + 2]) / window24;
                    }

                    break;
                case 32:
                    if (Stream._Float)
                    {
                        byte[] buffer32f = new byte[numSamples * 4];
                        Marshal.Copy(Buffer, buffer32f, 0, numSamples * 4);

                        for (int j = 0; j < buffer32f.Length; j += 4)
                        {
                            samples[j / 4] = System.BitConverter.ToSingle(new byte[] { buffer32f[j + 0], buffer32f[j + 1], buffer32f[j + 2], buffer32f[j + 3] }, 0);
                        }
                    }
                    else
                    {
                        byte[] buffer32 = new byte[numSamples * 4];
                        Marshal.Copy(Buffer, buffer32, 0, numSamples * 4);

                        var window32 = (float)(255 << 24 | 255 << 16 | 255 << 8 | 255);

                        for (int j = 0; j < buffer32.Length; j += 4)
                        {
                            samples[j / 4] = (buffer32[j] << 24 | buffer32[j + 1] << 16 | buffer32[j + 2] << 8 | buffer32[j + 3]) / window32;
                        }
                    }
                    break;
            }

            float[] result = new float[samplesPerChannel / 2];

            // 5.1 downmix to stereo
            //L = clamp((FL + RL + C*0.708 + LFE*0.708) / 2) 
            //R = clamp((FR + RR + C*0.708 + LFE*0.708) / 2)

            for (int i = 0; i < numSamples; i += Stream.Channels * 2)
            {
                double tmp = 0;

                for (int j = 0; j < Stream.Channels; j++)
                    tmp += samples[i + j] / Stream.Channels;

                result[i / (Stream.Channels * 2)] = GetWindowingValue((float)(tmp), i, 2048, WindowMode.wmBlackmanHarris);
            }

            _owner.FFTData = result;

            NewSize = Length;
            return 0;
        }

        public enum WindowMode
        {
            wmRectangular,
            wmBlackmanHarris,
            wmHamming,
            wmHanning,
            wmBlackman,
            wmGaussian,
            wmBartlett
        }

        public Single GetWindowingValue(Single Value, int Index, int FFTSize, WindowMode Window)
        {
            float TWO_PI = (float)(Math.PI * 2);
            float FOUR_PI = (float)(Math.PI * 4);
            double Result = Value;
            switch (Window)
            {
                case WindowMode.wmRectangular: Result = Value; break;
                case WindowMode.wmBlackmanHarris: Result = Value * (0.35875 - 0.48829 * Math.Cos(TWO_PI * (Index + 0.5) / FFTSize) + 0.14128 * Math.Cos(TWO_PI * 2 * (Index + 0.5) / FFTSize) - 0.01168 * Math.Cos(TWO_PI * 3 * (Index + 0.5) / FFTSize)); break;
                case WindowMode.wmHamming: Result = Value * (0.54 - (0.46 * Math.Cos(TWO_PI * Index / FFTSize))); break;
                case WindowMode.wmHanning: Result = Value * (0.50 - (0.50 * Math.Cos(TWO_PI * Index / FFTSize))); break;
                case WindowMode.wmBlackman: Result = Value * (0.42 - 0.50 * Math.Cos(TWO_PI * Index / FFTSize) + 0.08 * Math.Cos(FOUR_PI * Index / FFTSize)); break;
                case WindowMode.wmGaussian: Result = Value * (Math.Exp(-5.0 / (Math.Sqrt(FFTSize)) * (2 * Index - FFTSize) * (2 * Index - FFTSize))); break;
                case WindowMode.wmBartlett:

                    if (Index < (FFTSize / 2))
                        Result = Value * (2 * Index / (FFTSize - 1));
                    else
                        Result = Value * (2 - (2 * Index / (FFTSize - 1)));

                    break;
            }
            return (float)Result;
        }

        public int MediaTypeChanged(ref TDSStream Stream)
        {
            _channels = Stream.Channels;
            _frequency = Stream.Frequency;
            _owner.AudioStreamInfo = Stream;
            return 0;
        }

        public int Flush()
        {
            // nothing to flush since we always forward data each sample and dont cache
            return 0;
        }
    }
}
