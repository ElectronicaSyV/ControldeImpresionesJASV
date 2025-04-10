using System;
using System.Printing;
using System.Collections.Generic;
using System.Management;
using PrintControl.Models;
using System.Linq;
using System.Threading;
using System.IO;
using System.Windows.Xps;

namespace PrintControl.Services
{
    public class PrintMonitorService
    {
        private ManagementEventWatcher printJobWatcher;
        private FileSystemWatcher spoolWatcher;
        public event EventHandler<PrintJob> OnPrintJobDetected;
        public event EventHandler<string> OnMonitorStatusChanged;
        private bool isMonitoring;
        private readonly string spoolPath;

        public PrintMonitorService()
        {
            spoolPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "spool", "PRINTERS");
            InitializePrintMonitor();
            InitializeSpoolWatcher();
        }

        private void InitializeSpoolWatcher()
        {
            try
            {
                spoolWatcher = new FileSystemWatcher(spoolPath)
                {
                    Filter = "*.*",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
                };
                spoolWatcher.Created += SpoolFile_Created;
                UpdateMonitorStatus("Monitor de spool inicializado");
            }
            catch (Exception ex)
            {
                UpdateMonitorStatus($"Error al inicializar monitor de spool: {ex.Message}");
            }
        }

        private string GetPaperSizeFromPrinter(PrintQueue printer, ManagementBaseObject jobInfo = null)
        {
            try
            {
                // Intentar obtener del trabajo de impresión WMI primero
                if (jobInfo != null)
                {
                    // Intentar obtener el tamaño directamente del trabajo
                    var driverExtra = jobInfo["DriverExtra"]?.ToString().ToLower();
                    if (!string.IsNullOrEmpty(driverExtra))
                    {
                        if (driverExtra.Contains("letter")) return "Carta";
                        if (driverExtra.Contains("legal")) return "Oficio";
                        if (driverExtra.Contains("a4")) return "A4";
                        if (driverExtra.Contains("executive")) return "Ejecutivo";
                        if (driverExtra.Contains("tabloid")) return "Tabloide";
                    }

                    // Intentar obtener del nombre del documento
                    var description = jobInfo["Description"]?.ToString().ToLower();
                    if (!string.IsNullOrEmpty(description))
                    {
                        if (description.Contains("letter")) return "Carta";
                        if (description.Contains("legal")) return "Oficio";
                        if (description.Contains("a4")) return "A4";
                        if (description.Contains("executive")) return "Ejecutivo";
                        if (description.Contains("tabloid")) return "Tabloide";
                    }
                }

                // Intentar obtener de la impresora usando WMI con una consulta más específica
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PrinterConfiguration WHERE Name LIKE '%" + printer.Name + "%'"))
                {
                    foreach (ManagementObject config in searcher.Get())
                    {
                        // Intentar obtener por dimensiones específicas
                        var paperWidth = Convert.ToInt32(config["PaperWidth"]);
                        var paperLength = Convert.ToInt32(config["PaperLength"]);

                        // Medidas en décimas de milímetro
                        if (paperWidth == 2159 && paperLength == 2794) return "Carta";
                        if (paperWidth == 2159 && paperLength == 3302) return "Oficio";
                        if (paperWidth == 2100 && paperLength == 2970) return "A4";
                        if (paperWidth == 2794 && paperLength == 4318) return "Tabloide";
                        if (paperWidth == 1397 && paperLength == 2159) return "Ejecutivo";
                    }
                }

                // Si no funcionó, intentar con Win32_Printer
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_Printer WHERE Name LIKE '%" + printer.Name + "%'"))
                {
                    foreach (ManagementObject printerInfo in searcher.Get())
                    {
                        var paperSizes = printerInfo["PaperSizesSupported"] as uint[];
                        if (paperSizes != null)
                        {
                            foreach (uint size in paperSizes)
                            {
                                switch (size)
                                {
                                    case 1: return "Carta";
                                    case 5: 
                                    case 14: return "Oficio";
                                    case 9: return "A4";
                                    case 7: return "Ejecutivo";
                                    case 3: return "Tabloide";
                                }
                            }
                        }

                        // Intentar por el nombre del formulario predeterminado
                        var defaultForm = printerInfo["DefaultPaperType"]?.ToString().ToLower();
                        if (!string.IsNullOrEmpty(defaultForm))
                        {
                            if (defaultForm.Contains("letter")) return "Carta";
                            if (defaultForm.Contains("legal")) return "Oficio";
                            if (defaultForm.Contains("a4")) return "A4";
                            if (defaultForm.Contains("executive")) return "Ejecutivo";
                            if (defaultForm.Contains("tabloid")) return "Tabloide";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateMonitorStatus($"Error al obtener tamaño de papel: {ex.Message}");
            }

            return "Desconocido";
        }

        private PrintJob GetPrintJobDetails(PrintQueue printer, string documentName, ManagementBaseObject jobInfo = null)
        {
            try
            {
                var printJob = printer.GetPrintJobInfoCollection().Cast<PrintSystemJobInfo>()
                    .FirstOrDefault(j => j.Name == documentName || j.JobName == documentName);

                if (printJob != null || jobInfo != null)
                {
                    // Intentar determinar si es color basado en el nombre de la impresora y sus capacidades
                    bool isColor = printer.FullName.ToLower().Contains("color");
                    string paperSize = GetPaperSizeFromPrinter(printer, jobInfo);

                    // Obtener el número real de copias impresas
                    int printedCopies = 1;
                    
                    if (jobInfo != null)
                    {
                        try
                        {
                            // Intentar obtener las copias del trabajo WMI de diferentes propiedades
                            if (jobInfo["NumberOfCopies"] != null)
                            {
                                printedCopies = Convert.ToInt32(jobInfo["NumberOfCopies"]);
                            }
                            else if (jobInfo["Copies"] != null)
                            {
                                printedCopies = Convert.ToInt32(jobInfo["Copies"]);
                            }
                            else if (jobInfo["Parameters"] != null)
                            {
                                var parameters = jobInfo["Parameters"].ToString().ToLower();
                                if (parameters.Contains("copies="))
                                {
                                    var start = parameters.IndexOf("copies=") + 7;
                                    var copiesStr = new string(parameters.Substring(start).TakeWhile(char.IsDigit).ToArray());
                                    if (int.TryParse(copiesStr, out int copies) && copies > 0)
                                    {
                                        printedCopies = copies;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdateMonitorStatus($"Error al obtener número de copias WMI: {ex.Message}");
                        }
                    }
                    else if (printJob != null)
                    {
                        try
                        {
                            // Intentar obtener las copias usando WMI con una consulta más específica
                            using (var searcher = new ManagementObjectSearcher(
                                $"SELECT * FROM Win32_PrintJob WHERE JobId = {printJob.JobIdentifier} AND PrinterName LIKE '%{printer.Name}%'"))
                            {
                                foreach (ManagementObject job in searcher.Get())
                                {
                                    if (job["NumberOfCopies"] != null)
                                    {
                                        printedCopies = Convert.ToInt32(job["NumberOfCopies"]);
                                        break;
                                    }
                                    else if (job["Copies"] != null)
                                    {
                                        printedCopies = Convert.ToInt32(job["Copies"]);
                                        break;
                                    }
                                }
                            }

                            // Si no se encontró en WMI, intentar obtener del spooler
                            if (printedCopies == 1)
                            {
                                var spoolFile = Directory.GetFiles(spoolPath, $"*{printJob.JobIdentifier}*")
                                    .FirstOrDefault();
                                
                                if (spoolFile != null)
                                {
                                    // Leer los primeros bytes del archivo de spool para buscar el número de copias
                                    using (var fs = new FileStream(spoolFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                    using (var reader = new BinaryReader(fs))
                                    {
                                        var buffer = new byte[1024];
                                        var bytesRead = reader.Read(buffer, 0, buffer.Length);
                                        var content = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
                                        
                                        if (content.Contains("Copies="))
                                        {
                                            var start = content.IndexOf("Copies=") + 7;
                                            var copiesStr = new string(content.Substring(start).TakeWhile(char.IsDigit).ToArray());
                                            if (int.TryParse(copiesStr, out int copies) && copies > 0)
                                            {
                                                printedCopies = copies;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdateMonitorStatus($"Error al obtener número de copias del trabajo: {ex.Message}");
                        }
                    }

                    // Asegurarse de que tenemos al menos una copia
                    printedCopies = Math.Max(1, printedCopies);

                    return new PrintJob
                    {
                        JobId = printJob?.JobIdentifier ?? Convert.ToInt32(jobInfo?["JobId"] ?? new Random().Next(1000, 9999)),
                        PrinterName = printer.FullName,
                        DocumentName = printJob?.Name ?? jobInfo?["Document"]?.ToString() ?? documentName,
                        DocumentPath = printJob?.Name ?? documentName,
                        UserName = printJob?.Submitter ?? jobInfo?["Owner"]?.ToString() ?? Environment.UserName,
                        PrintedCopies = printedCopies,
                        TimeStamp = DateTime.Now,
                        Status = printJob?.JobStatus.ToString() ?? jobInfo?["Status"]?.ToString() ?? "Enviado a imprimir",
                        PaperSize = paperSize,
                        IsColor = isColor
                    };
                }
            }
            catch (Exception ex)
            {
                UpdateMonitorStatus($"Error al obtener detalles del trabajo: {ex.Message}");
            }

            return null;
        }

        private void SpoolFile_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Dar tiempo a que el archivo de spool se procese
                Thread.Sleep(500);

                using (var printServer = new LocalPrintServer())
                {
                    var defaultPrinter = printServer.DefaultPrintQueue;
                    if (defaultPrinter != null)
                    {
                        var printJob = GetPrintJobDetails(defaultPrinter, Path.GetFileNameWithoutExtension(e.Name));
                        
                        if (printJob == null)
                        {
                            // Si no pudimos obtener los detalles, crear un trabajo básico
                            printJob = new PrintJob
                            {
                                JobId = new Random().Next(1000, 9999),
                                PrinterName = defaultPrinter.FullName,
                                DocumentName = Path.GetFileNameWithoutExtension(e.Name),
                                DocumentPath = e.FullPath,
                                UserName = Environment.UserName,
                                PrintedCopies = 1,
                                TimeStamp = DateTime.Now,
                                Status = "Enviado a imprimir",
                                PaperSize = GetPaperSizeFromPrinter(defaultPrinter),
                                IsColor = defaultPrinter.FullName.ToLower().Contains("color")
                            };
                        }

                        OnPrintJobDetected?.Invoke(this, printJob);
                        UpdateMonitorStatus($"Nuevo trabajo de impresión detectado: {printJob.DocumentName}");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateMonitorStatus($"Error al procesar archivo de spool: {ex.Message}");
            }
        }

        private void InitializePrintMonitor()
        {
            try
            {
                var query = new WqlEventQuery(
                    "SELECT * FROM __InstanceCreationEvent WITHIN 1 " +
                    "WHERE TargetInstance ISA 'Win32_PrintJob'");

                printJobWatcher = new ManagementEventWatcher(query);
                printJobWatcher.EventArrived += PrintJobCreated;
                UpdateMonitorStatus("Monitor WMI inicializado correctamente");
            }
            catch (Exception ex)
            {
                UpdateMonitorStatus($"Error al inicializar monitor WMI: {ex.Message}");
            }
        }

        private void PrintJobCreated(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                string printerName = targetInstance["PrinterName"]?.ToString();
                
                using (var printServer = new LocalPrintServer())
                {
                    var printer = printServer.GetPrintQueue(printerName);
                    var documentName = targetInstance["Document"]?.ToString();
                    
                    var printJob = GetPrintJobDetails(printer, documentName, targetInstance);
                    
                    if (printJob == null)
                    {
                        // Si no pudimos obtener los detalles, usar la información de WMI
                        printJob = new PrintJob
                        {
                            JobId = Convert.ToInt32(targetInstance["JobId"]),
                            PrinterName = printerName,
                            DocumentName = documentName,
                            UserName = targetInstance["Owner"]?.ToString(),
                            PrintedCopies = Convert.ToInt32(targetInstance["Copies"]),
                            TimeStamp = DateTime.Now,
                            Status = targetInstance["Status"]?.ToString(),
                            PaperSize = GetPaperSizeFromPrinter(printer, targetInstance),
                            IsColor = printerName.ToLower().Contains("color")
                        };
                    }

                    OnPrintJobDetected?.Invoke(this, printJob);
                    UpdateMonitorStatus($"Nuevo trabajo de impresión detectado (WMI): {printJob.DocumentName}");
                }
            }
            catch (Exception ex)
            {
                UpdateMonitorStatus($"Error al procesar trabajo de impresión WMI: {ex.Message}");
            }
        }

        public void StartMonitoring()
        {
            try
            {
                if (printJobWatcher != null)
                    printJobWatcher.Start();
                if (spoolWatcher != null)
                    spoolWatcher.EnableRaisingEvents = true;
                isMonitoring = true;
                UpdateMonitorStatus("Monitoreo activo (WMI y Spool)");
            }
            catch (Exception ex)
            {
                isMonitoring = false;
                UpdateMonitorStatus($"Error al iniciar el monitoreo: {ex.Message}");
                throw;
            }
        }

        public void StopMonitoring()
        {
            try
            {
                if (printJobWatcher != null)
                    printJobWatcher.Stop();
                if (spoolWatcher != null)
                    spoolWatcher.EnableRaisingEvents = false;
                isMonitoring = false;
                UpdateMonitorStatus("Monitoreo detenido");
            }
            catch (Exception ex)
            {
                UpdateMonitorStatus($"Error al detener el monitoreo: {ex.Message}");
                throw;
            }
        }

        private void UpdateMonitorStatus(string status)
        {
            OnMonitorStatusChanged?.Invoke(this, status);
        }

        public bool IsMonitoring => isMonitoring;

        public List<string> GetLocalPrinters()
        {
            var printers = new List<string>();
            try
            {
                using (var printServer = new LocalPrintServer())
                {
                    var queues = printServer.GetPrintQueues();
                    foreach (var queue in queues)
                    {
                        printers.Add(queue.FullName);
                    }
                }
                UpdateMonitorStatus($"Impresoras detectadas: {printers.Count}");
            }
            catch (Exception ex)
            {
                UpdateMonitorStatus($"Error al obtener impresoras: {ex.Message}");
            }
            return printers;
        }

        public string GetDefaultPrinter()
        {
            try
            {
                using (var printServer = new LocalPrintServer())
                {
                    var defaultPrinter = printServer.DefaultPrintQueue?.FullName;
                    return defaultPrinter ?? "No hay impresora predeterminada";
                }
            }
            catch (Exception ex)
            {
                UpdateMonitorStatus($"Error al obtener impresora predeterminada: {ex.Message}");
                return "Error al obtener impresora predeterminada";
            }
        }

        public void RefreshPrintJobs()
        {
            try
            {
                using (var printServer = new LocalPrintServer())
                {
                    foreach (var printer in printServer.GetPrintQueues())
                    {
                        var jobs = printer.GetPrintJobInfoCollection();
                        foreach (PrintSystemJobInfo job in jobs)
                        {
                            var printJob = GetPrintJobDetails(printer, job.Name);
                            if (printJob != null)
                            {
                                OnPrintJobDetected?.Invoke(this, printJob);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateMonitorStatus($"Error al actualizar trabajos: {ex.Message}");
            }
        }
    }
}
