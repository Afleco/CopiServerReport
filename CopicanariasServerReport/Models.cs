
namespace CopicanariasServerReport
{
    public class DiskInfo
    {
        public string Model { get; set; } = "";
        public string Type { get; set; } = "";
        public string State { get; set; } = "";
        public double SizeGB { get; set; } = 0;

        // ── Datos S.M.A.R.T. detallados (obtenidos de root\wmi) ─────
        // Son nullable porque no todos los discos/controladores los exponen.
        public int? Temperature { get; set; } = null;       // ºC  (atributo 194 ó 190)
        public int? HoursUsed { get; set; } = null;    // h   (atributo 9)
        public int? HealthPercent { get; set; } = null;   // %   (atributo 202 — solo SSDs)
        public bool HasHealthData { get; set; } = false;  // true si el atributo 202 existe
    }

    public class LogicDiskInfo
    {
        public string Letter { get; set; } = "";
        public double TotalGB { get; set; } = 0;
        public double FreeGB { get; set; } = 0;
        public double FreePercent { get; set; } = 0;
    }

    public class NetworkInterfaceInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Speed { get; set; } = "";
        public string State { get; set; } = "";
    }

    public class NetworkDrivesInfo
    {
        public string Letter { get; set; } = "";
        public string Path { get; set; } = "";
        public double TotalGB { get; set; } = 0;
        public double FreeGB { get; set; } = 0;
        public double FreePercent { get; set; } = 0;
        public string VisualUse { get; set; } = "";
    }

    public class DriverInfo
    {
        public string Name { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public int ErrorCode { get; set; } = 0;
        public string ErrorDescription { get; set; } = "";
        public string DriverProvider { get; set; } = "Sin información";
        public string DriverVersion { get; set; } = "Sin driver instalado";
        public bool HasDriver { get; set; } = false;
    }

    // ── DF-Server ────────────────────────────────────────────────────

    public class DigitalCertificate
    {
        public string Name { get; set; } = "";
        public DateTime ExpirationDate { get; set; } = DateTime.Today.AddYears(1);

        // Caduca en 3 meses o menos (92 días como margen seguro)
        public bool IsExpiringSoon =>
            (ExpirationDate.Date - DateTime.Today).TotalDays <= 92;
    }

    public class DfServerInfo
    {
        public string Version { get; set; } = "No detectada";
        public bool HasCertifiedDigitization { get; set; } = false;
        public bool HasSignatures { get; set; } = false;
        public int RemainingSignatures { get; set; } = 0;
        public bool HasCertificates { get; set; } = false;
        public List<DigitalCertificate> Certificates { get; set; } = new();
    }

    // ── Modelo principal ─────────────────────────────────────────────

    public class ServerData
    {
        public string AssignedTechnician { get; set; } = "No asignado";
        public bool IsDfTechnician { get; set; } = false;
        public string ServerName { get; set; } = Environment.MachineName;
        public string OS { get; set; } = string.Empty;
        public string Architecture { get; set; } = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        public string ActiveUser { get; set; } = "";
        public string DateTimeString { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        public string RAM { get; set; } = string.Empty;

        public string BackupState { get; set; } = "No configurado";
        public string LastBackupDate { get; set; } = "--/--/----";

        public int DeletedFiles { get; set; } = 0;
        public long FreedBytes { get; set; } = 0;
        public bool IsCleanupExecuted { get; set; } = false;

        public bool IsDriversExecuted { get; set; }

        public List<DiskInfo> Disks { get; set; } = new();
        public List<LogicDiskInfo> LogicDisks { get; set; } = new();

        public bool IsUpdatesExecuted { get; set; } = false;
        public int ImportantUpdates { get; set; } = 0;
        public int OptionalUpdates { get; set; } = 0;
        public bool IsRestartRequired { get; set; } = false;
        public List<string> UpdateNames { get; set; } = new();

        public string AntivirusName { get; set; } = "";
        public string AntivirusState { get; set; } = "";
        public string AntivirusPath { get; set; } = "";

        public List<NetworkInterfaceInfo> NetworkInterfaces { get; set; } = new();
        public List<NetworkDrivesInfo> NetworkDrives { get; set; } = new();
        public List<DriverInfo> Drivers { get; set; } = new();

        public string JavaVersion { get; set; } = "";
        public string JavaVersionOnline { get; set; } = "";
        public bool IsJavaUpToDate { get; set; } = false;

        public DfServerInfo DfServer { get; set; } = new();
    }
}