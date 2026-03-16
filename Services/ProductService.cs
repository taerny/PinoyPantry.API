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
            var createdProduct = await _productRepository.CreateProductAsync(product);
            return _mapper.Map<ProductResponseDto>(createdProduct);
        }

        public async Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto productDto)
        {
            var existing = await _productRepository.GetProductByIdAsync(id);
            if (existing == null)
                return null;

            _mapper.Map(productDto, existing);
            var updated = await _productRepository.UpdateProductAsync(id, existing);

            return updated == null ? null : _mapper.Map<ProductResponseDto>(updated);
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
    }
}
