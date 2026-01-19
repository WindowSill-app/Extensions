using FluentAssertions;
using UnitsNet;
using UnitsNet.Units;
using WindowSill.UnitConverter.Exchange;
using WindowSill.UnitConverter;

namespace UnitTests.UnitConverter;

public class UnitHelperTests
{
    [Theory]
    [InlineData("25C", 25, TemperatureUnit.DegreeCelsius)]
    [InlineData("77F", 77, TemperatureUnit.DegreeFahrenheit)]
    [InlineData("273 Kelvin", 273, TemperatureUnit.Kelvin)]
    [InlineData("100˚C", 100, TemperatureUnit.DegreeCelsius)]
    [InlineData("32˚F", 32, TemperatureUnit.DegreeFahrenheit)]
    [InlineData("-40 degrees Celsius", -40, TemperatureUnit.DegreeCelsius)]
    [InlineData("98.6 degrees Fahrenheit", 98.6, TemperatureUnit.DegreeFahrenheit)]
    public void TryDetectTemperature_WithValidInput_ReturnsTrue(string input, double expectedValue, TemperatureUnit expectedUnit)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectTemperature(input, cts.Token, out UnitsNet.Temperature temperature);

        // Assert
        result.Should().BeTrue();
        temperature.Value.Should().BeApproximately(expectedValue, 0.01);
        temperature.Unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("abc degrees")]
    [InlineData("25 meters")]
    public void TryDetectTemperature_WithInvalidInput_ReturnsFalse(string? input)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectTemperature(input, cts.Token, out UnitsNet.Temperature temperature);

        // Assert
        result.Should().BeFalse();
        temperature.Should().Be(default(UnitsNet.Temperature));
    }

    [Fact]
    public void TryDetectTemperature_WithCancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Action act = () => UnitHelper.TryDetectTemperature("25C", cts.Token, out _);

        // Assert
        act.Should().Throw<OperationCanceledException>();
    }

    [Theory]
    [InlineData("$50", 50, "Dollar", "USD")]
    [InlineData("€100", 100, "Euro", "EUR")]
    [InlineData("£75", 75, "Pound", "GBP")]
    [InlineData("¥1000", 1000, "Japanese yen", "JPY")]
    [InlineData("50 dollars", 50, "Dollar", "USD")]
    [InlineData("100.50 euros", 100.50, "Euro", "EUR")]
    public void TryDetectCurrency_WithValidInput_ReturnsTrue(string input, double expectedValue, string expectedCurrency, string expectedIsoCurrency)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectCurrency(input, cts.Token, out CurrencyValue? currency);

        // Assert
        result.Should().BeTrue();
        currency.Should().NotBeNull();
        currency!.Value.Should().BeApproximately(expectedValue, 0.01);
        currency.Currency.Should().Be(expectedCurrency);
        currency.IsoCurrency.Should().Be(expectedIsoCurrency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("abc currency")]
    [InlineData("25 meters")]
    public void TryDetectCurrency_WithInvalidInput_ReturnsFalse(string? input)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectCurrency(input, cts.Token, out CurrencyValue? currency);

        // Assert
        result.Should().BeFalse();
        currency.Should().BeNull();
    }

    [Fact]
    public void TryDetectCurrency_WithCancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Action act = () => UnitHelper.TryDetectCurrency("$50", cts.Token, out _);

        // Assert
        act.Should().Throw<OperationCanceledException>();
    }

    [Theory]
    [InlineData("5 meters", typeof(Length), 5, LengthUnit.Meter)]
    [InlineData("10 feet", typeof(Length), 10, LengthUnit.Foot)]
    [InlineData("100 centimeters", typeof(Length), 100, LengthUnit.Centimeter)]
    [InlineData("2.5 kilometers", typeof(Length), 2.5, LengthUnit.Kilometer)]
    [InlineData("12 inches", typeof(Length), 12, LengthUnit.Inch)]
    [InlineData("5 yards", typeof(Length), 5, LengthUnit.Yard)]
    [InlineData("1 mile", typeof(Length), 1, LengthUnit.Mile)]
    public void TryDetectDimension_WithValidLengthInput_ReturnsTrue(string input, Type expectedType, double expectedValue, LengthUnit expectedUnit)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectDimension(input, cts.Token, out IConvertible? dimension);

        // Assert
        result.Should().BeTrue();
        dimension.Should().NotBeNull();
        dimension.Should().BeOfType(expectedType);
        var length = (Length)dimension!;
        length.Value.Should().BeApproximately(expectedValue, 0.01);
        length.Unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData("1 byte", typeof(Information), InformationUnit.Byte)]
    [InlineData("1024 bytes", typeof(Information), InformationUnit.Byte)]
    [InlineData("1 kilobyte", typeof(Information), InformationUnit.Kilobyte)]
    [InlineData("1 megabyte", typeof(Information), InformationUnit.Megabyte)]
    [InlineData("1 gigabyte", typeof(Information), InformationUnit.Gigabyte)]
    [InlineData("1 terabyte", typeof(Information), InformationUnit.Terabyte)]
    [InlineData("1 bit", typeof(Information), InformationUnit.Bit)]
    public void TryDetectDimension_WithValidInformationInput_ReturnsTrue(string input, Type expectedType, InformationUnit expectedUnit)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectDimension(input, cts.Token, out IConvertible? dimension);

        // Assert
        result.Should().BeTrue();
        dimension.Should().NotBeNull();
        dimension.Should().BeOfType(expectedType);
        var information = (Information)dimension!;
        information.Unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData("10 square meters", typeof(Area), 10, AreaUnit.SquareMeter)]
    [InlineData("5 square feet", typeof(Area), 5, AreaUnit.SquareFoot)]
    [InlineData("2 acres", typeof(Area), 2, AreaUnit.Acre)]
    [InlineData("100 square centimeters", typeof(Area), 100, AreaUnit.SquareCentimeter)]
    [InlineData("1 square kilometer", typeof(Area), 1, AreaUnit.SquareKilometer)]
    public void TryDetectDimension_WithValidAreaInput_ReturnsTrue(string input, Type expectedType, double expectedValue, AreaUnit expectedUnit)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectDimension(input, cts.Token, out IConvertible? dimension);

        // Assert
        result.Should().BeTrue();
        dimension.Should().NotBeNull();
        dimension.Should().BeOfType(expectedType);
        var area = (Area)dimension!;
        area.Value.Should().BeApproximately(expectedValue, 0.01);
        area.Unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData("60 kilometers per hour", typeof(Speed), 60, SpeedUnit.KilometerPerHour)]
    [InlineData("30 miles per hour", typeof(Speed), 30, SpeedUnit.MilePerHour)]
    [InlineData("10 meters per second", typeof(Speed), 10, SpeedUnit.MeterPerSecond)]
    [InlineData("50 feet per second", typeof(Speed), 50, SpeedUnit.FootPerSecond)]
    [InlineData("20 knots", typeof(Speed), 20, SpeedUnit.Knot)]
    public void TryDetectDimension_WithValidSpeedInput_ReturnsTrue(string input, Type expectedType, double expectedValue, SpeedUnit expectedUnit)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectDimension(input, cts.Token, out IConvertible? dimension);

        // Assert
        result.Should().BeTrue();
        dimension.Should().NotBeNull();
        dimension.Should().BeOfType(expectedType);
        var speed = (Speed)dimension!;
        speed.Value.Should().BeApproximately(expectedValue, 0.01);
        speed.Unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData("1 liter", typeof(Volume), 1, VolumeUnit.Liter)]
    [InlineData("500 milliliters", typeof(Volume), 500, VolumeUnit.Milliliter)]
    [InlineData("1 gallon", typeof(Volume), 1, VolumeUnit.UsGallon)]
    [InlineData("10 cubic meters", typeof(Volume), 10, VolumeUnit.CubicMeter)]
    [InlineData("2 pints", typeof(Volume), 2, VolumeUnit.UsPint)]
    [InlineData("1 quart", typeof(Volume), 1, VolumeUnit.UsQuart)]
    public void TryDetectDimension_WithValidVolumeInput_ReturnsTrue(string input, Type expectedType, double expectedValue, VolumeUnit expectedUnit)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectDimension(input, cts.Token, out IConvertible? dimension);

        // Assert
        result.Should().BeTrue();
        dimension.Should().NotBeNull();
        dimension.Should().BeOfType(expectedType);
        var volume = (Volume)dimension!;
        volume.Value.Should().BeApproximately(expectedValue, 0.01);
        volume.Unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData("5 kilograms", typeof(Mass), 5, MassUnit.Kilogram)]
    [InlineData("10 pounds", typeof(Mass), 10, MassUnit.Pound)]
    [InlineData("100 grams", typeof(Mass), 100, MassUnit.Gram)]
    [InlineData("16 ounces", typeof(Mass), 16, MassUnit.Ounce)]
    [InlineData("2 tons", typeof(Mass), 2, MassUnit.Tonne)]
    [InlineData("1 stone", typeof(Mass), 1, MassUnit.Stone)]
    public void TryDetectDimension_WithValidMassInput_ReturnsTrue(string input, Type expectedType, double expectedValue, MassUnit expectedUnit)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectDimension(input, cts.Token, out IConvertible? dimension);

        // Assert
        result.Should().BeTrue();
        dimension.Should().NotBeNull();
        dimension.Should().BeOfType(expectedType);
        var mass = (Mass)dimension!;
        mass.Value.Should().BeApproximately(expectedValue, 0.01);
        mass.Unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData("90 degrees", typeof(Angle), 90, AngleUnit.Degree)]
    [InlineData("3.14 radians", typeof(Angle), 3.14, AngleUnit.Radian)]
    [InlineData("180 degrees", typeof(Angle), 180, AngleUnit.Degree)]
    public void TryDetectDimension_WithValidAngleInput_ReturnsTrue(string input, Type expectedType, double expectedValue, AngleUnit expectedUnit)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectDimension(input, cts.Token, out IConvertible? dimension);

        // Assert
        result.Should().BeTrue();
        dimension.Should().NotBeNull();
        dimension.Should().BeOfType(expectedType);
        var angle = (Angle)dimension!;
        angle.Value.Should().BeApproximately(expectedValue, 0.01);
        angle.Unit.Should().Be(expectedUnit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("abc dimension")]
    [InlineData("$50")]
    [InlineData("25C")]
    public void TryDetectDimension_WithInvalidInput_ReturnsFalse(string? input)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectDimension(input, cts.Token, out IConvertible? dimension);

        // Assert
        result.Should().BeFalse();
        dimension.Should().BeNull();
    }

    [Fact]
    public void TryDetectDimension_WithCancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Action act = () => UnitHelper.TryDetectDimension("5 meters", cts.Token, out _);

        // Assert
        act.Should().Throw<OperationCanceledException>();
    }

    [Theory]
    [InlineData("5.5 meters")]
    [InlineData("10.25 feet")]
    [InlineData("3.14159 kilometers")]
    public void TryDetectDimension_WithDecimalValues_ParsesCorrectly(string input)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectDimension(input, cts.Token, out IConvertible? dimension);

        // Assert
        result.Should().BeTrue();
        dimension.Should().NotBeNull();
        dimension.Should().BeOfType<Length>();
    }

    [Theory]
    [InlineData("0 meters")]
    [InlineData("0 kilograms")]
    [InlineData("0 liters")]
    public void TryDetectDimension_WithZeroValue_ParsesCorrectly(string input)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectDimension(input, cts.Token, out IConvertible? dimension);

        // Assert
        result.Should().BeTrue();
        dimension.Should().NotBeNull();
    }

    [Theory]
    [InlineData("-5 meters", false)]
    [InlineData("-10 degrees Celsius", true)]
    public void TryDetectTemperature_WithNegativeValue_ParsesCorrectly(string input, bool expectedResult)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        bool result = UnitHelper.TryDetectTemperature(input, cts.Token, out UnitsNet.Temperature temperature);

        // Assert
        result.Should().Be(expectedResult);
    }
}
