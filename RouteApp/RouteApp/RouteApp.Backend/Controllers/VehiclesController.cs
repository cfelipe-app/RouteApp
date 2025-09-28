using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteApp.Backend.Helpers;
using RouteApp.Backend.Repositories.Interfaces;
using RouteApp.Backend.UnitsOfWork.Interfaces;
using RouteApp.Shared.DTOs;
using RouteApp.Shared.Entities;
using RouteApp.Shared.Responses;

namespace RouteApp.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : GenericController<Vehicle>
{
    private readonly IGenericUnitOfWork<Vehicle> _vehicleUnitOfWork;
    private readonly IGenericRepository<Vehicle> _vehicleRepository;

    public VehiclesController(
        IGenericUnitOfWork<Vehicle> vehicleUnitOfWork,
        IGenericRepository<Vehicle> vehicleRepository) : base(vehicleUnitOfWork)
    {
        _vehicleUnitOfWork = vehicleUnitOfWork;
        _vehicleRepository = vehicleRepository;
    }

    // GET /api/vehicles/paged?term=XYZ&page=1&recordsNumber=10&sortBy=Plate&sortDir=asc
    // Extras opcionales: providerId, isActive
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<Vehicle>>> GetPaged(
        [FromQuery] PaginationDTO pagination,
        [FromQuery] int? providerId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pagination.SortBy)) pagination.SortBy = "Plate";
        if (string.IsNullOrWhiteSpace(pagination.SortDir)) pagination.SortDir = "asc";

        // 1) Base como IQueryable SIN ordenar
        IQueryable<Vehicle> query = _vehicleRepository.Query()
            .ApplyFilter(pagination.Term)                         // por si algún día Vehicle tiene Name/Code
            .ApplySearch(pagination.Term, "Plate", "Brand", "Model"); // OR sobre varias columnas

        // Filtros opcionales
        if (providerId.HasValue) query = query.Where(v => v.ProviderId == providerId.Value);
        if (isActive.HasValue) query = query.Where(v => v.IsActive == isActive.Value);

        // (Opcional) incluir navegación si la necesitas en la grilla:
        // query = query.Include(v => v.Provider);

        // 2) Orden al final
        var orderedQuery = query.ApplySort(pagination.SortBy, pagination.SortDir);

        // 3) Total + página
        var totalRecords = await orderedQuery.CountAsync(cancellationToken);
        var items = await orderedQuery.Paginate(pagination).ToListAsync(cancellationToken);

        Response.Headers["X-Total-Count"] = totalRecords.ToString();

        return Ok(new PagedResult<Vehicle>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.RecordsNumber,
            Total = totalRecords
        });
    }
}