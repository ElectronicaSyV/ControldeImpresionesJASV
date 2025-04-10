using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using PrintControl.Models;  

namespace PrintControl.Services
{
    public class ReportService
    {
        public static string GenerateDailyReport(List<PrintJob> printJobs)
        {
            var today = DateTime.Now.Date;
            var todayJobs = printJobs.Where(j => j.TimeStamp.Date == today).ToList();

            var report = new StringBuilder();
            report.AppendLine("REPORTE DE IMPRESIONES DEL DÍA");
            report.AppendLine($"Fecha: {today:dd/MM/yyyy}");
            report.AppendLine("----------------------------------------");
            report.AppendLine();

            // Totales generales
            report.AppendLine($"Total de trabajos: {todayJobs.Count}");
            report.AppendLine($"Total de copias: {todayJobs.Sum(j => j.PrintedCopies)}");
            report.AppendLine();

            // Resumen por tamaño de papel
            var byPaperSize = todayJobs.GroupBy(j => j.PaperSize)
                .Select(g => new
                {
                    PaperSize = g.Key,
                    Jobs = g.Count(),
                    Copies = g.Sum(j => j.PrintedCopies)
                });

            report.AppendLine("RESUMEN POR TAMAÑO DE PAPEL");
            report.AppendLine("----------------------------------------");
            foreach (var size in byPaperSize)
            {
                report.AppendLine($"Tamaño: {size.PaperSize}");
                report.AppendLine($"  - Trabajos: {size.Jobs}");
                report.AppendLine($"  - Copias: {size.Copies}");
            }
            report.AppendLine();

            // Resumen por impresora
            var byPrinter = todayJobs.GroupBy(j => j.PrinterName)
                .Select(g => new
                {
                    Printer = g.Key,
                    Jobs = g.Count(),
                    Copies = g.Sum(j => j.PrintedCopies)
                });

            report.AppendLine("RESUMEN POR IMPRESORA");
            report.AppendLine("----------------------------------------");
            foreach (var printer in byPrinter)
            {
                report.AppendLine($"Impresora: {printer.Printer}");
                report.AppendLine($"  - Trabajos: {printer.Jobs}");
                report.AppendLine($"  - Copias: {printer.Copies}");
            }
            report.AppendLine();

            // Resumen por usuario
            var byUser = todayJobs.GroupBy(j => j.UserName)
                .Select(g => new
                {
                    User = g.Key,
                    Jobs = g.Count(),
                    Copies = g.Sum(j => j.PrintedCopies)
                });

            report.AppendLine("RESUMEN POR USUARIO");
            report.AppendLine("----------------------------------------");
            foreach (var user in byUser)
            {
                report.AppendLine($"Usuario: {user.User}");
                report.AppendLine($"  - Trabajos: {user.Jobs}");
                report.AppendLine($"  - Copias: {user.Copies}");
            }

            // Lista detallada de trabajos
            report.AppendLine();
            report.AppendLine("DETALLE DE TRABAJOS");
            report.AppendLine("----------------------------------------");
            foreach (var job in todayJobs.OrderBy(j => j.TimeStamp))
            {
                report.AppendLine($"Trabajo ID: {job.JobId}");
                report.AppendLine($"  - Hora: {job.TimeStamp:HH:mm:ss}");
                report.AppendLine($"  - Usuario: {job.UserName}");
                report.AppendLine($"  - Impresora: {job.PrinterName}");
                report.AppendLine($"  - Documento: {job.DocumentName}");
                report.AppendLine($"  - Tamaño: {job.PaperSize}");
                report.AppendLine($"  - Copias: {job.PrintedCopies}");
                report.AppendLine($"  - Color: {(job.IsColor ? "Sí" : "No")}");
                report.AppendLine();
            }

            return report.ToString();
        }

        public static void SaveReport(string report, string basePath = null)
        {
            basePath = basePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PrintControl",
                "Reports"
            );

            // Asegurar que el directorio existe
            Directory.CreateDirectory(basePath);

            // Crear nombre de archivo con fecha
            var fileName = $"PrintReport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            var fullPath = Path.Combine(basePath, fileName);

            // Guardar el reporte
            File.WriteAllText(fullPath, report, Encoding.UTF8);
        }
    }
}
