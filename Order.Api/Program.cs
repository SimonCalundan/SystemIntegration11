using Microsoft.EntityFrameworkCore;
using Orders.Api.data;
using Orders.Api.Models;
using Orders.Api.Services;
using Scalar.AspNetCore;
using Shipping.Api.Services;

namespace Orders.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.AddRabbitMQClient("messaging");
        builder.AddNpgsqlDbContext<OrdersContext>("ordersdb");
        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddSingleton<IShippingMessageSender, ShippingMessageSender>();
        builder.Services.AddHostedService<OutboxProcessor>();
        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();
        if (app.Environment.IsDevelopment())
        {
            // Ensure database is created and seeded
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OrdersContext>();
            await context.Database.EnsureCreatedAsync();
        }


        app.Run();
    }
}
