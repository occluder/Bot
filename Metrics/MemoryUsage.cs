﻿using System.Diagnostics;
using Bot.Interfaces;

namespace Bot.Metrics;

public class MemoryUsage: IMetric
{
    private uint _invc;

    public async Task Report()
    {
        if (++_invc % 4 != 0)
            return;

        using var conn = await NewDbConnection();
        try
        {
            long ts = Unix();
            object[] metrics =
            [
                new
                {
                    Measurement = "GC_Total",
                    Bytes = GC.GetTotalMemory(false),
                    Ts = ts
                },
                new
                {
                    Measurement = "GC_Allocated",
                    Bytes = GC.GetTotalAllocatedBytes(),
                    Ts = ts
                },
                new
                {
                    Measurement = "Process_Private",
                    Bytes = Process.GetCurrentProcess().PrivateMemorySize64,
                    Ts = ts
                },
                new
                {
                    Measurement = "Process_Working",
                    Bytes = Process.GetCurrentProcess().WorkingSet64,
                    Ts = ts
                },
                new
                {
                    Measurement = "Process_PeakWorking",
                    Bytes = Process.GetCurrentProcess().PeakWorkingSet64,
                    Ts = ts
                },
            ];

            await conn.ExecuteAsync(
                "insert into metrics_memory values (@Measurement, @Bytes, @Ts)",
                metrics
            );
        }
        catch (Exception ex)
        {
            ForContext<MemoryUsage>().Error(ex, "Something went wrong {InvocationCount}", _invc);
        }
    }
}