namespace Bot.Utils;

internal class BackgroundTimer
{
    private static readonly ILogger _logger = ForContext<BackgroundTimer>();
    private readonly PeriodicTimer _timer;
    private readonly Func<Task> _callback;
    private readonly double _minsUntilComplete;
    private Task? _timerTask;
    private nint _invocationsLeft;

    public BackgroundTimer(TimeSpan period, Func<Task> callback, nint maxInvocationCount = -1)
    {
        if (maxInvocationCount == 0)
            throw new ArgumentException("Max invocation count can't be 0", nameof(maxInvocationCount));

        _timer = new PeriodicTimer(period);
        _minsUntilComplete = maxInvocationCount * period.TotalMinutes;
        _callback = callback;
        _invocationsLeft = maxInvocationCount;
        _logger.Debug("New background timer created ({InvocationCount} * {Period})", maxInvocationCount, period);
    }

    public void Start() => _timerTask = _minsUntilComplete > 10 ? Task.Factory.StartNew(StartTimerTask, TaskCreationOptions.LongRunning) : StartTimerTask();

    public async Task StopAsync()
    {
        if (_timerTask is null)
        {
            return;
        }

        try
        {
            await _timerTask;
        }
        finally
        {
            _timerTask.Dispose();
        }

        _logger.Information("Background timer aborted");
    }

    private async Task StartTimerTask()
    {
        while (_invocationsLeft != 0 && await _timer.WaitForNextTickAsync())
        {
            try
            {
                await _callback();
            }
            finally
            {
                _invocationsLeft--;
            }
        }
    }
}
