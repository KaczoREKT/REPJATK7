using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseController : ControllerBase
    {
        private readonly string _connectionString;

        public WarehouseController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("addProduct")]
        public IActionResult AddProduct([FromBody] ProductRequest request)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Sprawdzamy, czy produkt istnieje
                        var cmd = new SqlCommand("SELECT COUNT(1) FROM Product WHERE Id = @ProductId", connection, transaction);
                        cmd.Parameters.AddWithValue("@ProductId", request.ProductId);
                        if ((int)cmd.ExecuteScalar() == 0)
                        {
                            return NotFound("Product not found");
                        }

                        // Sprawdzamy, czy magazyn istnieje
                        cmd = new SqlCommand("SELECT COUNT(1) FROM Warehouse WHERE Id = @WarehouseId", connection, transaction);
                        cmd.Parameters.AddWithValue("@WarehouseId", request.WarehouseId);
                        if ((int)cmd.ExecuteScalar() == 0)
                        {
                            return NotFound("Warehouse not found");
                        }

                        // Dalsza logika

                        transaction.Commit();
                        return Ok();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, ex.Message);
                    }
                }
            }
        }

        [HttpPost("addProductWithProc")]
        public IActionResult AddProductWithProc([FromBody] ProductRequest request)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                try
                {
                    var cmd = new SqlCommand("ExecuteAddProduct", connection);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductId", request.ProductId);
                    cmd.Parameters.AddWithValue("@WarehouseId", request.WarehouseId);
                    cmd.Parameters.AddWithValue("@Amount", request.Amount);
                    cmd.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

                    var result = cmd.ExecuteScalar();
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }
        }

        private decimal GetProductPrice(int productId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand("SELECT Price FROM Product WHERE Id = @ProductId", connection);
                cmd.Parameters.AddWithValue("@ProductId", productId);
                return (decimal)cmd.ExecuteScalar();
            }
        }
    }

    public class ProductRequest
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}