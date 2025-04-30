namespace C0deGeek.DeepAssert.Tests;

[TestClass]
public class AssertionScopeTests
{
    private class TestEntity
    {
        public int Id { get; init; }
        public string? Name { get; init; }
        public double Value { get; init; }
    }

    [TestMethod]
    public void SingleAssertion_NoScope_Works()
    {
        // Normal assertion outside a scope should work
        DeepAssert.Equivalent(1, 1);
    }

    [TestMethod]
    public void SingleAssertion_WithScope_Works()
    {
        // Assertion inside a scope should work
        using (new AssertionScope())
        {
            DeepAssert.Equivalent(1, 1);
        }
    }

    [TestMethod]
    public void MultipleSuccessfulAssertions_WithScope_AllSucceed()
    {
        using (new AssertionScope())
        {
            DeepAssert.Equivalent(1, 1);
            DeepAssert.Equivalent("test", "test");
            DeepAssert.Equivalent(3.14, 3.14);
        }
    }

    [TestMethod]
    public void SingleFailingAssertion_WithScope_ThrowsException()
    {
        var exception = Assert.ThrowsException<AssertFailedException>(() =>
        {
            using (new AssertionScope())
            {
                DeepAssert.Equivalent(1, 2); // This should fail
            }
        });

        // The exact format is "Objects are not equivalent:\n<expected> 1, <actual> 2"
        // So we check for each part separately
        StringAssert.Contains(exception.Message, "Objects are not equivalent");
        StringAssert.Contains(exception.Message, "<expected> 1");
        StringAssert.Contains(exception.Message, "<actual> 2");
    }

    [TestMethod]
    public void MultipleFailingAssertions_WithScope_CollectsAllFailures()
    {
        var exception = Assert.ThrowsException<AssertFailedException>(() =>
        {
            using (new AssertionScope())
            {
                DeepAssert.Equivalent(1, 2); // Fail
                DeepAssert.Equivalent("a", "b"); // Fail
                DeepAssert.Equivalent(true, true); // Success
            }
        });

        // Should contain info about both failures
        StringAssert.Contains(exception.Message, "2 assertion(s) failed");
        StringAssert.Contains(exception.Message, "<expected> 1");
        StringAssert.Contains(exception.Message, "<actual> 2");
        StringAssert.Contains(exception.Message, "<expected> a");
        StringAssert.Contains(exception.Message, "<actual> b");
    }

    [TestMethod]
    public void NestedScopes_InnerScopeFailures_DoNotAffectOuterScope()
    {
        using (new AssertionScope()) // Outer scope
        {
            DeepAssert.Equivalent(1, 1); // Success

            // Inner scope with failures
            Assert.ThrowsException<AssertFailedException>(() =>
            {
                using (new AssertionScope()) // Inner scope
                {
                    DeepAssert.Equivalent(2, 3); // Fail
                }
            });

            // Continue in outer scope
            DeepAssert.Equivalent("test", "test"); // Success
        }
    }

    [TestMethod]
    public void ComplexObjects_MultipleFailures_ReportsAll()
    {
        var obj1 = new TestEntity { Id = 1, Name = "Test1", Value = 1.0 };
        var obj2 = new TestEntity { Id = 2, Name = "Test2", Value = 3.0 };

        var exception = Assert.ThrowsException<AssertFailedException>(() =>
        {
            using (new AssertionScope())
            {
                DeepAssert.Equivalent(obj1, obj2);
            }
        });

        // Should report all three property differences
        StringAssert.Contains(exception.Message, "Id: expected <1>, actual <2>");
        StringAssert.Contains(exception.Message, "Name: expected <Test1>, actual <Test2>");
        StringAssert.Contains(exception.Message, "Value: expected <1>, actual <3>");
    }
        
    [TestMethod]
    public void ThrowsExactly_WorksWithScope()
    {
        using (new AssertionScope())
        {
            // This should succeed
            DeepAssert.ThrowsExactly<ArgumentException>(() => 
                throw new ArgumentException("Test"));
                
            // Add a successful assertion
            DeepAssert.Equivalent(1, 1);
        }
    }
        
    [TestMethod]
    public void ThrowsExactly_FailsCorrectly_WithScope()
    {
        var exception = Assert.ThrowsException<AssertFailedException>(() =>
        {
            using (new AssertionScope())
            {
                // This should fail - wrong exception type
                DeepAssert.ThrowsExactly<ArgumentException>(() => 
                    throw new InvalidOperationException("Test"));
            }
        });
            
        StringAssert.Contains(exception.Message, "Expected exception of type System.ArgumentException");
        StringAssert.Contains(exception.Message, "System.InvalidOperationException");
    }
        
    [TestMethod]
    public void EquivalentCollection_WorksWithScope()
    {
        using (new AssertionScope())
        {
            DeepAssert.EquivalentCollection(
                new[] { 1, 2, 3 },
                new[] { 1, 2, 3 }
            );
                
            // Add a successful assertion
            DeepAssert.Equivalent(1, 1);
        }
    }
        
    [TestMethod]
    public void EquivalentCollection_FailsCorrectly_WithScope()
    {
        var exception = Assert.ThrowsException<AssertFailedException>(() =>
        {
            using (new AssertionScope())
            {
                DeepAssert.EquivalentCollection(
                    new[] { 1, 2, 3 },
                    new[] { 1, 2, 4 } // Different
                );
            }
        });
            
        StringAssert.Contains(exception.Message, "Collections are not equivalent");
    }
        
    [TestMethod]
    public void MultipleAssertionTypes_AllCollected()
    {
        var exception = Assert.ThrowsException<AssertFailedException>(() =>
        {
            using (new AssertionScope())
            {
                // Mix of different assertion types
                DeepAssert.Equivalent(1, 2); // Fail
                    
                DeepAssert.ThrowsExactly<ArgumentException>(() => 
                    throw new InvalidOperationException("Test")); // Fail
                    
                DeepAssert.EquivalentCollection(
                    new[] { 1, 2 },
                    new[] { 3, 4 } // Different
                ); // Fail
            }
        });
            
        // Check for number of failures
        StringAssert.Contains(exception.Message, "3 assertion(s) failed");
            
        // Check that each type of assertion is represented
        StringAssert.Contains(exception.Message, "<expected> 1");
        StringAssert.Contains(exception.Message, "Expected exception of type System.ArgumentException");
        StringAssert.Contains(exception.Message, "Collections are not equivalent");
    }
}