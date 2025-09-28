using RouteApp.Shared.DTOs;
using System.Linq.Expressions;
using System.Reflection;

namespace RouteApp.Backend.Helpers;
/*
public static class QueryableExtensions
{
    public static IQueryable<T> Paginate<T>(this IQueryable<T> queryable, PaginationDTO pagination)
    {
        return queryable
            .Skip((pagination.Page - 1) * pagination.RecordsNumber)
            .Take(pagination.RecordsNumber);
    }
}
*/

public static class QueryableExtensions
{
    /// Aplica paginación (Skip/Take) a la consulta.
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, PaginationDTO pagination)
        => query.Skip((Math.Max(pagination.Page, 1) - 1) * Math.Max(pagination.RecordsNumber, 1))
                .Take(Math.Max(pagination.RecordsNumber, 1));

    /// Filtra dinámicamente por "Name" o "Code" si existen en la entidad.
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return query;

        var hasName = typeof(T).GetProperty("Name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null;
        var hasCode = typeof(T).GetProperty("Code", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null;

        // Si tiene ambos, busca por ambos con OR usando ApplySearch
        if (hasName && hasCode) return query.ApplySearch(term, "Name", "Code");
        if (hasName) return query.WhereDynamicContains("Name", term);
        if (hasCode) return query.WhereDynamicContains("Code", term);

        return query;
    }

    /// Genera un Where dinámico con Contains sobre un campo string (case-insensitive).
    public static IQueryable<T> WhereDynamicContains<T>(this IQueryable<T> query, string property, string term)
    {
        var prop = typeof(T).GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is null || prop.PropertyType != typeof(string)) return query;

        var p = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(p, prop);

        // x => x.Prop != null && x.Prop.ToLower().Contains(term.ToLower())
        var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
        var toLower = Expression.Call(member, nameof(string.ToLower), Type.EmptyTypes);
        var termConst = Expression.Constant(term.ToLower());
        var contains = Expression.Call(toLower, nameof(string.Contains), Type.EmptyTypes, termConst);
        var body = Expression.AndAlso(notNull, contains);

        var lambda = Expression.Lambda<Func<T, bool>>(body, p);
        return query.Where(lambda);
    }

    /// Busca 'term' en varias propiedades string con OR (p.ej. "Plate","Brand","Model").
    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> source, string? term, params string[] properties)
    {
        if (string.IsNullOrWhiteSpace(term) || properties is null || properties.Length == 0)
            return source;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? orChain = null;

        foreach (var propName in properties)
        {
            var prop = typeof(T).GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null || prop.PropertyType != typeof(string)) continue;

            var member = Expression.Property(parameter, prop);
            var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
            var toLower = Expression.Call(member, nameof(string.ToLower), Type.EmptyTypes);
            var contains = Expression.Call(toLower, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(term.ToLower()));

            var thisPropExpr = Expression.AndAlso(notNull, contains);
            orChain = orChain is null ? thisPropExpr : Expression.OrElse(orChain, thisPropExpr);
        }

        if (orChain is null) return source;

        var lambda = Expression.Lambda<Func<T, bool>>(orChain, parameter);
        return source.Where(lambda);
    }

    /// Variante que aplica búsqueda sólo sobre propiedades existentes.
    public static IQueryable<T> ApplyTermAuto<T>(this IQueryable<T> source, string? term, params string[] candidateProperties)
    {
        if (string.IsNullOrWhiteSpace(term)) return source;
        var props = candidateProperties
            .Where(p => typeof(T).GetProperty(p, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.PropertyType == typeof(string))
            .ToArray();
        return props.Length == 0 ? source : source.ApplySearch(term, props);
    }

    /// Orden dinámico seguro: usa sortBy si existe, si no busca defaults (Id, CreatedAt, Name, Code).
    public static IOrderedQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortBy, string sortDir)
    {
        var t = typeof(T);
        PropertyInfo? prop = null;

        if (!string.IsNullOrWhiteSpace(sortBy))
            prop = t.GetProperty(sortBy!, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        // fallback
        prop ??= t.GetProperty("Id") ??
                 t.GetProperty("CreatedAt") ??
                 t.GetProperty("Name") ??
                 t.GetProperty("Code");

        if (prop is null) return query.OrderBy(_ => 0);

        var p = Expression.Parameter(t, "x");
        var member = Expression.Property(p, prop);

        // Convertimos a object para no chocar con tipos value/reference
        var body = Expression.Convert(member, typeof(object));
        var keySelector = Expression.Lambda<Func<T, object>>(body, p);

        return sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }
}