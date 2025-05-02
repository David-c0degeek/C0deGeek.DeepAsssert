# C0deGeek.DeepAssert

A lightweight, powerful assertion library for .NET that provides deep object comparison capabilities for unit testing. DeepAssert enables complex object equivalency checks with fine-grained control over comparison behavior, making it perfect for effective test-driven development.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![.NET 8.0](https://img.shields.io/badge/.NET-9.0-blue)

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Functionality](#core-functionality)
    - [Basic Assertions](#basic-assertions)
    - [Deep Object Equivalency](#deep-object-equivalency)
    - [Collection Assertions](#collection-assertions)
    - [Exclusion Options](#exclusion-options)
    - [Exception Testing](#exception-testing)
    - [AssertionScope](#assertionscope)
- [Advanced Usage](#advanced-usage)
    - [Handling Circular References](#handling-circular-references)
    - [Numeric Comparison](#numeric-comparison)
    - [Customizing Type Comparison](#customizing-type-comparison)
    - [Path Tracking](#path-tracking)
- [Integration](#integration)
    - [MSTest Integration](#mstest-integration)
    - [NUnit Integration](#nunit-integration)
    - [xUnit Integration](#xunit-integration)
- [Best Practices](#best-practices)
- [Examples](#examples)
- [Performance Considerations](#performance-considerations)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Deep object equality checking** with proper handling of complex object graphs
- **Circular reference detection** to prevent stack overflows
- **Fine-grained control** over what properties to include/exclude in comparison
- **Collection comparison** with ordered and unordered options
- **AssertionScope** to collect and report multiple assertion failures at once
- **Exception testing** utilities for verifying exception types and messages
- **Type conversion** handling for numeric types and other common conversions
- **Path tracking** to easily identify differences in complex objects
- **Simple integration** with major testing frameworks (MSTest, xUnit, NUnit)
- **Zero dependencies** outside of standard .NET libraries

## Installation

```bash
dotnet add package C0deGeek.DeepAssert
```

## Quick Start

```csharp
// Import the namespace
using C0deGeek.DeepAssert;

// Basic comparison
DeepAssert.Equivalent(expected, actual);

// With exclusions
DeepAssert.Equivalent(expected, actual, opt => 
    opt.Excluding<User>(u => u.LastModified));

// Collection comparison
DeepAssert.EquivalentCollection(expectedList, actualList);

// Exception testing
Exception ex = DeepAssert.ThrowsExactly<ArgumentException>(() => 
    DoSomethingThatThrows());

// Multiple assertions with AssertionScope
using (new AssertionScope())
{
    DeepAssert.Equivalent(user.Id, expectedId);
    DeepAssert.Equivalent(user.Name, expectedName);
    DeepAssert.Equivalent(user.Email, expectedEmail);
}
```

## Core Functionality

### Basic Assertions

```csharp
// Simple value comparison
DeepAssert.Equivalent(1, 1);
DeepAssert.Equivalent("test", "test");
DeepAssert.Equivalent(3.14, 3.14);

// Reference type comparison
var user1 = new User { Id = 1, Name = "Test" };
var user2 = new User { Id = 1, Name = "Test" };
DeepAssert.Equivalent(user1, user2);

// Null handling
DeepAssert.Equivalent(null, null);
```

### Deep Object Equivalency

```csharp
// Complex object comparison
var order1 = new Order
{
    Id = 1,
    Customer = new Customer { Id = 100, Name = "John" },
    Items = new List<OrderItem>
    {
        new() { ProductId = 1, Quantity = 2, Price = 10.0m },
        new() { ProductId = 2, Quantity = 1, Price = 15.0m }
    }
};

var order2 = new Order
{
    Id = 1,
    Customer = new Customer { Id = 100, Name = "John" },
    Items = new List<OrderItem>
    {
        new() { ProductId = 1, Quantity = 2, Price = 10.0m },
        new() { ProductId = 2, Quantity = 1, Price = 15.0m }
    }
};

DeepAssert.Equivalent(order1, order2);
```

### Collection Assertions

```csharp
// Ordered collection comparison
var list1 = new List<int> { 1, 2, 3 };
var list2 = new List<int> { 1, 2, 3 };
DeepAssert.EquivalentCollection(list1, list2, opt => opt.AllowUnordered = false);

// Unordered collection comparison
var list3 = new List<int> { 3, 1, 2 };
DeepAssert.EquivalentCollection(list1, list3); // AllowUnordered = true by default

// Collection of complex objects
var users1 = new List<User> { new() { Id = 1, Name = "Alice" }, new() { Id = 2, Name = "Bob" } };
var users2 = new List<User> { new() { Id = 1, Name = "Alice" }, new() { Id = 2, Name = "Bob" } };
DeepAssert.EquivalentCollection(users1, users2);
```

### Exclusion Options

```csharp
// Exclude single property
DeepAssert.Equivalent(user1, user2, opt => 
    opt.Excluding<User>(u => u.LastModified));

// Exclude multiple properties
DeepAssert.Equivalent(user1, user2, opt => 
    opt.Excluding<User>(u => u.LastModified)
       .Excluding<User>(u => u.CreatedBy));

// Exclude nested property
DeepAssert.Equivalent(order1, order2, opt => 
    opt.Excluding<Order>(o => o.Customer!.Address!));

// Exclude properties for collection elements
DeepAssert.EquivalentCollection(orders1, orders2, opt => 
    opt.Excluding<Order>(o => o.OrderDate));
```

### Exception Testing

```csharp
// Test for exact exception type
var exception = DeepAssert.ThrowsExactly<ArgumentNullException>(() => 
    SomeMethod(null));

// Check exception properties
Assert.AreEqual("paramName", exception.ParamName);

// Async exception testing
var asyncException = await DeepAssert.ThrowsExactlyAsync<TimeoutException>(() => 
    SomeAsyncMethod());
```

### AssertionScope

```csharp
// Multiple assertions collected together
using (new AssertionScope())
{
    DeepAssert.Equivalent(user.Id, expectedId);
    DeepAssert.Equivalent(user.Name, expectedName);
    DeepAssert.Equivalent(user.Email, expectedEmail);
    DeepAssert.Equivalent(user.IsActive, true);
}
// If any fail, all failures will be reported together

// Can be used with standard Assert methods too
using (new AssertionScope())
{
    Assert.AreEqual(expected.Id, actual.Id);
    Assert.IsTrue(actual.IsValid);
    DeepAssert.EquivalentCollection(expected.Items, actual.Items);
}

// Nested scopes
using (new AssertionScope()) // Outer scope
{
    DeepAssert.Equivalent(user.Id, expectedId);
    
    // Inner scope for address
    using (new AssertionScope())
    {
        DeepAssert.Equivalent(user.Address.Street, expectedAddress.Street);
        DeepAssert.Equivalent(user.Address.City, expectedAddress.City);
    }
}
```

## Advanced Usage

### Handling Circular References

```csharp
// Objects with circular references
public class Node
{
    public string Value { get; set; }
    public Node Parent { get; set; }
    public List<Node> Children { get; set; } = new();
}

var root1 = new Node { Value = "Root" };
var child1 = new Node { Value = "Child", Parent = root1 };
root1.Children.Add(child1);

var root2 = new Node { Value = "Root" };
var child2 = new Node { Value = "Child", Parent = root2 };
root2.Children.Add(child2);

// Automatic circular reference detection
DeepAssert.Equivalent(root1, root2);
```

### Numeric Comparison

```csharp
// Comparing different numeric types
DeepAssert.Equivalent(5, 5.0); // int vs double
DeepAssert.Equivalent(5.0m, 5); // decimal vs int
DeepAssert.Equivalent(5, 5L); // int vs long

// Floating point comparison
var a = 0.1 + 0.2; // 0.30000000000000004 in floating point
var b = 0.3;

// These values aren't exactly equal due to floating point precision
// But DeepAssert handles this intelligently
DeepAssert.Equivalent(a, b);
```

### Customizing Type Comparison

```csharp
// Create a custom equivalency options for specific test needs
var options = new EquivalencyOptions();
options.Excluding("LastModified");
options.Excluding("CreatedBy");
options.AllowUnordered = true;

// Use in multiple assertions
DeepAssert.Equivalent(user1, user2, opt => options);
DeepAssert.EquivalentCollection(userList1, userList2, opt => options);
```

### Path Tracking

```csharp
// When an assertion fails, the detailed error message includes the property path
var order1 = new Order 
{
    Id = 1,
    Customer = new Customer { Id = 100, Name = "John" },
    Items = new List<OrderItem>
    {
        new() { ProductId = 1, Quantity = 2, Price = 10.0m }
    }
};

var order2 = new Order
{
    Id = 1,
    Customer = new Customer { Id = 100, Name = "John" },
    Items = new List<OrderItem>
    {
        new() { ProductId = 1, Quantity = 3, Price = 10.0m } // Different quantity
    }
};

// This will fail with a message like:
// Items[0].Quantity: expected <2>, actual <3>
DeepAssert.Equivalent(order1, order2);
```

## Integration

### MSTest Integration

```csharp
// Works out of the box with MSTest
using Microsoft.VisualStudio.TestTools.UnitTesting;
using C0deGeek.DeepAssert;

[TestClass]
public class UserTests
{
    [TestMethod]
    public void User_Should_BeEquivalent()
    {
        var user1 = new User { Id = 1, Name = "Test" };
        var user2 = new User { Id = 1, Name = "Test" };
        
        DeepAssert.Equivalent(user1, user2);
    }
    
    [TestMethod]
    public void Users_WithDifferentCasing_ShouldBeEquivalent()
    {
        var user1 = new User { Name = "test" };
        var user2 = new User { Name = "TEST" };
        
        // Normal assertion would fail
        // But we can configure comparison
        DeepAssert.Equivalent(user1, user2, opt => 
            // Custom configuration would go here
        );
    }
}
```

### NUnit Integration

```csharp
// Works with NUnit as well
using NUnit.Framework;
using C0deGeek.DeepAssert;

[TestFixture]
public class OrderTests
{
    [Test]
    public void Orders_Should_BeEquivalent()
    {
        var order1 = CreateTestOrder();
        var order2 = CreateTestOrder();
        
        DeepAssert.Equivalent(order1, order2);
    }
    
    [Test]
    public void Orders_ShouldBeEquivalent_ExcludingTimestamps()
    {
        var order1 = CreateTestOrder();
        var order2 = CreateTestOrder();
        order2.CreatedAt = DateTime.Now.AddDays(1); // Different timestamp
        
        DeepAssert.Equivalent(order1, order2, opt => 
            opt.Excluding<Order>(o => o.CreatedAt));
    }
}
```

### xUnit Integration

```csharp
// Works with xUnit as well
using Xunit;
using C0deGeek.DeepAssert;

public class ProductTests
{
    [Fact]
    public void Products_Should_BeEquivalent()
    {
        var product1 = new Product { Id = 1, Name = "Test", Price = 9.99m };
        var product2 = new Product { Id = 1, Name = "Test", Price = 9.99m };
        
        DeepAssert.Equivalent(product1, product2);
    }
    
    [Fact]
    public void MultipleAssertions_Should_BeReported()
    {
        using (new AssertionScope())
        {
            DeepAssert.Equivalent(1, 1);
            DeepAssert.Equivalent("test", "test");
            DeepAssert.Equivalent(true, true);
        }
    }
}
```

## Best Practices

1. **Use AssertionScope for Multiple Checks**
   ```csharp
   using (new AssertionScope())
   {
       DeepAssert.Equivalent(user.Id, expectedId);
       DeepAssert.Equivalent(user.Name, expectedName);
       DeepAssert.Equivalent(user.Email, expectedEmail);
   }
   ```

2. **Be Specific About What to Exclude**
   ```csharp
   // Good: Specifically exclude the properties you don't care about
   DeepAssert.Equivalent(user1, user2, opt => 
       opt.Excluding<User>(u => u.LastModified));
   
   // Bad: Don't use overly broad exclusions that might hide real issues
   // opt.Excluding("Id"); // Could hide important differences
   ```

3. **Handle Collections Appropriately**
   ```csharp
   // Use ordered comparison when order matters
   DeepAssert.EquivalentCollection(sortedList1, sortedList2, 
       opt => opt.AllowUnordered = false);
   
   // Use unordered comparison when order doesn't matter
   DeepAssert.EquivalentCollection(userSet1, userSet2);
   ```

4. **Test for Exact Exception Types**
   ```csharp
   // Good: Test for exact exception type
   var ex = DeepAssert.ThrowsExactly<ArgumentNullException>(() => method(null));
   
   // Better: Also verify exception properties
   Assert.AreEqual("paramName", ex.ParamName);
   ```

5. **Structure Tests for Readability**
   ```csharp
   // Arrange
   var expected = new Order { /* ... */ };
   var actual = orderService.GetOrder(orderId);
   
   // Act & Assert
   using (new AssertionScope())
   {
       // High-level properties first
       DeepAssert.Equivalent(actual.Id, expected.Id);
       DeepAssert.Equivalent(actual.Status, expected.Status);
       
       // Then nested objects
       DeepAssert.Equivalent(actual.Customer, expected.Customer);
       
       // Then collections
       DeepAssert.EquivalentCollection(actual.Items, expected.Items);
   }
   ```

## Examples

### Domain and DTO Comparison

```csharp
public class UserEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class UserDto
{
    public string Name { get; set; }
    public string Email { get; set; }
}

[TestMethod]
public void UserDto_ShouldMatchEntity_ExcludingId()
{
    var entity = new UserEntity
    {
        Id = 1,
        Name = "John Doe",
        Email = "john@example.com",
        CreatedAt = DateTime.Now,
        LastLogin = DateTime.Now.AddDays(-1)
    };
    
    var dto = new UserDto
    {
        Name = "John Doe",
        Email = "john@example.com"
    };
    
    // Compare objects of different types, focusing only on shared properties
    DeepAssert.Equivalent(entity, dto, opt => 
        opt.Excluding<UserEntity>(e => e.Id)
           .Excluding<UserEntity>(e => e.CreatedAt)
           .Excluding<UserEntity>(e => e.LastLogin));
}
```

### Complex Object Graph

```csharp
public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; }
    public List<OrderItem> Items { get; set; }
    public Address ShippingAddress { get; set; }
    public Address BillingAddress { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
}

[TestMethod]
public void Order_AfterProcessing_ShouldBeInCorrectState()
{
    // Arrange
    var originalOrder = CreateTestOrder();
    var expectedProcessedOrder = CreateExpectedProcessedOrder();
    
    // Act
    var actualProcessedOrder = orderProcessor.Process(originalOrder);
    
    // Assert
    using (new AssertionScope())
    {
        // ID and customer info should remain the same
        DeepAssert.Equivalent(actualProcessedOrder.Id, expectedProcessedOrder.Id);
        DeepAssert.Equivalent(actualProcessedOrder.Customer, expectedProcessedOrder.Customer);
        
        // Status should be updated
        DeepAssert.Equivalent(actualProcessedOrder.Status, OrderStatus.Processed);
        
        // Items should remain the same but may be reordered
        DeepAssert.EquivalentCollection(
            actualProcessedOrder.Items, 
            expectedProcessedOrder.Items);
        
        // Shipping/billing addresses should be unchanged
        DeepAssert.Equivalent(
            actualProcessedOrder.ShippingAddress, 
            expectedProcessedOrder.ShippingAddress);
            
        DeepAssert.Equivalent(
            actualProcessedOrder.BillingAddress, 
            expectedProcessedOrder.BillingAddress);
    }
}
```

### Testing Multiple Criteria on Collections

```csharp
[TestMethod]
public void FilteredProducts_ShouldMatchCriteria()
{
    // Arrange
    var products = GetSampleProducts();
    var criteria = new FilterCriteria
    {
        MinPrice = 10.0m,
        MaxPrice = 50.0m,
        Categories = new[] { "Electronics", "Accessories" }
    };
    
    // Act
    var filteredProducts = productService.Filter(products, criteria);
    
    // Assert
    using (new AssertionScope())
    {
        // Check each product meets the criteria
        foreach (var product in filteredProducts)
        {
            Assert.IsTrue(product.Price >= criteria.MinPrice,
                $"Product {product.Name} price {product.Price} is below minimum {criteria.MinPrice}");
                
            Assert.IsTrue(product.Price <= criteria.MaxPrice,
                $"Product {product.Name} price {product.Price} is above maximum {criteria.MaxPrice}");
                
            Assert.IsTrue(criteria.Categories.Contains(product.Category),
                $"Product {product.Name} category {product.Category} is not in allowed categories");
        }
        
        // Check we didn't miss any products that should have been included
        var shouldBeIncluded = products.Where(p => 
            p.Price >= criteria.MinPrice &&
            p.Price <= criteria.MaxPrice &&
            criteria.Categories.Contains(p.Category)).ToList();
            
        DeepAssert.EquivalentCollection(filteredProducts, shouldBeIncluded);
    }
}
```

## Performance Considerations

1. **Reuse EquivalencyOptions**

   Creating configuration objects repeatedly can be expensive. Consider reusing them:

   ```csharp
   // Create once
   private static readonly EquivalencyOptions StandardOptions = new();
   
   static MyTests()
   {
       // Configure once
       StandardOptions.Excluding("CreatedAt");
       StandardOptions.Excluding("LastModified");
   }
   
   // Use in tests
   [TestMethod]
   public void Test1()
   {
       DeepAssert.Equivalent(obj1, obj2, opt => StandardOptions);
   }
   ```

2. **Limit Comparison Depth for Very Complex Objects**

   For extremely complex objects, you might want to constrain comparison depth:

   ```csharp
   // Create custom comparison options for very deep objects
   var options = new EquivalencyOptions();
   options.MaxDepth = 5; // Limit depth to prevent performance issues
   
   DeepAssert.Equivalent(complexObj1, complexObj2, opt => options);
   ```

3. **Use Fast Comparisons for Collections When Possible**

   ```csharp
   // For simple value types, sorted arrays can be compared efficiently
   Array.Sort(array1);
   Array.Sort(array2);
   
   // Then use EquivalentCollection with ordered comparison
   DeepAssert.EquivalentCollection(array1, array2, opt => opt.AllowUnordered = false);
   ```

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Commit your changes: `git commit -m 'Add some feature'`
4. Push to the branch: `git push origin feature/your-feature-name`
5. Open a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.