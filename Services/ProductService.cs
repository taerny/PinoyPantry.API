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

            // Products with no supplier invoice Code (added by hand, not from an invoice) get
            // an internal one so the field is never blank — admin never types this themselves.
            if (string.IsNullOrWhiteSpace(createdProduct.Code))
            {
                createdProduct.Code = $"PP-{createdProduct.Id:D6}";
                await _productRepository.SetCodeAsync(createdProduct.Id, createdProduct.Code);
            }

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

        // CostPrice is no longer derived from Subtotal/Qty here — cost lives per batch now and
        // is kept in sync by BatchStockService.SyncProductCostAsync (see ProductBatchService).
        // This just recomputes RecommendedRetail from whatever CostPrice currently is plus the
        // admin-set Margin — never trusted from client input.
        private static void ApplyPricingCalculations(Product product)
        {
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
            var dtoList = products.ToList();
            var entities = _mapper.Map<List<Product>>(dtoList); // StockQuantity ignored — starts at 0
            var imported = await _productRepository.ImportProductsAsync(entities);

            // Each imported row's quantity becomes that product's initial batch, same as how
            // existing live products were backfilled with "Batch 1" when tracking began.
            for (var i = 0; i < entities.Count; i++)
            {
                if (dtoList[i].StockQuantity > 0)
                    await _productRepository.CreateInitialBatchAsync(entities[i].Id, dtoList[i].StockQuantity);
            }

            return imported;
        }

        public async Task<List<ImportPdfPreviewRowDto>> PreviewPdfImportAsync(Stream pdfStream)
        {
            var parsed = PdfInvoiceParseService.Parse(pdfStream);
            var existingCodes = await _productRepository.GetExistingCodesAsync(parsed.Select(r => r.Code));

            return parsed.Select(r => new ImportPdfPreviewRowDto
            {
                Code = r.Code,
                Name = r.Name,
                AlreadyExists = existingCodes.Contains(r.Code),
            }).ToList();
        }

        public async Task<int> ConfirmPdfImportAsync(IEnumerable<ConfirmImportPdfRowDto> rows)
        {
            var rowList = rows.ToList();
            var existingCodes = await _productRepository.GetExistingCodesAsync(rowList.Select(r => r.Code));

            // Skip duplicates server-side regardless of what the client sends — the preview
            // flag is a UI hint, not the source of truth.
            var newProducts = rowList
                .Where(r => !existingCodes.Contains(r.Code))
                .Select(r => new Product
                {
                    Name = r.Name,
                    Code = r.Code,
                    Description = string.Empty,
                    Category = string.Empty,
                    ImageUrl = string.Empty,
                    Price = 0,
                    CostPrice = 0,
                    StockQuantity = 0,
                    IsPublished = false, // incomplete — no cost/qty/category yet, never live by accident
                })
                .ToList();

            if (newProducts.Count == 0) return 0;

            return await _productRepository.ImportProductsAsync(newProducts);
        }
    }
}
