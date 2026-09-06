namespace PinoyPantry.API.DTOs;

public class DashboardStatsDto
{
    public int TotalProducts { get; set; }
    public int TotalUsers { get; set; }
    public int ProductsWithImages { get; set; }
    public int TotalCategories { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalProfitValue { get; set; }
    public List<CategoryStatDto> CategoryStats { get; set; } = new();
    public List<RecentProductDto> RecentProducts { get; set; } = new();
}

public class CategoryStatDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecentProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool HasImage { get; set; }
}
