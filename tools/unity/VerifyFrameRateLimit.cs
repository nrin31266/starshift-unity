using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

public static class VerifyFrameRateLimit
{
    public static async Task<object> Measure(int expectedTarget)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (Application.targetFrameRate != expectedTarget)
            throw new InvalidOperationException("Unexpected target frame rate.");
        if (expectedTarget > 0 && QualitySettings.vSyncCount != 0)
            throw new InvalidOperationException("VSync overrides the frame rate limit.");

        using var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime;
        var frames = Time.frameCount;
        var clock = Stopwatch.StartNew();
        await Task.Delay(3000);
        var seconds = clock.Elapsed.TotalSeconds;
        var fps = (Time.frameCount - frames) / seconds;
        if (fps <= 0 || (expectedTarget > 0 && fps > expectedTarget * 1.15))
            throw new InvalidOperationException("Frame rate limit did not take effect: " + fps);
        return new { target = expectedTarget, fps,
            cpuCorePercent = (process.TotalProcessorTime - cpu).TotalSeconds / seconds * 100 };
    }
}
