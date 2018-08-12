; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "HueLightBot"
#define MyAppVersion "2.0.2.0"
#define MyAppPublisher "HueLightBot"
#define MyAppURL "http://huelightbot.com/"
#define MyAppExeName "HueLightBot.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{CDAE9199-6734-469D-9D0C-E7EBB8ADD81E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputBaseFilename=huelightbot-setup
SetupIconFile=lightbot_xIQ_icon.ico
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\HueLightBot.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\AutoUpdater.NET.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\AutoUpdater.NET.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\AutoUpdater.NET.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\HueLightBot.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\HueLightBot.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\HueLightBot.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\Newtonsoft.Json.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\PubSub.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\Q42.HueApi.ColorConverters.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\Q42.HueApi.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\ServiceStack.Common.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\ServiceStack.Common.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\ServiceStack.Interfaces.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\ServiceStack.Interfaces.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\ServiceStack.Redis.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\ServiceStack.Redis.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\ServiceStack.Text.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\ServiceStack.Text.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\System.Runtime.InteropServices.RuntimeInformation.dll"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{commonprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser
