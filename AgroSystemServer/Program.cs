using AgroSystemServer;
using AgroSystemServer.Data;
using AgroSystemServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi; // 💡 OpenApiServer está directamente aquí en .NET 9/10
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuración de JWT Key
var jwtKet = builder.Configuration["Jwt:Key"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKet!);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IEmailService, EmailService>();

// ==========================================
// 🚀 CONFIGURACIÓN DE OPENAPI / SWAGGER FORZANDO HTTPS
// ==========================================
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

    // Forzar a Swagger a usar la URL HTTPS de Render
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = "https://agrosystemv1.onrender.com" }
        };
        return Task.CompletedTask;
    });
});

// ==========================================
// 🔓 CONFIGURACIÓN DE CORS
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
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

// Reenvío de encabezados HTTPS por el proxy de Render
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ==========================================
// 🚀 SWAGGER / OPENAPI EN PRODUCCIÓN
// ==========================================
app.MapOpenApi(); // Genera el JSON en /openapi/v1.json

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "AgroSystemServer v1");
    options.RoutePrefix = string.Empty;
});

// ==========================================
// 🌐 MIDDLEWARES
// ==========================================
app.UseCors("AllowReact");

app.UseHttpsRedirection();

// ==========================================
// 📂 CARPETA DE IMÁGENES
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