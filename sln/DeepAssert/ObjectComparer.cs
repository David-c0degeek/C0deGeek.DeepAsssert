using System.Collections;
using System.Reflection;

namespace C0deGeek.DeepAssert;

/// <summary>
/// Internal recursive comparer for deep equivalency based on reflection, including private members.
/// </summary>
internal static class ObjectComparer
{
    // Types treated as primitives for direct Equals comparison
    private static readonly Type[] SimpleTypes =
    [
        typeof(string), typeof(decimal), typeof(DateTime), typeof(Guid), typeof(TimeSpan)
    ];

    /// <summary>
    /// Entry-point: compares two objects (could be null), recording differences.
    /// </summary>
    public static void Compare(object? expected, object? actual, 
        EquivalencyOptions options, string path,
        List<string> differences, HashSet<(object, object)> visited)
    {
        if (HandleReferenceAndNullChecks(expected, actual, path, differences, visited))
        {
            return;
        }

        if (HandleCollectionComparison(expected, actual, options, path, differences, visited))
        {
            return;
        }

        if (HandleSimpleTypeComparison(expected, actual, path, differences))
        {
            return;
        }

        CompareMembers(expected!, actual!, options, path, differences, visited);
    }

    private static bool HandleReferenceAndNullChecks(object? expected, object? actual, string path,
        List<string> differences, HashSet<(object, object)> visited)
    {
        if (ReferenceEquals(expected, actual))
        {
            return true;
        }

        if (expected == null || actual == null)
        {
            differences.Add(FormatMessage(path, expected, actual));
            return true;
        }

        // If we've already compared these objects, skip to avoid infinite recursion
        var pair = (expected, actual);
        return !visited.Add(pair);
    }

    private static bool HandleCollectionComparison(object? expected, object? actual,
        EquivalencyOptions options, string path, 
        List<string> differences, HashSet<(object, object)> visited)
    {
        // Check if both objects are collections (but not strings)
        if (expected is not IEnumerable expectedEnum || actual is not IEnumerable actualEnum || 
            expected is string || actual is string)
        {
            return false;
        }

        CompareEnumerables(expectedEnum, actualEnum, options, path, differences, visited);
        return true;
    }

    private static bool HandleSimpleTypeComparison(object? expected, object? actual, string path,
        List<string> differences)
    {
        var expectedType = expected?.GetType();
        var actualType = actual?.GetType();
        
        // For simple types, we're looking for semantic equality, not just type equivalence
        if (expectedType == null || !IsSimple(expectedType) || actualType == null || !IsSimple(actualType))
        {
            return false;
        }
        
        // Handle special conversions like when comparing int to decimal, double, etc.
        if (TryConvertNumerics(expected, actual, out var expectedConverted, out var actualConverted))
        {
            expected = expectedConverted;
            actual = actualConverted;
        }

        if (expected == null || !expected.Equals(actual))
        {
            differences.Add(FormatMessage(path, expected, actual));
        }

        return true;
    }

    private static bool TryConvertNumerics(object? expected, object? actual, out object? expectedConverted, out object? actualConverted)
    {
        expectedConverted = expected;
        actualConverted = actual;
        
        if (expected == null || actual == null)
        {
            return false;
        }
        
        var expectedType = expected.GetType();
        var actualType = actual.GetType();
        
        // Only apply to numeric types
        if (!IsNumeric(expectedType) || !IsNumeric(actualType))
        {
            return false;
        }
        
        // If they're already the same type, no conversion needed
        if (expectedType == actualType)
        {
            return true;
        }
        
        try
        {
            // Try converting to the "larger" type to avoid precision loss
            if (GetNumericPrecedence(expectedType) >= GetNumericPrecedence(actualType))
            {
                actualConverted = Convert.ChangeType(actual, expectedType);
            }
            else
            {
                expectedConverted = Convert.ChangeType(expected, actualType);
            }
            return true;
        }
        catch
        {
            // If conversion fails, we'll fall back to normal comparison
            return false;
        }
    }
    
    private static bool IsNumeric(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }
    
    private static int GetNumericPrecedence(Type type)
    {
        // Higher precedence means "larger" type
        if (type == typeof(decimal)) return 10;
        if (type == typeof(double)) return 9;
        if (type == typeof(float)) return 8;
        if (type == typeof(ulong)) return 7;
        if (type == typeof(long)) return 6;
        if (type == typeof(uint)) return 5;
        if (type == typeof(int)) return 4;
        if (type == typeof(ushort)) return 3;
        if (type == typeof(short)) return 2;
        if (type == typeof(byte) || type == typeof(sbyte)) return 1;
        return 0;
    }

