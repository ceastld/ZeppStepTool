using Xunit;
using ZeppStepTool.Wpf.Services;

namespace ZeppStepTool.Tests;

public sealed class ZeppStepServiceIntegrationTests
{
    private static bool TryGetCredentials(out string account, out string password)
    {
        account = Environment.GetEnvironmentVariable("ZEPP_ACCOUNT") ?? string.Empty;
        password = Environment.GetEnvironmentVariable("ZEPP_PASSWORD") ?? string.Empty;
        return account.Length > 0 && password.Length > 0;
    }

    [SkippableFact]
    public async Task SubmitStepsAsync_Succeeds_WithConfiguredCredentials()
    {
        Skip.IfNot(
            TryGetCredentials(out var account, out var password),
            "Set ZEPP_ACCOUNT and ZEPP_PASSWORD environment variables.");

        var stepsText = Environment.GetEnvironmentVariable("ZEPP_TEST_STEPS");
        var steps = int.TryParse(stepsText, out var configuredSteps) ? configuredSteps : 8888;

        var service = new ZeppStepService();
        var result = await service.SubmitStepsAsync(account, password, steps);

        Assert.True(
            result.Success,
            $"Expected success but got: {result.Message}");
    }
}
