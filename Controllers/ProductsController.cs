using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using PinoyPantry.API.Models;
using PinoyPantry.API.Repositories;

namespace PinoyPantry.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {

        private readonly IProductRepository _productRepository;
        public ProductsController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        //Get: api/products
        [HttpGet]
        public  async Task<ActionResult> GetAllProducts()
        {
            var products = await _productRepository.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id) {
            var product = await _productRepository.GetProductyByIdAsync(id);
            if (product == null)
                return NotFound(new { message = $"Product {id} not found." });

            return Ok(product);
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product) {

            var createdProduct = await _productRepository.CreateProductAsync(product);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);

        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(int id, Product product) {

            var updatedProduct = await _productRepository.UpdateProductAsync(id, product);
            if (updatedProduct == null)
                return NotFound(new { message = $" Product with ID: {id} not found." });

            return Ok(updatedProduct);

        }
        
        [HttpDelete("{id}")]
        public async Task<ActionResult<Product>> DeleteProduct(int id) {

            var result = await _productRepository.DeleteProductAsync(id);
            if (!result)
                return NotFound(new { message = $"Product with ID {id} not found." });

            return NoContent();

        }

    }
}
