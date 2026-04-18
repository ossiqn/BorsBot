#define AppName "BorsaBot"
#define AppVersion "2.0"
#define AppPublisher "BorsaBot"
#define AppExeName "BorsaBot.exe"
#define PythonDir "Python"

[Setup]
AppId={{B0RSA-B0T-2024-KANKA-001}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=dist
OutputBaseFilename=BorsaBot_Setup_v2
SetupIconFile=assets\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardSizePercent=120
DisableProgramGroupPage=yes
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; GroupDescription: "Ek görevler:"
Name: "startupicon"; Description: "Windows başlangıcında çalıştır"; GroupDescription: "Ek görevler:"

[Files]
Source: "publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "{#PythonDir}\*"; DestDir: "{app}\engine"; Flags: ignoreversion recursesubdirs
Source: "assets\*"; DestDir: "{app}\assets"; Flags: ignoreversion recursesubdirs
Source: "vcredist_x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: startupicon

[Run]
Filename: "{tmp}\vcredist_x64.exe"; Parameters: "/quiet /norestart"; StatusMsg: "Visual C++ kuruluyor..."; Flags: waituntilterminated
Filename: "{cmd}"; Parameters: "/c cd ""{app}\engine"" && python -m pip install -r requirements.txt --quiet"; StatusMsg: "Python modülleri kuruluyor..."; Flags: waituntilterminated runhidden
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\engine\logs"
Type: filesandordirs; Name: "{app}\engine\borsa.db"
Type: filesandordirs; Name: "{app}\engine\__pycache__"