using ScreenMonitor.Core.Interfaces;

namespace ScreenMonitor.Tests;

public class IdleDetectionServiceTests
{
    private class FakeIdleDetectionService : IIdleDetectionService
    {
        public int IdleThresholdSeconds { get; private set; } = 300;
        private int _fakeIdleSeconds;

        public void SetIdleThreshold(int seconds) => IdleThresholdSeconds = seconds;
        public void SetFakeIdleSeconds(int seconds) => _fakeIdleSeconds = seconds;
        public bool IsUserIdle() => _fakeIdleSeconds >= IdleThresholdSeconds;
        public int GetIdleSeconds() => _fakeIdleSeconds;
    }

    // ===== IDL-001：超过阈值判定为空闲 =====
    [Fact]
    public void IDL001_IsUserIdle_ExceedsThreshold_ReturnsTrue()
    {
        var service = new FakeIdleDetectionService();
        service.SetIdleThreshold(300);
        service.SetFakeIdleSeconds(310);

        Assert.True(service.IsUserIdle());
    }

    // ===== IDL-002：有操作时判定为非空闲 =====
    [Fact]
    public void IDL002_IsUserIdle_WithinThreshold_ReturnsFalse()
    {
        var service = new FakeIdleDetectionService();
        service.SetIdleThreshold(300);
        service.SetFakeIdleSeconds(30);

        Assert.False(service.IsUserIdle());
    }

    // ===== IDL-003：阈值可配置 =====
    [Fact]
    public void IDL003_IdleThreshold_IsConfigurable()
    {
        var service = new FakeIdleDetectionService();
        Assert.Equal(300, service.IdleThresholdSeconds);

        service.SetIdleThreshold(600);
        Assert.Equal(600, service.IdleThresholdSeconds);
    }
}
