using AgroSystemServer;
using AgroSystemServer.Data;
using AgroSystemServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuración de JWT Key
var jwtKet = builder.Configuration["Jwt:Key"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKet!);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// ==========================================
// 🔓 CONFIGURACIÓN DE CORS (PERMISIVA)
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Permite localhost, Render, Swagger, etc.
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configuración de Autenticación JWT
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

// Inicializar conexión a base de datos
ClsConexion.Inicializar(app.Configuration);

// ==========================================
// 🚀 SWAGGER / OPENAPI EN PRODUCCIÓN
// ==========================================
app.MapOpenApi(); // Genera el JSON en /openapi/v1.json

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "AgroSystemServer v1");
    options.RoutePrefix = string.Empty; // Swagger abrirá directamente en la URL raíz https://agrosystemv1.onrender.com/
});

// ==========================================
// 🌐 MIDDLEWARES Y RUTAS
// ==========================================
// CORS debe ir ANTES de la redirección HTTPS y de la autenticación
app.UseCors("AllowReact");

app.UseHttpsRedirection();

// ==========================================
// 📂 CONFIGURACIÓN DE CARPETA DE IMÁGENES
// ==========================================
var imagenesPath = Path.Combine(builder.Environment.ContentRootPath, "Imagenes");
if (!Directory.Exists(imagenesPath))
{
    Directory.CreateDirectory(imagenesPath);
}

app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagenesPath),
    RequestPath = "/Imagenes"
});

// ==========================================
// 🔐 AUTENTICACIÓN Y CONTROLADORES
// ==========================================
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();