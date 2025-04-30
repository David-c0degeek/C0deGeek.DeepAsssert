namespace C0deGeek.DeepAssert.Tests;

[TestClass]
public class DeepAssertTests
{
    private class TestEntity
    {
        public int Id { get; init; }
        public string? Name { get; init; }
        public double Value { get; set; }
        public List<string>? Tags { get; set; }
        public TestEntity? Nested { get; set; }
        public int? A { get; init; }
        public int? B { get; init; }
        public List<int>? Numbers { get; init; }
        private string? PrivateField { get; set; }

        public TestEntity() { }

        public TestEntity(int id, string? name, string? privateField = null)
        {
            Id = id;
            Name = name;
            PrivateField = privateField;
        }
    }

    // Simple record for collection comparison tests
    private record ItemRecord(int Id, string Name, double Value);

    private record ItemRecordDto(string Name, double Value);

    [TestMethod]
    public void AreEquivalent_DomainAndDtoExcludingId_NoDifferences()
    {
        var domainItem = new ItemRecord(1, "Test", 1.0);
        var dtoItem = new ItemRecordDto("Test", 1.0);
            
        DeepAssert.Equivalent(domainItem, dtoItem, opt => opt.Excluding<ItemRecord>(x => x.Id));
    }
        
    [TestMethod]
    public void AreEquivalent_DomainAndDtoIncludingId_Different()
    {
        var domainItem = new ItemRecord(1, "Test", 1.0);
        var dtoItem = new ItemRecordDto("Test", 1.0);
            
        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.Equivalent(domainItem, dtoItem));
    }

    [TestMethod]
    public void AreEquivalent_SimpleObjects_NoDifferences()
    {
        var a = new TestEntity
        {
            Id = 1,
            Name = "Test",
            Value = 3.14,
            Tags = ["one", "two"],
            Nested = new TestEntity { Name = "X", A = 5 }
        };
        var b = new TestEntity
        {
            Id = 1,
            Name = "Test",
            Value = 3.14,
            Tags = ["one", "two"],
            Nested = new TestEntity { Name = "X", A = 5 }
        };

        DeepAssert.Equivalent(a, b);
    }

    [TestMethod]
    public void AreEquivalent_DifferentSimpleProperty_Fails()
    {
        var a = new TestEntity
        {
            Id = 1,
            Name = "Test",
            Value = 3.14,
            Tags = [],
            Nested = new TestEntity()
        };
        var b = new TestEntity
        {
            Id = 2,
            Name = "Test",
            Value = 3.14,
            Tags = [],
            Nested = new TestEntity()
        };

        DeepAssert.ThrowsExactly<AssertFailedException>(() => DeepAssert.Equivalent(a, b));
    }

    [TestMethod]
    public void AreEquivalent_ExcludeProperty_Ignored()
    {
        var a = new TestEntity
        {
            Id = 1,
            Name = "A",
            Value = 1.0,
            Tags = ["x"],
            Nested = new TestEntity { Name = "A", A = 1 }
        };
        var b = new TestEntity
        {
            Id = 2,
            Name = "A",
            Value = 1.0,
            Tags = ["x"],
            Nested = new TestEntity { Name = "A", A = 1 }
        };

        DeepAssert.Equivalent(a, b, opts => opts.Excluding<TestEntity>(x => x.Id));
    }

    [TestMethod]
    public void AreEquivalent_UnorderedCollections_Works()
    {
        var a = new[] { 1, 2, 3 };
        var b = new[] { 3, 1, 2 };

        DeepAssert.Equivalent(a, b);
    }

    [TestMethod]
    public void AreEquivalent_UnorderedCollections_FailsOnMissing()
    {
        var a = new[] { 1, 2, 4 };
        var b = new[] { 3, 1, 2 };

        DeepAssert.ThrowsExactly<AssertFailedException>(() => DeepAssert.Equivalent(a, b));
    }

    [TestMethod]
    public void AreEquivalent_WildcardExclusion_Works()
    {
        var a = new TestEntity { Nested = new TestEntity { Name = "X", A = 1 } };
        var b = new TestEntity { Nested = new TestEntity { Name = "Y", A = 2 } };

        DeepAssert.Equivalent(a, b, opts =>
            opts
                .Excluding<TestEntity>(x => x.Nested!.Name!)
                .Excluding<TestEntity>(x => x.Nested!.A!));
    }

    [TestMethod]
    public void PrimitiveValues_AreEquivalent()
    {
        DeepAssert.Equivalent(5, 5);
        DeepAssert.Equivalent("hello", "hello");
        DeepAssert.Equivalent(DateTime.Parse("2025-04-22"), DateTime.Parse("2025-04-22"));
        DeepAssert.Equivalent(Guid.Empty, Guid.Empty);
        DeepAssert.Equivalent(3.14m, 3.14m);
        DeepAssert.Equivalent(TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    [TestMethod]
    public void PrimitiveValues_NotEquivalent_Throws()
    {
        DeepAssert.ThrowsExactly<AssertFailedException>(() => DeepAssert.Equivalent(5, 6));
    }

    [TestMethod]
    public void SimpleClass_PropertiesAndPrivateFields_Equivalent()
    {
        var a = new TestEntity(1, "one");
        var b = new TestEntity(1, "one");
        DeepAssert.Equivalent(a, b);
    }

    [TestMethod]
    public void SimpleClass_DifferentPrivateField_Throws()
    {
        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.Equivalent(
                new TestEntity(1, "one"),
                new TestEntity(1, "two")
            )
        );
    }

    [TestMethod]
    public void Excluding_PublicProperty_ExcludesFromComparison()
    {
        var a = new TestEntity { A = 1, B = 2 };
        var b = new TestEntity { A = 1, B = 99 };
        // Exclude B should ignore the difference
        DeepAssert.Equivalent(a, b, opt => opt.Excluding<TestEntity>(x => x.B!));
    }

    [TestMethod]
    public void NestedObjects_AreEquivalent()
    {
        var a = new TestEntity { Name = "root", Nested = new TestEntity(2, "two") };
        var b = new TestEntity { Name = "root", Nested = new TestEntity(2, "two") };
        DeepAssert.Equivalent(a, b);
    }

    [TestMethod]
    public void NestedObjects_NotEquivalent_Throws()
    {
        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.Equivalent(
                new TestEntity { Name = "root", Nested = new TestEntity(2, "two") },
                new TestEntity { Name = "root", Nested = new TestEntity(2, "TWO") }
            )
        );
    }

    [TestMethod]
    public void Enumerable_PrimitiveCollections_Equivalent()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2, 3 };
        DeepAssert.Equivalent(list1, list2);
    }

    [TestMethod]
    public void Enumerable_CountMismatch_Throws()
    {
        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.Equivalent(
                new List<int> { 1, 2 },
                new List<int> { 1, 2, 3 }
            )
        );
    }

    [TestMethod]
    public void Enumerable_ObjectCollections_Equivalent()
    {
        var list1 = new List<TestEntity> { new(1, "one"), new(2, "two") };
        var list2 = new List<TestEntity> { new(1, "one"), new(2, "two") };
        DeepAssert.Equivalent(list1, list2);
    }

    [TestMethod]
    public void Enumerable_ObjectCollections_NotEquivalent_Throws()
    {
        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.Equivalent(
                new List<TestEntity> { new TestEntity(1, "one"), new TestEntity(2, "two") },
                new List<TestEntity> { new(1, "one"), new(2, "TWO") }
            )
        );
    }

    [TestMethod]
    public void CyclicReferences_AreHandled_WithoutStackOverflow()
    {
        var node1 = new TestEntity { Id = 1 };
        node1.Nested = node1;
        var node2 = new TestEntity { Id = 1 };
        node2.Nested = node2;
        DeepAssert.Equivalent(node1, node2);
    }

    [TestMethod]
    public void CyclicReferences_NotEquivalent_Throws()
    {
        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.Equivalent(
                new TestEntity { Id = 1, Nested = new TestEntity { Id = 1 } },
                new TestEntity { Id = 2, Nested = new TestEntity { Id = 2 } }
            )
        );
    }

    [TestMethod]
    public void NullAndNonNullValue_Tests()
    {
        string? s1 = null;
        string? s2 = null;
        DeepAssert.Equivalent(s1, s2);

        DeepAssert.ThrowsExactly<AssertFailedException>(() => DeepAssert.Equivalent(null, "non-null"));
        DeepAssert.ThrowsExactly<AssertFailedException>(() => DeepAssert.Equivalent("non-null", null));
    }

    [TestMethod]
    public void ComplexNested_DifferenceInChild_Throws()
    {
        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.Equivalent(
                new TestEntity { Nested = new TestEntity { A = 1, B = 2 } },
                new TestEntity { Nested = new TestEntity { A = 1, B = 99 } }
            )
        );
    }

    [TestMethod]
    public void Excluding_ChildProperty_ExcludesNestedProperty()
    {
        var a = new TestEntity { Nested = new TestEntity { A = 1, B = 2 } };
        var b = new TestEntity { Nested = new TestEntity { A = 1, B = 99 } };
        // Exclude Nested.B to ignore only B differences
        DeepAssert.Equivalent(a, b, opt => opt.Excluding<TestEntity>(x => x.Nested!.B!));
    }

    [TestMethod]
    public void Excluding_ChildObject_ExcludesEntireBranch()
    {
        var a = new TestEntity { Nested = new TestEntity { A = 1, B = 2 } };
        var b = new TestEntity { Nested = new TestEntity { A = 9, B = 99 } };
        // Exclude the whole Nested branch
        DeepAssert.Equivalent(a, b, opt => opt.Excluding<TestEntity>(x => x.Nested!));
    }

    [TestMethod]
    public void Excluding_CollectionProperty_ExcludesEntireCollection()
    {
        var a = new TestEntity { Numbers = [1, 2, 3] };
        var b = new TestEntity { Numbers = [4, 5, 6] };
        DeepAssert.Equivalent(a, b, opt => opt.Excluding<TestEntity>(x => x.Numbers!));
    }

    [TestMethod]
    public void ClassA_DifferenceInNestedBId_Throws()
    {
        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.Equivalent(
                new TestEntity { Id = 1, Name = "A", Nested = new TestEntity { Id = 10, Name = "B" } },
                new TestEntity { Id = 1, Name = "A", Nested = new TestEntity { Id = 20, Name = "B" } }
            )
        );
    }

    [TestMethod]
    public void Excluding_NestedBId_ExcludesDifference()
    {
        var a = new TestEntity { Id = 1, Name = "A", Nested = new TestEntity { Id = 10, Name = "B" } };
        var b = new TestEntity { Id = 1, Name = "A", Nested = new TestEntity { Id = 20, Name = "B" } };
        DeepAssert.Equivalent(a, b, opt => opt.Excluding<TestEntity>(x => x.Nested!.Id));
    }

    [TestMethod]
    public void Excluding_NullValue_Ignored()
    {
        var a = new TestEntity { Id = 1, Name = null };
        var b = new TestEntity { Id = 1, Name = "NonNull" };

        // Exclude Name, so the difference in its value (null vs "NonNull") is ignored
        DeepAssert.Equivalent(a, b, opt => opt.Excluding<TestEntity>(x => x.Name!));
    }

    [TestMethod]
    public void EquivalentCollection_DomainAndDtoExcludingId_Equivalent()
    {
        var domainItems = new List<ItemRecord>
        {
            new(1, "Test1", 1.0),
            new(2, "Test2", 2.0)
        };
            
        var dtoItems = new List<ItemRecordDto>
        {
            new("Test1", 1.0),
            new("Test2", 2.0)
        };
            
        DeepAssert.EquivalentCollection(
            domainItems,
            dtoItems,
            opt => opt.Excluding<ItemRecord>(x => x.Id)
        );
    }
        
    [TestMethod]
    public void EquivalentCollection_DomainAndDtoIncludingId_Different()
    {
        var domainItems = new List<ItemRecord>
        {
            new(1, "Test1", 1.0),
            new(2, "Test2", 2.0)
        };
            
        var dtoItems = new List<ItemRecordDto>
        {
            new("Test1", 1.0),
            new("Test2", 2.0)
        };
            
        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.EquivalentCollection(domainItems, dtoItems)
        );
    }
        
    [TestMethod]
    public void AreEquivalent_DifferentValueInRecord_Different()
    {
        var record1 = new ItemRecord(1, "Test", 1.0);
        var record2 = new ItemRecord(1, "Different", 1.0);
            
        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.Equivalent(record1, record2)
        );
    }
        
    [TestMethod]
    public void AreEquivalent_SameValuesDifferentIds_EquivalentWithExclusion()
    {
        var record1 = new ItemRecord(1, "Test", 1.0);
        var record2 = new ItemRecord(2, "Test", 1.0);
            
        DeepAssert.Equivalent(record1, record2, 
            opt => opt.Excluding<ItemRecord>(x => x.Id));
    }
        
    #region EquivalentCollection Tests

    [TestMethod]
    public void EquivalentCollection_SimpleItems_AreEquivalent()
    {
        var expected = new List<ItemRecord> { new(1, "One", 1.1), new(2, "Two", 2.2) };

        var actual = new List<ItemRecord> { new(1, "One", 1.1), new(2, "Two", 2.2) };

        DeepAssert.EquivalentCollection(expected, actual);
    }

    [TestMethod]
    public void EquivalentCollection_UnorderedItems_AreEquivalent()
    {
        var expected = new List<ItemRecord> { new(1, "One", 1.1), new(2, "Two", 2.2) };

        var actual = new List<ItemRecord> { new(2, "Two", 2.2), new(1, "One", 1.1) };

        DeepAssert.EquivalentCollection(expected, actual);
    }

    [TestMethod]
    public void EquivalentCollection_OrderedRequirement_DetectsDifference()
    {
        var expected = new List<ItemRecord> { new(1, "One", 1.1), new(2, "Two", 2.2) };

        var actual = new List<ItemRecord> { new(2, "Two", 2.2), new(1, "One", 1.1) };

        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.EquivalentCollection(expected, actual, opt => opt.AllowUnordered = false));
    }

    [TestMethod]
    public void EquivalentCollection_DifferentItems_FailsAssertion()
    {
        var expected = new List<ItemRecord> { new(1, "One", 1.1), new(2, "Two", 2.2) };

        var actual = new List<ItemRecord> { new(1, "One", 1.1), new(2, "Different", 2.2) };

        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.EquivalentCollection(expected, actual));
    }

    [TestMethod]
    public void EquivalentCollection_ExcludingProperty_IgnoresDifference()
    {
        var expected = new List<ItemRecord> { new(1, "One", 1.1), new(2, "Two", 2.2) };

        var actual = new List<ItemRecord>
        {
            new(1, "One", 1.1), new(2, "Different", 2.2) // Name differs
        };

        // Should pass when we exclude the Name property
        DeepAssert.EquivalentCollection(expected, actual,
            opt => opt.Excluding<ItemRecord>(x => x.Name));
    }

    [TestMethod]
    public void EquivalentCollection_ExcludingMultipleProperties_IgnoresMultipleDifferences()
    {
        var expected = new List<ItemRecord> { new(1, "One", 1.1), new(2, "Two", 2.2) };

        var actual = new List<ItemRecord>
        {
            new(10, "Different1", 1.1), // Id and Name differ
            new(20, "Different2", 2.2) // Id and Name differ
        };

        // Should pass when we exclude both Id and Name
        DeepAssert.EquivalentCollection(expected, actual,
            opt => opt.Excluding<ItemRecord>(x => x.Id).Excluding<ItemRecord>(x => x.Name));
    }

    [TestMethod]
    public void EquivalentCollection_NestedCollections_AreEquivalent()
    {
        var expected = new List<TestEntity>
        {
            new() { Id = 1, Numbers = [1, 2, 3] }, new() { Id = 2, Numbers = [4, 5, 6] }
        };

        var actual = new List<TestEntity>
        {
            new() { Id = 1, Numbers = [1, 2, 3] }, new() { Id = 2, Numbers = [4, 5, 6] }
        };

        DeepAssert.EquivalentCollection(expected, actual);
    }

    [TestMethod]
    public void EquivalentCollection_NestedCollectionsDifferent_FailsAssertion()
    {
        var expected = new List<TestEntity>
        {
            new() { Id = 1, Numbers = [1, 2, 3] }, new() { Id = 2, Numbers = [4, 5, 6] }
        };

        var actual = new List<TestEntity>
        {
            new() { Id = 1, Numbers = [1, 2, 3] }, new() { Id = 2, Numbers = [4, 5, 7] } // Last number differs
        };

        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.EquivalentCollection(expected, actual));
    }

    [TestMethod]
    public void EquivalentCollection_ExcludingNestedCollection_IgnoresDifference()
    {
        var expected = new List<TestEntity>
        {
            new() { Id = 1, Numbers = [1, 2, 3] }, new() { Id = 2, Numbers = [4, 5, 6] }
        };

        var actual = new List<TestEntity>
        {
            new() { Id = 1, Numbers = [1, 2, 3] },
            new() { Id = 2, Numbers = [9, 9, 9] } // Numbers completely different
        };

        // Should pass when we exclude the Numbers property
        DeepAssert.EquivalentCollection(expected, actual,
            opt => opt.Excluding<TestEntity>(x => x.Numbers!));
    }

    [TestMethod]
    public void EquivalentCollection_NestedEntities_AreEquivalent()
    {
        var expected = new List<TestEntity>
        {
            new() { Id = 1, Nested = new() { Id = 10, Name = "Inner1" } },
            new() { Id = 2, Nested = new() { Id = 20, Name = "Inner2" } }
        };

        var actual = new List<TestEntity>
        {
            new() { Id = 1, Nested = new() { Id = 10, Name = "Inner1" } },
            new() { Id = 2, Nested = new() { Id = 20, Name = "Inner2" } }
        };

        DeepAssert.EquivalentCollection(expected, actual);
    }

    [TestMethod]
    public void EquivalentCollection_NestedEntitiesDifferent_FailsAssertion()
    {
        var expected = new List<TestEntity>
        {
            new() { Id = 1, Nested = new() { Id = 10, Name = "Inner1" } },
            new() { Id = 2, Nested = new() { Id = 20, Name = "Inner2" } }
        };

        var actual = new List<TestEntity>
        {
            new() { Id = 1, Nested = new() { Id = 10, Name = "Inner1" } },
            new() { Id = 2, Nested = new() { Id = 20, Name = "Changed" } } // Nested Name differs
        };

        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.EquivalentCollection(expected, actual));
    }

    [TestMethod]
    public void EquivalentCollection_ExcludingNestedProperty_IgnoresDifference()
    {
        var expected = new List<TestEntity>
        {
            new() { Id = 1, Nested = new() { Id = 10, Name = "Inner1" } },
            new() { Id = 2, Nested = new() { Id = 20, Name = "Inner2" } }
        };

        var actual = new List<TestEntity>
        {
            new() { Id = 1, Nested = new() { Id = 10, Name = "Inner1" } },
            new() { Id = 2, Nested = new() { Id = 20, Name = "Changed" } } // Nested Name differs
        };

        // Should pass when we exclude the Nested.Name property
        DeepAssert.EquivalentCollection(expected, actual,
            opt => opt.Excluding<TestEntity>(x => x.Nested!.Name!));
    }

    [TestMethod]
    public void EquivalentCollection_CountMismatch_FailsAssertion()
    {
        var expected = new List<ItemRecord> { new(1, "One", 1.1), new(2, "Two", 2.2), new(3, "Three", 3.3) };

        var actual = new List<ItemRecord>
        {
            new(1, "One", 1.1), new(2, "Two", 2.2) // Missing the third item
        };

        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.EquivalentCollection(expected, actual));
    }

    [TestMethod]
    public void EquivalentCollection_EmptyCollections_AreEquivalent()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        var expected = new List<ItemRecord>();
        // ReSharper disable once CollectionNeverUpdated.Local
        var actual = new List<ItemRecord>();

        DeepAssert.EquivalentCollection(expected, actual);
    }

    [TestMethod]
    public void EquivalentCollection_NullValues_HandledCorrectly()
    {
        var expected = new List<TestEntity> { new() { Id = 1, Name = null }, new() { Id = 2, Name = "Two" } };

        var actual = new List<TestEntity> { new() { Id = 1, Name = null }, new() { Id = 2, Name = "Two" } };

        DeepAssert.EquivalentCollection(expected, actual);
    }

    [TestMethod]
    public void EquivalentCollection_NullVsNonNull_FailsAssertion()
    {
        var expected = new List<TestEntity> { new() { Id = 1, Name = null }, new() { Id = 2, Name = "Two" } };

        var actual = new List<TestEntity>
        {
            new() { Id = 1, Name = "One" }, // null vs "One"
            new() { Id = 2, Name = "Two" }
        };

        DeepAssert.ThrowsExactly<AssertFailedException>(() =>
            DeepAssert.EquivalentCollection(expected, actual));
    }

    [TestMethod]
    public void EquivalentCollection_ExcludingNullProperty_IgnoresDifference()
    {
        var expected = new List<TestEntity> { new() { Id = 1, Name = null }, new() { Id = 2, Name = "Two" } };

        var actual = new List<TestEntity>
        {
            new() { Id = 1, Name = "One" }, // null vs "One"
            new() { Id = 2, Name = "Two" }
        };

        // Should pass when we exclude the Name property
        DeepAssert.EquivalentCollection(expected, actual,
            opt => opt.Excluding<TestEntity>(x => x.Name!));
    }

    [TestMethod]
    public void EquivalentCollection_ComplexObjectGraph_AreEquivalent()
    {
        var expected = new List<TestEntity>
        {
            new()
            {
                Id = 1,
                Name = "First",
                Nested = new() { Id = 10, Name = "Nested1", Numbers = [5, 6, 7] },
                Numbers = [1, 2, 3]
            },
            new()
            {
                Id = 2,
                Name = "Second",
                Nested = new() { Id = 20, Name = "Nested2", Numbers = [8, 9, 10] },
                Numbers = [4, 5, 6]
            }
        };

        var actual = new List<TestEntity>
        {
            new()
            {
                Id = 1,
                Name = "First",
                Nested = new() { Id = 10, Name = "Nested1", Numbers = [5, 6, 7] },
                Numbers = [1, 2, 3]
            },
            new()
            {
                Id = 2,
                Name = "Second",
                Nested = new() { Id = 20, Name = "Nested2", Numbers = [8, 9, 10] },
                Numbers = [4, 5, 6]
            }
        };

        DeepAssert.EquivalentCollection(expected, actual);
    }

    #endregion
}