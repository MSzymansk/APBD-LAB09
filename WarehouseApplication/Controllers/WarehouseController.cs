using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WarehouseApplication.Models.DTOs;
using WarehouseApplication.Services;

namespace WarehouseApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController(IWarehouseService _warehouseService) : ControllerBase
    {
        /*  Handles HTTP POST requests to add a product to a warehouse.
            Validates the incoming ProductDTO payload, and if valid, delegates the operation to the warehouse service.
            If successful, returns the ID of the newly inserted record in the Product_Warehouse table.
            If the request body is null or an exception occurs during processing, returns a BadRequest with the error message. */
        [HttpPost]
        public async Task<IActionResult> AddProductToWarehouse([FromBody] ProductDTO productDTO)
        {
            if (productDTO == null)
            {
                return BadRequest("Product data cannot be null");
            }

            try
            {
                var id = await _warehouseService.AddProductToWarehouse(productDTO);
                return Ok(id);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        /* Handles HTTP POST requests to add a product to a warehouse using a stored procedure.
           Validates the incoming ProductDTO payload, and if valid, delegates the operation to the warehouse service.
           The stored procedure checks product, warehouse, and order validity, and inserts the data accordingly.
           If successful, returns the ID of the newly inserted record in the Product_Warehouse table.
           If the request data is invalid or an exception occurs (e.g. RAISERROR from SQL), returns a BadRequest with the error message. */
        [HttpPost("Procedure")]
        public async Task<IActionResult> AddProductToWarehouseProcedure([FromBody] ProductDTO productDTO)
        {
            if (productDTO == null)
            {
                return BadRequest("Product data cannot be null");
            }

            try
            {
                var result = await _warehouseService.AddProductToWarehouseProcedure(productDTO);
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}