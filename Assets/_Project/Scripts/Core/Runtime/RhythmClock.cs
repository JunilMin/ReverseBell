using System;
using UnityEngine;

namespace ReverseBell.Core
{
    /// <summary>
    /// Publishes beat and bar events using Unity's high precision DSP clock.
    /// </summary>
    public sealed class RhythmClock : MonoBehaviour
    {
        [SerializeField, Min(1f)] private double bpm = 120d;
        [SerializeField, Min(1)] private int beatsPerBar = 4;
        [SerializeField, Min(0f)] private double startDelaySeconds = 0.1d;

        private BeatTimeline timeline;
        private long lastPublishedBeat = -1;

        public event Action<long> Beat;
        public event Action<long> Bar;

        public double Bpm => bpm;

        public int BeatsPerBar => beatsPerBar;

        public double SecondsPerBeat => timeline?.SecondsPerBeat ?? 60d / bpm;

        public long CurrentBeat =>
            timeline?.GetBeatIndex(AudioSettings.dspTime) ?? -1;

        public double CurrentBeatProgress =>
            timeline?.GetBeatProgress(AudioSettings.dspTime) ?? 0d;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            StartClock(AudioSettings.dspTime + startDelaySeconds);
        }

        private void Update()
        {
            PublishElapsedBeats(AudioSettings.dspTime);
        }

        public void StartClock(double startDspTime)
        {
            timeline = new BeatTimeline(bpm, beatsPerBar, startDspTime);
            lastPublishedBeat = -1;
        }

        private void PublishElapsedBeats(double dspTime)
        {
            long currentBeat = timeline.GetBeatIndex(dspTime);

            while (lastPublishedBeat < currentBeat)
            {
                lastPublishedBeat++;
                Beat?.Invoke(lastPublishedBeat);

                if (lastPublishedBeat % beatsPerBar == 0)
                {
                    Bar?.Invoke(lastPublishedBeat / beatsPerBar);
                }
            }
        }
    }
}
