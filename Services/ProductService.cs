using AutoMapper;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;
using PinoyPantry.API.Repositories;

namespace PinoyPantry.API.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository productRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<ProductResponseDto>> GetAllProductsAsync(ProductQueryParams query)
        {
            var (products, totalCount) = await _productRepository.GetAllProductsAsync(query);

            return new PagedResult<ProductResponseDto>
            {
                Data = _mapper.Map<IEnumerable<ProductResponseDto>>(products),
                TotalCount = totalCount,
                Page = query.Page,
                Limit = query.Limit
            };
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            return product == null ? null : _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto productDto)
        {
            var product = _mapper.Map<Product>(productDto);
            ApplyPricingCalculations(product);
            var createdProduct = await _productRepository.CreateProductAsync(product);
            return _mapper.Map<ProductResponseDto>(createdProduct);
        }

        public async Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto productDto)
        {
            var existing = await _productRepository.GetProductByIdAsync(id);
            if (existing == null)
                return null;

            _mapper.Map(productDto, existing);
            ApplyPricingCalculations(existing);
            var updated = await _productRepository.UpdateProductAsync(id, existing);

            return updated == null ? null : _mapper.Map<ProductResponseDto>(updated);
        }

        // CostPrice is derived from Subtotal/Qty when both are present (locks it as invoice-
        // derived); otherwise whatever CostPrice was set directly is kept (manual products with
        // no supplier invoice line). RecommendedRetail is ALWAYS server-computed from the final
        // CostPrice + Margin — never trusted from client input.
        private static void ApplyPricingCalculations(Product product)
        {
            var derivedCost = PricingCalculator.UnitCost(product.Subtotal, product.PackQty);
            if (derivedCost.HasValue)
                product.CostPrice = derivedCost.Value;

            product.RecommendedRetail = PricingCalculator.RecommendedPrice(product.CostPrice, product.Margin);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            return await _productRepository.DeleteProductAsync(id);
        }

        public async Task<ProductResponseDto?> GetByIdAsync(int id)
        {
            return await GetProductByIdAsync(id);
        }

        public async Task UpdateImageUrlAsync(int id, string imageUrl)
        {
            await _productRepository.UpdateImageUrlAsync(id, imageUrl);
        }

        public async Task<int> DeleteAllProductsAsync()
        {
            return await _productRepository.DeleteAllProductsAsync();
        }

        public async Task<Dictionary<string, int>> GetCategoryCountsAsync()
        {
            return await _productRepository.GetCategoryCountsAsync();
        }

        public async Task<PagedResult<AdminProductResponseDto>> GetAllProductsAdminAsync(ProductQueryParams query)
        {
            query.IncludeUnpublished = true;
            var (products, totalCount) = await _productRepository.GetAllProductsAsync(query);

            return new PagedResult<AdminProductResponseDto>
            {
                Data = _mapper.Map<IEnumerable<AdminProductResponseDto>>(products),
                TotalCount = totalCount,
                Page = query.Page,
                Limit = query.Limit
            };
        }

        public async Task<int> ImportProductsAsync(IEnumerable<ImportProductDto> products)
        {
            var entities = _mapper.Map<IEnumerable<Product>>(products);
            return await _productRepository.ImportProductsAsync(entities);
        }
    }
}
