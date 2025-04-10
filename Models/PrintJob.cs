using System;

namespace PrintControl.Models
{
    public class PrintJob
    {
        public int JobId { get; set; }
        public string PrinterName { get; set; }
        public string DocumentName { get; set; }
        public string DocumentPath { get; set; }
        public string UserName { get; set; }
        public int PrintedCopies { get; set; }  // Total de copias realmente impresas
        public DateTime TimeStamp { get; set; }
        public string Status { get; set; }
        public string PaperSize { get; set; }
        public bool IsColor { get; set; }
    }
}
