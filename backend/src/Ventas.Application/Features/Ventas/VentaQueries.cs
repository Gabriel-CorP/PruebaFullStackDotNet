using MediatR;
using Ventas.Application.Common.Exceptions;
using Ventas.Application.Common.Interfaces;
using Ventas.Application.DTOs;

namespace Ventas.Application.Features.Ventas;

public sealed record GetVentasQuery(DateTime? Desde, DateTime? Hasta) : IRequest<List<VentaResumenDto>>;

public sealed class GetVentasQueryHandler(IUnitOfWork uow) : IRequestHandler<GetVentasQuery, List<VentaResumenDto>>
{
    public Task<List<VentaResumenDto>> Handle(GetVentasQuery request, CancellationToken ct) =>
        uow.Ventas.ListarAsync(request.Desde, request.Hasta, ct);
}

public sealed record GetVentaByIdQuery(int Id) : IRequest<VentaDto>;

public sealed class GetVentaByIdQueryHandler(IUnitOfWork uow) : IRequestHandler<GetVentaByIdQuery, VentaDto>
{
    public async Task<VentaDto> Handle(GetVentaByIdQuery request, CancellationToken ct) =>
        await uow.Ventas.ObtenerPorIdAsync(request.Id, ct)
        ?? throw new NotFoundException("La venta no existe.");
}

public sealed record ExportarReporteVentasQuery(DateTime? Desde, DateTime? Hasta, FormatoReporte Formato)
    : IRequest<ArchivoDto>;

public sealed class ExportarReporteVentasQueryHandler(IUnitOfWork uow, IReportService reports)
    : IRequestHandler<ExportarReporteVentasQuery, ArchivoDto>
{
    public async Task<ArchivoDto> Handle(ExportarReporteVentasQuery request, CancellationToken ct)
    {
        var filas = await uow.Ventas.ReporteAsync(request.Desde, request.Hasta, ct);
        if (filas.Count == 0)
            throw new NotFoundException("No se encontraron ventas en el rango seleccionado.");

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return request.Formato == FormatoReporte.Pdf
            ? new ArchivoDto(reports.GenerarPdf(filas, request.Desde, request.Hasta),
                "application/pdf", $"ReporteVentas_{stamp}.pdf")
            : new ArchivoDto(reports.GenerarExcel(filas, request.Desde, request.Hasta),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ReporteVentas_{stamp}.xlsx");
    }
}
