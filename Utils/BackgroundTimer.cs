namespace Bot.Utils;

internal class BackgroundTimer
{
    private static readonly ILogger _logger = ForContext<BackgroundTimer>();
    private readonly PeriodicTimer _timer;
    private readonly Func<Task> _callback;
    private readonly double _minsUntilComplete;
    private readonly SemaphoreSlim? _semaphore;
    private Task? _timerTask;
    private nint _invocationsLeft;

    public BackgroundTimer(TimeSpan period, Func<Task> callback, SemaphoreSlim? semaphore = null, nint maxInvocationCount = -1)
    {
        if (maxInvocationCount == 0)
            throw new ArgumentException("Max invocation count can't be 0", nameof(maxInvocationCount));

        _timer = new PeriodicTimer(period);
        _minsUntilComplete = maxInvocationCount * period.TotalMinutes;
        _callback = callback;
        _semaphore = semaphore;
        _invocationsLeft = maxInvocationCount;
        _logger.Debug("New background timer created ({InvocationCount} * {Period})", maxInvocationCount, period);
    }

    public void Start()
    {
        if (_minsUntilComplete > 10)
            _timerTask = Task.Factory.StartNew(StartTimerTask, TaskCreationOptions.LongRunning);
        else
            _timerTask = StartTimerTask();
    }

    public async Task StopAsync()
    {
        if (_timerTask is null)
            return;

        try
        {
            await (_semaphore?.WaitAsync() ?? Task.CompletedTask);
            await _timerTask;
        }
        finally
        {
            _ = _semaphore?.Release();
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
                await (_semaphore?.WaitAsync() ?? Task.CompletedTask);
                await _callback();
            }
            finally
            {
                _ = _semaphore?.Release();
                _invocationsLeft--;
            }
        }
    }
}
