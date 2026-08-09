; ═══════════════════════════════════════════════════════════════════════════
;  Vacanam Installer Script — Inno Setup 6.x
;  This file lives in: installer/Vacanam.iss
;  All paths are relative to this file's directory (installer/)
;  Produces: installer/VacanamSetup-{version}.exe
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
; OutputDir is relative to the .iss file location (installer/)
OutputDir=.
OutputBaseFilename=VacanamSetup-{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
MinVersion=10.0.17763
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
; Icon path relative to installer/ directory
SetupIconFile=..\src\Vacanam.App\Resources\Icons\vacanam.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startupicon"; Description: "Launch Vacanam automatically when Windows starts"; GroupDescription: "Startup"

[Files]
; publish/ is at repo root, so relative to installer/ it is ..\publish\
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Vacanam"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/IM {#AppExeName} /F"; Flags: runhidden skipifdoesntexist

[UninstallDelete]
Type: dirifempty; Name: "{app}"
