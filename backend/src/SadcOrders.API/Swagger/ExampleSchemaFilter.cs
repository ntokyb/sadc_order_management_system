using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SadcOrders.API.Models;
using SadcOrders.Application.Dtos;
using SadcOrders.Domain.Enums;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SadcOrders.API.Swagger;

public class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(CreateCustomerRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("Acme Trading"),
                ["email"] = new OpenApiString("orders@acme.co.za"),
                ["countryCode"] = new OpenApiString("ZA")
            };
        }
        else if (context.Type == typeof(CreateOrderRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["customerId"] = new OpenApiString("11111111-1111-1111-1111-111111111111"),
                ["currencyCode"] = new OpenApiString("ZAR"),
                ["lineItems"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["productSku"] = new OpenApiString("SKU-ZA-001"),
                        ["quantity"] = new OpenApiInteger(2),
                        ["unitPrice"] = new OpenApiDouble(150)
                    }
                }
            };
        }
        else if (context.Type == typeof(UpdateOrderStatusRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["status"] = new OpenApiString("Paid")
            };
        }
        else if (context.Type == typeof(CustomerDto))
        {
            schema.Example = new OpenApiObject
            {
                ["id"] = new OpenApiString("11111111-1111-1111-1111-111111111111"),
                ["name"] = new OpenApiString("Acme Trading (ZA)"),
                ["email"] = new OpenApiString("acme@example.co.za"),
                ["countryCode"] = new OpenApiString("ZA"),
                ["createdAt"] = new OpenApiString("2026-05-19T10:00:00Z")
            };
        }
        else if (context.Type == typeof(OrderDto))
        {
            schema.Example = new OpenApiObject
            {
                ["id"] = new OpenApiString("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ["customerId"] = new OpenApiString("11111111-1111-1111-1111-111111111111"),
                ["customerName"] = new OpenApiString("Acme Trading (ZA)"),
                ["status"] = new OpenApiString(nameof(OrderStatus.Pending)),
                ["currencyCode"] = new OpenApiString("ZAR"),
                ["totalAmount"] = new OpenApiDouble(300),
                ["createdAt"] = new OpenApiString("2026-05-19T10:00:00Z"),
                ["lineItems"] = new OpenApiArray()
            };
        }
    }
}
