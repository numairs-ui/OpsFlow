using System.Linq.Expressions;

namespace OpsFlow.Domain.Authorization;

/// <summary>
/// List-visibility filters that narrow an <see cref="IQueryable{T}"/> to what a <see cref="ScopeSpec"/>
/// may see. They take key selectors (e.g. <c>t =&gt; t.StoreId</c>) so the predicate stays
/// EF-translatable and the rule stays entity-agnostic — no marker interfaces on the entities.
/// </summary>
public static class ScopeQueryExtensions
{
    /// <summary>
    /// Store-rooted entities (tasks, recurring assignments, submissions): a store-scoped caller sees
    /// only its own store; a region caller sees stores whose region is in its set; super_admin sees all.
    /// </summary>
    public static IQueryable<T> WhereStoreInScope<T>(
        this IQueryable<T> query,
        ScopeSpec spec,
        Expression<Func<T, Guid>> storeIdSelector,
        Expression<Func<T, Guid>> regionIdSelector)
    {
        if (spec.IsGlobal) return query;

        var p = Expression.Parameter(typeof(T), "e");

        if (spec.IsStoreScoped)
        {
            if (spec.StoreId is not { } storeId) return query.Where(_ => false);
            var body = Expression.Equal(Rebind(storeIdSelector, p), Expression.Constant(storeId));
            return query.Where(Expression.Lambda<Func<T, bool>>(body, p));
        }

        // region-scoped (admin / supervisor) — or any other role: bounded to the region set
        var region = Rebind(regionIdSelector, p);
        var contains = ContainsCall(spec.RegionIds, region);
        return query.Where(Expression.Lambda<Func<T, bool>>(contains, p));
    }

    /// <summary>
    /// Scoped entities (templates, checklists, form templates): everyone sees System scope; a caller
    /// also sees Regional resources in its region set and Store resources for its own store.
    /// </summary>
    public static IQueryable<T> WhereScopedVisible<T>(
        this IQueryable<T> query,
        ScopeSpec spec,
        Expression<Func<T, string>> scopeSelector,
        Expression<Func<T, Guid?>> regionIdSelector,
        Expression<Func<T, Guid?>> storeIdSelector)
    {
        if (spec.IsGlobal) return query;

        var p = Expression.Parameter(typeof(T), "e");
        var scope = Rebind(scopeSelector, p);
        var region = Rebind(regionIdSelector, p);
        var store = Rebind(storeIdSelector, p);

        Expression predicate = Expression.Equal(scope, Expression.Constant("System"));

        if (spec.RegionIds.Count > 0)
        {
            var regionVal = Expression.Property(region, "Value");
            var regionalClause = Expression.AndAlso(
                Expression.Equal(scope, Expression.Constant("Regional")),
                Expression.AndAlso(
                    Expression.Property(region, "HasValue"),
                    ContainsCall(spec.RegionIds, regionVal)));
            predicate = Expression.OrElse(predicate, regionalClause);
        }

        if (spec.StoreId is { } storeId)
        {
            var storeVal = Expression.Property(store, "Value");
            var storeClause = Expression.AndAlso(
                Expression.Equal(scope, Expression.Constant("Store")),
                Expression.AndAlso(
                    Expression.Property(store, "HasValue"),
                    Expression.Equal(storeVal, Expression.Constant(storeId))));
            predicate = Expression.OrElse(predicate, storeClause);
        }

        return query.Where(Expression.Lambda<Func<T, bool>>(predicate, p));
    }

    // Build `spec.RegionIds.Contains(<value>)` — EF translates List.Contains to SQL IN.
    private static Expression ContainsCall(IReadOnlyList<Guid> regionIds, Expression value)
    {
        var list = Expression.Constant(regionIds.ToList(), typeof(List<Guid>));
        var contains = typeof(List<Guid>).GetMethod(nameof(List<Guid>.Contains), [typeof(Guid)])!;
        return Expression.Call(list, contains, value);
    }

    // Rewrite a selector's body to use a shared parameter so multiple selectors compose into one lambda.
    private static Expression Rebind<T, TResult>(Expression<Func<T, TResult>> selector, ParameterExpression p) =>
        new ParameterReplacer(selector.Parameters[0], p).Visit(selector.Body)!;

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}
