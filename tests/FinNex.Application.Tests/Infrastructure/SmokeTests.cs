namespace FinNex.Application.Tests.Infrastructure;

/// <summary>
/// Test infrastrukturunun düzgün qurulduğunu yoxlayan minimum testlər.
/// Bu testlər heç bir biznes məntiqi yoxlamır — yalnız xUnit + FluentAssertions
/// + NSubstitute paketlərinin layihəyə düzgün bağlandığını təsdiq edir.
/// Real biznes testləri sonrakı fayllarda olacaq.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Test_infrastructure_xUnit_islayir()
    {
        // Arrange
        int a = 2;
        int b = 3;

        // Act
        int sum = a + b;

        // Assert
        sum.Should().Be(5);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 2)]
    [InlineData(-1, 1, 0)]
    [InlineData(100, 200, 300)]
    public void Test_infrastructure_xUnit_Theory_inline_data_islayir(int a, int b, int expected)
    {
        (a + b).Should().Be(expected);
    }

    [Fact]
    public void Test_infrastructure_FluentAssertions_collection_yoxlamasi_islayir()
    {
        // Arrange
        var siyahi = new[] { 1, 2, 3, 4, 5 };

        // Act + Assert
        siyahi.Should().HaveCount(5);
        siyahi.Should().Contain(3);
        siyahi.Should().BeInAscendingOrder();
        siyahi.Should().NotContain(99);
    }

    [Fact]
    public void Test_infrastructure_NSubstitute_mock_islayir()
    {
        // Arrange — sadə bir interfeys mock-layırıq
        var calculator = Substitute.For<ITestCalculator>();
        calculator.Add(2, 3).Returns(5);

        // Act
        var netice = calculator.Add(2, 3);

        // Assert
        netice.Should().Be(5);
        calculator.Received(1).Add(2, 3);
    }

    // Mock üçün yardımçı interfeys — yalnız smoke test üçün
    public interface ITestCalculator
    {
        int Add(int a, int b);
    }
}
