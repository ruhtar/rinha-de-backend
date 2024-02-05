using Microsoft.EntityFrameworkCore;
using RinhaDeBackend.Data;
using RinhaDeBackend.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = "Host=db;Port=5432;Database=rinha;Username=postgres;Password=123";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.

builder.Services.AddScoped<ITransacaoService, TransacaoService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
