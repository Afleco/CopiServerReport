using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CopicanariasServerReport.Pdf
{
    public static class PdfGenerator
    {
        // Genera el documento PDF en 'ruta' a partir de los datos del informe.
        // Método síncrono: debe llamarse desde Task.Run para no bloquear la UI.
        public static void Generar(string ruta, DatosServidor r, byte[] logoBytes)
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
                            FilaColor(t, "Estado AV:", r.AntivirusEstado, cAv);

                            if (!string.IsNullOrWhiteSpace(r.AntivirusRuta))
                                Fila(t, "Ruta ejecutable:", r.AntivirusRuta, 8);

                            var cBackup = r.EstadoBackup.Contains("OK") ? Colors.Green.Darken2
                                        : r.EstadoBackup.Contains("Error") ? Colors.Red.Darken2
                                        : Colors.Grey.Darken2;
                            FilaColor(t, "Backup Windows:", $"{r.EstadoBackup}  [{r.FechaUltimoBackup}]", cBackup);
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
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            string espacio = r.BytesLiberados > 1073741824
                                ? $"{r.BytesLiberados / 1073741824.0:F2} GB"
                                : $"{r.BytesLiberados / 1048576.0:F2} MB";
                            Fila(t, "Archivos eliminados:", $"{r.ArchivosBorrados} archivos  ({espacio} liberados)");
                            Fila(t, "Áreas limpiadas:", "Temp usuario  ·  Temp Windows  ·  Caché Windows Update");
                        });

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
                                t.ColumnsDefinition(c => { c.ConstantColumn(45); c.RelativeColumn(); });
                                t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Letra").SemiBold().FontSize(8);
                                t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Ruta de red").SemiBold().FontSize(8);
                                foreach (var u in r.UnidadesRed)
                                {
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.Letra).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.Ruta).FontSize(8);
                                }
                            });
                        }

                        // ── 7. CONTROLADORES (DRIVERS) ────────────────────
                        Seccion(col, "7. Controladores (Drivers)");
                        if (r.DriversConError.Count == 0)
                        {
                            col.Item().PaddingLeft(4)
                                .Text("✅ Todos los dispositivos funcionan correctamente.")
                                .FontColor(Colors.Green.Darken2);
                        }
                        else
                        {
                            col.Item().PaddingLeft(4)
                                .Text($"⚠️  {r.DriversConError.Count} dispositivo(s) con error:")
                                .FontColor(Colors.Red.Darken2).SemiBold();
                            foreach (var d in r.DriversConError)
                                col.Item().PaddingLeft(10).Text($"• {d}").FontSize(8).FontColor(Colors.Red.Darken2);
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
                                    c.RelativeColumn(4); // Modelo
                                    c.RelativeColumn(1); // Interfaz
                                    c.RelativeColumn(1); // Tamaño
                                    c.RelativeColumn(2); // Estado S.M.A.R.T.
                                });
                                foreach (var h in new[] { "Modelo", "Interfaz", "Tamaño", "Estado S.M.A.R.T." })
                                    t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(8);

                                foreach (var d in r.Discos)
                                {
                                    var cSmart = d.Estado.Contains("OK") ? Colors.Green.Darken2 : Colors.Red.Darken2;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Modelo).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Tipo).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{d.TamanoGB:F0} GB").FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Estado).FontColor(cSmart).SemiBold().FontSize(8);
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
                    });

                    // ══ PIE DE PÁGINA ══════════════════════════════════════
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Grupo Copicanarias · Informe de Mantenimiento Automático · Página ")
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