using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using CarRentalSystem.Views.Windows;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;

namespace CarRentalSystem.Views.Pages
{
    public partial class AdminOrdersPage : System.Windows.Controls.Page
    {
        public AdminOrdersPage()
        {
            InitializeComponent();
            RefreshOrdersGrid();
        }

        private void RefreshOrdersGrid()
        {
            try
            {
                var orders = Core.Context.Orders
                    .Select(o => new
                    {
                        ID_Order = o.ID_Order,
                        ClientName = o.Clients != null ? o.Clients.LastName + " " + o.Clients.FirstName : "-",
                        CarBrand = o.Cars != null ? o.Cars.Brand : "-",
                        CarModel = o.Cars != null ? o.Cars.Model : "-",
                        StartDate = o.StartDate,
                        EndDate = o.EndDate,
                        TotalCost = o.TotalCost,
                        StatusName = o.OrderStatuses != null ? o.OrderStatuses.StatusName : "-"
                    })
                    .ToList()
                    .OrderByDescending(o => o.ID_Order)
                    .ToList();

                GridAllOrders.ItemsSource = orders;
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка истории аренд: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            Word.Application application = null;
            Word.Document document = null;
            Word.Paragraph titleParagraph = null;
            Word.Paragraph dateParagraph = null;
            Word.Paragraph tableParagraph = null;
            Word.Table paymentsTable = null;
            Word.Paragraph totalParagraph = null;

            try
            {
                Core.Context.Configuration.ProxyCreationEnabled = true;
                Core.Context.Configuration.LazyLoadingEnabled = true;

                var ordersList = Core.Context.Orders
                    .Include(o => o.Clients)
                    .Include(o => o.Cars)
                    .Where(o => Core.Context.Payments.Any(p => p.FK_Order == o.ID_Order && p.FK_PaymentStatus == 1))
                    .ToList()
                    .OrderByDescending(o => o.ID_Order)
                    .ToList();

                if (ordersList.Count == 0)
                {
                    CustomDialogWindow.ShowOk("Нет оплаченных данных для выгрузки отчета!", CustomDialogWindow.DialogIcon.Warning);
                    return;
                }

                application = new Word.Application();
                application.Visible = false;
                document = application.Documents.Add();

                titleParagraph = document.Paragraphs.Add();
                Word.Range titleRange = titleParagraph.Range;
                titleRange.Text = "СВОДНЫЙ РЕЕСТР ДОГОВОРОВ АРЕНДЫ АВТОПАРКА";
                titleRange.Font.Name = "Times New Roman";
                titleRange.Font.Size = 16;
                titleRange.Font.Bold = 1;
                titleParagraph.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                titleRange.InsertParagraphAfter();

                dateParagraph = document.Paragraphs.Add();
                Word.Range dateRange = dateParagraph.Range;
                dateRange.Text = $"Дата выгрузки отчета: {DateTime.Now:dd.MM.yyyy HH:mm}\n";
                dateRange.Font.Name = "Times New Roman";
                dateRange.Font.Size = 11;
                dateRange.Font.Italic = 1;
                dateParagraph.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                dateRange.InsertParagraphAfter();

                tableParagraph = document.Paragraphs.Add();
                Word.Range tableRange = tableParagraph.Range;

                paymentsTable = document.Tables.Add(tableRange, ordersList.Count + 1, 6);
                paymentsTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                paymentsTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                paymentsTable.Range.Cells.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

                string[] headers = { "№ Контракта", "Клиент ФИО", "Автомобиль", "Начало аренды", "Окончание", "Итого Сумма" };
                for (int c = 0; c < headers.Length; c++)
                {
                    paymentsTable.Cell(1, c + 1).Range.Text = headers[c];
                }

                var headerRowRange = paymentsTable.Rows[1].Range;
                headerRowRange.Font.Name = "Times New Roman";
                headerRowRange.Font.Size = 12;
                headerRowRange.Font.Bold = 1;
                headerRowRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                decimal totalSum = 0;

                for (int i = 0; i < ordersList.Count; i++)
                {
                    var order = ordersList[i];
                    int row = i + 2;

                    string clientName = order.Clients != null ? $"{order.Clients.LastName} {order.Clients.FirstName}" : "-";
                    string carName = order.Cars != null ? $"{order.Cars.Brand} {order.Cars.Model}" : "-";

                    var currentPayment = Core.Context.Payments.FirstOrDefault(p => p.FK_Order == order.ID_Order && p.FK_PaymentStatus == 1);
                    decimal rowAmount = currentPayment != null ? currentPayment.Amount : order.TotalCost;
                    totalSum += rowAmount;

                    paymentsTable.Cell(row, 1).Range.Text = order.ID_Order.ToString();
                    paymentsTable.Cell(row, 2).Range.Text = clientName;
                    paymentsTable.Cell(row, 3).Range.Text = carName;
                    paymentsTable.Cell(row, 4).Range.Text = order.StartDate.ToString("dd.MM.yyyy");
                    paymentsTable.Cell(row, 5).Range.Text = order.EndDate.ToString("dd.MM.yyyy");
                    paymentsTable.Cell(row, 6).Range.Text = rowAmount.ToString("N2") + " руб.";

                    var dataRowRange = paymentsTable.Rows[row].Range;
                    dataRowRange.Font.Name = "Times New Roman";
                    dataRowRange.Font.Size = 11;
                    dataRowRange.Font.Bold = 0;
                    dataRowRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                }

                paymentsTable.Columns.AutoFit();

                object unitStory = Word.WdUnits.wdStory;
                application.Selection.EndKey(ref unitStory);

                totalParagraph = document.Paragraphs.Add();
                Word.Range totalRange = totalParagraph.Range;
                totalRange.Text = $"\nОБЩАЯ ВЫРУЧКА КОМПАНИИ (ИТОГО): {totalSum.ToString("N2")} руб.";
                totalRange.Font.Name = "Times New Roman";
                totalRange.Font.Size = 12;
                totalRange.Font.Bold = 1;
                totalParagraph.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
                totalRange.InsertParagraphAfter();

                application.Visible = true;
                application.Activate();
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка официального экспорта Word: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
                if (application != null && !application.Visible)
                {
                    if (document != null) try { document.Close(false); } catch { }
                    try { application.Quit(); } catch { }
                }
            }
            finally
            {
                if (paymentsTable != null) Marshal.ReleaseComObject(paymentsTable);
                if (totalParagraph != null) Marshal.ReleaseComObject(totalParagraph);
                if (tableParagraph != null) Marshal.ReleaseComObject(tableParagraph);
                if (dateParagraph != null) Marshal.ReleaseComObject(dateParagraph);
                if (titleParagraph != null) Marshal.ReleaseComObject(titleParagraph);
                if (document != null) Marshal.ReleaseComObject(document);
                if (application != null) Marshal.ReleaseComObject(application);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            Excel.Application application = null;
            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;

            try
            {
                Core.Context.Configuration.ProxyCreationEnabled = true;
                Core.Context.Configuration.LazyLoadingEnabled = true;

                var orders = Core.Context.Orders
                    .Include(o => o.Clients)
                    .Include(o => o.Cars)
                    .Where(o => Core.Context.Payments.Any(p => p.FK_Order == o.ID_Order && p.FK_PaymentStatus == 1))
                    .ToList()
                    .OrderByDescending(o => o.ID_Order)
                    .ToList();

                if (orders.Count == 0) return;

                application = new Excel.Application();
                application.SheetsInNewWorkbook = 1;
                workbook = application.Workbooks.Add(Excel.XlWBATemplate.xlWBATWorksheet);
                worksheet = (Excel.Worksheet)workbook.ActiveSheet;
                worksheet.Name = "Реестр договоров аренды";

                int startRowIndex = 1;
                worksheet.Cells[startRowIndex, 1] = "№ Контракта";
                worksheet.Cells[startRowIndex, 2] = "Клиент ФИО";
                worksheet.Cells[startRowIndex, 3] = "Автомобиль";
                worksheet.Cells[startRowIndex, 4] = "Начало аренды";
                worksheet.Cells[startRowIndex, 5] = "Окончание";
                worksheet.Cells[startRowIndex, 6] = "Итого Сумма";
                Excel.Range columlHeaderRange = worksheet.Range[worksheet.Cells[startRowIndex, 1], worksheet.Cells[startRowIndex, 6]];
                columlHeaderRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                columlHeaderRange.Font.Bold = true;
                Marshal.ReleaseComObject(columlHeaderRange);
                startRowIndex++;
                int dataStartRow = startRowIndex;
                foreach (var order in orders)
                {
                    string clientName = order.Clients != null ? $"{order.Clients.LastName} {order.Clients.FirstName}" : "-";
                    string carName = order.Cars != null ? $"{order.Cars.Brand} {order.Cars.Model}" : "-";
                    var currentPayment = Core.Context.Payments.FirstOrDefault(p => p.FK_Order == order.ID_Order && p.FK_PaymentStatus == 1);
                    decimal rowAmount = currentPayment != null ? currentPayment.Amount : order.TotalCost;
                    worksheet.Cells[startRowIndex, 1] = order.ID_Order;
                    worksheet.Cells[startRowIndex, 2] = clientName;
                    worksheet.Cells[startRowIndex, 3] = carName;
                    worksheet.Cells[startRowIndex, 4] = order.StartDate.ToString("dd.MM.yyyy");
                    worksheet.Cells[startRowIndex, 5] = order.EndDate.ToString("dd.MM.yyyy");
                    worksheet.Cells[startRowIndex, 6] = rowAmount;
                    Excel.Range cellTotal = (Excel.Range)worksheet.Cells[startRowIndex, 6];
                    cellTotal.NumberFormat = "0.00";
                    Marshal.ReleaseComObject(cellTotal);
                    startRowIndex++;
                }
                Excel.Range sumRange = worksheet.Range[worksheet.Cells[startRowIndex, 1], worksheet.Cells[startRowIndex, 5]];
                sumRange.Merge();
                sumRange.Value = "ОБЩАЯ ВЫРУЧКА КОМПАНИИ (ИТОГО):";
                sumRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                sumRange.Font.Bold = true;
                Marshal.ReleaseComObject(sumRange);
                Excel.Range totalSumCell = (Excel.Range)worksheet.Cells[startRowIndex, 6];
                totalSumCell.Formula = $"=SUM(F{dataStartRow}:F{startRowIndex - 1})";
                totalSumCell.NumberFormat = "0.00";
                totalSumCell.Font.Bold = true;
                Marshal.ReleaseComObject(totalSumCell);
                Excel.Range rangeBorders = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[startRowIndex, 6]];
                rangeBorders.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                Marshal.ReleaseComObject(rangeBorders);
                worksheet.Columns.AutoFit();
                application.Visible = true;
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка официального экспорта Excel: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
                if (workbook != null) try { workbook.Close(false); } catch { }
                if (application != null) try { application.Quit(); } catch { }
            }
            finally
            {
                if (worksheet != null) Marshal.ReleaseComObject(worksheet);
                if (workbook != null) Marshal.ReleaseComObject(workbook);
                if (application != null) Marshal.ReleaseComObject(application);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Core.Context.Configuration.ProxyCreationEnabled = true;
                Core.Context.Configuration.LazyLoadingEnabled = true;
                var orders = Core.Context.Orders
                .Include(o => o.Clients)
                .Include(o => o.Cars)
                .Where(o => Core.Context.Payments.Any(p => p.FK_Order == o.ID_Order && p.FK_PaymentStatus == 1))
                .ToList()
                .OrderByDescending(o => o.ID_Order)
                .ToList();
                if (orders.Count == 0) return;
                string tempPath = Path.GetTempPath();
                string filePath = Path.Combine(tempPath, "Financial_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                    doc.Open();
                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
                    iTextSharp.text.Font fontTitle = new iTextSharp.text.Font(bf, 16, iTextSharp.text.Font.BOLD);
                    iTextSharp.text.Font fontTable = new iTextSharp.text.Font(bf, 10, iTextSharp.text.Font.NORMAL);
                    doc.Add(new iTextSharp.text.Paragraph("СВОДНЫЙ РЕЕСТР ДОГОВОРОВ АРЕНДЫ АВТОПАРКА\n\n", fontTitle));
                    PdfPTable table = new PdfPTable(6);
                    table.WidthPercentage = 100;
                    string[] headers = { "№ Контракта", "Клиент ФИО", "Автомобиль", "Начало аренды", "Окончание", "Итого Сумма" };
                    foreach (var h in headers)
                    {
                        PdfPCell headerCell = new PdfPCell(new Phrase(h, fontTable));
                        headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(headerCell);
                    }
                    decimal totalSum = 0;
                    foreach (var o in orders)
                    {
                        string clientName = o.Clients != null ? $"{o.Clients.LastName} {o.Clients.FirstName}" : "-";
                        string carName = o.Cars != null ? $"{o.Cars.Brand} {o.Cars.Model}" : "-";
                        var currentPayment = Core.Context.Payments.FirstOrDefault(p => p.FK_Order == o.ID_Order && p.FK_PaymentStatus == 1);
                        decimal rowAmount = currentPayment != null ? currentPayment.Amount : o.TotalCost;
                        totalSum += rowAmount;
                        table.AddCell(new PdfPCell(new Phrase(o.ID_Order.ToString(), fontTable)));
                        table.AddCell(new PdfPCell(new Phrase(clientName, fontTable)));
                        table.AddCell(new PdfPCell(new Phrase(carName, fontTable)));
                        table.AddCell(new PdfPCell(new Phrase(o.StartDate.ToString("dd.MM.yyyy"), fontTable)));
                        table.AddCell(new PdfPCell(new Phrase(o.EndDate.ToString("dd.MM.yyyy"), fontTable)));
                        table.AddCell(new PdfPCell(new Phrase(rowAmount.ToString("N2") + " руб.", fontTable)));
                    }
                    PdfPCell sumTitleCell = new PdfPCell(new Phrase("ОБЩАЯ ВЫРУЧКА КОМПАНИИ (ИТОГО):", fontTable));
                    sumTitleCell.Colspan = 5;
                    sumTitleCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    table.AddCell(sumTitleCell);
                    PdfPCell totalSumCell = new PdfPCell(new Phrase(totalSum.ToString("N2") + " руб.", fontTable));
                    totalSumCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    table.AddCell(totalSumCell);
                    doc.Add(table);
                    doc.Close();
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка экспорта PDF: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }
    }
}