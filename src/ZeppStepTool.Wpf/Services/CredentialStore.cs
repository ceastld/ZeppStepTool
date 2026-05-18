using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace ZeppStepTool.Wpf.Services
{
    public sealed class CredentialStore
    {
        private static readonly string StorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZeppStepTool",
            "credentials.json");

        public bool TryLoad(out string account, out string password)
        {
            account = string.Empty;
            password = string.Empty;

            if (!File.Exists(StorePath))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(StorePath, Encoding.UTF8);
                var data = JsonConvert.DeserializeObject<CredentialData>(json);
                if (data == null || string.IsNullOrEmpty(data.Account))
                {
                    return false;
                }

                account = data.Account;
                if (!string.IsNullOrEmpty(data.ProtectedPassword))
                {
                    var encrypted = Convert.FromBase64String(data.ProtectedPassword);
                    var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                    password = Encoding.UTF8.GetString(plain);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Save(string account, string password)
        {
            if (string.IsNullOrWhiteSpace(account))
            {
                return;
            }

            var directory = Path.GetDirectoryName(StorePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string? protectedPassword = null;
            if (!string.IsNullOrEmpty(password))
            {
                var encrypted = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(password),
                    null,
                    DataProtectionScope.CurrentUser);
                protectedPassword = Convert.ToBase64String(encrypted);
            }

            var data = new CredentialData
            {
                Account = account.Trim(),
                ProtectedPassword = protectedPassword,
            };

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(StorePath, json, Encoding.UTF8);
        }

        private sealed class CredentialData
        {
            [JsonProperty("account")]
            public string Account { get; set; } = string.Empty;

            [JsonProperty("protectedPassword")]
            public string? ProtectedPassword { get; set; }
        }
    }
}
