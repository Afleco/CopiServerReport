using System;
using System.IO;

namespace CopicanariasServerReport.Services
{
    public static class LimpiezaService
    {
        // Limpia un directorio recursivamente.
        // Devuelve (archivos borrados, bytes liberados).
        // Nunca lanza excepciones: cada fallo se ignora silenciosamente.
        public static (int Archivos, long Bytes) LimpiarDirectorio(string ruta)
        {
            int c = 0; long b = 0;
            if (!Directory.Exists(ruta)) return (c, b);

            // ── Archivos del directorio actual ──────────────────────
            string[] archivos = Array.Empty<string>();
            try { archivos = Directory.GetFiles(ruta, "*", SearchOption.TopDirectoryOnly); }
            catch { return (c, b); } // Sin acceso: abortamos silenciosamente

            foreach (string archivo in archivos)
            {
                try
                {
                    var fi = new FileInfo(archivo);
                    if (fi.IsReadOnly) fi.IsReadOnly = false;
                    long tam = fi.Length;
                    File.SetAttributes(archivo, FileAttributes.Normal);
                    File.Delete(archivo);
                    c++; b += tam;
                }
                catch { } // Archivo en uso o sin permisos: saltar
            }

            // ── Subdirectorios (recursivo) ───────────────────────────
            string[] carpetas = Array.Empty<string>();
            try { carpetas = Directory.GetDirectories(ruta); }
            catch { return (c, b); }

            foreach (string carpeta in carpetas)
            {
                try
                {
                    var res = LimpiarDirectorio(carpeta);
                    c += res.Archivos; b += res.Bytes;
                    Directory.Delete(carpeta, false);
                }
                catch { }
            }
            return (c, b);
        }
    }
}