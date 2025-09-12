using System.Diagnostics;
using System.Text.Json;
using CX.Engine.Common.Telemetry;
using Hardware.Info;

namespace CX.Engine.Common;

public class EnvMetrics : MetricsContainer
{
    public EnvMetrics(IServiceProvider sp) : base(sp, "EnvMetrics", "default")
    {
    }
    
    public override string ToJson()
    {
        var currentProcess = Process.GetCurrentProcess();
        var memoryUsageBytes = currentProcess.WorkingSet64;

        var hardwareInfo = new HardwareInfo();
        hardwareInfo.RefreshMemoryStatus();
        hardwareInfo.RefreshCPUList();
        var avgCpu = hardwareInfo.CpuList.SelectMany(cpu => cpu.CpuCoreList.Select(core => core.PercentProcessorTime)).Average(m => (double)m);
        
        return JsonSerializer.Serialize(new {
            AvailablePhysical = hardwareInfo.MemoryStatus.AvailablePhysical,
            TotalPhysical = hardwareInfo.MemoryStatus.TotalPhysical,
            AvailableVirtual = hardwareInfo.MemoryStatus.AvailableVirtual,
            TotalVirtual = hardwareInfo.MemoryStatus.TotalVirtual,
            AvailablePageFile = hardwareInfo.MemoryStatus.AvailablePageFile,
            TotalPageFile = hardwareInfo.MemoryStatus.TotalPageFile,
            ProcessWorkingSet = memoryUsageBytes,
            AvgCpu = avgCpu
        });
    }
}