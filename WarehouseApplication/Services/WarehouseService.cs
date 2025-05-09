using System.Data;
using Microsoft.Data.SqlClient;
using WarehouseApplication.Models.DTOs;

namespace WarehouseApplication.Services;

public class WarehouseService(IConfiguration configuration) : IWarehouseService
{
    public async Task<int> AddProductToWarehouse(ProductDTO product)
    {
        await using SqlConnection conn = new SqlConnection(configuration.GetConnectionString("Default"));
        await using SqlCommand cmd = new SqlCommand("", conn);

        await conn.OpenAsync();
        var transaction = await conn.BeginTransactionAsync();
        cmd.Transaction = transaction as SqlTransaction;

        try
        {
            /* Checks if product with given id exists */
            cmd.CommandText = @"SELECT COUNT(*) FROM Product WHERE IdProduct = @IdProduct";
            cmd.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            int count = (int)await cmd.ExecuteScalarAsync();

            if (count <= 0)
            {
                throw new Exception($"Product {product.IdProduct} does not exist");
            }

            /* Checks if warehouse with given id exists */
            cmd.CommandText = @"SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
            count = (int)await cmd.ExecuteScalarAsync();

            if (count <= 0)
            {
                throw new Exception($"Warehouse {product.IdWarehouse} does not exist");
            }

            /* Checks whether an order exists for the given product with the specified amount and a creation date earlier than the request date */
            cmd.CommandText = @"
            SELECT IdOrder
            FROM [Order]
            WHERE [Order].IdProduct = @IdProduct
            AND [Order].Amount = @Amount
            AND [Order].CreatedAt < @CreatedAt
            ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            cmd.Parameters.AddWithValue("@Amount", product.Amount);
            cmd.Parameters.AddWithValue("@CreatedAt", product.CreatedAt);
            var idOrder = await cmd.ExecuteScalarAsync();

            if (idOrder == null)
            {
                throw new Exception($"Order for product id: {product.IdProduct} does not exist");
            }

            /* Ensures that the order has not already been fulfilled (there's no entry in Product_Warehouse for the order).*/
            cmd.CommandText = @"
            SELECT COUNT(*)
            FROM Product_Warehouse
            WHERE IdOrder = @IdOrder
            ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdOrder", idOrder);
            count = (int)await cmd.ExecuteScalarAsync();

            if (count > 0)
            {
                throw new Exception($"Order for product {product.IdProduct} has been already fulfilled");
            }

            /* Updates fulfilled date and time in Order*/
            cmd.CommandText = @"
            UPDATE [Order]
            SET [Order].FulfilledAt = @FulfilledAt
            WHERE [Order].IdOrder = @IdOrder
            ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdOrder", idOrder);
            cmd.Parameters.AddWithValue("@FulfilledAt", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();

            /* Getting price of product */
            cmd.CommandText = @"
            SELECT Price
            FROM Product
            WHERE IdProduct = @IdProduct
            ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            decimal price = (decimal)await cmd.ExecuteScalarAsync();

            /* Inserts record into Product_Warehouse table */
            cmd.CommandText = @"
            INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)
            SELECT CAST(SCOPE_IDENTITY() as int);
            ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
            cmd.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            cmd.Parameters.AddWithValue("@IdOrder", idOrder);
            cmd.Parameters.AddWithValue("@Amount", product.Amount);
            cmd.Parameters.AddWithValue("@Price", price * product.Amount);
            cmd.Parameters.AddWithValue("@CreatedAt", product.CreatedAt);

            int result = (int)await cmd.ExecuteScalarAsync();

            await transaction.CommitAsync();
            return result;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> AddProductToWarehouseProcedure(ProductDTO product)
    {
        string command = "AddProductToWarehouse";

        await using SqlConnection conn = new SqlConnection(configuration.GetConnectionString("Default"));
        await using SqlCommand cmd = new SqlCommand(command, conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdProduct", product.IdProduct);
        cmd.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
        cmd.Parameters.AddWithValue("@Amount", product.Amount);
        cmd.Parameters.AddWithValue("@CreatedAt", product.CreatedAt);

        await conn.OpenAsync();
        int result = (int)await cmd.ExecuteScalarAsync();

        return result;
    }
}