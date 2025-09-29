using Microsoft.EntityFrameworkCore;
using ORM;
using StealAllTheCatsWebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddScoped<ICatSyncService, CatSyncService>();

builder.Services.AddDbContext<StealTheCatsContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddHttpClient("CatApi", (sp, http) => {
	var cfg = sp.GetRequiredService<IConfiguration>();
	http.BaseAddress = new Uri(cfg["CatApi:BaseUrl"]!);
	var apiKey = cfg["CatApi:ApiKey"];
	if (!string.IsNullOrWhiteSpace(apiKey))
		http.DefaultRequestHeaders.Add("x-api-key", apiKey);
});
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
