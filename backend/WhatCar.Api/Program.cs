using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using WhatCar.Api.Endpoints;
using WhatCar.Api.Services;
using WhatCar.ODataCore.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<VehicleSalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("whatcardb")));

builder.Services.AddSingleton<PromptBuilder>();
builder.Services.AddSingleton<Microsoft.OData.Edm.IEdmModel>(_ => ODataApiModelBuilder.GetEdmModel());
builder.Services.AddSingleton(_ =>
    new Azure.AI.OpenAI.AzureOpenAIClient(
        new Uri(builder.Configuration["Llm:Endpoint"] ?? throw new InvalidOperationException("Missing Llm:Endpoint")),
        new Azure.AzureKeyCredential(builder.Configuration["Llm:ApiKey"] ?? throw new InvalidOperationException("Missing Llm:ApiKey"))
    )
);
builder.Services.AddSingleton(_ =>
    builder.Configuration["Llm:Model"] ?? throw new InvalidOperationException("Missing Llm:Model")
);
builder.Services.AddScoped<LlmClient>();
builder.Services.AddHttpClient<ODataExecutor>(client =>
{
    var baseUrl = builder.Configuration["WHATCAR-ODATA_HTTPS"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(120); // Overall timeout - increased for complex aggregations
})
.AddStandardResilienceHandler(options =>
{
    // Configure timeout for OData queries with $apply aggregations which can be slow
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(90);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);

    // Minimize retries - OData queries are slow and retrying makes it worse
    options.Retry.MaxRetryAttempts = 1;
    options.Retry.UseJitter = true;

    // Configure circuit breaker - sampling duration must be at least 2x attempt timeout
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(180);
});

builder.AddRedisDistributedCache("redis");

builder.Services.AddScoped<SchemaSummarizer>();

builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development with Aspire, allow any localhost origin
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseResponseCompression();
app.UseResponseCaching();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
        .ExcludeFromDescription();
}

app.MapQueryEndpoints();

await app.RunAsync();