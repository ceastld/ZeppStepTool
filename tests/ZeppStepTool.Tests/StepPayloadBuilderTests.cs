using System.IO;
using System.Reflection;
using System.Text;
using Xunit;
using ZeppStepTool.Wpf.Services;

namespace ZeppStepTool.Tests;

public sealed class StepPayloadBuilderTests
{
    private static string LoadTemplate()
    {
        var assembly = typeof(ZeppStepService).Assembly;
        const string resourceName = "ZeppStepTool.Wpf.Assets.StepDataTemplate.txt";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Missing embedded resource: {resourceName}");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Fact]
    public void TryBuildEncodedDataJson_ReplacesDateAndSteps()
    {
        var template = LoadTemplate();
        const string date = "2026-05-18";
        const string steps = "1566";

        var ok = StepPayloadBuilder.TryBuildEncodedDataJson(template, date, steps, out var encoded, out var error);

        Assert.True(ok, error);
        Assert.DoesNotContain("2021-08-07", encoded, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString(date), encoded, StringComparison.Ordinal);
        Assert.Contains("ttl%5C%22%3A1566%2C%5C%22dis", encoded, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSubmitFormBody_KeepsDataJsonSingleEncoded()
    {
        var template = LoadTemplate();
        Assert.True(
            StepPayloadBuilder.TryBuildEncodedDataJson(template, "2026-05-18", "8888", out var encoded, out _));

        var body = StepPayloadBuilder.BuildSubmitFormBody("123456789", encoded);
        var dataJsonValue = body[(body.IndexOf("data_json=", StringComparison.Ordinal) + "data_json=".Length)..];

        Assert.Equal(encoded, dataJsonValue);
        Assert.DoesNotContain("%255B", body, StringComparison.Ordinal);
        Assert.StartsWith("userid=123456789&", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSubmitFormBody_MatchesPythonStyleConcatenation()
    {
        var template = LoadTemplate();
        Assert.True(
            StepPayloadBuilder.TryBuildEncodedDataJson(template, "2026-05-18", "1566", out var encoded, out _));

        var expected =
            "userid=999&last_sync_data_time=1597306380&device_type=0&last_deviceid=DA932FFFFE8816E7&data_json=" +
            encoded;

        Assert.Equal(expected, StepPayloadBuilder.BuildSubmitFormBody("999", encoded));
    }

    [Fact]
    public void TryBuildEncodedDataJson_FailsWhenTemplateMissingPlaceholders()
    {
        var ok = StepPayloadBuilder.TryBuildEncodedDataJson(
            "%5Binvalid%5D",
            "2026-05-18",
            "1000",
            out _,
            out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void DecodedDataJson_ContainsExpectedDate()
    {
        var template = LoadTemplate();
        Assert.True(
            StepPayloadBuilder.TryBuildEncodedDataJson(template, "2026-05-18", "1566", out var encoded, out _));

        var decoded = Uri.UnescapeDataString(encoded);
        Assert.Contains("2026-05-18", decoded, StringComparison.Ordinal);
        Assert.Contains("ttl%5C%22%3A1566%2C%5C%22dis", encoded, StringComparison.Ordinal);
    }
}
