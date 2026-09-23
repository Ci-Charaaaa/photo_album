; Inno Setup 打包脚本
; 用法：
;   1) 先在解决方案根目录执行发布：
;        dotnet publish src\PhotoAlbum.Presentation\PhotoAlbum.Presentation.csproj -c Release -r win-x64 --self-contained true -o publish
;   2) 用 Inno Setup 打开本文件并编译（Compile），产物输出到 installer\output\PhotoAlbum-Setup-0.1.0.exe

#define MyAppName "HS相册"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "闰土"
#define MyAppExeName "PhotoAlbum.Presentation.exe"

[Setup]
; AppId 唯一标识本应用，升级安装时靠它识别，不要随便改
AppId={{B7E3F1A2-9C4D-4E6B-8A1F-2D5C7E9B3A64}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\PhotoAlbum
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=PhotoAlbum-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"

[Files]
; 把发布目录的所有文件打进安装包
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "运行 {#MyAppName}"; Flags: nowait postinstall skipifsilent
