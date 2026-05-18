using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ZeppStepTool.Wpf.Models;

namespace ZeppStepTool.Wpf.Services
{
    public sealed class ZeppStepService
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private static readonly Regex PhonePattern = new Regex(@"^(1)\d{10}$", RegexOptions.Compiled);
        private readonly string _stepDataTemplate;

        public ZeppStepService()
        {
            _stepDataTemplate = LoadStepDataTemplate();
        }

        public async Task<StepSubmitResult> SubmitStepsAsync(string account, string password, int steps)
        {
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
            {
                return new StepSubmitResult
                {
                    Success = false,
                    Message = "账号或密码不能为空。",
                    Steps = steps,
                };
            }

            if (steps <= 0)
            {
                return new StepSubmitResult
                {
                    Success = false,
                    Message = "步数必须大于 0。",
                    Steps = steps,
                };
            }

            var session = await LoginAsync(account.Trim(), password).ConfigureAwait(false);
            if (session == null)
            {
                return new StepSubmitResult
                {
                    Success = false,
                    Message = "登录失败，请检查账号密码或稍后重试。",
                    Steps = steps,
                };
            }

            if (!StepPayloadBuilder.TryBuildEncodedDataJson(
                    _stepDataTemplate,
                    DateTime.Now.ToString("yyyy-MM-dd"),
                    steps.ToString(),
                    out var encodedDataJson,
                    out var buildError))
            {
                return new StepSubmitResult
                {
                    Success = false,
                    Message = buildError ?? "步数数据生成失败。",
                    Steps = steps,
                };
            }

            var timestamp = await GetTimestampAsync().ConfigureAwait(false);
            var formBytes = StepPayloadBuilder.BuildSubmitFormBytes(session.UserId, encodedDataJson);
            var formContent = new ByteArrayContent(formBytes);
            formContent.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api-mifit-cn.huami.com/v1/data/band_data.json?&t={timestamp}")
            {
                Content = formContent,
            };
            request.Headers.TryAddWithoutValidation("apptoken", session.AppToken);

            using var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new StepSubmitResult
                {
                    Success = false,
                    Message = $"提交失败：HTTP {(int)response.StatusCode} {body}",
                    Steps = steps,
                };
            }

            var json = JObject.Parse(body);
            var message = json["message"]?.ToString() ?? "unknown";
            var success = string.Equals(message, "success", StringComparison.OrdinalIgnoreCase);
            return new StepSubmitResult
            {
                Success = success,
                Message = success ? $"步数已提交：{steps}" : message,
                Steps = steps,
            };
        }

        private static StringContent CreateFormUrlEncodedContent(string body)
        {
            // Media type must not include "; charset=..." — encoding is set separately.
            return new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "MiFit/6.12.0 (MCE16; Android 16; Density/1.5)");
            client.DefaultRequestHeaders.TryAddWithoutValidation("app_name", "com.xiaomi.hm.health");
            return client;
        }

        private static string LoadStepDataTemplate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "ZeppStepTool.Wpf.Assets.StepDataTemplate.txt";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException("Missing embedded step data template.");
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static async Task<ZeppSession?> LoginAsync(string account, string password)
        {
            string loginAccount;
            string thirdName;
            if (PhonePattern.IsMatch(account))
            {
                loginAccount = "+86" + account;
                thirdName = "huami_phone";
            }
            else
            {
                loginAccount = account;
                thirdName = "huami";
            }

            var loginBody =
                $"client_id=HuaMi&country_code=CN&json_response=true&name={loginAccount}" +
                $"&password={password}" +
                "&redirect_uri=https://s3-us-west-2.amazonaws.com/hm-registration/successsignin.html" +
                "&state=REDIRECTION&token=access";

            using var tokenRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api-user.zepp.com/registrations/" + loginAccount + "/tokens")
            {
                Content = CreateFormUrlEncodedContent(loginBody),
            };

            using var tokenResponse = await HttpClient.SendAsync(tokenRequest).ConfigureAwait(false);
            if ((int)tokenResponse.StatusCode == 429)
            {
                return null;
            }

            var tokenJsonText = await tokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var tokenJson = JObject.Parse(tokenJsonText);
            var accessCode = tokenJson["access"]?.ToString();
            if (string.IsNullOrEmpty(accessCode))
            {
                return null;
            }

            var loginPayload =
                "app_name=com.xiaomi.hm.health&country_code=CN&code=" + accessCode +
                "&device_id=fuck1069-2002-7869-0129-757geoi6sam1&device_model=android_phone" +
                "&app_version=6.12.0&grant_type=access_token&allow_registration=false" +
                "&dn=account.zepp.com,api-user.zepp.com,api-mifit.zepp.com,api-watch.zepp.com," +
                "app-analytics.zepp.com,api-analytics.huami.com,auth.zepp.com" +
                "&source=com.xiaomi.hm.health&third_name=" + thirdName;

            using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "https://account.zepp.com/v2/client/login")
            {
                Content = CreateFormUrlEncodedContent(loginPayload),
            };

            using var loginResponse = await HttpClient.SendAsync(loginRequest).ConfigureAwait(false);
            var loginJsonText = await loginResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!loginResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var loginJson = JObject.Parse(loginJsonText);
            var tokenInfo = loginJson["token_info"];
            if (tokenInfo == null)
            {
                return null;
            }

            return new ZeppSession
            {
                UserId = tokenInfo["user_id"]?.ToString() ?? string.Empty,
                AppToken = tokenInfo["app_token"]?.ToString() ?? string.Empty,
            };
        }

        private static async Task<string> GetTimestampAsync()
        {
            try
            {
                using var response = await HttpClient.GetAsync(
                    "https://acs.m.taobao.com/gw/mtop.common.getTimestamp/").ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = JObject.Parse(body);
                return json["data"]?["t"]?.ToString()
                    ?? GetUnixTimeMilliseconds().ToString();
            }
            catch
            {
                return GetUnixTimeMilliseconds().ToString();
            }
        }

        private static long GetUnixTimeMilliseconds()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        private sealed class ZeppSession
        {
            public string UserId { get; set; } = string.Empty;
            public string AppToken { get; set; } = string.Empty;
        }
    }
}
