using Kros.Ocelot.ETagCaching;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOcelot();
builder.Services.AddSwaggerForOcelot(builder.Configuration);

builder.Services.AddOcelotETagCaching(conf =>
{
    conf.AddPolicy("getProducts", builder =>
    {
        builder.Expire(TimeSpan.FromMinutes(2));
        builder.TagTemplates("product:{tenantId}", "all:{tenantId}");
    });

    conf.AddPolicy("getProduct", builder =>
    {
        builder.Expire(TimeSpan.FromMinutes(10));
        builder.TagTemplates("product:{tenantId}", "product:{tenantId}:{id}", "all:{tenantId}");
    });
});

var app = builder.Build();

app.UseSwaggerForOcelotUI();

app.UseOcelot(c =>
{
    c.AddETagCaching();
}).Wait();

app.Run();
