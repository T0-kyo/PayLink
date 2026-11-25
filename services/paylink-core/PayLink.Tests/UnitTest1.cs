namespace PayLink.Tests;

public class SmokeTests
{
    [Fact]
    public void System_DecimalPrecision_WorksAsExpected()
    {
        decimal bill = 350.00m;
        decimal fee = 2.50m;
        Assert.Equal(352.50m, bill + fee);
    }
}