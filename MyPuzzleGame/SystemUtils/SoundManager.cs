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
            Console.WriteLine($"Attempting to play sound: {name} at default volume.");
            if (_soundBuffers.TryGetValue(name, out int buffer))
            {
                ALError error = AL.GetError(); // Clear any previous errors
                int source = AL.GenSource();
                if (AL.GetError() != ALError.NoError) Console.WriteLine($"OpenAL Error after GenSource: {AL.GetError()}");

                AL.Source(source, ALSourcei.Buffer, buffer);
                if (AL.GetError() != ALError.NoError) Console.WriteLine($"OpenAL Error after Source Buffer: {AL.GetError()}");

                AL.Source(source, ALSourcef.Gain, 1.0f); // Use default volume
                if (AL.GetError() != ALError.NoError) Console.WriteLine($"OpenAL Error after Source Gain: {AL.GetError()}");

                AL.SourcePlay(source);
                if (AL.GetError() != ALError.NoError) Console.WriteLine($"OpenAL Error after SourcePlay: {AL.GetError()}");

                Console.WriteLine("Sound played successfully.");

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
            else
            {
                Console.WriteLine($"Sound not found in buffer: {name}");
            }
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
