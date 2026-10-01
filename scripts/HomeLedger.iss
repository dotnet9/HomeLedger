; HomeLedger Windows installer.
; Build from the repository root with Inno Setup 6 and pass /DAppVersion=x.y.z.

#ifndef AppVersion
#define AppVersion "0.0.0"
#endif

[Setup]
AppId={{74EF99B5-9EB4-4D02-8164-8F78D465D08C}
AppName=HomeLedger
AppVersion={#AppVersion}
AppPublisher=Dotnet9
AppPublisherURL=https://github.com/dotnet9/HomeLedger
AppSupportURL=https://github.com/dotnet9/HomeLedger/issues
DefaultDirName={autopf}\HomeLedger
DefaultGroupName=HomeLedger
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=HomeLedger-v{#AppVersion}-win-x64-setup
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
ChangesAssociations=no
CloseApplications=yes
RestartApplications=yes
CloseApplicationsFilter=HomeLedger.Desktop.exe
UninstallDisplayIcon={app}\HomeLedger.Desktop.exe
WizardStyle=modern

[Languages]
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\HomeLedger"; Filename: "{app}\HomeLedger.Desktop.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\HomeLedger"; Filename: "{app}\HomeLedger.Desktop.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\HomeLedger.Desktop.exe"; Description: "Launch HomeLedger"; Flags: nowait postinstall skipifsilent
