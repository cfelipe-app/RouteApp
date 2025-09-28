using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteApp.Backend.Data;
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
    public class OrdersController : GenericController<Order>
    {
        private readonly IGenericUnitOfWork<Order> _orderUnitOfWork;
        private readonly IGenericRepository<Order> _orderRepository;

        //public OrdersController(IGenericUnitOfWork<Order> unitOfWork) : base(unitOfWork)
        //{
        //}

        public OrdersController(
            IGenericUnitOfWork<Order> orderUnitOfWork,
            IGenericRepository<Order> orderRepository) : base(orderUnitOfWork)
        {
            _orderUnitOfWork = orderUnitOfWork;
            _orderRepository = orderRepository;
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<Order>>> GetPaged([FromQuery] PaginationDTO pagination)
        {
            // Valores predeterminados amigables
            if (string.IsNullOrWhiteSpace(pagination.SortBy))
                pagination.SortBy = "CreatedAt";

            if (string.IsNullOrWhiteSpace(pagination.SortDir))
                pagination.SortDir = "asc";

            var query = _orderRepository.Query()
                                        .ApplyFilter(pagination.Term)
                                        .ApplySort(pagination.SortBy, pagination.SortDir);

            var totalRecords = await query.CountAsync();
            var items = await query.Paginate(pagination).ToListAsync();

            // Cabecera opcional con el total
            Response.Headers["X-Total-Count"] = totalRecords.ToString();

            var result = new PagedResult<Order>
            {
                Items = items,
                Page = pagination.Page,
                PageSize = pagination.RecordsNumber,
                Total = totalRecords
            };

            return Ok(result);
        }
    }
}