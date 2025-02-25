using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;


namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductRepository productRepository) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetProducts(string? brand, string? type, string? sort)
    {
        return Ok(await productRepository.GetProductsAsync(brand, type, sort));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await productRepository.GetProductByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return product;
    }
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        productRepository.AddProduct(product);
        if (await productRepository.SaveChangesAsync())
        {
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        return BadRequest("Failed to create product");
    }
    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateProduct(int id, Product product)
    {
        if (id != product.Id || !ProductExists(id))
        {
            return BadRequest("Cannot update product");
        }
        productRepository.UpdateProduct(product);
        if (await productRepository.SaveChangesAsync())
        {
            return NoContent();
        }
        return BadRequest("Failed to update product");
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await productRepository.GetProductByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        productRepository.DeleteProduct(product);
        if (await productRepository.SaveChangesAsync())
        {
            return NoContent();
        }
        return BadRequest("Failed to delete product");
    }
    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
    {
        return Ok(await productRepository.GetBrandsAsync());
    }

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
    {
        return Ok(await productRepository.GetTypesAsync());
    }
    private bool ProductExists(int id)
    {
        return productRepository.ProductExists(id);
    }
}
