using BuildingBlocks.Exceptions.Handler;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var assembly = typeof(Program).Assembly;
//Carter maps endpoints
builder.Services.AddCarter();
//MediatR mediator design pattern for APIs - abstracts and encapsulates communication between API classes through a mediator object
builder.Services.AddMediatR(config =>
{
  config.RegisterServicesFromAssembly(assembly);
  config.AddOpenBehavior(typeof(ValidationBehavior<,>));
  config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

//Marten abstracts DB interaction
builder.Services.AddMarten(opts =>
{
  opts.Connection(builder.Configuration.GetConnectionString("Database")!);
  opts.Schema.For<ShoppingCart>().Identity(x => x.UserName!);
}).UseLightweightSessions();

//Register the services needed for the repository and the cache
builder.Services.AddScoped<IBasketRepository, BasketRepository>();
builder.Services.Decorate<IBasketRepository, CachedBasketRepository>();

//Register cache service
builder.Services.AddStackExchangeRedisCache(options =>
{
  options.Configuration = builder.Configuration.GetConnectionString("Redis");
  //options.InstanceName = "Basket";
});

builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.Services.AddHealthChecks()
  .AddNpgSql(builder.Configuration.GetConnectionString("Database")!)
  .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapCarter();
app.UseExceptionHandler(options => { });
app.UseHealthChecks("/health", 
  new HealthCheckOptions
  {
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
  }
);

app.Run();
