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
                "http://localhost:3000",
                "https://*.onrender.com" // Permite solicitudes desde tus subdominios en Render
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
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

// ==========================================
// 💡 HABILITADO PARA TODOS LOS ENTORNOS (Dev y Prod)
// ==========================================
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "AgroSystemServer v1");
    options.RoutePrefix = "swagger"; // La documentación abrirá en /swagger
});

// Endpoint raíz de comprobación (Health Check) para evitar el 404 al entrar a la URL base
app.MapGet("/", () => Results.Ok(new 
{ 
    status = "AgroSystem Server API Activa 🚀", 
    timestamp = DateTime.UtcNow 
}));

app.UseHttpsRedirection();

app.UseCors("AllowReact");

// ==========================================
// 💡 CONFIGURACIÓN PARA SERVIR LAS IMÁGENES
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();