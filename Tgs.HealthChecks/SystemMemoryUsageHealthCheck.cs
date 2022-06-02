using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tgs.HealthChecks;

internal class SystemMemoryUsageHealthCheck : IHealthCheck
{
	private readonly int _memoryUsagePercentageLimit;

	public SystemMemoryUsageHealthCheck(int memoryUsagePercentageLimit)
		=> _memoryUsagePercentageLimit = memoryUsagePercentageLimit;

	public Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		var metrics = new MemoryMetrics();

		using var proc = Process.GetCurrentProcess();

		var memInfo = GC.GetGCMemoryInfo();
		metrics.Total = memInfo.TotalAvailableMemoryBytes;
		metrics.Used = proc.PrivateMemorySize64;
		metrics.Free = metrics.Total - metrics.Free;

		var usagePercentage = metrics.Used * 100 / metrics.Total;
		return usagePercentage > _memoryUsagePercentageLimit
			? Task.FromResult(new HealthCheckResult(
				context.Registration.FailureStatus,
				description: $"The system memory usage over limit: {usagePercentage:0.##}% of {_memoryUsagePercentageLimit}%"))
			: Task.FromResult(HealthCheckResult.Healthy(
				description: $"The system memory usage: {usagePercentage:0.##}% of {_memoryUsagePercentageLimit}%"));
	}

	private class MemoryMetrics
	{
		public double Total;
		public double Used;
		public double Free;
	}
}
