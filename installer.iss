[Setup]
AppName=Solar Quotation & Billing System
AppVersion=1.3
DefaultDirName={autopf}\Solar Quotation Billing System
DefaultGroupName=Solar Quotation Billing System
OutputDir=Output
OutputBaseFilename=Solar_Billing_Update
SetupIconFile=Assets\app_icon.ico
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Solar Quotation & Billing System"; Filename: "{app}\SolarQuotationBillingSystem.exe"
Name: "{autodesktop}\Solar Quotation & Billing System"; Filename: "{app}\SolarQuotationBillingSystem.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SolarQuotationBillingSystem.exe"; Description: "{cm:LaunchProgram,Solar Quotation & Billing System}"; Flags: nowait postinstall skipifsilent

[Code]
// Check if LocalDB is installed
function IsLocalDBInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  Result := RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions');
end;

procedure InitializeWizard();
begin
  if not IsLocalDBInstalled() then
  begin
    MsgBox('Microsoft SQL Server LocalDB is not installed on this system.' + #13#10 + 
           'This application requires LocalDB to function properly.' + #13#10 +
           'Please install Microsoft SQL Server Express LocalDB after this installation completes.', 
           mbInformation, MB_OK);
  end;
end;
