using BuildingBlocks.ValidationBehavior;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var assembly = typeof(Program).Assembly;
//MediatR mediator design pattern for APIs - abstracts and encapsulates communication between classes through a mediator object
builder.Services.AddMediatR(config =>
{
  config.RegisterServicesFromAssemblies(assembly);
  config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
//Validation of client requests
builder.Services.AddValidatorsFromAssembly(assembly);
//Carter maps endpoints
builder.Services.AddCarter();
//Marten abstracts DB interaction
builder.Services.AddMarten(opts =>
{
  opts.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapCarter();

app.UseExceptionHandler(exceptionHandlerApp =>
{
  exceptionHandlerApp.Run(async context =>
  {
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (exception is null)
    {
      return;
    }
    var problemDetails = new ProblemDetails
    {
      Title = exception.Message,
      Status = StatusCodes.Status500InternalServerError,
      Detail = exception.StackTrace
    };
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogError(exception, exception.Message);

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";

    await context.Response.WriteAsJsonAsync(problemDetails);
  });
});

app.Run();
