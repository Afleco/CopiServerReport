
namespace CopicanariasServerReport
{
    public class DiscoInfo
    {
        public string Modelo { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Estado { get; set; } = "";
        public double TamanoGB { get; set; } = 0;

        // ── Datos S.M.A.R.T. detallados (obtenidos de root\wmi) ─────
        // Son nullable porque no todos los discos/controladores los exponen.
        public int? Temperatura { get; set; } = null;       // ºC  (atributo 194 ó 190)
        public int? HorasEncendido { get; set; } = null;    // h   (atributo 9)
        public int? PorcentajeSalud { get; set; } = null;   // %   (atributo 202 — solo SSDs)
        public bool TieneDatosSalud { get; set; } = false;  // true si el atributo 202 existe
    }

    public class DiscoLogicoInfo
    {
        public string Letra { get; set; } = "";
        public double TotalGB { get; set; } = 0;
        public double LibreGB { get; set; } = 0;
        public double PorcentajeLibre { get; set; } = 0;
    }

    public class RedInfo
    {
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Velocidad { get; set; } = "";
        public string Estado { get; set; } = "";
    }

    public class UnidadRedInfo
    {
        public string Letra { get; set; } = "";
        public string Ruta { get; set; } = "";
        public double TotalGB { get; set; } = 0;
        public double LibreGB { get; set; } = 0;
        public double PorcentajeLibre { get; set; } = 0;
        public string UsoVisual { get; set; } = "";
    }

    public class DriverInfo
    {
        public string Nombre { get; set; } = "";
        public string Fabricante { get; set; } = "";
        public int CodigoError { get; set; } = 0;
        public string DescripcionError { get; set; } = "";
        public string ProveedorDriver { get; set; } = "Sin información";
        public string VersionDriver { get; set; } = "Sin driver instalado";
        public bool TieneDriver { get; set; } = false;
    }

    // ── DF-Server ────────────────────────────────────────────────────

    public class CertificadoDigital
    {
        public string Nombre { get; set; } = "";
        public DateTime FechaCaducidad { get; set; } = DateTime.Today.AddYears(1);

        // Caduca en 3 meses o menos (92 días como margen seguro)
        public bool ProximoACaducar =>
            (FechaCaducidad.Date - DateTime.Today).TotalDays <= 92;
    }

    public class DfServerInfo
    {
        public string VersionSoftware { get; set; } = "No detectada";
        public bool DigitalizacionCertificada { get; set; } = false;
        public bool TieneFirmas { get; set; } = false;
        public int FirmasRestantes { get; set; } = 0;
        public bool TieneCertificados { get; set; } = false;
        public List<CertificadoDigital> Certificados { get; set; } = new();
    }

    // ── Modelo principal ─────────────────────────────────────────────

    public class DatosServidor
    {
        public string TecnicoResponsable { get; set; } = "No asignado";
        public bool EsTecnicoDf { get; set; } = false;
        public string NombreServidor { get; set; } = Environment.MachineName;
        public string SistemaOperativo { get; set; } = string.Empty;
        public string Arquitectura { get; set; } = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        public string UsuarioActivo { get; set; } = "";
        public string FechaHora { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        public string MemoriaRAM { get; set; } = string.Empty;

        public string EstadoBackup { get; set; } = "No configurado";
        public string FechaUltimoBackup { get; set; } = "--/--/----";

        public int ArchivosBorrados { get; set; } = 0;
        public long BytesLiberados { get; set; } = 0;
        public bool LimpiezaEjecutada { get; set; } = false;

        public bool DriversEjecutado { get; set; }

        public List<DiscoInfo> Discos { get; set; } = new();
        public List<DiscoLogicoInfo> DiscosLogicos { get; set; } = new();

        public bool UpdatesEjecutado { get; set; } = false;
        public int UpdatesImportantes { get; set; } = 0;
        public int UpdatesOpcionales { get; set; } = 0;
        public bool RequiereReinicio { get; set; } = false;
        public List<string> NombresUpdates { get; set; } = new();

        public string AntivirusNombre { get; set; } = "";
        public string AntivirusEstado { get; set; } = "";
        public string AntivirusRuta { get; set; } = "";

        public List<RedInfo> InterfacesRed { get; set; } = new();
        public List<UnidadRedInfo> UnidadesRed { get; set; } = new();
        public List<DriverInfo> Drivers { get; set; } = new();

        public string VersionJava { get; set; } = "";
        public string JavaVersionOnline { get; set; } = "";
        public bool JavaAlDia { get; set; } = false;

        public DfServerInfo DfServer { get; set; } = new();
    }
}