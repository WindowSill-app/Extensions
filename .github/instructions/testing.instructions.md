---
description: 'Testing guidelines for xUnit, FluentAssertions, and Moq'
applyTo: '**/*Tests*/**/*.cs, **/*.Tests.cs, **/tests/**/*.cs'
---

# Testing Guidelines

## Framework Stack

- **Test Framework**: xUnit
- **Assertions**: FluentAssertions
- **Mocking**: Moq
- **Code Coverage**: Coverlet

## Test Project Structure

```
tests/
├── WindowSill.Tests/           # Unit tests for WinUI app
├── WindowSillWebTests/         # Unit/Integration tests for web
└── WindowSill.API.Tests/       # API library tests
```

## Naming Conventions

### Test Classes

- Name: `{ClassUnderTest}Tests`
- Location: Mirror the source folder structure

```csharp
// Source: WindowSill/Core/Services/UserService.cs
// Test:   WindowSill.Tests/Core/Services/UserServiceTests.cs
public class UserServiceTests
```

### Test Methods

Use descriptive names that explain the scenario:

```csharp
// Pattern: Method_Scenario_ExpectedResult
[Fact]
public void GetUser_WithValidId_ReturnsUser()

[Fact]
public async Task CreateUser_WhenEmailExists_ThrowsDuplicateException()

[Fact]
public void CalculateDiscount_ForPremiumUser_AppliesTwentyPercent()
```

## Test Structure

### No Arrange/Act/Assert Comments

Do NOT include "Arrange", "Act", or "Assert" comments. The structure should be self-evident:

```csharp
// ✅ Good
[Fact]
public void GetUser_WithValidId_ReturnsUser()
{
    var repository = new Mock<IUserRepository>();
    var user = new User { Id = 1, Name = "John" };
    repository.Setup(r => r.GetById(1)).Returns(user);
    var service = new UserService(repository.Object);

    var result = service.GetUser(1);

    result.Should().NotBeNull();
    result.Name.Should().Be("John");
}

// ❌ Bad
[Fact]
public void GetUser_WithValidId_ReturnsUser()
{
    // Arrange
    var repository = new Mock<IUserRepository>();
    // ... etc
}
```

## FluentAssertions

### Basic Assertions

```csharp
result.Should().NotBeNull();
result.Should().Be(expected);
result.Should().BeEquivalentTo(expected);
count.Should().BeGreaterThan(0);
collection.Should().HaveCount(3);
collection.Should().Contain(item);
text.Should().StartWith("Hello");
text.Should().Contain("world");
```

### Object Assertions

```csharp
user.Should().BeEquivalentTo(new
{
    Name = "John",
    Email = "john@example.com"
}, options => options.ExcludingMissingMembers());
```

### Exception Assertions

```csharp
var action = () => service.GetUser(-1);
action.Should().Throw<ArgumentException>()
    .WithMessage("*invalid*");

await asyncAction.Should().ThrowAsync<NotFoundException>();
```

### Collection Assertions

```csharp
users.Should().HaveCount(3);
users.Should().Contain(u => u.Name == "John");
users.Should().BeInAscendingOrder(u => u.CreatedAt);
users.Should().OnlyContain(u => u.IsActive);
users.Should().NotContainNulls();
```

## Moq Mocking

### Creating Mocks

```csharp
var repository = new Mock<IUserRepository>();
var logger = new Mock<ILogger<UserService>>();
```

### Configuring Returns

```csharp
repository.Setup(r => r.GetById(1)).Returns(new User { Id = 1 });
repository.Setup(r => r.GetById(It.IsAny<int>())).Returns(new User());
repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1 });
```

### Conditional Returns

```csharp
repository.Setup(r => r.GetById(It.Is<int>(x => x > 0))).Returns(new User());
repository.Setup(r => r.GetById(It.Is<int>(x => x <= 0))).Returns((User?)null);
```

### Verifying Calls

```csharp
repository.Verify(r => r.Save(It.IsAny<User>()), Times.Once);
repository.Verify(r => r.Save(It.Is<User>(u => u.Name == "John")));
repository.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
repository.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Once);
```

### Argument Capture

```csharp
User? capturedUser = null;
repository.Setup(r => r.Save(It.IsAny<User>()))
    .Callback<User>(u => capturedUser = u);

service.CreateUser("John");

capturedUser.Should().NotBeNull();
capturedUser!.Name.Should().Be("John");
```

## Test Categories

### Fact vs Theory

```csharp
// Single test case
[Fact]
public void Add_TwoNumbers_ReturnsSum()
{
    Calculator.Add(2, 3).Should().Be(5);
}

// Parameterized tests
[Theory]
[InlineData(2, 3, 5)]
[InlineData(0, 0, 0)]
[InlineData(-1, 1, 0)]
public void Add_TwoNumbers_ReturnsSum(int a, int b, int expected)
{
    Calculator.Add(a, b).Should().Be(expected);
}
```

### Member Data

```csharp
public static IEnumerable<object[]> TestCases =>
[
    [new User { Name = "John" }, true],
    [new User { Name = "" }, false],
];

[Theory]
[MemberData(nameof(TestCases))]
public void Validate_User_ReturnsExpectedResult(User user, bool expected)
{
    Validator.IsValid(user).Should().Be(expected);
}
```

## Async Testing

```csharp
[Fact]
public async Task GetUserAsync_WithValidId_ReturnsUser()
{
    var repository = new Mock<IUserRepository>();
    repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1 });
    var service = new UserService(repository.Object);

    var result = await service.GetUserAsync(1);

    result.Should().NotBeNull();
}
```

## Test Fixtures

### Shared Setup

```csharp
public class UserServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _repository;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _repository = new Mock<IUserRepository>();
        _sut = new UserService(_repository.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public void GetUser_WithValidId_ReturnsUser()
    {
        _repository.Setup(r => r.GetById(1)).Returns(new User { Id = 1 });

        var result = _sut.GetUser(1);

        result.Should().NotBeNull();
    }
}
```

### Collection Fixtures (Expensive Setup)

```csharp
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

public class DatabaseFixture : IAsyncLifetime
{
    public AppDbContext Context { get; private set; }

    public async Task InitializeAsync()
    {
        Context = await CreateTestDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }
}

[Collection("Database")]
public class UserRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
}
```

## Integration Testing

### WebApplicationFactory

```csharp
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Best Practices

### Test Independence

Each test must be independent and not rely on other tests:

```csharp
// ❌ Bad - relies on shared state
private static User _createdUser;

[Fact]
public void CreateUser_CreatesUser() => _createdUser = service.Create();

[Fact]
public void GetUser_ReturnsCreatedUser() => service.Get(_createdUser.Id);

// ✅ Good - self-contained
[Fact]
public void GetUser_WithValidId_ReturnsUser()
{
    var user = service.Create(new User { Name = "Test" });
    var result = service.Get(user.Id);
    result.Should().NotBeNull();
}
```

### Test One Thing

Each test should verify a single behavior:

```csharp
// ❌ Bad - testing multiple things
[Fact]
public void UserOperations_Work()
{
    var user = service.Create();
    user.Should().NotBeNull();
    service.Update(user);
    service.Delete(user.Id);
}

// ✅ Good - focused tests
[Fact]
public void Create_ReturnsNewUser() { }

[Fact]
public void Update_ModifiesExistingUser() { }

[Fact]
public void Delete_RemovesUser() { }
```

### Meaningful Assertions

```csharp
// ❌ Bad - not specific
result.Should().NotBeNull();

// ✅ Good - verifies expected state
result.Should().BeEquivalentTo(new { Name = "John", Status = "Active" });
```
