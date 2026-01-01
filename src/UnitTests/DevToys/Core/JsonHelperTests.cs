using FluentAssertions;
using WindowSill.DevToys.Core;

namespace UnitTests.DevToys.Core;

public class JsonHelperTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("   {  }  ", "{}")]
    [InlineData("   [  ]  ", "[]")]
    [InlineData("   { \"foo\": 123 }  ", "{\r\n  \"foo\": 123\r\n}")]
    public void FormatTwoSpaces(string? input, string expectedResult)
    {
        expectedResult = expectedResult.Replace("\r\n", Environment.NewLine);
        string result = JsonHelper.Format(input, Indentation.TwoSpaces);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("   {  }  ", "{}")]
    [InlineData("   [  ]  ", "[]")]
    [InlineData("   { \"foo\": 123 }  ", "{\r\n    \"foo\": 123\r\n}")]
    public void FormatFourSpaces(string? input, string expectedResult)
    {
        expectedResult = expectedResult.Replace("\r\n", Environment.NewLine);
        string result = JsonHelper.Format(input, Indentation.FourSpaces);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("   {  }  ", "{}")]
    [InlineData("   [  ]  ", "[]")]
    [InlineData("   { \"foo\": 123 }  ", "{\r\n\t\"foo\": 123\r\n}")]
    public void FormatOneTab(string? input, string expectedResult)
    {
        expectedResult = expectedResult.Replace("\r\n", Environment.NewLine);
        string result = JsonHelper.Format(input, Indentation.OneTab);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("   {  }  ", "{}")]
    [InlineData("   [  ]  ", "[]")]
    [InlineData("   { \"foo\": 123 }  ", "{\"foo\":123}")]
    public void FormatMinified(string? input, string expectedResult)
    {
        string result = JsonHelper.Format(input, Indentation.Minified);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("{ \"Date\": \"2012-04-21T18:25:43-05:00\" }", "{\"Date\":\"2012-04-21T18:25:43-05:00\"}")]
    public void FormatDoesNotAlterateDateTimes(string? input, string expectedResult)
    {
        string result = JsonHelper.Format(input, Indentation.Minified);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("{ invalid json", "")]
    [InlineData("{ \"unclosed\": ", "")]
    [InlineData("{ \"foo\": bar }", "")] // unquoted value
    [InlineData("{ \"foo\": 123, }", "")] // trailing comma
    public void FormatHandlesInvalidJson(string? input, string expectedResult)
    {
        string result = JsonHelper.Format(input, Indentation.TwoSpaces);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("{ \"bool\": true, \"nullValue\": null, \"number\": 42.5 }", "{\r\n  \"bool\": true,\r\n  \"nullValue\": null,\r\n  \"number\": 42.5\r\n}")]
    [InlineData("[ true, false, null, 123, \"text\" ]", "[\r\n  true,\r\n  false,\r\n  null,\r\n  123,\r\n  \"text\"\r\n]")]
    public void FormatHandlesDifferentValueTypes(string input, string expectedResult)
    {
        expectedResult = expectedResult.Replace("\r\n", Environment.NewLine);
        string result = JsonHelper.Format(input, Indentation.TwoSpaces);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    public void SortHandlesNullOrEmptyInput(string? input, string expectedResult)
    {
        string result = JsonHelper.Sort(input);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void SortSortsObjectPropertiesAlphabetically()
    {
        string input = "{ \"zebra\": 1, \"apple\": 2, \"banana\": 3 }";
        string expectedResult = $"{{\"apple\":2,\"banana\":3,\"zebra\":1}}";

        string result = JsonHelper.Sort(input);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void SortSortsObjectPropertiesCaseInsensitive()
    {
        string input = "{ \"Zebra\": 1, \"apple\": 2, \"Banana\": 3 }";
        string expectedResult = $"{{\"apple\":2,\"Banana\":3,\"Zebra\":1}}";

        string result = JsonHelper.Sort(input);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void SortPreservesArrayOrder()
    {
        string input = "[ 3, 1, 2 ]";
        string expectedResult = "[3,1,2]";

        string result = JsonHelper.Sort(input);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void SortHandlesNestedObjectsAndArrays()
    {
        string input = "{ \"z_obj\": { \"z_nested\": 1, \"a_nested\": 2 }, \"a_array\": [3, 1, 2], \"m_primitive\": \"value\" }";
        string expectedResult = "{\"a_array\":[3,1,2],\"m_primitive\":\"value\",\"z_obj\":{\"a_nested\":2,\"z_nested\":1}}";

        string result = JsonHelper.Sort(input);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void SortHandlesDifferentValueTypes()
    {
        string input = "{ \"z_bool\": true, \"a_null\": null, \"m_number\": 42.5, \"b_string\": \"text\", \"n_false\": false }";
        string expectedResult = "{\"a_null\":null,\"b_string\":\"text\",\"m_number\":42.5,\"n_false\":false,\"z_bool\":true}";

        string result = JsonHelper.Sort(input);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("{\r\n  \"b\": 2,\r\n  \"a\": 1\r\n}", "{\r\n  \"a\": 1,\r\n  \"b\": 2\r\n}")]
    [InlineData("{\r\n    \"b\": 2,\r\n    \"a\": 1\r\n}", "{\r\n    \"a\": 1,\r\n    \"b\": 2\r\n}")]
    [InlineData("{\r\n\t\"b\": 2,\r\n\t\"a\": 1\r\n}", "{\r\n\t\"a\": 1,\r\n\t\"b\": 2\r\n}")]
    public void SortDetectsAndPreservesIndentation(string input, string expectedResult)
    {
        expectedResult = expectedResult.Replace("\r\n", Environment.NewLine);
        string result = JsonHelper.Sort(input);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void SortDetectsMinifiedFormat()
    {
        string input = "{\"b\":2,\"a\":1}";
        string expectedResult = "{\"a\":1,\"b\":2}";

        string result = JsonHelper.Sort(input);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void SortHandlesComplexNestedStructure()
    {
        string input = """
        {
          "z_top": {
            "z_nested": [
              { "z_item": 1, "a_item": 2 },
              { "b_item": 3, "a_item": 4 }
            ],
            "a_nested": {
              "z_deep": true,
              "a_deep": false
            }
          },
          "a_top": "value"
        }
        """;

        string expectedResult = """
        {
          "a_top": "value",
          "z_top": {
            "a_nested": {
              "a_deep": false,
              "z_deep": true
            },
            "z_nested": [
              {
                "a_item": 2,
                "z_item": 1
              },
              {
                "a_item": 4,
                "b_item": 3
              }
            ]
          }
        }
        """.Replace("\r\n", Environment.NewLine).Trim();

        string result = JsonHelper.Sort(input);
        result = result.Replace("\r\n", Environment.NewLine).Trim();
        result.Should().Be(expectedResult);
    }
}
