
namespace CopicanariasServerReport.Services
{
    public static class CleanService
    {
        // Limpia un directorio recursivamente.
        // Devuelve (archivos borrados, bytes liberados).
        // No lanza excepciones: cada fallo se ignora
        public static (int Archivos, long Bytes) CleanDirectory(string ruta)
        {
            int c = 0; long b = 0;
            if (!Directory.Exists(ruta)) return (c, b);

            // ── Archivos del directorio actual ──────────────────────
            string[] files = Array.Empty<string>();
            try { files = Directory.GetFiles(ruta, "*", SearchOption.TopDirectoryOnly); }
            catch { return (c, b); } // Sin acceso: abortamos silenciosamente

            foreach (string file in files)
            {
                try
                {
                    var fi = new FileInfo(file);
                    if (fi.IsReadOnly) fi.IsReadOnly = false;
                    long tam = fi.Length;
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    c++; b += tam;
                }
                catch { } // Archivo en uso o sin permisos: saltar
            }

            // ── Subdirectorios (recursivo) ───────────────────────────
            string[] folders = Array.Empty<string>();
            try { folders = Directory.GetDirectories(ruta); }
            catch { return (c, b); }

            foreach (string folder in folders)
            {
                try
                {
                    var res = CleanDirectory(folder);
                    c += res.Archivos; b += res.Bytes;
                    Directory.Delete(folder, false);
                }
                catch { }
            }
            return (c, b);
        }
    }
}