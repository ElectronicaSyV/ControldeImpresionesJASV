using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using PrintControl.Models;
using PrintControl.Services;

namespace PrintControl
{
    public partial class MainForm : Form
    {
        private PrintMonitorService _printMonitor;
        private BindingSource _bindingSource;
        private NotifyIcon _notifyIcon;

        public MainForm()
        {
            InitializeComponent();
            
            // Establecer el color de fondo del formulario
            this.BackColor = Color.White;
            
            // Primero configuramos el menú
            InitializeMenu();
            
            // Luego el resto de componentes
            InitializePrintMonitor();
            InitializeDataGridView();
            InitializeNotifyIcon();
            UpdatePrinterStatus();
        }

        private void InitializePrintMonitor()
        {
            _printMonitor = new PrintMonitorService();
            _printMonitor.OnPrintJobDetected += PrintMonitor_OnPrintJobDetected;
            _printMonitor.OnMonitorStatusChanged += PrintMonitor_OnMonitorStatusChanged;
            
            try
            {
                _printMonitor.StartMonitoring();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar el monitoreo: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdatePrinterStatus()
        {
            try
            {
                var defaultPrinter = _printMonitor?.GetDefaultPrinter();
                if (defaultPrinter != null)
                {
                    lblPrinterStatus.Text = $"Impresora predeterminada: {defaultPrinter}";
                    lblMonitorStatus.Text = $"Estado: {(_printMonitor.IsMonitoring ? "Monitoreando" : "Detenido")}";
                }
                else
                {
                    lblPrinterStatus.Text = "No se encontró impresora predeterminada";
                    lblMonitorStatus.Text = "Estado: No disponible";
                }
            }
            catch (Exception ex)
            {
                lblPrinterStatus.Text = "Error al obtener estado de impresora";
                lblMonitorStatus.Text = $"Error: {ex.Message}";
            }
        }

        private void PrintMonitor_OnMonitorStatusChanged(object sender, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateMonitorStatus(status)));
            }
            else
            {
                UpdateMonitorStatus(status);
            }
        }

        private void UpdateMonitorStatus(string status)
        {
            lblMonitorStatus.Text = $"Estado: {status}";
        }

