using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tgs.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

public static class HealthChecksBuilderExtensions
{
	private const string DefaultName = "sysmemusage";

	public static IHealthChecksBuilder AddSystemMemory(
		this IHealthChecksBuilder builder,
		int memoryUsagePercentageLimit,
		string? name = default,
		HealthStatus? failureStatus = default,
		IEnumerable<string>? tags = default,
		TimeSpan? timeout = default)
	{
		if (memoryUsagePercentageLimit <= 0)
			throw new ArgumentException($"{nameof(memoryUsagePercentageLimit)} should be greater than zero");

		var registrationName = name ?? DefaultName;

		return builder.Add(new HealthCheckRegistration(
		   registrationName,
		   sp => new SystemMemoryUsageHealthCheck(memoryUsagePercentageLimit),
		   failureStatus,
		   tags,
		   timeout));
	}
}