    private static void CompareMembers(object expected, object actual, 
        EquivalencyOptions options, string path, 
        List<string> differences, HashSet<(object, object)> visited)
    {
        var expectedType = expected.GetType();
        var actualType = actual.GetType();
        
        // For same-type comparisons, we can compare all members directly
        if (expectedType == actualType)
        {
            CompareSameTypeMembers(expected, actual, options, path, differences, visited);
            return;
        }
        
        // For different types, we need to match properties by name
        CompareDifferentTypeMembers(expected, actual, options, path, differences, visited);
    }
    
    private static void CompareSameTypeMembers(object expected, object actual,
        EquivalencyOptions options, string path,
        List<string> differences, HashSet<(object, object)> visited)
    {
        var type = expected.GetType();
        
        // Get all members that aren't excluded
        var members = GetPublicMembers(type)
            .Where(m => !options.IsExcluded(m.Name))
            .ToList();
        
        // Compare each member
        foreach (var member in members)
        {
            try
            {
                var expectedValue = GetMemberValue(member, expected);
                var actualValue = GetMemberValue(member, actual);
                var currentPath = string.IsNullOrEmpty(path) ? member.Name : $"{path}.{member.Name}";
                
                Compare(expectedValue, actualValue, options, currentPath, differences, visited);
            }
            catch (Exception)
            {
                // Skip members that can't be accessed
            }
        }
    }
    
    private static void CompareDifferentTypeMembers(object expected, object actual,
        EquivalencyOptions options, string path,
        List<string> differences, HashSet<(object, object)> visited)
    {
        var expectedType = expected.GetType();
        var actualType = actual.GetType();
        
        // Get expected public members that aren't excluded
        var expectedMembers = GetPublicMembers(expectedType)
            .Where(m => !options.IsExcluded(m.Name))
            .ToList();
        
        // Get actual public members that aren't excluded
        var actualMembers = GetPublicMembers(actualType)
            .Where(m => !options.IsExcluded(m.Name))
            .ToDictionary(m => m.Name, m => m);
        
        // Track what we've compared to detect missing members
        var handledMembers = new HashSet<string>();
        
        // Compare each expected member with its corresponding actual member, if it exists
        foreach (var expectedMember in expectedMembers)
        {
            if (options.IsExcluded(expectedMember.Name))
            {
                handledMembers.Add(expectedMember.Name);
                continue;
            }
            
            if (actualMembers.TryGetValue(expectedMember.Name, out var actualMember))
            {
                try
                {
                    var expectedValue = GetMemberValue(expectedMember, expected);
                    var actualValue = GetMemberValue(actualMember, actual);
                    var currentPath = string.IsNullOrEmpty(path) ? expectedMember.Name : $"{path}.{expectedMember.Name}";
                    
                    Compare(expectedValue, actualValue, options, currentPath, differences, visited);
                    handledMembers.Add(expectedMember.Name);
                }
                catch (Exception)
                {
                    // Skip members that can't be accessed
                    handledMembers.Add(expectedMember.Name);
                }
            }
            else
            {
                // Member exists in expected but not in actual
                try
                {
                    var expectedValue = GetMemberValue(expectedMember, expected);
                    // Only report if the value isn't null or default
                    if (expectedValue != null && !IsDefaultValue(expectedValue))
                    {
                        var currentPath = string.IsNullOrEmpty(path) ? expectedMember.Name : $"{path}.{expectedMember.Name}";
                        differences.Add($"{currentPath}: expected <{FormatValue(expectedValue)}>, but member not found in actual object");
                    }
                    handledMembers.Add(expectedMember.Name);
                }
                catch (Exception)
                {
                    // Skip members that can't be accessed
                    handledMembers.Add(expectedMember.Name);
                }
            }
        }
        
        // Check for members in actual that weren't in expected
        foreach (var actualMember in actualMembers.Values.Where(actualMember => !handledMembers.Contains(actualMember.Name) && !options.IsExcluded(actualMember.Name)))
        {
            try
            {
                var actualValue = GetMemberValue(actualMember, actual);
                // Only report if the value isn't null or default
                if (actualValue == null || IsDefaultValue(actualValue))
                {
                    continue;
                }
                
                var currentPath = string.IsNullOrEmpty(path) ? actualMember.Name : $"{path}.{actualMember.Name}";
                differences.Add($"{currentPath}: member not found in expected but had value <{FormatValue(actualValue)}> in actual object");
            }
            catch (Exception)
            {
                // Skip members that can't be accessed
            }
        }
    }
    
