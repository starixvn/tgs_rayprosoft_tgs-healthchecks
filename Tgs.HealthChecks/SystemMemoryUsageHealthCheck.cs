using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tgs.HealthChecks;

internal class SystemMemoryUsageHealthCheck : IHealthCheck
{
	private readonly int _memoryUsagePercentageLimit;

	private static bool IsUnix => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
		|| RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

	public SystemMemoryUsageHealthCheck(int memoryUsagePercentageLimit)
		=> _memoryUsagePercentageLimit = memoryUsagePercentageLimit;

	public Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		var metrics = new MemoryMetrics();

		if (IsUnix)
		{
			var info = new ProcessStartInfo("free -m")
			{
				FileName = "/bin/bash",
				Arguments = "-c \"free -m\"",
				RedirectStandardOutput = true
			};

			using var process = Process.Start(info)!;
			string output = process.StandardOutput.ReadToEnd();

			var lines = output.Split("\n");
			var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

			metrics.Total = double.Parse(memory[1]);
			metrics.Used = double.Parse(memory[2]);
			metrics.Free = double.Parse(memory[3]);
		}
		else
		{
			var info = new ProcessStartInfo
			{
				FileName = "wmic",
				Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value",
				RedirectStandardOutput = true
			};

			using var process = Process.Start(info)!;
			string output = process.StandardOutput.ReadToEnd();

			var lines = output.Trim().Split("\n");
			var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
			var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

			metrics.Total = Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0);
			metrics.Free = Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0);
			metrics.Used = metrics.Total - metrics.Free;
		}

		var usagePercentage = metrics.Used * 100 / metrics.Total;
		if (usagePercentage > _memoryUsagePercentageLimit)
		{
			return Task.FromResult(
				new HealthCheckResult(
					context.Registration.FailureStatus,
					description: $"The system memory usage over limit: {usagePercentage:0.##}% of {_memoryUsagePercentageLimit}%"));
		}

		return Task.FromResult(HealthCheckResult.Healthy(description:
			$"The system memory usage: {usagePercentage:0.##}% of {_memoryUsagePercentageLimit}%"));
	}

	private class MemoryMetrics
	{
		public double Total;
		public double Used;
		public double Free;
	}
}
