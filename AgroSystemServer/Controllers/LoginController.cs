using AgroSystemServer.Data;
using AgroSystemServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroSystemServer.Services;
using System.Threading.Tasks;

namespace AgroSystemServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public LoginController(IConfiguration configuration, IEmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost("Autenticar")]
        public IActionResult Autenticar([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Usuario) || string.IsNullOrWhiteSpace(request?.Password))
            {
                return BadRequest(new { success = false, mensaje = "El usuario y la contraseña son obligatorios." });
            }

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@Usuario", request.Usuario)
            };

            DataTable dt = null;

            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("AutenticarUsuario", parametros);

                if (dt != null && dt.Rows.Count > 0)
                {
                    string hashGuardado = dt.Rows[0]["password_hash"].ToString();

                    if (BCrypt.Net.BCrypt.Verify(request.Password, hashGuardado))
                    {
                        var secretKey = _configuration["Jwt:Key"];
                        var issuer = _configuration["Jwt:Issuer"];
                        var audience = _configuration["Jwt:Audience"];
                        var keyBytes = Encoding.UTF8.GetBytes(secretKey);

                        var tokenDescriptor = new SecurityTokenDescriptor
                        {
                            Subject = new ClaimsIdentity(new[] {
                                new Claim(ClaimTypes.Name, request.Usuario),
                                new Claim(ClaimTypes.Role, dt.Rows[0]["Rol"].ToString())
                            }),
                            Expires = DateTime.UtcNow.AddHours(2),
                            Issuer = issuer,
                            Audience = audience,
                            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
                        };

                        var tokenHandler = new JwtSecurityTokenHandler();
                        var token = tokenHandler.CreateToken(tokenDescriptor);

                        return Ok(new
                        {
                            success = true,
                            mensaje = "Autenticación exitosa.",
                            token = tokenHandler.WriteToken(token),
                            data = new
                            {
                                id = Convert.ToInt32(dt.Rows[0]["UsuarioId"]),
                                nombre = dt.Rows[0]["Nombre"].ToString(),
                                username = dt.Rows[0]["Username"].ToString(),
                                rol = dt.Rows[0]["Rol"].ToString(),
                                rolId = Convert.ToInt32(dt.Rows[0]["RolId"])
                            }
                        });
                    }
                }

                return Unauthorized(new { success = false, mensaje = "Credenciales incorrectas." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
            finally
            {
                parametros?.Clear();
                dt?.Dispose();
            }
        }

        [HttpPost("Registrar")]
        public IActionResult Registrar([FromBody] RegistroRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Usuario) ||
                string.IsNullOrWhiteSpace(request?.Password) ||
                string.IsNullOrWhiteSpace(request?.Nombre))
            {
                return BadRequest(new { success = false, mensaje = "Todos los campos son obligatorios." });
            }

            string hashSeguro = BCrypt.Net.BCrypt.HashPassword(request.Password);

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@NombreCompleto", request.Nombre),
                new SqlParameter("@Username", request.Usuario),
                new SqlParameter("@PasswordHash", hashSeguro),
                new SqlParameter("@RolId", request.RolId)
            };

            try
            {
                int filasAfectadas = ClsConexion.EjecutarProcedimientoAccion("RegistrarUsuario", parametros);

                if (filasAfectadas > 0)
                {
                    return Ok(new { success = true, mensaje = "Usuario registrado exitosamente." });
                }

                return BadRequest(new { success = false, mensaje = "No se pudo registrar el usuario." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor." });
            }
            finally
            {
                parametros?.Clear();
            }
        }

        [HttpGet("GenerarHash")]
        public IActionResult GenerarHash(string Password)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(Password);
            return Ok(new { PasswordOriginal = Password, HashParaBaseDeDatos = hash });
        }

        [HttpPost("SolicitarRecuperacion")]
        public async Task<IActionResult> SolicitarRecuperacion([FromBody] SolicitarRecuperacionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Correo))
                return BadRequest(new { success = false, mensaje = "El correo es obligatorio." });

            string tokenGenerado = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@Correo", request.Correo),
                new SqlParameter("@Token", tokenGenerado)
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("GenerarTokenRecuperacion", parametros);

                await _emailService.EnviarCorreoRecuperacionAsync(request.Correo, tokenGenerado);

                return Ok(new
                {
                    success = true,
                    mensaje = "Se ha enviado un código de recuperación a tu correo."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost("RestablecerPassword")]
        public IActionResult RestablecerPassword([FromBody] RestablecerPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Token) || string.IsNullOrWhiteSpace(request?.NuevaPassword))
                return BadRequest(new { success = false, mensaje = "El token y la nueva contraseña son obligatorios." });

            string hashSeguro = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@Token", request.Token),
                new SqlParameter("@NuevoPasswordHash", hashSeguro)
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("RestablecerPassword", parametros);
                return Ok(new { success = true, mensaje = "Contraseña actualizada correctamente. Ya puedes iniciar sesión." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
        }
    }

    public class LoginRequest
    {
        public string? Usuario { get; set; }
        public string? Password { get; set; }
    }

    public class RegistroRequest
    {
        public string? Nombre { get; set; }
        public string? Usuario { get; set; }
        public string? Password { get; set; }
        public int RolId { get; set; }
    }

    public class SolicitarRecuperacionRequest
    {
        public string? Correo { get; set; }
    }

    public class RestablecerPasswordRequest
    {
        public string? Token { get; set; }
        public string? NuevaPassword { get; set; }
    }
}