using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DesktopBulkEmailSender
{
    public class RecipientItem
    {
        public string Email { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }

    public class AppSettings
    {
        // Credentials & SMTP
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        // Legacy plain-text password (pre-1.2). Read for migration only, always saved empty.
        public string AppPassword { get; set; } = string.Empty;
        // Password encrypted with Windows DPAPI (CurrentUser scope), Base64-encoded.
        public string AppPasswordProtected { get; set; } = string.Empty;
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public int MinDelay { get; set; } = 5;
        public int MaxDelay { get; set; } = 15;

        // Email Content
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;

        // Attachments
        public List<string> Attachments { get; set; } = new List<string>();

        // Recipients & Paste Box
        public List<RecipientItem> Recipients { get; set; } = new List<RecipientItem>();
        public string RawPasteText { get; set; } = string.Empty;
        public int GroupByIndex { get; set; } = 0;
    }

    internal static class SecretProtector
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DesktopBulkEmailSender.v1");

        public static string Protect(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return string.Empty;
            var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>Returns the decrypted secret, or empty if it cannot be decrypted (e.g. another Windows user).</summary>
        public static string Unprotect(string protectedBase64)
        {
            if (string.IsNullOrEmpty(protectedBase64)) return string.Empty;
            try
            {
                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(protectedBase64), Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex) when (ex is CryptographicException || ex is FormatException)
            {
                return string.Empty;
            }
        }
    }
}
