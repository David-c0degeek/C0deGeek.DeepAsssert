using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace C0deGeek.DeepAssert;

/// <summary>
/// C0deGeek assertion extensions for deep equivalency.
/// </summary>
public static class DeepAssert
{
    public static T ThrowsExactly<T>(Action action, string? message = null) where T : Exception
    {
        var expectedType = typeof(T);
        try
        {
            // Execute the action that should throw
            action();
            
            // If we get here, no exception was thrown - that's a test failure
            var failMessage = $"Expected exception of type {expectedType} but no exception was thrown.";
            
            // If we're in an assertion scope, record the failure and return a default instance
            // to allow the test to continue
            var scope = AssertionScope.Current;
            if (scope != null)
            {
                scope.RecordFailure(failMessage);
                return null!; // The test will fail at the end of the scope
            }
            
            // Otherwise, fail immediately
            Assert.Fail(failMessage);
            return null!; // Will never reach here
        }
        catch (Exception ex)
        {
            // Check if the correct exception type was thrown
            if (ex.GetType() == expectedType)
            {
                return (T)ex;
            }
            
            var failMessage = $"Expected exception of type {expectedType} but got {ex.GetType()}";
                
            // If we're in an assertion scope, record the failure and continue
            var scope = AssertionScope.Current;
            if (scope != null)
            {
                scope.RecordFailure(failMessage);
                return null!; // The test will fail at the end of the scope
            }
                
            // Otherwise, fail immediately
            Assert.Fail(failMessage);

            // Correct exception type was thrown
            return (T)ex;
        }
    }

    public static async Task<T> ThrowsExactlyAsync<T>(Func<Task> action) where T : Exception
    {
        var expectedType = typeof(T);
        try
        {
            // Execute the async action that should throw
            await action();
            
            // If we get here, no exception was thrown - that's a test failure
            var failMessage = $"Expected exception of type {expectedType} but no exception was thrown.";
            
            // If we're in an assertion scope, record the failure and return a default instance
            // to allow the test to continue
            var scope = AssertionScope.Current;
            if (scope != null)
            {
                scope.RecordFailure(failMessage);
                return null!; // The test will fail at the end of the scope
            }
            
            // Otherwise, fail immediately
            Assert.Fail(failMessage);
            return null!; // Will never reach here
        }
        catch (Exception ex)
        {
            // Check if the correct exception type was thrown
            if (ex.GetType() == expectedType)
            {
                return (T)ex;
            }
            
            var failMessage = $"Expected exception of type {expectedType} but got {ex.GetType()}";
                
            // If we're in an assertion scope, record the failure and continue
            var scope = AssertionScope.Current;
            if (scope != null)
            {
                scope.RecordFailure(failMessage);
                return null!; // The test will fail at the end of the scope
            }
                
            // Otherwise, fail immediately
            Assert.Fail(failMessage);

            // Correct exception type was thrown
            return (T)ex;
        }
    }

    /// <summary>
    /// Asserts that two objects are equivalent by comparing their properties and fields, with optional exclusions.
    /// Works for both same-type and different-type objects, similar to FluentAssertions' BeEquivalentTo.
    /// </summary>
    public static void Equivalent(object? expected, object? actual,
        Action<EquivalencyOptions>? configureOptions = null)
    {
        var options = new EquivalencyOptions();
        configureOptions?.Invoke(options);

        var differences = new List<string>();
        var visited = new HashSet<(object, object)>(new ReferenceTupleEqualityComparer());
        ObjectComparer.Compare(expected, actual, options, string.Empty, differences, visited);

        if (differences.Count <= 0)
        {
            return;
        }

        var message = "Objects are not equivalent:" + Environment.NewLine +
                      string.Join(Environment.NewLine, differences);
        AssertInternal.Fail(message);
    }

    /// <summary>
    /// Asserts that two collections are equivalent by comparing their properties and fields, with optional exclusions.
    /// Works for both same-type and different-type collection elements.
    /// </summary>
    public static void EquivalentCollection<TExpected, TActual>(
        IEnumerable<TExpected> expected,
        IEnumerable<TActual> actual,
        Action<EquivalencyOptions>? configureElementOptions = null)
    {
        var expectedList = expected.ToList();
        var actualList = actual.ToList();

        var options = new EquivalencyOptions();
        configureElementOptions?.Invoke(options);

        if (expectedList.Count != actualList.Count)
        {
            AssertInternal.Fail($"Collection count mismatch: expected <{expectedList.Count}>, actual <{actualList.Count}>");
            return;
        }

        if (options.AllowUnordered)
        {
            CompareUnorderedCollections(expectedList, actualList, options);
        }
        else
        {
            CompareOrderedCollections(expectedList, actualList, options);
        }
    }

    private static void CompareOrderedCollections<TExpected, TActual>(
        List<TExpected> expectedList,
        List<TActual> actualList,
        EquivalencyOptions options)
    {
        var differences = new List<string>();
        
        for (var i = 0; i < expectedList.Count; i++)
        {
            var visited = new HashSet<(object, object)>(new ReferenceTupleEqualityComparer());
            var elementDifferences = new List<string>();
            
            ObjectComparer.Compare(expectedList[i], actualList[i], options, string.Empty, elementDifferences, visited);
            
            if (elementDifferences.Count > 0)
            {
                differences.Add($"Element at index {i} is not equivalent:" + Environment.NewLine +
                               string.Join(Environment.NewLine, elementDifferences));
            }
        }

        if (differences.Count > 0)
        {
            AssertInternal.Fail("Collections are not equivalent:" + Environment.NewLine +
                      string.Join(Environment.NewLine, differences));
        }
    }

    private static void CompareUnorderedCollections<TExpected, TActual>(
        List<TExpected> expectedList,
        List<TActual> actualList,
        EquivalencyOptions options)
    {
        var actualMatched = new HashSet<int>();
        var unmatchedIndices = new List<int>();

        for (var i = 0; i < expectedList.Count; i++)
        {
            if (!TryFindMatchingElement(expectedList[i], actualList, actualMatched, options))
            {
                unmatchedIndices.Add(i);
            }
        }

        if (unmatchedIndices.Count <= 0)
        {
            return;
        }
        
        var message = "Collections are not equivalent (unordered comparison):" + Environment.NewLine +
                      string.Join(Environment.NewLine, unmatchedIndices.Select(i => 
                          $"Element at index {i} in expected collection has no equivalent in actual collection"));
        AssertInternal.Fail(message);
    }

    private static bool TryFindMatchingElement<TExpected, TActual>(
        TExpected expectedElement,
        List<TActual> actualList,
        HashSet<int> actualMatched,
        EquivalencyOptions options)
    {
        for (var j = 0; j < actualList.Count; j++)
        {
            if (actualMatched.Contains(j))
            {
                continue;
            }

            if (!AreElementsEquivalent(expectedElement, actualList[j], options))
            {
                continue;
            }

            actualMatched.Add(j);
            return true;
        }

        return false;
    }

    private static bool AreElementsEquivalent<TExpected, TActual>(
        TExpected expectedElement,
        TActual actualElement,
        EquivalencyOptions options)
    {
        var differences = new List<string>();
        var visited = new HashSet<(object, object)>(new ReferenceTupleEqualityComparer());
        
        ObjectComparer.Compare(expectedElement, actualElement, options, string.Empty, differences, visited);
        return differences.Count == 0;
    }
}
