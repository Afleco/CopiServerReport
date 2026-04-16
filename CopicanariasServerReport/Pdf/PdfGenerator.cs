using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace CopicanariasServerReport.Pdf
{
    public static class PdfGenerator
    {
        // Genera el documento PDF en 'ruta' a partir de los datos del informe.
        // Método síncrono: debe llamarse desde Task.Run para no bloquear la UI.
        public static void Generate(string path, ServerData r, byte[] logoBytes, byte[] dfLogoBytes = null)
        {
            // PROCESAMOS EL LOGO PARA HACERLO SEMITRANSPARENTE (6% de opacidad)
            byte[] watermarkBytes = MakeImageTransparent(logoBytes, 0.06f);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                    // ══ MARCA DE AGUA (FONDO SEMITRANSPARENTE PROCESADO) ══
                    page.Background()
                        .AlignCenter()
                        .AlignMiddle()
                        .Container()
                        .Width(14, Unit.Centimetre)
                        .Image(watermarkBytes); // Le pasamos la imagen ya procesada

                    // ══ CABECERA ══════════════════════════════════════════
                    page.Header().Column(hdr =>
                    {
                        // LÓGICA DE NOMBRES Y DEPARTAMENTOS
                        bool isDfTechnician = r.AssignedTechnician != null && r.AssignedTechnician.Contains("(DF-Server)");

                        // 1. Nombre del departamento
                        string departamentName = isDfTechnician ? "Departamento de DF-Server" : "Departamento de Sistemas";

                        // 2. Limpiar el nombre del técnico (quitamos " (DF-Server)" si existe)
                        string cleanTechnician = r.AssignedTechnician?.Replace(" (DF-Server)", "").Trim() ?? "No especificado";

                        hdr.Item().Row(row =>
                        {
                            row.RelativeItem().Column(txt =>
                            {
                                txt.Item().Text("INFORME DE AUDITORÍA Y MANTENIMIENTO")
                                    .SemiBold().FontSize(15).FontColor(Colors.Blue.Darken4);

                                // Usamos el nombre del departamento dinámico
                                txt.Item().Text($"Grupo Copicanarias — {departamentName}")
                                    .FontSize(11).FontColor(Colors.Grey.Darken1);

                                // Usamos el nombre del técnico limpio
                                txt.Item().Text($"Técnico responsable: {cleanTechnician}")
                                    .FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(100).AlignRight().Height(40).Image(logoBytes).FitArea();
                        });
                        hdr.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Blue.Darken3);
                        hdr.Item().PaddingTop(3).AlignRight()
                            .Text($"Fecha de emisión: {r.DateTimeString}   |   Equipo: {r.ServerName}")
                            .FontSize(7.5f).FontColor(Colors.Grey.Medium);
                    });

                    // ══ CONTENIDO ═════════════════════════════════════════
                    page.Content().PaddingVertical(0.3f, Unit.Centimetre).Column(col =>
                    {
                        // ── 1. SISTEMA Y HARDWARE ────────────────────────
                        Section(col, "1. Sistema y Hardware");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            Row(t, "Hostname / OS:",
                                $"{r.ServerName} · {r.OS} ({r.Architecture})");
                            Row(t, "Memoria RAM:", r.RAM);
                            Row(t, "Usuario activo:", r.ActiveUser);
                        });

                        // ── 2. SEGURIDAD ──────────────────────────────────
                        Section(col, "2. Seguridad");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            Row(t, "Antivirus:", r.AntivirusName);

                            var cAv = (r.AntivirusState.Contains("Activo") || r.AntivirusState.Contains("Monitori"))
                                ? Colors.Green.Darken2 : Colors.Red.Darken2;
                            RowColor(t, "Estado:", r.AntivirusState, cAv);

                            if (!string.IsNullOrWhiteSpace(r.AntivirusPath))
                                Row(t, "Ruta ejecutable:", r.AntivirusPath, 8);

                            // Solo mostramos la fila del Backup si el técnico es de DF-Server
                            if (r.IsDfTechnician)
                            {
                                var cBackup = r.BackupState.Contains("OK") ? Colors.Green.Darken2
                                            : r.BackupState.Contains("Error") ? Colors.Red.Darken2
                                            : Colors.Grey.Darken2;
                                RowColor(t, "Backup Windows:", $"{r.BackupState}  [{r.LastBackupDate}]", cBackup);
                            }
                        });

                        // ── 3. WINDOWS UPDATE ─────────────────────────────
                        Section(col, "3. Windows Update");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });

                            if (!r.IsUpdatesExecuted)
                            {
                                Row(t, "Estado:", "No analizado en esta sesión.");
                            }
                            else if (r.ImportantUpdates == 0 && r.OptionalUpdates == 0)
                            {
                                RowColor(t, "Estado:", "✅ Sistema completamente actualizado.", Colors.Green.Darken2);
                            }
                            else
                            {
                                RowColor(t, "Estado:",
                                    $"⚠️  {r.ImportantUpdates} importantes  |  {r.OptionalUpdates} opcionales pendientes",
                                    Colors.Orange.Darken3);

                                if (r.UpdateNames.Count > 0)
                                {
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text("Actualizaciones pendientes:").SemiBold();
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Column(lc =>
                                    {
                                        foreach (var u in r.UpdateNames)
                                            lc.Item().Text($"• {u}").FontSize(8);
                                    });
                                }
                            }

                            if (r.IsUpdatesExecuted)
                            {
                                var cReboot = r.IsRestartRequired ? Colors.Red.Darken2 : Colors.Green.Darken2;
                                RowColor(t, "Reinicio pendiente:",
                                    r.IsRestartRequired ? "⚠️  Sí — Se debe reiniciar el equipo" : "✅ No requerido",
                                    cReboot);
                            }
                        });

                        // ── 4. SOFTWARE CLAVE ─────────────────────────────
                        Section(col, "4. Software Clave");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            bool isJavaInstalled = !string.IsNullOrEmpty(r.JavaVersion)
                                                 && !r.JavaVersion.Contains("No instalado");
                            var cJava = !isJavaInstalled ? Colors.Grey.Darken1
                                      : r.IsJavaUpToDate ? Colors.Green.Darken2
                                      : Colors.Orange.Darken3;
                            RowColor(t, "Java (JRE):", r.JavaVersion, cJava);
                            if (!string.IsNullOrEmpty(r.JavaVersionOnline))
                                Row(t, "Última versión:", r.JavaVersionOnline);
                        });

                        // ── 5. LIMPIEZA ───────────────────────────────────
                        Section(col, "5. Limpieza de Archivos Temporales");
                        if (!r.IsCleanupExecuted)
                        {
                            col.Item().PaddingLeft(4)
                                .Text("— Limpieza no realizada en esta sesión.")
                                .FontColor(Colors.Grey.Darken2).Italic();
                        }
                        else if (r.DeletedFiles == 0)
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
                                string espacio = r.FreedBytes > 1073741824
                                    ? $"{r.FreedBytes / 1073741824.0:F2} GB"
                                    : $"{r.FreedBytes / 1048576.0:F2} MB";
                                Row(t, "Archivos eliminados:", $"{r.DeletedFiles} archivos  ({espacio} liberados)");
                                Row(t, "Áreas limpiadas:", "Temp de usuarios  ·  Temp Windows  ·  Caché Windows Update");
                            });
                        }

                        // ── 6. INTERFACES DE RED ──────────────────────────
                        Section(col, "6. Interfaces de Red");
                        if (r.NetworkInterfaces.Count == 0)
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

                                foreach (var rr in r.NetworkInterfaces)
                                {
                                    var cEst = rr.State == "Conectado" ? Colors.Green.Darken2 : Colors.Grey.Darken1;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(rr.Name).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(rr.Type).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(rr.Speed).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(rr.State).FontColor(cEst).FontSize(8);
                                }
                            });
                        }

                        if (r.NetworkDrives.Count > 0)
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

                                foreach (var u in r.NetworkDrives)
                                {
                                    // Comprobamos si se pudo leer el espacio. Si TotalGB es 0, asumimos que no hay acceso.
                                    bool accessOk = u.TotalGB > 0;
                                    bool critical = accessOk && u.FreePercent < 20;
                                    var cText = critical ? Colors.Red.Darken2 : Colors.Black;
                                    var cBar = critical ? Colors.Red.Medium : Colors.Blue.Medium;

                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(u.Letter).SemiBold().FontSize(8);

                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(u.Path).FontSize(8);

                                    if (accessOk)
                                    {
                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text($"{u.TotalGB:F1} GB").FontSize(8);

                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text($"{u.FreeGB:F1} GB").FontColor(cText).FontSize(8);

                                        var pctText = t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text($"{u.FreePercent:F1}%{(critical ? " ⚠️" : "")}").FontColor(cText).FontSize(8);
                                        if (critical) pctText.SemiBold();

                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text(u.VisualUse).FontColor(cBar).FontSize(7);
                                    }
                                    else
                                    {
                                        // Si no se pudo leer el espacio (ej. servidor apagado o sin permisos)
                                        t.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text(u.VisualUse).FontColor(Colors.Grey.Darken1).Italic().FontSize(8);
                                    }
                                }
                            });
                        }

                        // ── 7. CONTROLADORES (DRIVERS) ────────────────────
                        Section(col, "7. Controladores (Drivers)");
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
                                    string versionTxt = d.HasDriver ? d.DriverVersion : "Sin driver";
                                    string providerTxt = d.HasDriver ? d.DriverProvider : "—";
                                    var cVersion = d.HasDriver ? Colors.Black : Colors.Red.Darken2;

                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Name).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Manufacturer).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.ErrorCode.ToString()).FontColor(Colors.Red.Darken2).SemiBold().FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.ErrorDescription).FontColor(Colors.Red.Darken2).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(versionTxt).FontColor(cVersion).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(providerTxt).FontSize(7.5f);
                                }
                            });
                        }

                        // ── 8. ALMACENAMIENTO ─────────────────────────────
                        Section(col, "8. Almacenamiento");

                        if (r.Disks.Count > 0)
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
                                foreach (var h in new[] { "Modelo", "Interfaz", "Tamaño", "Estado S.M.A.R.T", "Temp", "Horas", "Salud" })
                                    t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(7.5f);

                                foreach (var d in r.Disks)
                                {
                                    var cSmart = d.State.Contains("Operativo") ? Colors.Green.Darken2 : Colors.Red.Darken2;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Model).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Type).FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{d.SizeGB:F0} GB").FontSize(7.5f);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.State).FontColor(cSmart).SemiBold().FontSize(7.5f);

                                    // Temperatura
                                    string tempTxt = d.Temperature.HasValue ? $"{d.Temperature}°C" : "—";
                                    var cTemp = d.Temperature.HasValue && d.Temperature > 55
                                        ? Colors.Red.Darken2
                                        : d.Temperature.HasValue && d.Temperature > 45
                                            ? Colors.Orange.Darken3
                                            : Colors.Black;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(tempTxt).FontColor(cTemp).FontSize(7.5f);

                                    // Horas encendido
                                    string hoursTxt = d.HoursUsed.HasValue
                                        ? $"{d.HoursUsed:N0}h" : "—";
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(hoursTxt).FontSize(7.5f);

                                    // % Salud (solo SSDs con atributo 202)
                                    string healthTxt = d.HasHealthData
                                        ? $"{d.HealthPercent}%" : "—";
                                    var cHealth = d.HasHealthData
                                        ? (d.HealthPercent < 10 ? Colors.Red.Darken2
                                           : d.HealthPercent < 30 ? Colors.Orange.Darken3
                                           : Colors.Green.Darken2)
                                        : Colors.Grey.Darken1;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(healthTxt).FontColor(cHealth).FontSize(7.5f);
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

                            foreach (var d in r.LogicDisks)
                            {
                                int blocks = Math.Clamp((int)((100 - d.FreePercent) / 10), 0, 10);
                                string bars = new string('█', blocks) + new string('░', 10 - blocks);
                                bool critical = d.FreePercent < 20;
                                var cDisk = critical ? Colors.Red.Darken2 : Colors.Black;

                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(d.Letter).SemiBold().FontSize(8);
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text($"{d.TotalGB:F1} GB").FontSize(8);
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text($"{d.FreeGB:F1} GB").FontColor(cDisk).FontSize(8);
                                var pctText = t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text($"{d.FreePercent:F1}%{(critical ? " ⚠️" : "")}").FontColor(cDisk).FontSize(8);
                                if (critical) pctText.SemiBold();
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(bars).FontColor(critical ? Colors.Red.Medium : Colors.Blue.Medium).FontSize(7);
                            }
                        });
                        // ── 9. DF-SERVER (solo si el técnico es DF) ──────────
                        if (r.IsDfTechnician)
                        {
                            var df = r.DfServer;

                            // Cabecera de sección con fondo azul DF 
                            col.Item().PaddingTop(10).Background("#f69511")
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
                                // Versión de DF-Server
                                var cVers = df.Version.Contains("No detectada") ? Colors.Red.Darken2 : Colors.Blue.Darken3;
                                RowColor(t, "Versión instalada:", df.Version, cVers);

                                // Digitalización certificada
                                var cDig = df.HasCertifiedDigitization
                                    ? Colors.Green.Darken2 : Colors.Grey.Darken2;
                                RowColor(t, "Digitalización certificada:",
                                    df.HasCertifiedDigitization
                                        ? "✅ Activa y funcionando"
                                        : "— No tiene / No activa", cDig);

                                // Firmas DF-Signature
                                if (df.HasSignatures)
                                {
                                    bool isOutOfStock = df.RemainingSignatures == 0;
                                    bool hasLowSignatures = df.RemainingSignatures <= 100;
                                    var cSignatures = hasLowSignatures ? Colors.Red.Darken2 : Colors.Green.Darken2;
                                    string signaturesTxt;
                                    if (isOutOfStock)
                                        signaturesTxt = "❌ Sin stock de firmas · Cliente avisado";
                                    else if (hasLowSignatures)
                                        signaturesTxt = $"⚠️  {df.RemainingSignatures} firmas restantes — Stock bajo · Cliente avisado";
                                    else
                                        signaturesTxt = $"✅ {df.RemainingSignatures} firmas restantes";
                                    RowColor(t, "Firmas DF-Signature:", signaturesTxt, cSignatures);
                                }
                                else
                                {
                                    Row(t, "Firmas DF-Signature:", "Módulo no contratado");
                                }
                            });

                            // Tabla de certificados digitales
                            if (df.HasCertificates && df.Certificates.Count > 0)
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

                                    foreach (var cert in df.Certificates)
                                    {
                                        int remainingDays = (cert.ExpirationDate.Date - DateTime.Today).Days;
                                        bool isExpired = remainingDays < 0;
                                        bool isExpiringSoon = cert.IsExpiringSoon && !isExpired;

                                        string stateTxt = isExpired
                                            ? "❌ Caducado · Cliente avisado"
                                            : isExpiringSoon
                                                ? $"⚠️  Caduca en {remainingDays} días · Cliente avisado"
                                                : $"✅ Válido ({remainingDays} días)";

                                        var cState = isExpired ? Colors.Red.Darken2
                                                    : isExpiringSoon ? Colors.Orange.Darken3
                                                    : Colors.Green.Darken2;

                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                            .Padding(3).Text(cert.Name).FontSize(8);
                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                            .Padding(3).Text(cert.ExpirationDate.ToString("dd/MM/yyyy")).FontSize(8);
                                        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                            .Padding(3).Text(stateTxt).FontColor(cState).SemiBold().FontSize(8);
                                    }
                                });
                            }
                            else if (!df.HasCertificates)
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
            }).GeneratePdf(path);
        }

        // ── Helpers de layout ────────────────────────────────────────

        private static void Section(ColumnDescriptor col, string title)
        {
            col.Item().PaddingTop(10).PaddingBottom(2).Text(title)
                .FontSize(11).SemiBold().FontColor(Colors.Blue.Darken3);
            col.Item().LineHorizontal(1).LineColor(Colors.Blue.Darken2);
            col.Item().PaddingBottom(4);
        }

        private static void Row(TableDescriptor t, string label, string value, float fontSize = 9)
        {
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(label).SemiBold();
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(value).FontSize(fontSize);
        }

        private static void RowColor(TableDescriptor t, string label, string value, string color)
        {
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(label).SemiBold();
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(value).FontColor(color).SemiBold();
        }

        // ── Helper para Marca de Agua ─────────────────────────────────
        private static byte[] MakeImageTransparent(byte[] imageBytes, float opacity)
        {
            using (var msIn = new MemoryStream(imageBytes))
            using (var img = System.Drawing.Image.FromStream(msIn))
            using (var bmp = new System.Drawing.Bitmap(img.Width, img.Height))
            {
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    // Usamos las rutas completas de System.Drawing.Imaging
                    var matrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = opacity };
                    var attributes = new System.Drawing.Imaging.ImageAttributes();
                    attributes.SetColorMatrix(matrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);

                    // Dibujamos especificando que usamos el Rectangle de Windows
                    g.DrawImage(img, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                        0, 0, img.Width, img.Height, System.Drawing.GraphicsUnit.Pixel, attributes);
                }

                using (var msOut = new MemoryStream())
                {
                    // Guardamos usando el ImageFormat de Windows
                    bmp.Save(msOut, System.Drawing.Imaging.ImageFormat.Png);
                    return msOut.ToArray();
                }
            }
        }
    }
}