using RinhaDeBackend.Services;

var builder = WebApplication.CreateBuilder(args);


//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseNpgsql(Utils.ConnectionString));

// Add services to the container.

builder.Services.AddCors();
//builder.Services.AddScoped<ITransacaoService, TransacaoService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseRouting();

app.MapControllers();

app.Run();
