using FluentAssertions;
using WindowSill.DevToys.Core;

namespace UnitTests.DevToys.Core;

public class YamlHelperTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    public void ConvertFromJsonShouldReturnEmptyString(string? input, string expected)
    {
        // prepare & act
        string actual = YamlHelper.ConvertFromJson(
            input,
            Indentation.FourSpaces);

        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("{\r\n    \"key\": \"value\"\r\n  }", "key: value\r\n")]
    [InlineData("{\r\n    \"key\": \"value\",\r\n    \"key2\": 1\r\n  }", "key: value\r\nkey2: 1\r\n")]
    public void ConvertFromJsonWithTwoSpaces(string input, string expectedResult)
    {
        expectedResult = expectedResult.Replace("\r\n", Environment.NewLine);
        string result = YamlHelper.ConvertFromJson(
             input,
             Indentation.TwoSpaces);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("{\r\n    \"key\": \"value\"\r\n  }", "key: value\r\n")]
    [InlineData("{\r\n    \"key\": \"value\",\r\n    \"key2\": 1\r\n  }", "key: value\r\nkey2: 1\r\n")]
    public void ConvertFromJsonWithFourSpaces(string input, string expectedResult)
    {
        expectedResult = expectedResult.Replace("\r\n", Environment.NewLine);
        string result = YamlHelper.ConvertFromJson(
             input,
             Indentation.FourSpaces);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("[\r\n  {\r\n    \"key\": \"value\"\r\n  }\r\n]", "- key: value\r\n")]
    [InlineData("[\r\n  {\r\n    \"key\": \"value\",\r\n    \"key2\": 1\r\n  }\r\n]", "- key: value\r\n  key2: 1\r\n")]
    public void ConvertFromJsonWithJsonRootArrayWithTwoSpaces(string input, string expectedResult)
    {
        expectedResult = expectedResult.Replace("\r\n", Environment.NewLine);
        string result = YamlHelper.ConvertFromJson(
             input,
             Indentation.TwoSpaces);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("[\r\n  {\r\n    \"key\": \"value\"\r\n  }\r\n]", "-   key: value\r\n")]
    [InlineData("[\r\n  {\r\n    \"key\": \"value\",\r\n    \"key2\": 1\r\n  }\r\n]", "-   key: value\r\n    key2: 1\r\n")]
    public void ConvertFromJsonWithJsonRootArrayWithFourSpaces(string input, string expectedResult)
    {
        expectedResult = expectedResult.Replace("\r\n", Environment.NewLine);
        string result = YamlHelper.ConvertFromJson(
             input,
             Indentation.FourSpaces);
        result.Should().Be(expectedResult);
    }
}
