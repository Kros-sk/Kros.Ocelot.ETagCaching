using AutoBogus;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IProductRepository, DummyProductRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "Hello World!");

app.MapGet("/{tenantId}/products", async (int tenantId, [FromServices] IProductRepository repository) =>
{
    var products = await repository.GetAllProductsAsync(tenantId);
    return products;
});

app.MapGet("/{tenantId}/products/{id}", async (int tenantId, int id, [FromServices] IProductRepository repository) =>
{
    var product = await repository.GetProductByIdAsync(tenantId, id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

app.MapPost("/{tenantId}/products", async (
    int tenantId,
    [FromBody] Product product,
    [FromServices] IProductRepository repository) =>
{
    product.TenantId = tenantId;
    await repository.AddProductAsync(product);
    return Results.Created($"/{tenantId}/products/{product.Id}", product);
});

app.MapPut("/{tenantId}/products/{id}", async (int tenantId,
    int id,
    [FromBody] Product product,
    [FromServices] IProductRepository repository) =>
{
    product.TenantId = tenantId;
    product.Id = id;
    await repository.EditProductAsync(product);
    return Results.Ok(product);
});

app.Run();

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllProductsAsync(int tenantId);

    Task<Product?> GetProductByIdAsync(int tenantId, int id);

    Task AddProductAsync(Product product);

    Task EditProductAsync(Product product);
}

public class DummyProductRepository : IProductRepository
{
    private readonly List<Product> _products;

    public DummyProductRepository()
    {
        var id = 1;
        var faker = new AutoFaker<Product>()
            .RuleFor(p => p.TenantId, _ => 1)
            .RuleFor(p => p.Id, _ => id++)
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Category, f => f.Commerce.Categories(1).First())
            .RuleFor(p => p.Price, f => f.Random.Decimal(1, 1000));

        _products = faker.Generate(100);
    }

    public Task<IEnumerable<Product>> GetAllProductsAsync(int tenantId)
        => Task.FromResult(_products.Where(p => p.TenantId == tenantId));

    public Task<Product?> GetProductByIdAsync(int tenantId, int id)
        => Task.FromResult(_products.FirstOrDefault(p => p.TenantId == tenantId && p.Id == id));

    public Task AddProductAsync(Product product)
    {
        product.Id = _products.Count + 1;
        _products.Add(product);

        return Task.CompletedTask;
    }

    public Task EditProductAsync(Product product)
    {
        var existingProduct = _products.FirstOrDefault(p => p.TenantId == product.TenantId && p.Id == product.Id);
        if (existingProduct is null)
        {
            throw new InvalidOperationException("Product not found");
        }

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Category = product.Category;
        existingProduct.Price = product.Price;

        return Task.CompletedTask;
    }
}

public class Product
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTime DateTime => DateTime.Now;
}
