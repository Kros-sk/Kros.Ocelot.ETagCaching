# Ocelot ETag Caching

> Toto je len prvotný návrh, ktorý sa môže meniť.

## Úvod

Ocelot ETag Caching knižnica pridáva podporu pre ETag caching do Ocelot API Gateway. ETag caching je mechanizmus, ktorý umožňuje klientovi ukladať dáta do cache a pri ďalšej požiadavke na rovnaké dáta, klient môže overiť, či sú dáta stále aktuálne. Ak sú dáta stále aktuálne, server vráti status `304 Not Modified` a klient použije dáta z cache. Ak dáta nie sú aktuálne, server vráti dáta a klient ich uloží do cache.

Podstata je v tom, že server do odpovede pridá dve dôležité hlavičky:

- `ETag` - identifikátor dát (náhodne generovaná hodnota)
- `cache-control` - identifikátor, že je možné dáta ukladať do cache. Obsahuje len hodnotu `private` (⚠️ pozor nesmie byť `public`, pretože vtedy môžu dáta ostať kešované kdekoľvek.) Rovnako neobsahuje ani `max-age` pretože v tomto prípade by klient neoveroval dáta na serveri (po danú dobu. Občas to môže byť OK).

Klient (prehliadač) pokiaľ v odpovedi nájde tieto dve hlavičky, tak v ďalšej požiadavke na server s rovnakou cestou pridá hlavičku `If-None-Match` s hodnotou `ETag` a server na základe tejto hodnoty vie, či sú dáta stále aktuálne. Ak sú aktuálne tak vráti status `304 Not Modified` a klient použije dáta z cache.
**👌 Server neposiela dáta. Šetríme tým prostriedky a šírku pásma**

Na klientovi toto funguje automaticky, pretože toto správanie je definované v špecifikácii HTTP (nie je potrebné nič robiť).

## Implementácia

Implementácia je založená na Ocelot middleware. Celé kešovanie sa udeje v tomto middleware a na strane služieb nebude potrebné nič robiť. Kešovať sa nebudú samotné dáta, ale iba ich identifikátor (ETag), na základe ktorého sa bude overovať aktuálnosť dát.

Na uchovávanie ETagov a ich invalidovanie využijeme nový `IOutputCacheStore`.

## Použitie

### Ocelot konfigurácia

```json
{
    "Routes": [
        {
            "Key": "getAllProducts",
            "DownstreamPathTemplate": "/api/producsts/",
            "UpstreamPathTemplate": "/products/",
            "CachePolicy": "getAllProduct",
            ...
        },
        {
            "Key": "getProduct",
            "DownstreamPathTemplate": "/api/producsts/{id}",
            "UpstreamPathTemplate": "/products/{id}",
            "CachePolicy": "getProduct",
            ...
        },
        {
            "Key": "deleteProduct",
            "DownstreamPathTemplate": "/api/producsts/{id}",
            "UpstreamPathTemplate": "/products/{id}",
            "InvalidateCachePolicies": ["getProduct", "getAllProducts"],
            ...
        }
    ]
}
```

**❓ Poprosím o váš názor.**

### Konfigurácia služby

```csharp
builder.Services.AddOcelotETagCaching((c) =>
    {
        // 👇 Add ETag caching policies
        // 👇 Simple policy with Expire and tag templates
        c.AddPolicy("getAllProducts", p =>
        {
            p.Expire(TimeSpan.FromMinutes(5));
            p.TagTemplates("products:{tenantId}", "all", "tenantAll:{tenantId}");
        });

        // 👇 Policy with custom cache key, etag generator and custom cache control
        c.AddPolicy("getProduct", p =>
        {
            p.Expire(TimeSpan.FromMinutes(5));
            p.TagTemplates("product:{tenantId}:{id}", "tenant:{tenantId}:all", "all");

            p.CacheKey(context => context.Request.Query["id"]); // 👈 Custom cache key
            p.ETag(context => new($"\"{Guid.NewGuid()}\"")); // 👈 Custom etag
            p.CacheControl(context =>  new(){Public = false}); // 👈 Custom cache control
            p.StatusCode(222); // 👈 Custom status code
        });
    }
);

...

app.UseOcelot(c =>
{
    // 👇 Add etag caching middleware
    c.AddETagCaching();

    // or with custom post processing
    c.AddETagCaching(
        (context, cacheEntry, resposne) =>
            {
                // this handler is called when data is not found in cache
                response.Headers.Add(new ("X-Custom-Header", ["Custom value"]));
                cacheEntry.ExtraProps.Add("CustomData", "Custom value");
            },
        (context, cacheEntry, response) =>
            {
                // this handler is called when data is found in cache
                var customData = cacheEntry.ExtraProps["CustomData"];
                response.Headers.Add(new ("X-Custom-Header", customData));
            });
    }).Wait();

app.Run();
```

## Invalidácia

> Rozmýšľam nad tým, že spravím invalidáciu aj na úrovní Ocelotu. Pokiaľ sa jedná o `POST`, `PUT`, `DELETE` a `PATCH` požiadavku, tak sa invaliduje cache pokiaľ to má daná routa nastavené (musí mať zoznam tagov). Ale toto tu zatiaľ nerozoberám, pretože bude potrebná aj invaldácia na strane služby (dáta sa menia nie len požiadavkami cez gateway).

### Invalidácia na strane downstream služby

```csharp

public async Task DeleteProduct(TenantId tenantId, Guid id, CancellationToken cancellationToken)
{
    await _productRepository.DeleteProductAsync(tenantId, id, cancellationToken);
    // 👇 Cache invalidation
    // In project can exist some helper for this
    await store.EvictByTagAsync($"product:{tenantId}:{id}");
    await store.EvictByTagAsync($"products:{tenantId}");
}

```

## Redis

```csharp
builder.Services.AddStackExchangeRedisOutputCache(options =>
{
    options.Configuration = 
        builder.Configuration.GetConnectionString("MyRedisConStr");
    options.InstanceName = "SampleInstance";
});
```
