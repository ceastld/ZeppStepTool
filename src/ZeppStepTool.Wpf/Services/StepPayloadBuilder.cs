using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeppStepTool.Wpf.Services
{
    public static class StepPayloadBuilder
    {
        private static readonly Regex DatePattern = new Regex(
            @".*?date%22%3A%22(.*?)%22%2C%22data.*?",
            RegexOptions.Compiled);

        private static readonly Regex StepPattern = new Regex(
            @".*?ttl%5C%22%3A(.*?)%2C%5C%22dis.*?",
            RegexOptions.Compiled);

        public static bool TryBuildEncodedDataJson(
            string template,
            string date,
            string steps,
            out string encodedDataJson,
            out string? error)
        {
            encodedDataJson = string.Empty;
            error = null;

            template = template.Trim('\uFEFF', ' ', '\t', '\r', '\n');
            if (string.IsNullOrEmpty(template))
            {
                error = "Step data template is empty.";
                return false;
            }

            var dateMatch = DatePattern.Match(template);
            if (!dateMatch.Success || dateMatch.Groups.Count < 2)
            {
                error = "Step data template is missing the date placeholder.";
                return false;
            }

            if (!StepPattern.IsMatch(template))
            {
                error = "Step data template is missing the steps placeholder.";
                return false;
            }

            // Match Python bushu.py: replace only the captured date/steps values, not the whole regex match.
            encodedDataJson = ReplaceCapture(template, dateMatch.Groups[1], date);

            var stepMatch = StepPattern.Match(encodedDataJson);
            if (!stepMatch.Success || stepMatch.Groups.Count < 2)
            {
                error = "Step data template is missing the steps placeholder.";
                return false;
            }

            encodedDataJson = ReplaceCapture(encodedDataJson, stepMatch.Groups[1], steps);

            if (!encodedDataJson.StartsWith("%5B", StringComparison.Ordinal))
            {
                error = "Encoded data_json has an invalid prefix.";
                return false;
            }

            return true;
        }

        public static string BuildSubmitFormBody(string userId, string encodedDataJson)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }

            if (string.IsNullOrEmpty(encodedDataJson))
            {
                throw new ArgumentException("Encoded data_json is required.", nameof(encodedDataJson));
            }

            return string.Concat(
                "userid=",
                userId,
                "&last_sync_data_time=1597306380&device_type=0&last_deviceid=DA932FFFFE8816E7&data_json=",
                encodedDataJson);
        }

        public static byte[] BuildSubmitFormBytes(string userId, string encodedDataJson)
        {
            return Encoding.UTF8.GetBytes(BuildSubmitFormBody(userId, encodedDataJson));
        }

        private static string ReplaceCapture(string source, Group group, string value)
        {
            return source.Remove(group.Index, group.Length).Insert(group.Index, value);
        }
    }
}
