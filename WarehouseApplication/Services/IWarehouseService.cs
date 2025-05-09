using WarehouseApplication.Models.DTOs;

namespace WarehouseApplication.Services;

public interface IWarehouseService
{
    
    Task<int> AddProductToWarehouse(ProductDTO product);
    
    Task<int> AddProductToWarehouseProcedure(ProductDTO product);
    
}