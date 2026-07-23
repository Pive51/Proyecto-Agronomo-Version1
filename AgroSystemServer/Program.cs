using AgroSystemServer;
using AgroSystemServer.Data;
using AgroSystemServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.FileProviders; // 💡 Asegúrate de tener este using para la carpeta física
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtKet = builder.Configuration["Jwt:Key"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKet!);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(
                "http://localhost:61517",
                "http://localhost:5173",
                "http://localhost:3000"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(option =>
    {
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

ClsConexion.Inicializar(app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "AgroSystemServer v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowReact");

// ==========================================
// 💡 CONFIGURACIÓN PARA SERVIR LAS IMÁGENES
// ==========================================
// 1. Esto permite servir archivos estáticos estándar (como wwwroot si existiera)
app.UseStaticFiles();

// 2. Esto expone tu carpeta física "Imagenes" bajo la ruta web "/Imagenes"
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Imagenes")),
    RequestPath = "/Imagenes"
});
// ==========================================

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();