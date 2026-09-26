using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SagaOrchestrator.Models;

namespace SagaOrchestrator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SagaController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SagaController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessOrder([FromBody] Order order)
    {
        order.Status = "Pending";

        // Step 1: Create Order
        var orderResponse = await CreateOrder(order);
        if (!orderResponse.IsSuccessStatusCode)
            return BadRequest("Order creation failed.");

        var createdOrder = await orderResponse.Content.ReadFromJsonAsync<Order>();
        if (createdOrder is null)
            return BadRequest("Order service returned an invalid response.");

        order.Id = createdOrder.Id;
        order.Status = createdOrder.Status;

        // Step 2: Reserve Inventory
        var inventoryResponse = await ReserveInventory(order);
        if (!inventoryResponse.IsSuccessStatusCode)
        {
            await CancelOrder(order.Id);
            return BadRequest("Inventory reservation failed.");
        }

        // Step 3: Process Payment
        var paymentResponse = await ProcessPayment(order);
        if (!paymentResponse.IsSuccessStatusCode)
        {
            await ReleaseInventory(order.ProductId, order.Quantity);
            await CancelOrder(order.Id);
            return BadRequest("Payment processing failed.");
        }

        order.Status = "Completed";
        return Ok(order);
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient();

    private async Task<HttpResponseMessage> CreateOrder(Order order)
    {
        return await CreateClient().PostAsJsonAsync(
            "http://localhost:5001/api/orders", order);
    }

    private async Task<HttpResponseMessage> ReserveInventory(Order order)
    {
        return await CreateClient().PostAsJsonAsync(
            "http://localhost:5002/api/inventory/reserve",
            new { order.ProductId, order.Quantity });
    }

    private async Task<HttpResponseMessage> ProcessPayment(Order order)
    {
        return await CreateClient().PostAsJsonAsync(
            "http://localhost:5003/api/payments",
            new { order.Id, Amount = order.Quantity * 10m });
    }

    private async Task<HttpResponseMessage> CancelOrder(int orderId)
    {
        return await CreateClient().DeleteAsync(
            $"http://localhost:5001/api/orders/{orderId}");
    }

    private async Task<HttpResponseMessage> ReleaseInventory(string productId, int quantity)
    {
        return await CreateClient().PostAsJsonAsync(
            "http://localhost:5002/api/inventory/release",
            new { productId, quantity });
    }
}
