using System;

namespace ReverseBell.Core
{
    /// <summary>
    /// Converts DSP time into musical beat and bar positions.
    /// This class has no Unity lifecycle dependency so its timing rules can be tested.
    /// </summary>
    public sealed class BeatTimeline
    {
        public BeatTimeline(double bpm, int beatsPerBar, double startDspTime)
        {
            if (bpm <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be greater than zero.");
            }

            if (beatsPerBar <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(beatsPerBar),
                    "Beats per bar must be greater than zero.");
            }

            Bpm = bpm;
            BeatsPerBar = beatsPerBar;
            StartDspTime = startDspTime;
        }

        public double Bpm { get; }

        public int BeatsPerBar { get; }

        public double StartDspTime { get; }

        public double SecondsPerBeat => 60d / Bpm;

        public long GetBeatIndex(double dspTime)
        {
            if (dspTime < StartDspTime)
            {
                return -1;
            }

            return (long)Math.Floor((dspTime - StartDspTime) / SecondsPerBeat);
        }

        public long GetBarIndex(double dspTime)
        {
            long beatIndex = GetBeatIndex(dspTime);
            return beatIndex < 0 ? -1 : beatIndex / BeatsPerBar;
        }

        public double GetBeatProgress(double dspTime)
        {
            if (dspTime < StartDspTime)
            {
                return 0d;
            }

            double elapsedBeats = (dspTime - StartDspTime) / SecondsPerBeat;
            return elapsedBeats - Math.Floor(elapsedBeats);
        }

        public double GetDspTimeForBeat(long beatIndex)
        {
            if (beatIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(beatIndex));
            }

            return StartDspTime + (beatIndex * SecondsPerBeat);
        }
    }
}
