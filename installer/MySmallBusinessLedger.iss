#define MyAppName "My Small Business Ledger"
#define MyAppVersion "1.31.0"
#define MyAppPublisher "visualtech.dk"
#define MyAppExeName "MySmallBusinessLedger.App.exe"

#ifndef PublishDir
#define PublishDir "..\artifacts\publish\MySmallBusinessLedger\win-x64"
#endif

#ifndef OutputDir
#define OutputDir "..\artifacts\installer"
#endif

[Setup]
AppId={{8F2B99B4-0C13-4980-8B57-0A58D9CCFC3F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\MySmallBusinessLedger
DefaultGroupName=My Small Business Ledger
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=MySmallBusinessLedger-1.31.0-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
SetupLogging=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
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
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
