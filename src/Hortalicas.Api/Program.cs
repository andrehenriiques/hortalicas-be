using Hortalicas.Api.Data;
using Hortalicas.Api.Services;
using Microsoft.EntityFrameworkCore;

const string CorsFrontLocal = "FrontLocal";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("Hortalicas"));
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

builder.Services.AddCors(options =>
    options.AddPolicy(CorsFrontLocal, policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(CorsFrontLocal);

app.MapControllers();

app.Run();

public partial class Program { }
