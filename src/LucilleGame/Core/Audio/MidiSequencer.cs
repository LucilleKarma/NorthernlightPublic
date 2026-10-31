namespace Northernlight.Core.Audio;

/// <summary>
///     A sequencer system that converts MIDI files into
///     digestible commands for sequential readback.
/// </summary>
public sealed class MidiSequencer
{
    public readonly record struct TimedEvent(uint Tick, IMidiEvent Event);

    public readonly record struct TempoChangeEvent(uint Tick, double MicrosecondsPerQuarter);

    public readonly record struct PlaybackAction(double TimeStart, double Duration, byte Status, int ChannelIndex, int Data1, int Data2);

    private readonly MidiFile file;

    private List<TimedEvent> timeline = [];

    private List<TempoChangeEvent> tempoMap = [];

    /// <summary>
    ///     The set of all actions in this sequencer.
    /// </summary>
    public readonly List<PlaybackAction> Actions = [];

    /// <summary>
    ///     The current index in the <see cref="Actions"/> list.
    /// </summary>
    public int ActionIndex
    {
        get;
        private set;
    }

    /// <summary>
    ///     Whether this sequencer is paused or not.
    /// </summary>
    public bool Paused
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this sequencer is done or not.
    /// </summary>
    public bool Done => ActionIndex >= Actions.Count;

    /// <summary>
    ///     The event to fire when a new action arises.
    /// </summary>
    public event Action<PlaybackAction> OnPlaybackActionFire;

    public MidiSequencer(MidiFile file, bool startPaused)
    {
        this.file = file;
        Paused = startPaused;

        BuildTimeline();
        BuildTempoMap();
        BuildActions();
    }

    private void BuildTimeline()
    {
        foreach (MidiTrack track in file.Tracks)
        {
            uint absoluteTime = 0;
            foreach (IMidiEvent e in track.Events)
            {
                absoluteTime += e.DeltaTime;
                timeline.Add(new TimedEvent(absoluteTime, e));
            }
        }

        timeline = timeline.OrderBy(a => a.Tick).ToList();
    }

    private void BuildTempoMap()
    {
        foreach (TimedEvent timedEvent in timeline)
        {
            if (timedEvent.Event is MetaMidiEvent meta && meta.Identifier == 0x51)
            {
                uint microseconds = (uint)(meta.Data[0] << 16 | meta.Data[1] << 8 | meta.Data[2]);
                tempoMap.Add(new TempoChangeEvent(timedEvent.Tick, microseconds));
            }
        }

        tempoMap = tempoMap.OrderBy(a => a.Tick).ToList();
    }

    private void BuildActions()
    {
        double lastTimeValue = 0f;
        foreach (TimedEvent timedEvent in timeline)
        {
            double time = TickToSeconds(timedEvent.Tick);

            if (timedEvent.Event is ChannelMidiEvent channel)
            {
                double duration = time - lastTimeValue;
                Actions.Add(new PlaybackAction(lastTimeValue, duration, timedEvent.Event.Status, channel.Channel, channel.Data[0], channel.Data[1]));
                lastTimeValue = time;
            }
        }
    }

    /// <summary>
    ///     Peeks the upcoming action.
    /// </summary>
    public PlaybackAction PeekNext() => Done ? default : Actions[ActionIndex + 1];

    /// <summary>
    ///     Updates this sequencer.
    /// </summary>
    public void Update(double audioTimeSeconds)
    {
        double maxTime = Actions.Max(a => a.TimeStart);
        if (audioTimeSeconds >= maxTime)
        {
            if (ActionIndex != 0)
            {
                while (ActionIndex < Actions.Count)
                {
                    OnPlaybackActionFire?.Invoke(Actions[ActionIndex]);
                    ActionIndex++;
                }

                ActionIndex = 0;
            }
            return;
        }

        PlaybackAction action = Actions[ActionIndex];
        while (audioTimeSeconds >= action.TimeStart)
        {
            OnPlaybackActionFire?.Invoke(action);

            ActionIndex++;
            if (Done)
            {
                ActionIndex = 0;
                return;
            }

            action = Actions[ActionIndex];
        }
    }

    /// <summary>
    ///     Resets this sequencer so that it can start anew.
    /// </summary>
    public void Reset()
    {
        ActionIndex = 0;
    }

    private double TickToSeconds(uint tick)
    {
        uint previousTick = 0;
        double time = 0f;
        double currentUsPerQuarter = 500000f;
        for (int i = 0; i < tempoMap.Count; i++)
        {
            var (changeTick, us) = tempoMap[i];
            if (tick < changeTick)
                break;

            uint deltaTicks = changeTick - previousTick;
            time += deltaTicks * currentUsPerQuarter / file.PulsesPerQuarterNote / 1000000f;

            previousTick = changeTick;
            currentUsPerQuarter = us;
        }

        uint remaining = tick - previousTick;
        time += remaining * currentUsPerQuarter / file.PulsesPerQuarterNote / 1000000f;

        return time;
    }
}
