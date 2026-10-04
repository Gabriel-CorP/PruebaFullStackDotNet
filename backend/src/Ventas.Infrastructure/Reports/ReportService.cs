using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Ventas.Application.Common.Interfaces;
using Ventas.Application.DTOs;

namespace Ventas.Infrastructure.Reports;

public sealed class ReportService : IReportService
{
    private const string MoneyFormat = "$#,##0.00";

    static ReportService() => QuestPDF.Settings.License = LicenseType.Community;

    // ============================ EXCEL ============================
    public byte[] GenerarExcel(IReadOnlyList<VentaReporteRow> filas, DateTime? desde, DateTime? hasta)
    {
        using var wb = new XLWorkbook();
        var ventas = filas.GroupBy(f => f.VentaId).Select(g => g.First()).ToList();

        // ---- Hoja 1: Resumen ----
        var ws = wb.Worksheets.Add("Resumen");
        ws.Cell(1, 1).Value = "Reporte de Ventas";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = RangoTexto(desde, hasta);

        string[] h1 = ["N° Venta", "Fecha", "Usuario", "Subtotal", "IVA", "Total"];
        for (var i = 0; i < h1.Length; i++) ws.Cell(4, i + 1).Value = h1[i];
        EstiloEncabezado(ws.Range(4, 1, 4, h1.Length));

        var row = 5;
        foreach (var v in ventas)
        {
            ws.Cell(row, 1).Value = v.NumeroVenta;
            ws.Cell(row, 2).Value = v.Fecha;
            ws.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            ws.Cell(row, 3).Value = v.Usuario;
            ws.Cell(row, 4).Value = v.Subtotal;
            ws.Cell(row, 5).Value = v.Iva;
            ws.Cell(row, 6).Value = v.Total;
            row++;
        }
        ws.Cell(row, 3).Value = "TOTAL";
        ws.Cell(row, 4).Value = ventas.Sum(v => v.Subtotal);
        ws.Cell(row, 5).Value = ventas.Sum(v => v.Iva);
        ws.Cell(row, 6).Value = ventas.Sum(v => v.Total);
        ws.Range(row, 3, row, 6).Style.Font.Bold = true;
        ws.Range(5, 4, row, 6).Style.NumberFormat.Format = MoneyFormat;
        ws.Columns().AdjustToContents();

        // ---- Hoja 2: Detalle ----
        var wd = wb.Worksheets.Add("Detalle");
        string[] h2 = ["N° Venta", "Fecha", "Usuario", "Código", "Producto", "Cantidad", "Precio unit.", "Subtotal"];
        for (var i = 0; i < h2.Length; i++) wd.Cell(1, i + 1).Value = h2[i];
        EstiloEncabezado(wd.Range(1, 1, 1, h2.Length));

        var r = 2;
        foreach (var f in filas)
        {
            wd.Cell(r, 1).Value = f.NumeroVenta;
            wd.Cell(r, 2).Value = f.Fecha;
            wd.Cell(r, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            wd.Cell(r, 3).Value = f.Usuario;
            wd.Cell(r, 4).Value = f.Codigo;
            wd.Cell(r, 5).Value = f.Producto;
            wd.Cell(r, 6).Value = f.Cantidad;
            wd.Cell(r, 7).Value = f.PrecioUnitario;
            wd.Cell(r, 8).Value = f.SubtotalLinea;
            r++;
        }
        wd.Range(2, 7, r, 8).Style.NumberFormat.Format = MoneyFormat;
        wd.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void EstiloEncabezado(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
    }

    // ============================ PDF ============================
    public byte[] GenerarPdf(IReadOnlyList<VentaReporteRow> filas, DateTime? desde, DateTime? hasta)
    {
        var ventas = filas.GroupBy(f => f.VentaId).ToList();
        var totalSubtotal = ventas.Sum(g => g.First().Subtotal);
        var totalIva = ventas.Sum(g => g.First().Iva);
        var totalGeneral = ventas.Sum(g => g.First().Total);

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Text("Reporte de Ventas").FontSize(18).Bold();
                col.Item().Text(RangoTexto(desde, hasta)).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                foreach (var venta in ventas)
                {
                    var cab = venta.First();

                    col.Item().PaddingTop(12).Text($"{cab.NumeroVenta}  |  {cab.Fecha:dd/MM/yyyy HH:mm}  |  {cab.Usuario}")
                        .Bold().FontSize(10);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(60);
                            c.RelativeColumn();
                            c.ConstantColumn(45);
                            c.ConstantColumn(65);
                            c.ConstantColumn(65);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Element(Head).Text("Código");
                            h.Cell().Element(Head).Text("Producto");
                            h.Cell().Element(Head).AlignRight().Text("Cant.");
                            h.Cell().Element(Head).AlignRight().Text("Precio");
                            h.Cell().Element(Head).AlignRight().Text("Subtotal");
                        });

                        foreach (var l in venta)
                        {
                            t.Cell().Element(Body).Text(l.Codigo);
                            t.Cell().Element(Body).Text(l.Producto);
                            t.Cell().Element(Body).AlignRight().Text(l.Cantidad.ToString());
                            t.Cell().Element(Body).AlignRight().Text(l.PrecioUnitario.ToString("C2"));
                            t.Cell().Element(Body).AlignRight().Text(l.SubtotalLinea.ToString("C2"));
                        }
                    });

                    col.Item().AlignRight().Text(
                        $"Subtotal: {cab.Subtotal:C2}   IVA: {cab.Iva:C2}   Total: {cab.Total:C2}").Bold();
                }

                col.Item().PaddingTop(20).LineHorizontal(1);
                col.Item().PaddingTop(6).AlignRight().Text(
                    $"{ventas.Count} venta(s)   |   Subtotal: {totalSubtotal:C2}   IVA: {totalIva:C2}   TOTAL: {totalGeneral:C2}")
                    .Bold().FontSize(11);
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ");
                x.CurrentPageNumber();
                x.Span(" de ");
                x.TotalPages();
            });
        })).GeneratePdf();

        static IContainer Head(IContainer c) =>
            c.Background(Colors.Grey.Lighten3).Padding(3).DefaultTextStyle(x => x.SemiBold());

        static IContainer Body(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3);
    }

    private static string RangoTexto(DateTime? desde, DateTime? hasta) =>
        desde is null && hasta is null
            ? "Rango: todas las ventas"
            : $"Rango: {(desde?.ToString("dd/MM/yyyy") ?? "inicio")} - {(hasta?.ToString("dd/MM/yyyy") ?? "hoy")}";
}
