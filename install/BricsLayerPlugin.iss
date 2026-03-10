; =============================================================
; BricsLayerPlugin – Inno Setup instalátor
; =============================================================
;
; Vytvoří jeden EXE soubor, který:
; 1. Zkontroluje a nainstaluje .NET 8.0 Desktop Runtime
; 2. Detekuje nainstalovanou verzi BricsCAD
; 3. Zkopíruje plugin DLL do zvolené složky
; 4. Zaregistruje plugin pro automatické načítání
; 5. Vytvoří zástupce v nabídce Start
; 6. Umožní čistou odinstalaci
;
; PRO SESTAVENÍ INSTALÁTORU:
;   1. Stáhněte Inno Setup z https://jrsoftware.org/isdownload.php
;   2. Spusťte build.ps1 (sestaví plugin + vytvoří instalátor)
;   nebo ručně: otevřete tento soubor v Inno Setup Compiler
;

#define MyAppName "BricsLayerPlugin"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "BricsLayerPlugin"
#define MyAppDescription "Vrstvy a třídy ve stylu Vectorworks pro BricsCAD"

[Setup]
AppId={{B7A3C1D4-E5F6-4890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppSupportURL=https://github.com/rousvit/brics-civil
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=BricsLayerPlugin_Setup_{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=
LicenseFile=
; Moderní vizuální styl
WizardResizable=no
WizardSizePercent=110

[Languages]
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
czech.WelcomeLabel2=Tento průvodce nainstaluje plugin {#MyAppName} pro BricsCAD.%n%nPlugin přidá systém vrstev a tříd ve stylu Vectorworks.%n%nDoporučujeme zavřít BricsCAD před pokračováním.

[Types]
Name: "full"; Description: "Kompletní instalace (doporučeno)"
Name: "custom"; Description: "Vlastní instalace"; Flags: iscustom

[Components]
Name: "main"; Description: "Plugin BricsLayerPlugin"; Types: full custom; Flags: fixed
Name: "dotnet"; Description: ".NET 8.0 Desktop Runtime (potřebné)"; Types: full

[Files]
; Plugin DLL a závislosti
Source: "..\bin\Release\net8.0-windows\BricsLayerPlugin.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: main
Source: "..\bin\Release\net8.0-windows\BricsLayerPlugin.pdb"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: main
Source: "..\bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: main
; .NET Runtime instalátor (offline bundle)
Source: "deps\windowsdesktop-runtime-8.0-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall skipifsourcedoesntexist; Components: dotnet

[Icons]
Name: "{group}\Odinstalovat {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{group}\Otevřít složku pluginu"; Filename: "{app}"

[Registry]
; Auto-load registrace – detekovaná verze BricsCAD se doplní přes kód
Root: HKCU; Subkey: "Software\Bricsys\BricsCAD\{code:GetBricsCADVersion}\en_US\Applications\BricsLayerPlugin"; ValueType: string; ValueName: "DESCRIPTION"; ValueData: "{#MyAppDescription}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Bricsys\BricsCAD\{code:GetBricsCADVersion}\en_US\Applications\BricsLayerPlugin"; ValueType: dword; ValueName: "LOADCTRLS"; ValueData: "2"
Root: HKCU; Subkey: "Software\Bricsys\BricsCAD\{code:GetBricsCADVersion}\en_US\Applications\BricsLayerPlugin"; ValueType: string; ValueName: "LOADER"; ValueData: "{app}\BricsLayerPlugin.dll"
Root: HKCU; Subkey: "Software\Bricsys\BricsCAD\{code:GetBricsCADVersion}\en_US\Applications\BricsLayerPlugin"; ValueType: dword; ValueName: "MANAGED"; ValueData: "1"

[Run]
; Instalace .NET Runtime (pokud soubor existuje a komponenta je vybrána)
Filename: "{tmp}\windowsdesktop-runtime-8.0-win-x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Instaluji .NET 8.0 Desktop Runtime..."; Flags: skipifdoesntexist waituntilterminated; Components: dotnet
; Zobrazit README po instalaci
Filename: "{app}"; Description: "Otevřít složku pluginu"; Flags: postinstall shellexec skipifsilent unchecked

[UninstallRun]
; Žádné speciální akce – registr se smaže automaticky díky uninsdeletekey

[Code]
var
  BricsCADVersionPage: TInputOptionWizardPage;
  DetectedVersion: String;

// ---------------------------------------------------------------
// Detekce nainstalované verze BricsCAD z registru
// ---------------------------------------------------------------
function DetectBricsCADVersions: TStringList;
var
  Names: TArrayOfString;
  I: Integer;
  Versions: TStringList;
begin
  Versions := TStringList.Create;
  if RegGetSubkeyNames(HKEY_CURRENT_USER, 'Software\Bricsys\BricsCAD', Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      // Hledáme klíče jako V24, V25, V26...
      if (Length(Names[I]) >= 2) and (Names[I][1] = 'V') then
        Versions.Add(Names[I]);
    end;
  end;
  // Zkusit i HKLM
  if RegGetSubkeyNames(HKEY_LOCAL_MACHINE, 'Software\Bricsys\BricsCAD', Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      if (Length(Names[I]) >= 2) and (Names[I][1] = 'V') then
      begin
        if Versions.IndexOf(Names[I]) < 0 then
          Versions.Add(Names[I]);
      end;
    end;
  end;
  Result := Versions;
end;

// ---------------------------------------------------------------
// Detekce nainstalovaného .NET Runtime
// ---------------------------------------------------------------
function IsDotNet8Installed: Boolean;
var
  ResultCode: Integer;
begin
  // Spustit dotnet --list-runtimes a hledat 8.0
  Result := Exec('cmd.exe', '/C dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 8." >nul', '',
    SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

// ---------------------------------------------------------------
// Vytvoření stránky pro výběr verze BricsCAD
// ---------------------------------------------------------------
procedure InitializeWizard;
var
  Versions: TStringList;
  I: Integer;
begin
  Versions := DetectBricsCADVersions;

  BricsCADVersionPage := CreateInputOptionPage(
    wpSelectComponents,
    'Verze BricsCAD',
    'Vyberte verzi BricsCAD, pro kterou chcete plugin nainstalovat.',
    'Nalezené verze BricsCAD na tomto počítači:',
    True, False);

  if Versions.Count > 0 then
  begin
    for I := 0 to Versions.Count - 1 do
      BricsCADVersionPage.Add('BricsCAD ' + Versions[I]);
    // Vybrat poslední (nejnovější) verzi
    BricsCADVersionPage.SelectedValueIndex := Versions.Count - 1;
    DetectedVersion := Versions[Versions.Count - 1];
  end
  else
  begin
    // Žádná verze nenalezena – nabídnout ruční zadání
    BricsCADVersionPage.Add('BricsCAD V24');
    BricsCADVersionPage.Add('BricsCAD V25');
    BricsCADVersionPage.Add('BricsCAD V26');
    BricsCADVersionPage.SelectedValueIndex := 1; // V25
    DetectedVersion := 'V25';
  end;

  Versions.Free;

  // Upozornit na .NET
  if not IsDotNet8Installed then
    MsgBox('Na tomto počítači nebyl nalezen .NET 8.0 Desktop Runtime.' + #13#10 +
           #13#10 +
           'Instalátor se pokusí jej nainstalovat automaticky.' + #13#10 +
           'Pokud nemáte offline balíček, stáhněte jej z:' + #13#10 +
           'https://dotnet.microsoft.com/download/dotnet/8.0' + #13#10 +
           #13#10 +
           'Pokud je .NET 8.0 již nainstalovaný, ignorujte tuto zprávu.',
           mbInformation, MB_OK);
end;

// ---------------------------------------------------------------
// Vrátí vybranou verzi BricsCAD pro registr
// ---------------------------------------------------------------
function GetBricsCADVersion(Param: String): String;
var
  Selected: String;
begin
  Selected := BricsCADVersionPage.Values[BricsCADVersionPage.SelectedValueIndex];
  // Extrahovat verzi (např. "BricsCAD V25" -> "V25")
  if Pos('V', Selected) > 0 then
    Result := Copy(Selected, Pos('V', Selected), Length(Selected))
  else
    Result := DetectedVersion;
end;

// ---------------------------------------------------------------
// Kontrola před instalací
// ---------------------------------------------------------------
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = wpReady then
  begin
    // Poslední kontrola – BricsCAD by měl být zavřený
    if FindWindowByClassName('BricscadMainWindow') <> 0 then
    begin
      if MsgBox('BricsCAD je pravděpodobně spuštěný.' + #13#10 +
                'Doporučujeme jej zavřít před instalací.' + #13#10 +
                #13#10 +
                'Pokračovat přesto?',
                mbConfirmation, MB_YESNO) = IDNO then
        Result := False;
    end;
  end;
end;

// ---------------------------------------------------------------
// Po instalaci – informační zpráva
// ---------------------------------------------------------------
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MsgBox('Instalace dokončena!' + #13#10 +
           #13#10 +
           'Plugin se automaticky načte při příštím spuštění BricsCAD.' + #13#10 +
           #13#10 +
           'Dostupné příkazy:' + #13#10 +
           '  VW_LAYERS   – otevře panel vrstev' + #13#10 +
           '  VW_CLASSES  – otevře panel tříd' + #13#10 +
           #13#10 +
           'Panel lze dokovat k okraji nebo použít jako plovoucí okno.',
           mbInformation, MB_OK);
  end;
end;
