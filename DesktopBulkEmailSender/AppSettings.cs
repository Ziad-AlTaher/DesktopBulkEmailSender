using System;
using System.Collections.Generic;

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
        public string AppPassword { get; set; } = string.Empty;
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
}
