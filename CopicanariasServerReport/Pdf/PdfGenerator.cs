using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CopicanariasServerReport.Pdf
{
    public static class PdfGenerator
    {
        // Genera el documento PDF en 'ruta' a partir de los datos del informe.
        // Método síncrono: debe llamarse desde Task.Run para no bloquear la UI.
        public static void Generar(string ruta, DatosServidor r, byte[] logoBytes, byte[] dfLogoBytes = null)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                    // ══ CABECERA ══════════════════════════════════════════
                    page.Header().Column(hdr =>
                    {
                        hdr.Item().Row(row =>
                        {
                            row.RelativeItem().Column(txt =>
                            {
                                txt.Item().Text("INFORME DE AUDITORÍA Y MANTENIMIENTO")
                                    .SemiBold().FontSize(15).FontColor(Colors.Blue.Darken4);
                                txt.Item().Text("Grupo Copicanarias — Dpto. Sistemas")
                                    .FontSize(11).FontColor(Colors.Grey.Darken1);
                                txt.Item().Text($"Técnico responsable: {r.TecnicoResponsable}")
                                    .FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(100).AlignRight().Height(40).Image(logoBytes).FitArea();
                        });
                        hdr.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Blue.Darken3);
                        hdr.Item().PaddingTop(3).AlignRight()
                            .Text($"Fecha de emisión: {r.FechaHora}   |   Equipo: {r.NombreServidor}")
                            .FontSize(7.5f).FontColor(Colors.Grey.Medium);
                    });

                    // ══ CONTENIDO ═════════════════════════════════════════
                    page.Content().PaddingVertical(0.3f, Unit.Centimetre).Column(col =>
                    {
                        // ── 1. SISTEMA Y HARDWARE ────────────────────────
                        Seccion(col, "1. Sistema y Hardware");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            Fila(t, "Hostname / OS:",
                                $"{r.NombreServidor} · {r.SistemaOperativo} ({r.Arquitectura})");
                            Fila(t, "Memoria RAM:", r.MemoriaRAM);
                            Fila(t, "Usuario activo:", r.UsuarioActivo);
                        });

                        // ── 2. SEGURIDAD ──────────────────────────────────
                        Seccion(col, "2. Seguridad");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            Fila(t, "Antivirus:", r.AntivirusNombre);

                            var cAv = (r.AntivirusEstado.Contains("Activo") || r.AntivirusEstado.Contains("Monitori"))
                                ? Colors.Green.Darken2 : Colors.Red.Darken2;
                            FilaColor(t, "Estado:", r.AntivirusEstado, cAv);

                            if (!string.IsNullOrWhiteSpace(r.AntivirusRuta))
                                Fila(t, "Ruta ejecutable:", r.AntivirusRuta, 8);

                            // Solo mostramos la fila del Backup si el técnico es de DF-Server
                            if (r.EsTecnicoDf)
                            {
                                var cBackup = r.EstadoBackup.Contains("OK") ? Colors.Green.Darken2
                                            : r.EstadoBackup.Contains("Error") ? Colors.Red.Darken2
                                            : Colors.Grey.Darken2;
                                FilaColor(t, "Backup Windows:", $"{r.EstadoBackup}  [{r.FechaUltimoBackup}]", cBackup);
                            }
                        });

                        // ── 3. WINDOWS UPDATE ─────────────────────────────
                        Seccion(col, "3. Windows Update");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });

                            if (!r.UpdatesEjecutado)
                            {
                                Fila(t, "Estado:", "No analizado en esta sesión.");
                            }
                            else if (r.UpdatesImportantes == 0 && r.UpdatesOpcionales == 0)
                            {
                                FilaColor(t, "Estado:", "✅ Sistema completamente actualizado.", Colors.Green.Darken2);
                            }
                            else
                            {
                                FilaColor(t, "Estado:",
                                    $"⚠️  {r.UpdatesImportantes} importantes  |  {r.UpdatesOpcionales} opcionales pendientes",
                                    Colors.Orange.Darken3);

                                if (r.NombresUpdates.Count > 0)
                                {
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text("Actualizaciones pendientes:").SemiBold();
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Column(lc =>
                                    {
                                        foreach (var u in r.NombresUpdates)
                                            lc.Item().Text($"• {u}").FontSize(8);
                                    });
                                }
                            }

                            if (r.UpdatesEjecutado)
                            {
                                var cReboot = r.RequiereReinicio ? Colors.Red.Darken2 : Colors.Green.Darken2;
                                FilaColor(t, "Reinicio pendiente:",
                                    r.RequiereReinicio ? "⚠️  Sí — Se debe reiniciar el equipo" : "✅ No requerido",
                                    cReboot);
                            }
                        });

                        // ── 4. SOFTWARE CLAVE ─────────────────────────────
                        Seccion(col, "4. Software Clave");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            bool javaInstalado = !string.IsNullOrEmpty(r.VersionJava)
                                                 && !r.VersionJava.Contains("No instalado");
                            var cJava = !javaInstalado ? Colors.Grey.Darken1
                                      : r.JavaAlDia ? Colors.Green.Darken2
                                      : Colors.Orange.Darken3;
                            FilaColor(t, "Java (JRE/JDK):", r.VersionJava, cJava);
                            if (!string.IsNullOrEmpty(r.JavaVersionOnline))
                                Fila(t, "Última versión LTS:", r.JavaVersionOnline);
                        });

                        // ── 5. LIMPIEZA ───────────────────────────────────
                        Seccion(col, "5. Limpieza de Archivos Temporales");
                        if (!r.LimpiezaEjecutada)
                        {
                            col.Item().PaddingLeft(4)
                                .Text("— Limpieza no realizada en esta sesión.")
                                .FontColor(Colors.Grey.Darken2).Italic();
                        }
                        else if (r.ArchivosBorrados == 0)
                        {
                            col.Item().PaddingLeft(4)
                                .Text("✅ Limpieza ejecutada — No se encontraron archivos temporales.")
                                .FontColor(Colors.Green.Darken2);
                        }
                        else
                        {
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                                string espacio = r.BytesLiberados > 1073741824
                                    ? $"{r.BytesLiberados / 1073741824.0:F2} GB"
                                    : $"{r.BytesLiberados / 1048576.0:F2} MB";
                                Fila(t, "Archivos eliminados:", $"{r.ArchivosBorrados} archivos  ({espacio} liberados)");
                                Fila(t, "Áreas limpiadas:", "Temp usuario  ·  Temp Windows  ·  Caché Windows Update");
                            });
                        }

                        // ── 6. INTERFACES DE RED ──────────────────────────
                        Seccion(col, "6. Interfaces de Red");
                        if (r.InterfacesRed.Count == 0)
                        {
                            col.Item().PaddingLeft(4).Text("No se detectaron interfaces de red activas.")
                                .FontColor(Colors.Red.Medium);
                        }
                        else
                        {
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                });
                                foreach (var h in new[] { "Adaptador", "Tipo", "Velocidad", "Estado" })
                                    t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(8);

                                foreach (var rr in r.InterfacesRed)
                                {
                                    var cEst = rr.Estado == "Conectado" ? Colors.Green.Darken2 : Colors.Grey.Darken1;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(rr.Nombre).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(rr.Tipo).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(rr.Velocidad).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(rr.Estado).FontColor(cEst).FontSize(8);
                                }
                            });
                        }

                        if (r.UnidadesRed.Count > 0)
                        {
                            col.Item().PaddingTop(7).PaddingBottom(2)
                                .Text("Unidades de Red Mapeadas:").SemiBold().FontSize(9);
                            col.Item().Table(t =>
                            {
                                // Actualizamos la definición de columnas para que coincida con la de discos locales
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(38);  // Letra
                                    c.RelativeColumn(3);   // Ruta de red (más ancha para la ruta UNC)
                                    c.RelativeColumn(1.2f);// Total
                                    c.RelativeColumn(1.2f);// Libre
                                    c.RelativeColumn(1);   // % Libre
                                    c.RelativeColumn(2);   // Uso visual
                                });

                                // cabeceras
                                foreach (var h in new[] { "Letra", "Ruta de red", "Total", "Libre", "% Libre", "Uso visual" })
                                    t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(8);

                                foreach (var u in r.UnidadesRed)
                                {
                                    // Comprobamos si se pudo leer el espacio. Si TotalGB es 0, asumimos que no hay acceso.
                                    bool accesoOk = u.TotalGB > 0;
                                    bool critico = accesoOk && u.PorcentajeLibre < 20;
                                    var cTexto = critico ? Colors.Red.Darken2 : Colors.Black;
                                    var cBarra = critico ? Colors.Red.Medium : Colors.Blue.Medium;

                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(u.Letra).SemiBold().FontSize(8);

                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(u.Ruta).FontSize(8);

                                    if (accesoOk)
                                    {
                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text($"{u.TotalGB:F1} GB").FontSize(8);

                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text($"{u.LibreGB:F1} GB").FontColor(cTexto).FontSize(8);

                                        var pctText = t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text($"{u.PorcentajeLibre:F1}%{(critico ? " ⚠️" : "")}").FontColor(cTexto).FontSize(8);
                                        if (critico) pctText.SemiBold();

                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text(u.UsoVisual).FontColor(cBarra).FontSize(7);
                                    }
                                    else
                                    {
                                        // Si no se pudo leer el espacio (ej. servidor apagado o sin permisos)
                                        t.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text(u.UsoVisual).FontColor(Colors.Grey.Darken1).Italic().FontSize(8);
                                    }
                                }
                            });
                        }

                        // ── 7. CONTROLADORES (DRIVERS) ────────────────────
                        Seccion(col, "7. Controladores (Drivers)");
                        if (r.Drivers.Count == 0)
                        {
                            col.Item().PaddingLeft(4)
                                .Text("✅ Todos los dispositivos funcionan correctamente.")
                                .FontColor(Colors.Green.Darken2);
                        }
                        else
                        {
                            col.Item().PaddingLeft(4).PaddingBottom(4)
                                .Text($"⚠️  {r.Drivers.Count} dispositivo(s) con problemas detectados:")
                                .FontColor(Colors.Red.Darken2).SemiBold();

                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3); // Dispositivo
                                    c.RelativeColumn(2); // Fabricante
                                    c.RelativeColumn(1); // Código
                                    c.RelativeColumn(4); // Descripción del error
                                    c.RelativeColumn(2); // Versión driver
                                    c.RelativeColumn(2); // Proveedor
                                });

                                foreach (var h in new[] { "Dispositivo", "Fabricante", "Cód.", "Descripción del error", "Versión driver", "Proveedor" })
                                    t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(7.5f);

                                foreach (var d in r.Drivers)
                                {
                                    string versionTxt = d.TieneDriver ? d.VersionDriver : "Sin driver";
                                    string proveedorTxt = d.TieneDriver ? d.ProveedorDriver : "—";
                                    var cVersion = d.TieneDriver ? Colors.Black : Colors.Red.Darken2;

                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Nombre).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Fabricante).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.CodigoError.ToString()).FontColor(Colors.Red.Darken2).SemiBold().FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.DescripcionError).FontColor(Colors.Red.Darken2).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(versionTxt).FontColor(cVersion).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(proveedorTxt).FontSize(7.5f);
                                }
                            });
                        }

                        // ── 8. ALMACENAMIENTO ─────────────────────────────
                        Seccion(col, "8. Almacenamiento");

                        if (r.Discos.Count > 0)
                        {
                            col.Item().PaddingBottom(3).Text("Discos físicos (S.M.A.R.T.):").SemiBold().FontSize(9);
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(4);    // Modelo
                                    c.RelativeColumn(1.2f); // Interfaz
                                    c.RelativeColumn(1);    // Tamaño
                                    c.RelativeColumn(2);    // Estado S.M.A.R.T.
                                    c.ConstantColumn(36);   // Temp
                                    c.ConstantColumn(48);   // Horas
                                    c.ConstantColumn(44);   // Salud
                                });
                                foreach (var h in new[] { "Modelo", "Interfaz", "Tamaño", "Estado S.M.A.R.T.", "Temp", "Horas", "Salud" })
                                    t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(7.5f);

                                foreach (var d in r.Discos)
                                {
                                    var cSmart = d.Estado.Contains("OK") ? Colors.Green.Darken2 : Colors.Red.Darken2;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Modelo).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Tipo).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{d.TamanoGB:F0} GB").FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Estado).FontColor(cSmart).SemiBold().FontSize(7.5f);

                                    // Temperatura
                                    string tempTxt = d.Temperatura.HasValue ? $"{d.Temperatura}°C" : "—";
                                    var cTemp = d.Temperatura.HasValue && d.Temperatura > 55
                                        ? Colors.Red.Darken2
                                        : d.Temperatura.HasValue && d.Temperatura > 45
                                            ? Colors.Orange.Darken3
                                            : Colors.Black;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(tempTxt).FontColor(cTemp).FontSize(7.5f);

                                    // Horas encendido
                                    string horasTxt = d.HorasEncendido.HasValue
                                        ? $"{d.HorasEncendido:N0}h" : "—";
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(horasTxt).FontSize(7.5f);

                                    // % Salud (solo SSDs con atributo 202)
                                    string saludTxt = d.TieneDatosSalud
                                        ? $"{d.PorcentajeSalud}%" : "—";
                                    var cSalud = d.TieneDatosSalud
                                        ? (d.PorcentajeSalud < 10 ? Colors.Red.Darken2
                                           : d.PorcentajeSalud < 30 ? Colors.Orange.Darken3
                                           : Colors.Green.Darken2)
                                        : Colors.Grey.Darken1;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(saludTxt).FontColor(cSalud).FontSize(7.5f);
                                }
                            });
                            col.Item().PaddingTop(6);
                        }

                        col.Item().PaddingBottom(3).Text("Volúmenes lógicos:").SemiBold().FontSize(9);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(38);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                            });
                            foreach (var h in new[] { "Vol.", "Total", "Libre", "% Libre", "Uso visual" })
                                t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(8);

                            foreach (var d in r.DiscosLogicos)
                            {
                                bool critico = d.PorcentajeLibre < 20;
                                var cDisco = critico ? Colors.Red.Darken2 : Colors.Black;
                                int bloques = Math.Clamp((int)((100 - d.PorcentajeLibre) / 10), 0, 10);
                                string barra = new string('█', bloques) + new string('░', 10 - bloques);

                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(d.Letra).SemiBold().FontSize(8);
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text($"{d.TotalGB:F1} GB").FontSize(8);
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text($"{d.LibreGB:F1} GB").FontColor(cDisco).FontSize(8);
                                var pctText = t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text($"{d.PorcentajeLibre:F1}%{(critico ? " ⚠️" : "")}").FontColor(cDisco).FontSize(8);
                                if (critico) pctText.SemiBold();
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(barra).FontColor(critico ? Colors.Red.Medium : Colors.Blue.Medium).FontSize(7);
                            }
                        });
                        // ── 9. DF-SERVER (solo si el técnico es DF) ──────────
                        if (r.EsTecnicoDf)
                        {
                            var df = r.DfServer;

                            // Cabecera de sección con fondo azul DF 
                            col.Item().PaddingTop(10).Background(Colors.Blue.Darken3)
                                .Padding(5).Row(row =>
                                {
                                    row.RelativeItem().AlignMiddle()
                                        .Text("9. Servicios DF-Server")
                                        .FontSize(11).SemiBold().FontColor(Colors.White);
                                    if (dfLogoBytes != null)
                                        row.ConstantItem(80).AlignRight().Height(28)
                                            .Image(dfLogoBytes).FitArea();
                                });
                            col.Item().PaddingBottom(4);

                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });

                                // Digitalización certificada
                                var cDig = df.DigitalizacionCertificada
                                    ? Colors.Green.Darken2 : Colors.Grey.Darken2;
                                FilaColor(t, "Digitalización certificada:",
                                    df.DigitalizacionCertificada
                                        ? "✅ Activa y funcionando"
                                        : "— No tiene / No activa", cDig);

                                // Firmas DF-Signature
                                if (df.TieneFirmas)
                                {
                                    bool sinStock = df.FirmasRestantes == 0;
                                    bool pocasFirmas = df.FirmasRestantes <= 100;
                                    var cFirmas = pocasFirmas ? Colors.Red.Darken2 : Colors.Green.Darken2;
                                    string textoFirmas;
                                    if (sinStock)
                                        textoFirmas = "❌ Sin stock de firmas · Cliente avisado";
                                    else if (pocasFirmas)
                                        textoFirmas = $"⚠️  {df.FirmasRestantes} firmas restantes — Stock bajo · Cliente avisado";
                                    else
                                        textoFirmas = $"✅ {df.FirmasRestantes} firmas restantes";
                                    FilaColor(t, "Firmas DF-Signature:", textoFirmas, cFirmas);
                                }
                                else
                                {
                                    Fila(t, "Firmas DF-Signature:", "No tiene el módulo");
                                }
                            });

                            // Tabla de certificados digitales
                            if (df.TieneCertificados && df.Certificados.Count > 0)
                            {
                                col.Item().PaddingTop(6).PaddingBottom(2)
                                    .Text("Certificados digitales:").SemiBold().FontSize(9);

                                col.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn(4); // Nombre
                                        c.RelativeColumn(2); // Fecha caducidad
                                        c.RelativeColumn(3); // Estado
                                    });

                                    foreach (var h in new[] { "Certificado", "Fecha caducidad", "Estado" })
                                        t.Cell().Background(Colors.Blue.Lighten4).Padding(3)
                                            .Text(h).SemiBold().FontSize(8);

                                    foreach (var cert in df.Certificados)
                                    {
                                        int diasRestantes = (cert.FechaCaducidad.Date - DateTime.Today).Days;
                                        bool caducado = diasRestantes < 0;
                                        bool proximo = cert.ProximoACaducar && !caducado;

                                        string estadoTxt = caducado
                                            ? "❌ Caducado · Cliente avisado"
                                            : proximo
                                                ? $"⚠️  Caduca en {diasRestantes} días · Cliente avisado"
                                                : $"✅ Válido ({diasRestantes} días)";

                                        var cEstado = caducado ? Colors.Red.Darken2
                                                    : proximo ? Colors.Orange.Darken3
                                                    : Colors.Green.Darken2;

                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                            .Padding(3).Text(cert.Nombre).FontSize(8);
                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                            .Padding(3).Text(cert.FechaCaducidad.ToString("dd/MM/yyyy")).FontSize(8);
                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                            .Padding(3).Text(estadoTxt).FontColor(cEstado).SemiBold().FontSize(8);
                                    }
                                });
                            }
                            else if (!df.TieneCertificados)
                            {
                                col.Item().PaddingLeft(4).PaddingTop(4)
                                    .Text("Sin certificados digitales registrados.")
                                    .FontColor(Colors.Grey.Darken2).FontSize(8.5f);
                            }
                        }
                    });

                    // ══ PIE DE PÁGINA ══════════════════════════════════════
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Grupo Copicanarias · Informe de Preventiva · Página ")
                            .FontSize(7).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                        x.Span(" de ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(ruta);
        }

        // ── Helpers de layout ────────────────────────────────────────

        private static void Seccion(ColumnDescriptor col, string titulo)
        {
            col.Item().PaddingTop(10).PaddingBottom(2).Text(titulo)
                .FontSize(11).SemiBold().FontColor(Colors.Blue.Darken3);
            col.Item().LineHorizontal(1).LineColor(Colors.Blue.Darken2);
            col.Item().PaddingBottom(4);
        }

        private static void Fila(TableDescriptor t, string etiqueta, string valor, float fontSize = 9)
        {
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(etiqueta).SemiBold();
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(valor).FontSize(fontSize);
        }

        private static void FilaColor(TableDescriptor t, string etiqueta, string valor, string color)
        {
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(etiqueta).SemiBold();
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(valor).FontColor(color).SemiBold();
        }
    }
}