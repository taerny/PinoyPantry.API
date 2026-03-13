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
        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductResponseDto>>> GetAllProducts([FromQuery] ProductQueryParams query)
        {
            var result = await _productService.GetAllProductsAsync(query);
            return Ok(result);
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
        [HttpPost]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct(CreateProductDto productDto)
        {
            var createdProduct = await _productService.CreateProductAsync(productDto);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ProductResponseDto>> UpdateProduct(int id, UpdateProductDto productDto)
        {
            var updatedProduct = await _productService.UpdateProductAsync(id, productDto);
            if (updatedProduct == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            return Ok(updatedProduct);
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            if (!result)
                return NotFound(new { message = $"Product with ID {id} not found." });

            return NoContent();
        }
    }
}
