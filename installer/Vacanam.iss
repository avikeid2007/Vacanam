; ═══════════════════════════════════════════════════════════════════════════
;  Vacanam Installer Script — Inno Setup 6.x
;  Produces: VacanamSetup-{version}.exe
;  Target:   Windows 10 / 11  x64
; ═══════════════════════════════════════════════════════════════════════════

#define AppName      "Vacanam"
#define AppPublisher "Vacanam"
#define AppURL       "https://github.com/avikeid2007/Vacanam"
#define AppExeName   "Vacanam.exe"
#define AppVersion   GetEnv("APP_VERSION")
#if AppVersion == ""
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{F3A2B1C4-9D7E-4F8A-A3B2-C1D4E5F6A7B8}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=installer
OutputBaseFilename=VacanamSetup-{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
; Require Windows 10 1809+ (build 17763)
MinVersion=10.0.17763
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
SetupIconFile=src\Vacanam.App\Resources\Icons\vacanam.ico
WizardImageFile=installer\assets\wizard-banner.bmp
WizardSmallImageFile=installer\assets\wizard-small.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startupicon"; Description: "Launch Vacanam automatically when Windows starts"; GroupDescription: "Startup"

[Files]
; Main publish output (self-contained, single-file)
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
; "Start with Windows" — mirrors Vacanam's own StartWithWindows setting
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Vacanam"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/IM {#AppExeName} /F"; Flags: runhidden skipifdoesntexist

[UninstallDelete]
; Remove user data directory on uninstall (optional — user is prompted via code below)
Type: dirifempty; Name: "{app}"
