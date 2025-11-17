using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Airline Web API",
        Version = "v1",
        Description = "CIS 3342 – Airline Web API Project (Fall 2025)"
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Access-Control-Allow-Origin", policy =>
    {
        policy.AllowAnyOrigin()
              .WithOrigins("http://www.temple.edu")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
}

app.UseCors("Access-Control-Allow-Origin");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
   // c.SwaggerEndpoint("/swagger/v1/swagger.json", "Airline Web API v1");
    c.SwaggerEndpoint("/Fall2025/CIS3342_tuo90411/WebAPI/swagger/v1/swagger.json", "Airline Web API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
