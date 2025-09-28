using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteApp.Backend.Helpers;
using RouteApp.Backend.Repositories.Interfaces;
using RouteApp.Backend.UnitsOfWork.Interfaces;
using RouteApp.Shared.DTOs;
using RouteApp.Shared.Entities;
using RouteApp.Shared.Responses;

namespace RouteApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CapacityRequestsController : GenericController<CapacityRequest>
    {
        //public CapacityRequestsController(IGenericUnitOfWork<CapacityRequest> unitOfWork) : base(unitOfWork)
        //{
        //}

        private readonly IGenericUnitOfWork<CapacityRequest> _capacityRequestUnitOfWork;
        private readonly IGenericRepository<CapacityRequest> _capacityRequestRepository;

        public CapacityRequestsController(
            IGenericUnitOfWork<CapacityRequest> capacityRequestUnitOfWork,
            IGenericRepository<CapacityRequest> capacityRequestRepository) : base(capacityRequestUnitOfWork)
        {
            _capacityRequestUnitOfWork = capacityRequestUnitOfWork;
            _capacityRequestRepository = capacityRequestRepository;
        }

        // GET /api/capacityrequests/paged?term=Surco&page=1&recordsNumber=10&sortBy=ServiceDate&sortDir=asc
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<CapacityRequest>>> GetPaged([FromQuery] PaginationDTO pagination)
        {
            // valores por defecto
            var sortBy = string.IsNullOrWhiteSpace(pagination.SortBy) ? "ServiceDate" : pagination.SortBy!;
            var sortDir = string.IsNullOrWhiteSpace(pagination.SortDir) ? "asc" : pagination.SortDir;

            // 1) Base como IQueryable SIN ordenar
            IQueryable<CapacityRequest> query = _capacityRequestRepository.Query()
                .ApplyFilter(pagination.Term); // si tu ApplyFilter no pega, no pasa nada

            // 2) Filtros adicionales (todos los Where ANTES del sort)
            if (!string.IsNullOrWhiteSpace(pagination.Term))
            {
                query = query.WhereDynamicContains("Zone", pagination.Term);
                // añade más .WhereDynamicContains(...) si quieres OR sobre otras columnas
            }

            // 3) Orden al final
            var ordered = query.ApplySort(sortBy, sortDir);

            // 4) Total + página
            var totalRecords = await ordered.CountAsync();
            var items = await ordered.Paginate(pagination).ToListAsync();

            Response.Headers["X-Total-Count"] = totalRecords.ToString();

            return Ok(new PagedResult<CapacityRequest>
            {
                Items = items,
                Page = pagination.Page,
                PageSize = pagination.RecordsNumber,
                Total = totalRecords
            });
        }
    }
}