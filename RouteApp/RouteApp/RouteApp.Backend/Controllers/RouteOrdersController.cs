using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteApp.Backend.Data;
using RouteApp.Shared.Entities;

namespace RouteApp.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RouteOrdersController : ControllerBase
{
    private readonly DataContext _context;

    public RouteOrdersController(DataContext context) => _context = context;

    // GET api/routeorders/{routeId}
    [HttpGet("{routeId:int}")]
    public async Task<IActionResult> GetByRoute(int routeId, CancellationToken cancellationToken)
    {
        var list = await _context.RouteOrders
            .AsNoTracking()
            .Include(ro => ro.Order)
            .Where(ro => ro.RouteId == routeId)
            .OrderBy(ro => ro.StopSequence)
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    // POST api/routeorders  (upsert SIN cambiar secuencia si existe)
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] RouteOrder routeOrder, CancellationToken cancellationToken)
    {
        if (routeOrder is null) return BadRequest("Payload inválido.");

        var existing = await _context.RouteOrders.FindAsync(
            new object?[] { routeOrder.RouteId, routeOrder.OrderId }, cancellationToken);

        if (existing is null)
        {
            // Nuevo registro: si no viene StopSequence o viene <= 0, lo agregamos al final.
            var maxSeq = await _context.RouteOrders
                .Where(x => x.RouteId == routeOrder.RouteId)
                .Select(x => (int?)x.StopSequence)
                .MaxAsync(cancellationToken) ?? 0;

            if (routeOrder.StopSequence <= 0)
            {
                routeOrder.StopSequence = maxSeq + 1;
            }
            else
            {
                // (OPCIONAL) Insertar en posición y desplazar los siguientes
                // Descomenta si quieres permitir insertar en una posición específica:
                //
                // var newPos = Math.Min(Math.Max(routeOrder.StopSequence, 1), maxSeq + 1);
                // await ShiftOnInsertAsync(routeOrder.RouteId, newPos, cancellationToken);
                // routeOrder.StopSequence = newPos;
                //
                // Si no habilitas lo anterior, forzamos append seguro:
                routeOrder.StopSequence = maxSeq + 1;
            }

            _context.RouteOrders.Add(routeOrder);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(routeOrder);
        }
        else
        {
            // Actualiza otros campos, pero preserva la secuencia para no violar el índice único.
            var originalSeq = existing.StopSequence;

            _context.Entry(existing).CurrentValues.SetValues(routeOrder);
            existing.StopSequence = originalSeq; // ¡clave!

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(existing);
        }
    }

    // DELETE api/routeorders/{routeId}/{orderId}  (compacta secuencias)
    [HttpDelete("{routeId:int}/{orderId:int}")]
    public async Task<IActionResult> Delete(int routeId, int orderId, CancellationToken cancellationToken)
    {
        var entity = await _context.RouteOrders.FindAsync(new object?[] { routeId, orderId }, cancellationToken);
        if (entity is null) return NotFound();

        using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        var removedSeq = entity.StopSequence;

        _context.RouteOrders.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Compacta: todos los > removedSeq bajan 1
        var toShift = await _context.RouteOrders
            .Where(x => x.RouteId == routeId && x.StopSequence > removedSeq)
            .ToListAsync(cancellationToken);

        foreach (var ro in toShift) ro.StopSequence--;

        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(true);
    }

    // (Opcional) mover secuencia: api/routeorders/move
    public record MoveDto(int RouteId, int OrderId, int NewSeq);

    [HttpPost("move")]
    public async Task<IActionResult> Move([FromBody] MoveDto dto, CancellationToken cancellationToken)
    {
        if (dto is null) return BadRequest("Payload inválido.");

        var routeOrder = await _context.RouteOrders
            .FirstOrDefaultAsync(x => x.RouteId == dto.RouteId && x.OrderId == dto.OrderId, cancellationToken);

        if (routeOrder is null) return NotFound("RouteOrder no encontrado.");

        // Normaliza newSeq dentro del rango [1 .. max]
        var maxSeq = await _context.RouteOrders
            .Where(x => x.RouteId == dto.RouteId)
            .MaxAsync(x => (int?)x.StopSequence, cancellationToken) ?? 0;

        if (maxSeq == 0) return BadRequest("La ruta no tiene paradas.");

        var current = routeOrder.StopSequence;
        var target = dto.NewSeq;
        if (target < 1) target = 1;
        if (target > maxSeq) target = maxSeq;

        if (target == current) return Ok(routeOrder); // nada que mover

        using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (target < current)
        {
            // Mover hacia arriba: [target, current-1] +1
            var affected = await _context.RouteOrders
                .Where(x => x.RouteId == dto.RouteId &&
                            x.StopSequence >= target &&
                            x.StopSequence < current)
                .ToListAsync(cancellationToken);

            foreach (var ro in affected) ro.StopSequence++;
        }
        else
        {
            // Mover hacia abajo: (current, target] -1
            var affected = await _context.RouteOrders
                .Where(x => x.RouteId == dto.RouteId &&
                            x.StopSequence > current &&
                            x.StopSequence <= target)
                .ToListAsync(cancellationToken);

            foreach (var ro in affected) ro.StopSequence--;
        }

        routeOrder.StopSequence = target;
        await _context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(routeOrder);
    }

    // ---------- Helpers opcionales ----------

    /// <summary>
    /// Inserta en posición 'newPos' desplazando +1 las secuencias >= newPos.
    /// Requiere transacción externa.
    /// </summary>
    private async Task ShiftOnInsertAsync(int routeId, int newPos, CancellationToken ct)
    {
        var affected = await _context.RouteOrders
            .Where(x => x.RouteId == routeId && x.StopSequence >= newPos)
            .ToListAsync(ct);

        foreach (var ro in affected) ro.StopSequence++;
    }
}