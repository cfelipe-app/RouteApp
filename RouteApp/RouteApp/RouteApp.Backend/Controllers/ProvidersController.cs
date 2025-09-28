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
public class ProvidersController : GenericController<Provider>
{
    private readonly IGenericUnitOfWork<Provider> _providerUnitOfWork;
    private readonly IGenericRepository<Provider> _providerRepository;

    public ProvidersController(
        IGenericUnitOfWork<Provider> providerUnitOfWork,
        IGenericRepository<Provider> providerRepository) : base(providerUnitOfWork)
    {
        _providerUnitOfWork = providerUnitOfWork;
        _providerRepository = providerRepository;
    }

    // GET /api/providers/paged?term=...&page=1&recordsNumber=10&sortBy=Name&sortDir=asc
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResult<Provider>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Provider>>> GetPaged(
        [FromQuery] PaginationDTO pagination,
        [FromQuery] bool? isActive = null,
        [FromQuery] DateTime? fromCreated = null,
        [FromQuery] DateTime? toCreated = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pagination.SortBy)) pagination.SortBy = "Name";
        if (string.IsNullOrWhiteSpace(pagination.SortDir)) pagination.SortDir = "asc";

        // 1) Base como IQueryable SIN ordenar
        IQueryable<Provider> query = _providerRepository.Query();

        // 2) Búsqueda (OR) en varias columnas string
        if (!string.IsNullOrWhiteSpace(pagination.Term))
        {
            // Requiere la extensión ApplySearch (que arma un OR sobre propiedades string)
            query = query.ApplySearch(pagination.Term, "Name", "ContactName", "TaxId", "Email", "Phone");
        }

        // 3) Filtros opcionales
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);
        if (fromCreated.HasValue) query = query.Where(p => p.CreatedAt >= fromCreated.Value);
        if (toCreated.HasValue)
        {
            var inclusive = toCreated.Value.Date.AddDays(1); // fin del día
            query = query.Where(p => p.CreatedAt < inclusive);
        }

        // 4) Orden SIEMPRE al final
        var orderedQuery = query.ApplySort(pagination.SortBy, pagination.SortDir);

        // 5) Total + página
        var totalRecords = await orderedQuery.CountAsync(cancellationToken);
        var items = await orderedQuery.Paginate(pagination).ToListAsync(cancellationToken);

        Response.Headers["X-Total-Count"] = totalRecords.ToString();

        return Ok(new PagedResult<Provider>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.RecordsNumber,
            Total = totalRecords
        });
    }
}