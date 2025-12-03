; Inno Setup Script for ImgViewer
; インストーラ生成スクリプト

#define MyAppName "ImgViewer"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "idlast"
#define MyAppURL "https://github.com/idlast/imageViewer"
#define MyAppExeName "ImgViewer.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
OutputDir=installer
OutputBaseFilename=ImgViewer-Setup-{#MyAppVersion}
SetupIconFile=Resources\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "fileassoc"; Description: "画像ファイルをImgViewerに関連付ける"; GroupDescription: "ファイルの関連付け:"

[Files]
Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
; ファイル関連付け - 画像形式
Root: HKCR; Subkey: ".jpg"; ValueType: string; ValueName: ""; ValueData: "ImgViewer.Image"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".jpeg"; ValueType: string; ValueName: ""; ValueData: "ImgViewer.Image"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".png"; ValueType: string; ValueName: ""; ValueData: "ImgViewer.Image"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".gif"; ValueType: string; ValueName: ""; ValueData: "ImgViewer.Image"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".bmp"; ValueType: string; ValueName: ""; ValueData: "ImgViewer.Image"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".webp"; ValueType: string; ValueName: ""; ValueData: "ImgViewer.Image"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".heic"; ValueType: string; ValueName: ""; ValueData: "ImgViewer.Image"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".heif"; ValueType: string; ValueName: ""; ValueData: "ImgViewer.Image"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".tiff"; ValueType: string; ValueName: ""; ValueData: "ImgViewer.Image"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".tif"; ValueType: string; ValueName: ""; ValueData: "ImgViewer.Image"; Flags: uninsdeletevalue; Tasks: fileassoc

; ファイルタイプの登録
Root: HKCR; Subkey: "ImgViewer.Image"; ValueType: string; ValueName: ""; ValueData: "画像ファイル"; Flags: uninsdeletekey; Tasks: fileassoc
Root: HKCR; Subkey: "ImgViewer.Image\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: fileassoc
Root: HKCR; Subkey: "ImgViewer.Image\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: fileassoc

; アプリケーションの登録（既定のアプリ設定用）
Root: HKLM; Subkey: "SOFTWARE\RegisteredApplications"; ValueType: string; ValueName: "ImgViewer"; ValueData: "SOFTWARE\ImgViewer\Capabilities"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer"; Flags: uninsdeletekey; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "ImgViewer"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "軽量で高速なタブ付き画像ビューア"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpg"; ValueData: "ImgViewer.Image"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpeg"; ValueData: "ImgViewer.Image"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".png"; ValueData: "ImgViewer.Image"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gif"; ValueData: "ImgViewer.Image"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".bmp"; ValueData: "ImgViewer.Image"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".webp"; ValueData: "ImgViewer.Image"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".heic"; ValueData: "ImgViewer.Image"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".heif"; ValueData: "ImgViewer.Image"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tiff"; ValueData: "ImgViewer.Image"; Tasks: fileassoc
Root: HKLM; Subkey: "SOFTWARE\ImgViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tif"; ValueData: "ImgViewer.Image"; Tasks: fileassoc

[Code]
// シェルにファイル関連付けの変更を通知
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // SHChangeNotify を呼び出してシェルに通知
    Exec('cmd.exe', '/c echo. > nul', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;
