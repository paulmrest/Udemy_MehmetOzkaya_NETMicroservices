var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//Carter maps endpoints
builder.Services.AddCarter();
//MediatR mediator design pattern for APIs - abstracts and encapsulates communication between classes through a mediator object
builder.Services.AddMediatR(config =>
{
  config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
});
//Marten abstracts DB interaction
builder.Services.AddMarten(opts =>
{
  opts.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapCarter();

app.Run();
