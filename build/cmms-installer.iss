; CMMS Installer - Inno Setup Script
; Installs CMMS with embedded PostgreSQL setup and Windows Service

#define MyAppName "CMMS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "CMMS"
#define MyAppURL "http://localhost:5000"

[Setup]
AppId={{B8F3A1D2-4C5E-6F78-9A0B-C1D2E3F4A5B6}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\publish
OutputBaseFilename=cmms-{#MyAppVersion}-setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitModeOnly=x64compatible
SetupLogging=yes
UninstallDisplayIcon={app}\CMMS.API.exe
MinVersion=10.0
LicenseFile=license.rtf

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Main application files (from publish/app/)
Source: "..\publish\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; PostgreSQL installer (bundled dependency - not in git, downloaded separately)
Source: "deps\postgresql-16-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsPostgreSQL

[Icons]
Name: "{group}\CMMS Web Interface"; Filename: "http://localhost:5000"
Name: "{group}\Uninstall CMMS"; Filename: "{uninstallexe}"

[Run]
; Install PostgreSQL silently if needed
Filename: "{tmp}\postgresql-16-x64.exe"; \
    Parameters: "--mode unattended --superpassword ""{code:GetPostgresPassword}"" --servicename postgresql-x64-16 --servicepassword ""{code:GetPostgresPassword}"" --prefix ""{code:GetPostgresDir}"""; \
    StatusMsg: "Installing PostgreSQL 16..."; \
    Flags: waituntilterminated; \
    Check: NeedsPostgreSQL

; Create PostgreSQL database and user
Filename: "{code:GetPsqlPath}"; \
    Parameters: "-U postgres -c ""CREATE USER cmms WITH PASSWORD '{code:GetCmmsDbPassword}';"""; \
    StatusMsg: "Creating database user..."; \
    Flags: waituntilterminated runhidden; \
    Environment: "PGPASSWORD={code:GetPostgresPassword}"

Filename: "{code:GetPsqlPath}"; \
    Parameters: "-U postgres -c ""CREATE DATABASE \""CMMS\"" OWNER cmms;"""; \
    StatusMsg: "Creating database..."; \
    Flags: waituntilterminated runhidden; \
    Environment: "PGPASSWORD={code:GetPostgresPassword}"

[UninstallRun]
; Stop and delete the service on uninstall
Filename: "sc.exe"; Parameters: "stop CmmsService"; Flags: runhidden; RunOnceId: "StopCmmsService"
Filename: "sc.exe"; Parameters: "delete CmmsService"; Flags: runhidden; RunOnceId: "DeleteCmmsService"
; Remove firewall rule
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""CMMS Web Application"""; Flags: runhidden; RunOnceId: "RemoveFirewallRule"

[Code]
var
  PostgresPasswordPage: TInputQueryWizardPage;
  CmmsDbPassword: string;
  PostgresDir: string;

function NeedsPostgreSQL: Boolean;
var
  InstallPath: string;
begin
  Result := True;
  // Check common registry locations for PostgreSQL
  if RegQueryStringValue(HKLM, 'SOFTWARE\PostgreSQL\Installations\postgresql-x64-16', 'Base Directory', InstallPath) then
    Result := False
  else if RegQueryStringValue(HKLM, 'SOFTWARE\PostgreSQL\Installations\postgresql-x64-15', 'Base Directory', InstallPath) then
    Result := False
  else if RegQueryStringValue(HKLM, 'SOFTWARE\PostgreSQL\Installations\postgresql-x64-14', 'Base Directory', InstallPath) then
    Result := False;

  if not Result then
    PostgresDir := InstallPath;
end;

function GetPostgresDir(Param: string): string;
begin
  if PostgresDir <> '' then
    Result := PostgresDir
  else
    Result := ExpandConstant('{autopf}\PostgreSQL\16');
end;

function GetPsqlPath(Param: string): string;
begin
  Result := GetPostgresDir('') + '\bin\psql.exe';
end;

function GetPostgresPassword(Param: string): string;
begin
  if PostgresPasswordPage <> nil then
    Result := PostgresPasswordPage.Values[0]
  else
    Result := 'postgres';
end;

function GetCmmsDbPassword(Param: string): string;
begin
  Result := CmmsDbPassword;
end;

function GenerateRandomPassword(Length: Integer): string;
var
  I: Integer;
  Chars: string;
begin
  Chars := 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
  Result := '';
  for I := 1 to Length do
    Result := Result + Chars[Random(Length(Chars)) + 1];
end;

function GenerateJwtSecret: string;
var
  I: Integer;
  Chars: string;
begin
  Chars := 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*';
  Result := '';
  for I := 1 to 48 do
    Result := Result + Chars[Random(Length(Chars)) + 1];
end;

procedure InitializeWizard;
begin
  // Generate random password for cmms database user
  CmmsDbPassword := GenerateRandomPassword(20);

  // Add page for PostgreSQL superuser password (only shown if PostgreSQL needs installing)
  if NeedsPostgreSQL then
  begin
    PostgresPasswordPage := CreateInputQueryPage(wpSelectDir,
      'PostgreSQL Configuration',
      'Set the PostgreSQL superuser (postgres) password.',
      'This password is used for database administration. Keep it safe.');
    PostgresPasswordPage.Add('PostgreSQL Password:', True);
    PostgresPasswordPage.Values[0] := GenerateRandomPassword(16);
  end;
end;

procedure WriteProductionConfig;
var
  ConfigPath: string;
  ConnString: string;
  JwtSecret: string;
  ConfigContent: string;
begin
  ConfigPath := ExpandConstant('{app}\appsettings.Production.json');
  ConnString := 'Host=localhost;Port=5432;Database=CMMS;Username=cmms;Password=' + CmmsDbPassword;
  JwtSecret := GenerateJwtSecret;

  ConfigContent :=
    '{' + #13#10 +
    '  "Kestrel": {' + #13#10 +
    '    "Endpoints": {' + #13#10 +
    '      "Http": {' + #13#10 +
    '        "Url": "http://0.0.0.0:5000"' + #13#10 +
    '      }' + #13#10 +
    '    }' + #13#10 +
    '  },' + #13#10 +
    '  "DatabaseSettings": {' + #13#10 +
    '    "Provider": "PostgreSql"' + #13#10 +
    '  },' + #13#10 +
    '  "ConnectionStrings": {' + #13#10 +
    '    "DefaultConnection": "' + ConnString + '"' + #13#10 +
    '  },' + #13#10 +
    '  "JwtSettings": {' + #13#10 +
    '    "Secret": "' + JwtSecret + '",' + #13#10 +
    '    "Issuer": "CMMS",' + #13#10 +
    '    "Audience": "CMMS-Users",' + #13#10 +
    '    "AccessTokenExpirationMinutes": 60,' + #13#10 +
    '    "RefreshTokenExpirationDays": 7' + #13#10 +
    '  },' + #13#10 +
    '  "Security": {' + #13#10 +
    '    "MaxFailedLoginAttempts": 5,' + #13#10 +
    '    "LockoutMinutes": 15' + #13#10 +
    '  },' + #13#10 +
    '  "Licensing": {' + #13#10 +
    '    "Enabled": true,' + #13#10 +
    '    "LicenseServerUrl": "http://fragbox:5100",' + #13#10 +
    '    "GracePeriodDays": 30,' + #13#10 +
    '    "PhoneHomeIntervalHours": 24,' + #13#10 +
    '    "WarningDaysBeforeExpiry": 14' + #13#10 +
    '  },' + #13#10 +
    '  "LdapSettings": {' + #13#10 +
    '    "Enabled": false' + #13#10 +
    '  },' + #13#10 +
    '  "EmailCalendar": {' + #13#10 +
    '    "Enabled": false' + #13#10 +
    '  },' + #13#10 +
    '  "Cors": {' + #13#10 +
    '    "AllowedOrigins": ["http://localhost:5000"]' + #13#10 +
    '  },' + #13#10 +
    '  "Serilog": {' + #13#10 +
    '    "MinimumLevel": {' + #13#10 +
    '      "Default": "Information",' + #13#10 +
    '      "Override": {' + #13#10 +
    '        "Microsoft": "Warning",' + #13#10 +
    '        "Microsoft.EntityFrameworkCore": "Warning",' + #13#10 +
    '        "System": "Warning"' + #13#10 +
    '      }' + #13#10 +
    '    }' + #13#10 +
    '  },' + #13#10 +
    '  "AllowedHosts": "*"' + #13#10 +
    '}';

  // Only write if config doesn't already exist (preserve on upgrades)
  if not FileExists(ConfigPath) then
    SaveStringToFile(ConfigPath, ConfigContent, False);
end;

procedure CreateWindowsService;
var
  ResultCode: Integer;
  BinPath: string;
begin
  BinPath := ExpandConstant('{app}\CMMS.API.exe');

  // Create the service
  Exec('sc.exe', 'create CmmsService binPath= "' + BinPath + '" start= auto DisplayName= "CMMS Web Application"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  // Set description
  Exec('sc.exe', 'description CmmsService "Computerized Maintenance Management System - Web Application"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  // Configure restart-on-failure recovery (restart after 10s, 30s, 60s)
  Exec('sc.exe', 'failure CmmsService reset= 86400 actions= restart/10000/restart/30000/restart/60000',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CreateFirewallRule;
var
  ResultCode: Integer;
begin
  // Remove existing rule first (ignore errors)
  Exec('netsh', 'advfirewall firewall delete rule name="CMMS Web Application"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  // Add inbound rule for port 5000
  Exec('netsh', 'advfirewall firewall add rule name="CMMS Web Application" dir=in action=allow protocol=TCP localport=5000',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure StartCmmsService;
var
  ResultCode: Integer;
begin
  Exec('sc.exe', 'start CmmsService', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure SetEnvironmentVariable;
var
  ResultCode: Integer;
begin
  // Set ASPNETCORE_ENVIRONMENT for the service via registry
  RegWriteStringValue(HKLM,
    'SYSTEM\CurrentControlSet\Services\CmmsService',
    'Environment',
    'ASPNETCORE_ENVIRONMENT=Production');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Write production config with generated credentials
    WriteProductionConfig;

    // Create and configure the Windows Service
    CreateWindowsService;

    // Set the environment to Production
    SetEnvironmentVariable;

    // Add firewall rule
    CreateFirewallRule;

    // Start the service (first run triggers auto-migration + admin seed)
    StartCmmsService;
  end;
end;
