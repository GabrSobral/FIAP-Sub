using FiapSub.Core.Application.Common;

namespace FiapSub.Core.Application.Tests.Common;

public class PagedListTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    public void TotalPages_RoundsUp(int totalCount, int pageSize, int expected)
    {
        new PagedList<int>([], 1, pageSize, totalCount).TotalPages.ShouldBe(expected);
    }

    [Fact]
    public void TotalPages_IsZeroForANonPositivePageSize()
    {
        new PagedList<int>([], 1, 0, 10).TotalPages.ShouldBe(0);
    }

    [Fact]
    public void HasNextPage_TracksThePageCount()
    {
        new PagedList<int>([1], 1, 1, 3).HasNextPage.ShouldBeTrue();
        new PagedList<int>([1], 3, 1, 3).HasNextPage.ShouldBeFalse();
    }
}
