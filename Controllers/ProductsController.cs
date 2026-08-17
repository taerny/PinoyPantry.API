using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Services;

namespace PinoyPantry.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: api/products?page=1&limit=12&category=snacks&search=vinegar
        // Public endpoint — always published-only, regardless of what's in the query string.
        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductResponseDto>>> GetAllProducts([FromQuery] ProductQueryParams query)
        {
            query.IncludeUnpublished = false;
            var result = await _productService.GetAllProductsAsync(query);
            return Ok(result);
        }

        // GET: api/products/admin?page=1&limit=50 — Admin only, includes unpublished products and cost price
        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<PagedResult<AdminProductResponseDto>>> GetAllProductsAdmin([FromQuery] ProductQueryParams query)
        {
            var result = await _productService.GetAllProductsAdminAsync(query);
            return Ok(result);
        }

        // GET: api/products/category-counts
        [HttpGet("category-counts")]
        public async Task<ActionResult<Dictionary<string, int>>> GetCategoryCounts()
        {
            var counts = await _productService.GetCategoryCountsAsync();
            return Ok(counts);
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound(new { message = $"Product {id} not found." });

            return Ok(product);
        }

        // POST: api/products
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct(CreateProductDto productDto)
        {
            var createdProduct = await _productService.CreateProductAsync(productDto);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
        }

        // PUT: api/products/5
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<ProductResponseDto>> UpdateProduct(int id, UpdateProductDto productDto)
        {
            var updatedProduct = await _productService.UpdateProductAsync(id, productDto);
            if (updatedProduct == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            return Ok(updatedProduct);
        }

        // DELETE: api/products/5
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            if (!result)
                return NotFound(new { message = $"Product with ID {id} not found." });

            return NoContent();
        }

        // DELETE: api/products/all  — Admin only, clears entire products table
        [Authorize(Roles = "Admin")]
        [HttpDelete("all")]
        public async Task<ActionResult> DeleteAllProducts()
        {
            var deleted = await _productService.DeleteAllProductsAsync();
            return Ok(new { message = $"Cleared {deleted} product(s) successfully." });
        }

        // POST: api/products/import — Admin only, bulk-creates products from a parsed CSV
        [Authorize(Roles = "Admin")]
        [HttpPost("import")]
        public async Task<ActionResult> ImportProducts(List<ImportProductDto> products)
        {
            if (products == null || products.Count == 0)
                return BadRequest(new { message = "No products provided." });

            var imported = await _productService.ImportProductsAsync(products);
            return Ok(new { message = $"Imported {imported} product(s) successfully." });
        }
    }
}
