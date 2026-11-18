; ==============================================================================
; Inno Setup Script: Desktop Bulk Email Sender
; Compiler: Inno Setup 6.x (https://jrsoftware.org/isdl.php)
; ==============================================================================

#define MyAppName        "Desktop Bulk Email Sender"
#define MyAppPublisher   "Ziad Al-Taher"
#define MyAppExeName     "DesktopBulkEmailSender.exe"
#define MyAppURL         "https://github.com/Ziad-AlTaher/DesktopBulkEmailSender"
; Output of build_installer.ps1 (self-contained, single-file win-x64 publish)
#define SourceDir        "publish"

; The version lives only in DesktopBulkEmailSender.csproj (<Version>).
; build_installer.ps1 passes it as /DMyAppVersion=x.y.z; when compiling from the
; Inno Setup IDE instead, it is read from the published exe's file version.
#ifndef MyAppVersion
  #if !FileExists(AddBackslash(SourceDir) + MyAppExeName)
    #error "publish\DesktopBulkEmailSender.exe not found - run build_installer.ps1 first"
  #endif
  #define MyAppVersion GetVersionNumbersString(AddBackslash(SourceDir) + MyAppExeName)
#endif

[Setup]
; Unique GUID identifying this application for install / updates
AppId={{8A56DF71-44B7-4D2A-B981-89B772A0D523}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation folders
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Installer output properties
OutputDir=installer_output
OutputBaseFilename=DesktopBulkEmailSender_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupIconFile=DesktopBulkEmailSender\app_icon.ico

; Execution privileges (Allows standard user install or admin for all users)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Uninstaller configuration
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

; Modern styling & visual tweaks
ShowLanguageDialog=no
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Packages the published payload (exe + app_icon.ico/png copied by the csproj); debug symbols are left out
Source: "{#SourceDir}\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu shortcuts
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

; Optional Desktop shortcut
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Option to launch the application immediately after setup finishes
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
