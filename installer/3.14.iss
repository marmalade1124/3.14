#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
[Setup]
AppId={{A796D89A-0339-4B99-9B67-70FD6520C5E8}
AppName=3.14
AppVersion={#AppVersion}
AppPublisher=3.14
AppPublisherURL=https://pi-color-studio.vercel.app
AppSupportURL=https://github.com/marmalade1124/3.14/issues
AppUpdatesURL=https://github.com/marmalade1124/3.14/releases
DefaultDirName={localappdata}\Programs\3.14
DefaultGroupName=3.14
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.15063
WizardStyle=modern
OutputDir=..\dist
OutputBaseFilename=3.14-Setup
UninstallDisplayIcon={app}\3.14.exe
Compression=lzma2
SolidCompression=yes
AppMutex=Local\PiStudio.3.14
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
[Files]
Source: "..\dist\3.14.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{group}\3.14"; Filename: "{app}\3.14.exe"
Name: "{autodesktop}\3.14"; Filename: "{app}\3.14.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\3.14.exe"; Description: "Open 3.14"; Flags: nowait postinstall skipifsilent
[Code]
function InitializeSetup(): Boolean;
var Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then
    MsgBox('3.14 requires Microsoft .NET Framework 4.8. Install it from Microsoft, then run this installer again.', mbError, MB_OK);
end;
