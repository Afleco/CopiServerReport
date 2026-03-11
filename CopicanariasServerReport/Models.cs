using System;
using System.Collections.Generic;

namespace CopicanariasServerReport
{
    public class DiscoInfo
    {
        public string Modelo { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Estado { get; set; } = "";
        public double TamanoGB { get; set; } = 0;
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
    }

    public class DatosServidor
    {
        public string TecnicoResponsable { get; set; } = "No asignado";
        public string NombreServidor { get; set; } = Environment.MachineName;
        public string SistemaOperativo { get; set; } = string.Empty;
        public string Arquitectura { get; set; } = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        public string UsuarioActivo { get; set; } = Environment.UserName;
        public string FechaHora { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        public string MemoriaRAM { get; set; } = string.Empty;

        public string EstadoBackup { get; set; } = "No configurado";
        public string FechaUltimoBackup { get; set; } = "--/--/----";

        public int ArchivosBorrados { get; set; } = 0;
        public long BytesLiberados { get; set; } = 0;

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
        public List<string> DriversConError { get; set; } = new();

        public string VersionJava { get; set; } = "";
        public string JavaVersionOnline { get; set; } = "";
        public bool JavaAlDia { get; set; } = false;
    }
}