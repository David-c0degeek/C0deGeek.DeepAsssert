using System.Linq.Expressions;

namespace C0deGeek.DeepAssert;

/// <summary>
/// Options to control equivalency comparison between objects.
/// </summary>
public class EquivalencyOptions
{
    /// <summary>
    /// Collection of member names (properties or fields) to exclude from comparison.
    /// </summary>
    private readonly HashSet<string> _excludedMembers = [];
    
    /// <summary>
    /// Collection of member names to exclude from collection elements.
    /// </summary>
    private readonly HashSet<string> _excludedElementMembers = [];
    
    /// <summary>
    /// Control whether collection comparison allows elements to be in different order.
    /// </summary>
    public bool AllowUnordered { get; set; } = true;
    
    /// <summary>
    /// Exclude a property or field from comparison by name.
    /// </summary>
    public EquivalencyOptions Excluding(string memberName)
    {
        ArgumentNullException.ThrowIfNull(memberName);
        _excludedMembers.Add(memberName);
        return this;
    }
    
    /// <summary>
    /// Exclude a property or field from comparison by lambda expression.
    /// </summary>
    public EquivalencyOptions Excluding<T>(Expression<Func<T, object?>> memberExpression)
    {
        ArgumentNullException.ThrowIfNull(memberExpression);
        var memberName = ExtractMemberName(memberExpression);
        _excludedMembers.Add(memberName);
        return this;
    }
    
    /// <summary>
    /// Exclude a property or field from collection elements by name.
    /// </summary>
    public EquivalencyOptions ExcludingElementMember(string memberName)
    {
        ArgumentNullException.ThrowIfNull(memberName);
        _excludedElementMembers.Add(memberName);
        return this;
    }
    
    /// <summary>
    /// Exclude a property or field from collection elements by lambda expression.
    /// </summary>
    public EquivalencyOptions ExcludingElementMember<T>(Expression<Func<T, object?>> memberExpression)
    {
        ArgumentNullException.ThrowIfNull(memberExpression);
        var memberName = ExtractMemberName(memberExpression);
        _excludedElementMembers.Add(memberName);
        return this;
    }
    
    /// <summary>
    /// Check if a member is excluded from comparison.
    /// </summary>
    internal bool IsExcluded(string memberName)
    {
        return _excludedMembers.Contains(memberName) ||
               // Exclude auto-property backing fields for any excluded member
               _excludedMembers.Any(excluded => memberName == $"<{excluded}>k__BackingField");
    }
    
    /// <summary>
    /// Check if a member is excluded for collection elements.
    /// </summary>
    internal bool IsElementMemberExcluded(string memberName)
    {
        return _excludedElementMembers.Contains(memberName) ||
               // Exclude auto-property backing fields for any excluded element member
               _excludedElementMembers.Any(excluded => memberName == $"<{excluded}>k__BackingField");
    }
    
    /// <summary>
    /// Helper method to extract member name from expression.
    /// </summary>
    private static string ExtractMemberName<T>(Expression<Func<T, object?>> memberExpression)
    {
        // Unwrap conversions (e.g., boxing/unboxing)
        var body = memberExpression.Body;
        if (body is UnaryExpression { Operand: MemberExpression memberUnary })
        {
            body = memberUnary;
        }
        
        if (body is not MemberExpression member)
        {
            throw new ArgumentException("Expression must be a member expression", nameof(memberExpression));
        }
        
        return member.Member.Name;
    }
}
