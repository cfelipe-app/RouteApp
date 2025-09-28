using Microsoft.EntityFrameworkCore;
using RouteApp.Backend.Data;
using RouteApp.Backend.Helpers;
using RouteApp.Backend.Repositories.Interfaces;
using RouteApp.Shared.DTOs;
using RouteApp.Shared.Responses;

namespace RouteApp.Backend.Repositories.Implementations;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly DataContext _context;
    private readonly DbSet<T> _entity;

    public GenericRepository(DataContext context)
    {
        _context = context;
        _entity = context.Set<T>();
    }

    public IQueryable<T> Query(bool asNoTracking = true)
        => asNoTracking ? _entity.AsNoTracking() : _entity;

    public virtual async Task<ActionResponse<T>> AddAsync(T entity)
    {
        _context.Add(entity);
        try
        {
            await _context.SaveChangesAsync();
            return new ActionResponse<T> { WasSuccess = true, Result = entity };
        }
        catch (DbUpdateException)
        {
            return DbUpdateExceptionActionResponse();
        }
        catch (Exception exception)
        {
            return ExceptionActionResponse(exception);
        }
    }

    public virtual async Task<ActionResponse<T>> UpdateAsync(T entity)
    {
        try
        {
            _context.Update(entity);
            await _context.SaveChangesAsync();
            return new ActionResponse<T> { WasSuccess = true, Result = entity };
        }
        catch (DbUpdateException)
        {
            return DbUpdateExceptionActionResponse();
        }
        catch (Exception exception)
        {
            return ExceptionActionResponse(exception);
        }
    }

    public virtual async Task<ActionResponse<T>> DeleteAsync(int id)
    {
        var row = await _entity.FindAsync(id);
        if (row == null)
        {
            return new ActionResponse<T> { WasSuccess = false, Message = "Registro no encontrado" };
        }

        try
        {
            _entity.Remove(row);
            await _context.SaveChangesAsync();
            return new ActionResponse<T> { WasSuccess = true };
        }
        catch
        {
            return new ActionResponse<T>
            {
                WasSuccess = false,
                Message = "No se puede borrar, porque tiene registros relacionados"
            };
        }
    }

    public virtual async Task<ActionResponse<T>> GetAsync(int id)
    {
        var row = await _entity.FindAsync(id);
        if (row != null)
            return new ActionResponse<T> { WasSuccess = true, Result = row };

        return new ActionResponse<T> { WasSuccess = false, Message = "Registro no encontrado" };
    }

    public virtual async Task<ActionResponse<IEnumerable<T>>> GetAsync()
    {
        return new ActionResponse<IEnumerable<T>>
        {
            WasSuccess = true,
            Result = await _entity.AsNoTracking().ToListAsync()
        };
    }

    // Paginación simple (offset)
    public virtual async Task<ActionResponse<IEnumerable<T>>> GetAsync(PaginationDTO pagination)
    {
        var q = _entity.AsNoTracking().AsQueryable();
        return new ActionResponse<IEnumerable<T>>
        {
            WasSuccess = true,
            Result = await q.Paginate(pagination).ToListAsync()
        };
    }

    public virtual async Task<ActionResponse<int>> GetTotalRecordsAsync(PaginationDTO pagination)
    {
        var q = _entity.AsNoTracking().AsQueryable();
        var count = await q.CountAsync();
        return new ActionResponse<int> { WasSuccess = true, Result = count };
    }

    // Paginación con filtro y orden (opcional)
    public virtual async Task<ActionResponse<IEnumerable<T>>> GetAsync(PaginationDTO pagination, string? term, string? sortBy, string sortDir)
    {
        var q = _entity.AsNoTracking()
                       .ApplyFilter(term)
                       .ApplySort(sortBy, sortDir);

        var items = await q.Paginate(pagination).ToListAsync();
        return new ActionResponse<IEnumerable<T>> { WasSuccess = true, Result = items };
    }

    public virtual async Task<ActionResponse<int>> GetTotalRecordsAsync(string? term)
    {
        var q = _entity.AsNoTracking().ApplyFilter(term);
        var total = await q.CountAsync();
        return new ActionResponse<int> { WasSuccess = true, Result = total };
    }

    // Helpers de error
    private static ActionResponse<T> ExceptionActionResponse(Exception exception)
        => new() { WasSuccess = false, Message = exception.Message };

    private static ActionResponse<T> DbUpdateExceptionActionResponse()
        => new() { WasSuccess = false, Message = "Ya existe el registro que estás intentando crear." };
}