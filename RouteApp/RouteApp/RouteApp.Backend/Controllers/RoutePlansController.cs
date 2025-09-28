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
public class RoutePlansController : GenericController<RoutePlan>
{
    private readonly IGenericUnitOfWork<RoutePlan> _routePlanUnitOfWork;
    private readonly IGenericRepository<RoutePlan> _routePlanRepository;

    public RoutePlansController(
        IGenericUnitOfWork<RoutePlan> routePlanUnitOfWork,
        IGenericRepository<RoutePlan> routePlanRepository) : base(routePlanUnitOfWork)
    {
        _routePlanUnitOfWork = routePlanUnitOfWork;
        _routePlanRepository = routePlanRepository;
    }

    // GET /api/routeplans/paged?term=RP-2025&page=1&recordsNumber=10&sortBy=ServiceDate&sortDir=desc
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResult<RoutePlan>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RoutePlan>>> GetPaged(
        [FromQuery] PaginationDTO pagination,
        [FromQuery] RouteStatus? status = null,
        [FromQuery] DateTime? fromServiceDate = null,
        [FromQuery] DateTime? toServiceDate = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pagination.SortBy)) pagination.SortBy = "ServiceDate";
        if (string.IsNullOrWhiteSpace(pagination.SortDir)) pagination.SortDir = "desc";

        // 1) Base como IQueryable SIN ordenar
        IQueryable<RoutePlan> query = _routePlanRepository.Query();

        // 2) Búsqueda por código (OR-ready si luego agregas más props, p.ej. "DepotCode")
        if (!string.IsNullOrWhiteSpace(pagination.Term))
            query = query.ApplySearch(pagination.Term, "Code");

        // 3) Filtros opcionales
        if (status.HasValue) query = query.Where(rp => rp.Status == status.Value);

        if (fromServiceDate.HasValue)
            query = query.Where(rp => rp.ServiceDate >= fromServiceDate.Value);

        if (toServiceDate.HasValue)
        {
            // fin de día inclusivo
            var inclusive = toServiceDate.Value.Date.AddDays(1);
            query = query.Where(rp => rp.ServiceDate < inclusive);
        }

        // 4) Orden SIEMPRE al final
        var orderedQuery = query.ApplySort(pagination.SortBy, pagination.SortDir);

        // 5) Total + página
        var totalRecords = await orderedQuery.CountAsync(cancellationToken);
        var items = await orderedQuery.Paginate(pagination).ToListAsync(cancellationToken);

        Response.Headers["X-Total-Count"] = totalRecords.ToString();

        return Ok(new PagedResult<RoutePlan>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.RecordsNumber,
            Total = totalRecords
        });
    }
}