        private void InitializeDataGridView()
        {
            // Configurar el DataGridView
            dgvPrintJobs.Dock = DockStyle.Fill;
            dgvPrintJobs.AutoGenerateColumns = false;
            dgvPrintJobs.AllowUserToAddRows = false;
            dgvPrintJobs.AllowUserToDeleteRows = false;
            dgvPrintJobs.ReadOnly = true;
            dgvPrintJobs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPrintJobs.MultiSelect = false;
            dgvPrintJobs.RowHeadersVisible = false;
            dgvPrintJobs.BackgroundColor = Color.White;
            dgvPrintJobs.BorderStyle = BorderStyle.None;
            dgvPrintJobs.ColumnHeadersVisible = true;
            dgvPrintJobs.EnableHeadersVisualStyles = true;
            dgvPrintJobs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPrintJobs.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgvPrintJobs.GridColor = Color.LightGray;
            dgvPrintJobs.CellBorderStyle = DataGridViewCellBorderStyle.Single;

            // Configurar el binding source
            _bindingSource = new BindingSource();
            dgvPrintJobs.DataSource = _bindingSource;

            // Agregar las columnas con proporciones relativas
            dgvPrintJobs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TimeStamp",
                DataPropertyName = "TimeStamp",
                HeaderText = "Fecha/Hora",
                FillWeight = 15
            });

            dgvPrintJobs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DocumentName",
                DataPropertyName = "DocumentName",
                HeaderText = "Documento",
                FillWeight = 25
            });

            dgvPrintJobs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PrinterName",
                DataPropertyName = "PrinterName",
                HeaderText = "Impresora",
                FillWeight = 20
            });

            dgvPrintJobs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "UserName",
                DataPropertyName = "UserName",
                HeaderText = "Usuario",
                FillWeight = 10
            });

            dgvPrintJobs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PrintedCopies",
                DataPropertyName = "PrintedCopies",
                HeaderText = "Copias Impresas",
                FillWeight = 10
            });

            dgvPrintJobs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PaperSize",
                DataPropertyName = "PaperSize",
                HeaderText = "Tamaño",
                FillWeight = 10
            });

            dgvPrintJobs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                DataPropertyName = "Status",
                HeaderText = "Estado",
                FillWeight = 10
            });

            // Ajustar el orden de los controles
            dgvPrintJobs.BringToFront();
            statusStrip.BringToFront();
        }

        private void PrintMonitor_OnPrintJobDetected(object sender, PrintJob e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddPrintJobToGrid(e)));
            }
            else
            {
                AddPrintJobToGrid(e);
            }
        }

        private void AddPrintJobToGrid(PrintJob job)
        {
            // Buscar si ya existe un trabajo similar
            var existingJob = _bindingSource.Cast<PrintJob>()
                .FirstOrDefault(j => j.DocumentName == job.DocumentName && 
                                   j.UserName == job.UserName &&
                                   j.PrinterName == job.PrinterName &&
                                   (DateTime.Now - j.TimeStamp).TotalSeconds < 5); // dentro de 5 segundos

            if (existingJob != null)
            {
                // Actualizar el trabajo existente
                int index = _bindingSource.IndexOf(existingJob);
                existingJob.PrintedCopies += job.PrintedCopies;
                _bindingSource.ResetItem(index);
            }
            else
            {
                // Agregar nuevo trabajo
                _bindingSource.Add(job);
            }
            
            UpdatePrinterStatus();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                _bindingSource.Clear();
                _printMonitor.RefreshPrintJobs();
                UpdatePrinterStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Monitor de Impresión",
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Focus();
            };

            // Crear menú contextual
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Abrir", null, (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Focus();
            });
            contextMenu.Items.Add("Actualizar", null, (s, e) => BtnRefresh_Click(s, e));
            contextMenu.Items.Add("-"); // Separador
            contextMenu.Items.Add("Salir", null, (s, e) => Application.Exit());

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void InitializeMenu()
        {
            var menuStrip = new MenuStrip();
            menuStrip.BackColor = Color.White;

            // Menú Archivo
            var fileMenu = new ToolStripMenuItem("Archivo");
            fileMenu.DropDownItems.Add("Actualizar", null, BtnRefresh_Click);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Generar Reporte del Día", null, GenerateReport_Click);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Salir", null, (s, e) => Close());

            // Menú Configuración
            var configMenu = new ToolStripMenuItem("Configuración");
            configMenu.DropDownItems.Add("Cambiar Contraseña", null, (s, e) =>
            {
                using (var changePasswordForm = new ChangePasswordForm())
                {
                    if (changePasswordForm.ShowDialog(this) == DialogResult.OK)
                    {
                        PasswordForm.CurrentPassword = changePasswordForm.NewPassword;
                        MessageBox.Show("Contraseña cambiada exitosamente", "Éxito",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            });

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(configMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            menuStrip.BringToFront();
        }

        private void GenerateReport_Click(object sender, EventArgs e)
        {
            try
            {
                // Obtener todos los trabajos del BindingSource
                var printJobs = _bindingSource.List.Cast<PrintJob>().ToList();
                
                // Generar el reporte
                var report = ReportService.GenerateDailyReport(printJobs);
                
                // Guardar el reporte
                ReportService.SaveReport(report);
                
                // Mostrar mensaje de éxito con la ubicación del reporte
                var reportPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "PrintControl",
                    "Reports"
                );
                
                MessageBox.Show(
                    $"Reporte generado exitosamente.\nUbicación: {reportPath}",
                    "Reporte Generado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al generar el reporte: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                using (var passwordForm = new PasswordForm())
                {
                    if (passwordForm.ShowDialog(this) != DialogResult.OK)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            _notifyIcon.Dispose();
            base.OnFormClosing(e);
            _printMonitor?.StopMonitoring();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                _notifyIcon.ShowBalloonTip(2000, "Monitor de Impresión", 
                    "La aplicación sigue ejecutándose en segundo plano.", 
                    ToolTipIcon.Info);
            }
        }
    }
}
