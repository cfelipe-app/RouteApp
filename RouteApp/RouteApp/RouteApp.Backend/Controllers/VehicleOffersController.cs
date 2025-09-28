using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteApp.Backend.Helpers;
using RouteApp.Backend.Repositories.Interfaces;
using RouteApp.Backend.UnitsOfWork.Interfaces;
using RouteApp.Shared.DTOs;
using RouteApp.Shared.Entities;
using RouteApp.Shared.Enums;
using RouteApp.Shared.Responses;

namespace RouteApp.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleOffersController : GenericController<VehicleOffer>
{
    private readonly IGenericUnitOfWork<VehicleOffer> _vehicleOfferUnitOfWork;
    private readonly IGenericRepository<VehicleOffer> _vehicleOfferRepository;

    public VehicleOffersController(
        IGenericUnitOfWork<VehicleOffer> vehicleOfferUnitOfWork,
        IGenericRepository<VehicleOffer> vehicleOfferRepository) : base(vehicleOfferUnitOfWork)
    {
        _vehicleOfferUnitOfWork = vehicleOfferUnitOfWork;
        _vehicleOfferRepository = vehicleOfferRepository;
    }

    /// <summary>
    /// Listado paginado con filtros y orden.
    /// </summary>
    /// <remarks>
    /// Ejemplos:
    /// GET /api/vehicleoffers/paged?page=1&recordsNumber=10&term=urgente
    /// GET /api/vehicleoffers/paged?capacityRequestId=5&status=Accepted&sortBy=CreatedAt&sortDir=desc
    /// GET /api/vehicleoffers/paged?fromCreated=2025-09-01&toCreated=2025-09-30
    /// </remarks>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResult<VehicleOffer>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<VehicleOffer>>> GetPaged(
        [FromQuery] PaginationDTO pagination,
        [FromQuery] int? capacityRequestId = null,
        [FromQuery] int? providerId = null,
        [FromQuery] int? vehicleId = null,
        [FromQuery] VehicleOfferStatus? status = null,
        [FromQuery] DateTime? fromCreated = null,
        [FromQuery] DateTime? toCreated = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pagination.SortBy)) pagination.SortBy = "CreatedAt";
        if (string.IsNullOrWhiteSpace(pagination.SortDir)) pagination.SortDir = "desc";

        // 1) Base como IQueryable (sin ordenar)
        IQueryable<VehicleOffer> query = _vehicleOfferRepository.Query()
            .ApplyFilter(pagination.Term); // Name/Code si existieran (no afecta si no están)

        // 2) Búsqueda por varias columnas string (OR). Requiere tu extensión ApplySearch.
        if (!string.IsNullOrWhiteSpace(pagination.Term))
        {
            // Si no tienes ApplySearch, reemplaza esta línea por:
            // query = query.WhereDynamicContains("Notes", pagination.Term);
            query = query.ApplySearch(pagination.Term, "Notes", "Currency");
        }

        // 3) Filtros específicos
        if (capacityRequestId.HasValue) query = query.Where(o => o.CapacityRequestId == capacityRequestId.Value);
        if (providerId.HasValue) query = query.Where(o => o.ProviderId == providerId.Value);
        if (vehicleId.HasValue) query = query.Where(o => o.VehicleId == vehicleId.Value);
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        if (fromCreated.HasValue) query = query.Where(o => o.CreatedAt >= fromCreated.Value);
        if (toCreated.HasValue)
        {
            var inclusive = toCreated.Value.Date.AddDays(1); // fin del día
            query = query.Where(o => o.CreatedAt < inclusive);
        }

        // (Opcional) includes si necesitas datos relacionados en la grilla:
        // query = query.Include(o => o.Vehicle).Include(o => o.Provider);

        // 4) Orden SIEMPRE al final
        var orderedQuery = query.ApplySort(pagination.SortBy, pagination.SortDir);

        // 5) Total + página
        var totalRecords = await orderedQuery.CountAsync(cancellationToken);
        var items = await orderedQuery.Paginate(pagination).ToListAsync(cancellationToken);

        Response.Headers["X-Total-Count"] = totalRecords.ToString();

        return Ok(new PagedResult<VehicleOffer>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.RecordsNumber,
            Total = totalRecords
        });
    }
}