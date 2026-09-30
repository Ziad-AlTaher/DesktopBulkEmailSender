# ✉️ Desktop Bulk Email Sender

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0--windows-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Language: C#](https://img.shields.io/badge/Language-C%23-239120?style=flat&logo=c-sharp)](https://docs.microsoft.com/dotnet/csharp/)
[![MailKit](https://img.shields.io/badge/MailKit-4.10.0-blue?style=flat)](https://github.com/jstedfast/MailKit)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%20x64%20|%20x86-0078D6?style=flat&logo=windows)](https://microsoft.com/windows)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A modern, high-performance Windows desktop application built with **C# (.NET 8 WinForms)** and **MailKit** for managing and dispatching personalized, high-deliverability bulk email campaigns directly through standard SMTP (optimized for Gmail and Google Workspace App Passwords).

---

## 📌 Table of Contents

- [Overview](#-overview)
- [Key Features](#-key-features)
- [Architecture & Tech Stack](#-architecture--tech-stack)
- [System Requirements](#-system-requirements)
- [Quick Start Guide](#-quick-start-guide)
- [Application Workflow](#-application-workflow)
- [Gmail App Password Setup](#-gmail-app-password-setup)
- [Configuration & Persistence](#-configuration--persistence)
- [Anti-Spam & Deliverability Strategies](#-anti-spam--deliverability-strategies)
- [Building from Source](#-building-from-source)
- [Troubleshooting & FAQs](#-troubleshooting--faqs)

---

## 🔍 Overview

**Desktop Bulk Email Sender** is designed for professionals, developers, and marketers who require a straightforward, local, privacy-respecting tool to send bulk or cold outreach emails without paying for expensive SaaS platforms or exposing recipient lists to third-party cloud services. 

All operations, recipient data, and credentials remain 100% on your local machine, connecting directly to your configured SMTP mail server using TLS encryption.

---

## ✨ Key Features

### 1. ⚙️ SMTP & Credential Configuration
- **Universal SMTP Support**: Seamlessly works with Gmail, Google Workspace, Outlook / Office 365, Amazon SES, SendGrid, or custom corporate SMTP relays.
- **Secure Authentication**: Supports TLS/STARTTLS (`Port 587`, `Port 465`, or custom ports).
- **Password Masking**: Toggleable visibility for sensitive SMTP and App Passwords.
- **Dynamic Jitter Delays**: Configure min/max random delays (e.g., 5s to 15s) between dispatched messages to avoid rate limits and anti-spam flags.

### 2. 👥 Advanced Recipient Management
- **Interactive Data Grid**: Individual `Send?` checkbox toggles for granular control over who receives each dispatch.
- **Bulk Paste & Import**: Rapidly import hundreds of emails from CSV, newline-delimited text, or comma/semicolon-separated lists.
- **Comment Support**: Prefix any line with `#` to import it in a disabled/unselected state.
- **Automatic Domain & Company Extraction**: Extracts root domains and generates company tags on the fly (e.g., `john@acme.corp` → `ACME`).
- **One-Click Deduplication**: Automatically deduplicates emails during bulk paste, inline manual edits, or via the **Dedup** tool.
- **Smart Grouping & Sorting**: Sort by original order, grouped by company/domain, or alphabetically (A → Z, Z → A).

### 3. 📝 Rich HTML Email Body & Preview
- **Subject Line Editor**: Clean input with full-width responsive sizing.
- **Code-Friendly HTML Editor**: Dark high-contrast editor optimized for raw HTML templates.
- **Load HTML Template**: One-click file picker to import `.html` / `.htm` newsletters or templates.
- **In-Browser Rendering Preview**: Automatically renders and launches your HTML template in your default browser before launching the campaign.

### 4. 📎 Multi-Attachment Handling
- Attach multiple files (PDFs, brochures, resumes, catalogs) to be dispatched with every individual email.
- Real-time attachment verification before sending.

### 5. 🚀 Safe Sending Engine & Live Telemetry
- **Asynchronous Execution**: Fully responsive UI powered by `Task.Run` and `MailKit.Net.Smtp.SmtpClient`.
- **Pre-Flight Summary**: Real-time validation card showing sender address, active recipient count, attachments, and subject line.
- **Graceful Cancellation**: Instant **Stop** button backed by `CancellationTokenSource` to halt sending at any moment.
- **Real-Time Progress & Terminal Logging**: Colored, timestamped output tracking dispatches, delays, SMTP responses, and error traces.
- **Anti-Threading Unique Token**: Appends a hidden zero-size GUID to each message body, preventing mail clients from bundling separate recipients into single conversation threads.

### 6. 💾 Zero-Config State Persistence
- All settings, recipient lists (with checked states), subject lines, HTML bodies, raw paste boxes, and attachments are automatically persisted locally to `%AppData%\DesktopBulkEmailSender\settings.json`.
- State is preserved across application launches and saved automatically before sending and upon form close.

---

## 🛠 Architecture & Tech Stack

| Component | Technology / Spec | Description |
| :--- | :--- | :--- |
| **Framework** | .NET 8.0 Windows | Modern, cross-generation high-performance .NET runtime |
| **UI Technology** | Windows Forms (WinForms) | Native Windows rendering with custom flat theme |
| **Email Protocol** | [MailKit](https://github.com/jstedfast/MailKit) 4.10.0 | RFC-compliant, secure SMTP & MIME message generation |
| **Message Parser** | [MimeKit](https://github.com/jstedfast/MimeKit) 4.10.0 | Full multipart MIME builder with attachment support |
| **Serialization** | `System.Text.Json` | Fast, structured settings and state persistence |
| **Target OS** | Windows 10 / 11 | Any 64-bit or 32-bit Windows operating environment |

```
DesktopBulkEmailSender/
├── DesktopBulkEmailSender.slnx          # Visual Studio Solution descriptor
└── DesktopBulkEmailSender/
    ├── DesktopBulkEmailSender.csproj   # Project file (.NET 8 WinForms, MailKit)
    ├── Program.cs                      # Main application entry point
    ├── Form1.cs                        # UI layout, event handling, SMTP dispatch logic
    ├── Form1.Designer.cs               # Component designer backing file
    ├── Form1.resx                      # Form resource store
    └── AppSettings.cs                  # Data models for settings & recipients persistence
```

---

## 💻 System Requirements

- **Operating System**: Windows 10 (version 1809 or higher) or Windows 11.
- **Runtime**: [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) (if running an unbundled binary).
- **Network**: Active internet connection with outbound access to SMTP ports (default: `587` or `465`).

---

## 🚀 Quick Start Guide

1. **Launch the Application**: Run the executable or launch from Visual Studio / VS Code.
2. **Configure SMTP** (*Config Tab*):
   - Enter your **Sender Email** (e.g., `user@gmail.com`).
   - Enter your **Sender Display Name** (e.g., `John Doe`).
   - Enter your **App Password** (16-character Google App Password).
   - Verify SMTP host (`smtp.gmail.com`) and port (`587`).
   - Set randomized delays between emails (recommended: **5 to 15 seconds**).
3. **Import Recipients** (*Recipients Tab*):
   - Paste recipient emails into the right pane.
   - Click **⬅ Add to Recipients**.
   - Sort by Company or check/uncheck specific contacts as desired.
4. **Compose Email** (*Email Body Tab*):
   - Provide a subject line.
   - Type or paste your HTML code, or click **📂 Load HTML File**.
   - Click **👁 Preview in Browser** to verify styling.
5. **Attach Files** (*Attachments Tab* - Optional):
   - Click **➕ Add File(s)** to append brochures, documents, or catalogs.
6. **Execute Campaign** (*Send Tab*):
   - Review the summary details.
   - Click **▶ Send Emails** and confirm the prompt.
   - Monitor the real-time progress bar and log console.

---

## 🔒 Gmail App Password Setup

If you are using a standard `@gmail.com` or Google Workspace account, Google requires an **App Password** instead of your standard password:

1. Navigate to your [Google Account Security Settings](https://myaccount.google.com/security).
2. Ensure **2-Step Verification** is enabled.
3. Search for or navigate to **App passwords** (or visit [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords)).
4. Create a new app password:
   - App Name: `Bulk Email Sender`
5. Copy the generated **16-character code** (without spaces) and paste it into the **App Password** field in the application's **Config** tab.

---

## 🛡 Anti-Spam & Deliverability Strategies

To ensure high inbox placement rates and protect your domain reputation, this application incorporates several safety protocols:

1. **Randomized Delay Intervals (Jitter)**:
   - Rather than blasting messages in parallel, emails are dispatched sequentially with randomized waiting periods (e.g., 5–15 seconds). This mimics natural human behavior and satisfies provider rate-limit policies.
2. **Thread Breaker GUIDs**:
   - Each email has a hidden, zero-dimensional GUID appended to its HTML body. This prevents email services (like Gmail) from mistakenly threading all outbound emails into a single conversation chain.
3. **Clean Header Construction**:
   - Outgoing messages use standard RFC MIME formats via MailKit with proper sender display names and encoding.
4. **Duplicate Prevention**:
   - Built-in deduplication prevents accidentally sending duplicate messages to the same contact during a single campaign run.

---

## ⚙️ Configuration & Persistence

All state is preserved in:
```
%APPDATA%\DesktopBulkEmailSender\settings.json
```
(e.g., `C:\Users\<User>\AppData\Roaming\DesktopBulkEmailSender\settings.json`)

The stored schema includes:
- **SMTP credentials & server parameters**
- **Min/max randomized delay configuration**
- **Subject line & full HTML email body content**
- **File attachment paths**
- **Recipients list with individual inclusion toggles**
- **Raw paste input buffer & active sort ordering**

---

## 🔨 Building from Source

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (with .NET desktop development workload) or VS Code with C# Dev Kit.

### Build via .NET CLI

1. Clone or open the project folder in your terminal:
   ```powershell
   cd DesktopBulkEmailSender
   ```

2. Restore NuGet dependencies:
   ```powershell
   dotnet restore
   ```

3. Build the solution in Release mode:
   ```powershell
   dotnet build DesktopBulkEmailSender/DesktopBulkEmailSender.csproj -c Release
   ```

4. Run the application:
   ```powershell
   dotnet run --project DesktopBulkEmailSender/DesktopBulkEmailSender.csproj
   ```

### Publish Single-File Executable

To generate a standalone, self-contained `.exe` that runs without requiring .NET to be installed on target machines:

```powershell
dotnet publish DesktopBulkEmailSender/DesktopBulkEmailSender.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -o ./publish
```

The resulting executable will be available in `./publish/DesktopBulkEmailSender.exe`.

---

## ❓ Troubleshooting & FAQs

### 1. `Authentication failed: 535-5.7.8 Username and Password not accepted`
- **Cause**: Using a regular Gmail login password instead of a Google App Password, or 2-Step Verification is disabled.
- **Solution**: Follow the [Gmail App Password Setup](#-gmail-app-password-setup) guide to generate a dedicated 16-character App Password.

### 2. `Connection timed out` or `Cannot reach host`
- **Cause**: Network firewall, antivirus, or ISP blocking outbound traffic on port 587.
- **Solution**: Verify your internet connection, test port connectivity (`Test-NetConnection smtp.gmail.com -Port 587`), or configure port 465 with SSL.

### 3. Emails arriving in Spam / Junk folders
- **Recommendations**:
  - Keep delays between emails at 10–25 seconds or higher.
  - Avoid spam trigger words in your subject and body.
  - Send from a verified domain with valid SPF, DKIM, and DMARC DNS records.
  - Warm up new email accounts by sending small batches initially (20–50/day).

---

## 📄 License

This project is licensed under the [MIT License](LICENSE) — free for personal, educational, and commercial use.
