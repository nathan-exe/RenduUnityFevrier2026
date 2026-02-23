using System;
using UnityEngine;

public static class TimeStamp
{
    public static float Now => (DateTime.UtcNow.Ticks-DateTime.Today.Ticks) / TimeSpan.TicksPerMillisecond;
}