    private static bool IsDefaultValue(object value)
    {
        var type = value.GetType();
        
        // Value types have default values
        if (!type.IsValueType)
        {
            // For reference types, null is the default
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return value == null;
        }
        
        var defaultValue = Activator.CreateInstance(type);
        return value.Equals(defaultValue);
    }
    
    private static IEnumerable<MemberInfo> GetPublicMembers(Type type)
    {
        // For records, skip compiler-generated members except public properties
        var isRecord = type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null ||
                       type.GetMethods().Any(m => m.Name == "<Clone>$");
        
        return type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m is PropertyInfo or FieldInfo)
            .Where(m => !(m is PropertyInfo pi && pi.GetIndexParameters().Length > 0)) // Skip indexers
            .Where(m => !isRecord || !m.Name.Contains('<')) // Filter out compiler-generated backing fields in records
            .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_")); // Skip property accessor methods
    }

    private static object? GetMemberValue(MemberInfo member, object obj)
    {
        try
        {
            return member switch
            {
                PropertyInfo prop => prop.GetValue(obj),
                FieldInfo field => field.GetValue(obj),
                _ => throw new ArgumentException($"Member {member.Name} is not a property or field")
            };
        }
        catch (TargetInvocationException)
        {
            // Skip properties that throw exceptions (like EF navigation properties)
            return null;
        }
    }

    private static void CompareEnumerables(IEnumerable expectedEnum, IEnumerable actualEnum,
        EquivalencyOptions options, string path, 
        List<string> differences, HashSet<(object, object)> visited)
    {
        var expectedList = expectedEnum.Cast<object?>().ToList();
        var actualList = actualEnum.Cast<object?>().ToList();

        if (expectedList.Count != actualList.Count)
        {
            differences.Add($"{path}.Count: expected <{expectedList.Count}>, actual <{actualList.Count}>");
            return;
        }

        if (options.AllowUnordered)
        {
            CompareUnorderedEnumerables(expectedList, actualList, options, path, differences, visited);
        }
        else
        {
            // For ordered comparison, just compare elements at the same positions
            for (var i = 0; i < expectedList.Count; i++)
            {
                Compare(expectedList[i], actualList[i], options, $"{path}[{i}]", differences, visited);
            }
        }
    }
    
    private static void CompareUnorderedEnumerables(
        List<object?> expectedList, 
        List<object?> actualList,
        EquivalencyOptions options, 
        string path, 
        List<string> differences, 
        HashSet<(object, object)> visited)
    {
        var actualMatched = new HashSet<int>();
        
        for (var i = 0; i < expectedList.Count; i++)
        {
            var expectedItem = expectedList[i];
            var foundMatch = false;
            
            // Try to find a matching unmatched item in the actual list
            for (var j = 0; j < actualList.Count; j++)
            {
                if (actualMatched.Contains(j))
                {
                    continue;
                }
                
                var tempDifferences = new List<string>();
                var actualItem = actualList[j];
                
                // Create a new visited set for this temporary comparison to avoid affecting the main traversal
                var tempVisited = new HashSet<(object, object)>(visited, new ReferenceTupleEqualityComparer());
                
                Compare(expectedItem, actualItem, options, string.Empty, tempDifferences, tempVisited);

                if (tempDifferences.Count != 0)
                {
                    continue;
                }
                
                actualMatched.Add(j);
                foundMatch = true;
                break;
            }
            
            if (!foundMatch)
            {
                differences.Add($"{path}[{i}]: No matching element found for {FormatValue(expectedItem)}");
            }
        }
    }

    private static bool IsSimple(Type type)
        => type.IsPrimitive
           || type.IsEnum
           || SimpleTypes.Contains(type)
           || Nullable.GetUnderlyingType(type) is { } underlying && IsSimple(underlying);

    private static string FormatMessage(string path, object? expected, object? actual)
        => string.IsNullOrEmpty(path)
            ? $"<expected> {FormatValue(expected)}, <actual> {FormatValue(actual)}"
            : $"{path}: expected <{FormatValue(expected)}>, actual <{FormatValue(actual)}>";

    private static string FormatValue(object? value)
        => value?.ToString() ?? "null";
}
