; MD Viewer — Windows installer (Inno Setup 6).
;
; What this script gives you, in the order a user meets it:
;   * a machine-wide install into {autopf}\Deynekin\MD.Viewer;
;   * a Tasks page with shortcuts (Start menu, desktop, Quick Launch, Favorites)
;     and the .md association, each optional;
;   * a Finish page with two checkboxes: launch the viewer and open README.md;
;   * an entry in Settings → Apps → Installed apps with a working uninstaller;
;   * association cleanup on uninstall that only touches what still points at us.
;
; Build:  "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" MD.Viewer.iss

#define AppName        "MD Viewer"
#ifndef AppVersion
  #define AppVersion   "1.3.2"
#endif
#ifndef PayloadFile
  #define PayloadFile  "..\artifacts\publish\mdview.exe"
#endif
#define AppPublisher   "Mikhail Deynekin"
#define AppUrl         "https://Deynekin.com"
#define RepoUrl        "https://github.com/paulmann/MD.Viewer.Windows"
#define AppExe         "mdview.exe"
#define AppId          "{{7F2C9E4A-3B6D-4C1E-9A2F-5D8B1E7C4A31}"
#define ProgId         "MDViewer.Markdown"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#RepoUrl}/issues
AppUpdatesURL={#RepoUrl}/releases
AppComments=A native Markdown viewer and editor for Windows. No browser engine, no .NET installation, no telemetry.
DefaultDirName={autopf}\Deynekin\MD.Viewer
DefaultGroupName=Deynekin\MD Viewer
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\artifacts\installer
OutputBaseFilename=MD.Viewer-{#AppVersion}-setup
SetupIconFile=..\assets\mdview.ico
WizardImageFile=wizard-large.bmp
WizardSmallImageFile=wizard-small.bmp
WizardStyle=modern
WizardSizePercent=110
Compression=lzma2/ultra64
SolidCompression=yes
InternalCompressLevel=max
LZMAUseSeparateProcess=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
ChangesAssociations=yes
CloseApplications=yes
RestartApplications=no
UninstallDisplayName={#AppName} {#AppVersion}
UninstallDisplayIcon={app}\{#AppExe}
Uninstallable=yes
AllowNoIcons=yes
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} setup
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoCopyright=Copyright (C) 2026 {#AppPublisher}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
russian.TaskGroupShortcuts=Ярлыки
russian.TaskGroupIntegration=Интеграция с Windows
russian.TaskStartMenu=Ярлык в меню «Пуск»
russian.TaskStartMenuHint=Папка «Deynekin\MD Viewer» с ярлыками программы, README и удаления
russian.TaskDesktop=Значок на рабочем столе
russian.TaskQuickLaunch=Ярлык в панели быстрого запуска
russian.TaskFavorites=Ярлык в «Избранном»
russian.TaskAssociate=Открывать файлы .md в MD Viewer
russian.TaskAssociateHint=Регистрирует программу как обработчик Markdown в «Открыть с помощью» и в приложениях по умолчанию
russian.ReadmeName=README.ru.md
russian.RunApp=Запустить MD Viewer
russian.OpenReadme=Открыть README.md
russian.AssocDescription=Документ Markdown
russian.UserChoiceHintShort=Файлы .md уже были связаны с другой программой — Windows сохраняет личный выбор пользователя. Чтобы открывать их этим просмотрщиком: Параметры → Приложения → Приложения по умолчанию → Выбор приложений по умолчанию по типу файла → .md.

english.TaskGroupShortcuts=Shortcuts
english.TaskGroupIntegration=Windows integration
english.TaskStartMenu=Start menu shortcut
english.TaskStartMenuHint=Folder "Deynekin\MD Viewer" with the application, README and uninstall shortcuts
english.TaskDesktop=Desktop icon
english.TaskQuickLaunch=Quick Launch shortcut
english.TaskFavorites=Favorites shortcut
english.TaskAssociate=Open .md files with MD Viewer
english.TaskAssociateHint=Registers the viewer in "Open with" and in Default apps
english.ReadmeName=README.md
english.RunApp=Launch MD Viewer
english.OpenReadme=Open README.md
english.AssocDescription=Markdown document
english.UserChoiceHintShort=Windows already keeps a personal default for .md files, and it wins over any installer. To open them with MD Viewer: Settings → Apps → Default apps → Choose defaults by file type → .md.

[Tasks]
Name: "startmenu";    Description: "{cm:TaskStartMenu}";    GroupDescription: "{cm:TaskGroupShortcuts}";   Flags: checkedonce
Name: "desktopicon";  Description: "{cm:TaskDesktop}";      GroupDescription: "{cm:TaskGroupShortcuts}"
Name: "quicklaunch";  Description: "{cm:TaskQuickLaunch}";  GroupDescription: "{cm:TaskGroupShortcuts}"
Name: "favorites";    Description: "{cm:TaskFavorites}";    GroupDescription: "{cm:TaskGroupShortcuts}"
Name: "associate";    Description: "{cm:TaskAssociate}";    GroupDescription: "{cm:TaskGroupIntegration}"; Flags: checkedonce

[Files]
Source: "{#PayloadFile}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md";                   DestDir: "{app}";            Flags: ignoreversion
Source: "..\README.ru.md";                DestDir: "{app}";            Flags: ignoreversion
Source: "..\CHANGELOG.md";                DestDir: "{app}";            Flags: ignoreversion
Source: "..\LICENSE";                     DestDir: "{app}";            Flags: ignoreversion
Source: "..\docs\*.md";                   DestDir: "{app}\docs";       Flags: ignoreversion
Source: "..\docs\img\*";                  DestDir: "{app}\docs\img";   Flags: ignoreversion
Source: "..\samples\*";                   DestDir: "{app}\samples";    Flags: ignoreversion

[Icons]
; Start menu folder: the app, its README and the uninstaller
Name: "{group}\MD Viewer";             Filename: "{app}\{#AppExe}";                WorkingDir: "{app}"; Tasks: startmenu
Name: "{group}\README";                Filename: "{app}\{#AppExe}"; Parameters: """{app}\{cm:ReadmeName}"""; WorkingDir: "{app}"; Tasks: startmenu
Name: "{group}\Удалить MD Viewer";     Filename: "{uninstallexe}";                 Tasks: startmenu

; The requested optional shortcuts
Name: "{autodesktop}\MD Viewer";       Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\MD Viewer"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; Tasks: quicklaunch
Name: "{userfavorites}\MD Viewer"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; Tasks: favorites

[Registry]
; --- file type: our ProgID
Root: HKA; Subkey: "Software\Classes\{#ProgId}"; ValueType: string; ValueName: ""; ValueData: "{cm:AssocDescription}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#ProgId}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExe},0"
Root: HKA; Subkey: "Software\Classes\{#ProgId}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""

; --- default handler for Markdown extensions (only when the task is selected)
Root: HKA; Subkey: "Software\Classes\.md";       ValueType: string; ValueName: ""; ValueData: "{#ProgId}"; Tasks: associate
Root: HKA; Subkey: "Software\Classes\.markdown"; ValueType: string; ValueName: ""; ValueData: "{#ProgId}"; Tasks: associate
Root: HKA; Subkey: "Software\Classes\.mdown";    ValueType: string; ValueName: ""; ValueData: "{#ProgId}"; Tasks: associate

; --- "Open with" candidates, independent of the default choice
Root: HKA; Subkey: "Software\Classes\.md\OpenWithProgids";       ValueType: none; ValueName: "{#ProgId}"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.markdown\OpenWithProgids"; ValueType: none; ValueName: "{#ProgId}"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.mdown\OpenWithProgids";    ValueType: none; ValueName: "{#ProgId}"; Flags: uninsdeletevalue

; --- so the app shows up in Settings → Default apps as a candidate
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "{#AppName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: none; ValueName: ".md"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: none; ValueName: ".markdown"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: none; ValueName: ".mdown"; Flags: uninsdeletevalue

Root: HKA; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "MDViewer"; ValueData: "Software\MDViewer\Capabilities"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\MDViewer"; ValueType: string; ValueName: ""; ValueData: "{#AppName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\MDViewer\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#AppName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\MDViewer\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Native Markdown viewer and editor"
Root: HKA; Subkey: "Software\MDViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".md"; ValueData: "{#ProgId}"
Root: HKA; Subkey: "Software\MDViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".markdown"; ValueData: "{#ProgId}"
Root: HKA; Subkey: "Software\MDViewer\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mdown"; ValueData: "{#ProgId}"

; --- Win+R "mdview" and a stable App Path for other tools
Root: HKA; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#AppExe}"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExe}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#AppExe}"; ValueType: string; ValueName: "Path"; ValueData: "{app}"

[Run]
; Both entries appear as checkboxes on the Finish page (postinstall), not on the Tasks page,
; because that is where Windows users expect "launch now" and "read me".
Filename: "{app}\{#AppExe}"; Description: "{cm:RunApp}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\{#AppExe}"; Parameters: """{app}\{cm:ReadmeName}"""; Description: "{cm:OpenReadme}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\docs"
Type: filesandordirs; Name: "{app}\samples"
Type: dirifempty;     Name: "{app}"

[InstallDelete]
; An in-place upgrade keeps files that the new version no longer ships. These two
; directories belong entirely to the product, so purging them is safe and keeps a
; help page that was dropped in a later release from lingering forever.
Type: filesandordirs; Name: "{app}\docs"
Type: filesandordirs; Name: "{app}\samples"

[Code]
// Windows keeps a hashed per-user default (FileExts\.md\UserChoice) that an installer
// cannot change. Saying that in a modal box in the middle of "Installing..." looks like a
// failure, so the note is appended to the Finish page instead — visible, not blocking.
procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = wpFinished) and (not WizardSilent) and WizardIsTaskSelected('associate') then
  begin
    if RegKeyExists(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.md\UserChoice') then
      WizardForm.FinishedLabel.Caption := WizardForm.FinishedLabel.Caption + #13#10#13#10 +
        ExpandConstant('{cm:UserChoiceHintShort}');
  end;
end;

// Remove the default-handler value only if it still points at us: an uninstaller must
// never clear a choice that the user made afterwards.
procedure RemoveAssociationIfOurs(const Extension: String);
var
  Current: String;
begin
  if RegQueryStringValue(HKA, 'Software\Classes\' + Extension, '', Current) then
  begin
    if CompareText(Current, '{#ProgId}') = 0 then
      RegDeleteValue(HKA, 'Software\Classes\' + Extension, '');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RemoveAssociationIfOurs('.md');
    RemoveAssociationIfOurs('.markdown');
    RemoveAssociationIfOurs('.mdown');
  end;
end;


