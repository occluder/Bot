namespace Bot.Utils;

internal class BackgroundTimer
{
    private static readonly ILogger _logger = ForContext<BackgroundTimer>();
    private readonly PeriodicTimer _timer;
    private readonly Func<Task> _callback;
    private readonly CancellationTokenSource _cts = new();
    private Task? _timerTask;
    private nint _invocationsLeft;

    public BackgroundTimer(TimeSpan period, Func<Task> callback, nint maxInvocationCount = -1)
    {
        if (maxInvocationCount == 0)
            throw new ArgumentException("Max invocation count can't be 0", nameof(maxInvocationCount));

        _timer = new PeriodicTimer(period);
        _callback = callback;
        _invocationsLeft = maxInvocationCount;
        _logger.Debug("New background timer created ({InvocationCount} * {Period})", maxInvocationCount, period);
    }

    public void Start() => _timerTask = Task.Run(StartTimerTask);

    public async Task StopAsync()
    {
        if (_timerTask is null)
            return;

        _cts.Cancel();
        _timer.Dispose();
        try
        {
            await _timerTask;
        }
        catch (OperationCanceledException)
        {
        }

        _logger.Information("Background timer stopped");
    }

    private async Task StartTimerTask()
    {
        try
        {
            while (_invocationsLeft != 0 && await _timer.WaitForNextTickAsync(_cts.Token))
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
        catch (OperationCanceledException)
        {
        }
    }
}
