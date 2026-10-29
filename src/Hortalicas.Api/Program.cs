using System.Text.Json.Serialization;
using Hortalicas.Api.Data;
using Hortalicas.Api.Services;
using Microsoft.EntityFrameworkCore;

const string CorsFrontLocal = "FrontLocal";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SupportNonNullableReferenceTypes());

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Hortalicas")));
builder.Services.AddHostedService<MigrarBanco>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRelogio, Relogio>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IEstufaService, EstufaService>();
builder.Services.AddScoped<IHortalicaService, HortalicaService>();
builder.Services.AddScoped<IPlantioService, PlantioService>();
builder.Services.AddScoped<IIrrigacaoService, IrrigacaoService>();
builder.Services.AddScoped<IColheitaService, ColheitaService>();
builder.Services.AddScoped<IPainelService, PainelService>();

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
