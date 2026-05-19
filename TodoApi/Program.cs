using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title       = "Todo API",
        Version     = "v1",
        Description = "A simple Todo REST API built with .NET 8 — used as a Docker & Kubernetes learning project.",
    });

    // Include XML comments for richer Swagger docs
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Enable Swagger in all environments (useful inside containers)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API v1");
    options.RoutePrefix = string.Empty; // serve Swagger UI at root "/"
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
