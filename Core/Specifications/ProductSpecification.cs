using Core.Entities;

namespace Core.Specifications;

public class ProductSpecification : BaseSpecification<Product>
{
    public ProductSpecification(string? brand, string? type, string? sort) : base(p =>
    (string.IsNullOrWhiteSpace(brand) || p.Brand == brand) &&
    (string.IsNullOrWhiteSpace(type) || p.Type == type))
    {
        switch (sort)
        {
            case "priceAsc":
                AddOrderBy(p => p.Price);
                break;
            case "priceDesc":
                AddOrderByDescending(p => p.Price);
                break;
            default:
                AddOrderBy(n => n.Name);
                break;
        }
    }
}