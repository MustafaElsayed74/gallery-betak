using System.Linq.Expressions;

namespace ElMasria.Domain.Interfaces;

/// <summary>
/// Encapsulates query logic (filtering, sorting, including, paging) into a reusable specification.
/// </summary>
public interface ISpecification<T>
{
    /// <summary>The filter criteria (WHERE clause).</summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>Navigational properties to eager-load (INCLUDE).</summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>String-based navigational properties to eager-load.</summary>
    List<string> IncludeStrings { get; }

    /// <summary>Sorting (ORDER BY ASC).</summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>Sorting (ORDER BY DESC).</summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>Pagination (SKIP).</summary>
    int Skip { get; }

    /// <summary>Pagination (TAKE).</summary>
    int Take { get; }

    /// <summary>Whether paging is enabled for this specification.</summary>
    bool IsPagingEnabled { get; }
}
