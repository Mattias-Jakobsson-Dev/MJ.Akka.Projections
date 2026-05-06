namespace MJ.Akka.Projections;

/// <summary>
/// Accumulates stash/unstash signals from handlers during a single event handling invocation.
/// </summary>
public class ProjectionStashContext
{
    public bool ShouldStash { get; private set; }
    public uint? NumberToUnstash { get; private set; }
    public bool ShouldUnstash { get; private set; }

    public void RequestStash()
    {
        ShouldStash = true;
    }

    public void RequestUnstash(uint? numberOfMessages)
    {
        ShouldUnstash = true;
        // null means "all"; if multiple UnStash calls, take the largest (null wins)
        if (NumberToUnstash == null) return;
        if (numberOfMessages == null)
            NumberToUnstash = null;
        else
            NumberToUnstash = Math.Max(NumberToUnstash.Value, numberOfMessages.Value);
    }

    public void Reset()
    {
        ShouldStash = false;
        ShouldUnstash = false;
        NumberToUnstash = null;
    }
}


