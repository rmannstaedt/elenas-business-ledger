#define MyAppName "Elena's Business Ledger"
#define MyAppDescription "A Windows app for managing student lessons, invoices, and payments."
#ifndef MyAppVersion
#error Use scripts/build-installer.ps1 to supply the app version.
#endif
#define MyAppPublisher "visualtech.dk"
#define MyAppExeName "ElenasBusinessLedger.App.exe"
; Replace the prior launcher in place so existing Windows shortcuts keep working.
#define MyInstalledExeName "MySmallBusinessLedger.App.exe"

#ifndef PublishDir
#define PublishDir "..\artifacts\publish\ElenasBusinessLedger\win-x64"
#endif

#ifndef OutputDir
#define OutputDir "..\artifacts\installer"
#endif

[Setup]
AppId={{8F2B99B4-0C13-4980-8B57-0A58D9CCFC3F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppComments={#MyAppDescription}
DefaultDirName={localappdata}\Programs\MySmallBusinessLedger
DefaultGroupName={#MyAppName}
UsePreviousGroup=no
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=ElenasBusinessLedger-{#MyAppVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
SetupLogging=yes
UninstallDisplayIcon={app}\{#MyInstalledExeName}
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "\{#MyAppExeName}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; DestName: "{#MyInstalledExeName}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyInstalledExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyInstalledExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyInstalledExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
