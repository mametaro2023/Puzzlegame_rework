using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace MyPuzzleGame.SystemUtils
{
    public class SoundManager : IDisposable
    {
        private readonly ALDevice _device;
        private readonly ALContext _context;
        private readonly Dictionary<string, int> _soundBuffers = new Dictionary<string, int>();
        private bool _disposed = false;
        private float _volume = 0.5f;
        private bool _isMuted = false;

        public SoundManager()
        {
            _device = ALC.OpenDevice(null);
            _context = ALC.CreateContext(_device, (int[]?)null);
            ALC.MakeContextCurrent(_context);
        }

        public void LoadSound(string name, string path)
        {
            if (_soundBuffers.ContainsKey(name))
            {
                return;
            }

            int buffer = AL.GenBuffer();

            using (var stream = new FileStream(path, FileMode.Open))
            using (var reader = new BinaryReader(stream))
            {
                // Basic WAV file parsing
                reader.ReadChars(4); // "RIFF"
                reader.ReadInt32(); // chunk size
                reader.ReadChars(4); // "WAVE"
                reader.ReadChars(4); // "fmt "
                reader.ReadInt32(); // subchunk 1 size
                short audioFormat = reader.ReadInt16();
                short numChannels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // byte rate
                reader.ReadInt16(); // block align
                short bitsPerSample = reader.ReadInt16();
                reader.ReadChars(4); // "data"
                int dataSize = reader.ReadInt32();
                byte[] audioData = reader.ReadBytes(dataSize);

                Console.WriteLine($"Loaded WAV file: {Path.GetFileName(path)}");
                Console.WriteLine($"  - Channels: {numChannels}");
                Console.WriteLine($"  - Sample Rate: {sampleRate} Hz");
                Console.WriteLine($"  - Bits Per Sample: {bitsPerSample}");

                ALFormat format = GetSoundFormat(numChannels, bitsPerSample);
                
                GCHandle handle = GCHandle.Alloc(audioData, GCHandleType.Pinned);
                try
                {
                    AL.BufferData(buffer, format, handle.AddrOfPinnedObject(), audioData.Length, sampleRate);
                }
                finally
                {
                    handle.Free();
                }
            }

            _soundBuffers[name] = buffer;
        }

        public void PlaySound(string name)
        {
            if (_isMuted)
            {
                return;
            }

            if (_soundBuffers.TryGetValue(name, out int buffer))
            {
                int source = AL.GenSource();

                AL.Source(source, ALSourcei.Buffer, buffer);
                AL.Source(source, ALSourcef.Gain, _volume);
                AL.SourcePlay(source);

                // This is not ideal for performance, but simple.
                // A better implementation would use a pool of sources.
                System.Threading.Tasks.Task.Run(() =>
                {
                    int state;
                    do
                    {
                        AL.GetSource(source, ALGetSourcei.SourceState, out state);
                    } while ((ALSourceState)state == ALSourceState.Playing);
                    AL.DeleteSource(source);
                });
            }
        }

        public void SetVolume(float volume)
        {
            _volume = Math.Clamp(volume, 0.0f, 1.0f);
        }

        public float GetVolume()
        {
            return _volume;
        }

        public void ToggleMute()
        {
            _isMuted = !_isMuted;
        }

        public bool IsMuted()
        {
            return _isMuted;
        }

        private ALFormat GetSoundFormat(int channels, int bits)
        {
            if (channels == 1)
            {
                return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
            }
            else
            {
                return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var buffer in _soundBuffers.Values)
            {
                AL.DeleteBuffer(buffer);
            }
            _soundBuffers.Clear();

            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(_context);
            ALC.CloseDevice(_device);

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
