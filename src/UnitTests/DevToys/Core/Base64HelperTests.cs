using FluentAssertions;
using WindowSill.DevToys.Core;

namespace UnitTests.DevToys.Core;

public class Base64HelperTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("aGVsbG8gd29ybGQ=", true)]
    [InlineData("aGVsbG8gd2f9ybGQ=", false)]
    [InlineData("SGVsbG8gV29y", true)]
    [InlineData("SGVsbG8gVa29y", false)]
    public void IsValid(string? input, bool expectedResult)
    {
        Base64Helper.IsBase64DataStrict(input).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("SGVsbG8gV29ybGQh", "Hello World!")]
    [InlineData("SGVsbG8gV29ybGQhIMOpKcOg", "Hello World! é)à")]
    [InlineData("SGVsbG8gV29ybGQhID8pPw==", "Hello World! ?)?")]
    internal void FromBase64ToText(string? input, string expectedResult)
    {
        Base64Helper.FromBase64ToText(input)
            .Should()
            .Be(expectedResult);
    }
}
