using AgroSystemServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace AgroSystemServer.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        [HttpGet("Obtener")]
        public IActionResult ObtenerUsuarios([FromQuery] bool solo_activos = false)
        {
            try
            {
                DataTable dt = ClsConexion.EjecutarProcedimientoConsulta("dbo.ObtenerUsuarios");
                var usuarios = new List<Dictionary<string, object>>();

                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                    }
                    usuarios.Add(dict);
                }

                return Ok(new { success = true, data = usuarios });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost("Insertar")]
        public IActionResult InsertarUsuario([FromBody] UsuarioInsertarRequest request)
        {
            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                List<SqlParameter> parametros = new List<SqlParameter>
                {
                    new SqlParameter("@id_rol", request.IdRol),
                    new SqlParameter("@nombre_completo", request.NombreCompleto ?? (object)DBNull.Value),
                    new SqlParameter("@username", request.Username ?? (object)DBNull.Value),
                    new SqlParameter("@password_hash", passwordHash ?? (object)DBNull.Value),
                    new SqlParameter("@correo", request.Correo ?? (object)DBNull.Value)
                };

                ClsConexion.EjecutarProcedimientoAccion("dbo.InsertarUsuarios", parametros);
                return Ok(new { success = true, mensaje = "Usuario registrado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPut("Actualizar")]
        public IActionResult ActualizarUsuario([FromBody] UsuarioActualizarRequest request)
        {
            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                List<SqlParameter> parametros = new List<SqlParameter>
                {
                    new SqlParameter("@id_usuario", request.IdUsuario),
                    new SqlParameter("@id_rol", request.IdRol),
                    new SqlParameter("@nombre_completo", request.NombreCompleto ?? (object)DBNull.Value),
                    new SqlParameter("@username", request.Username ?? (object)DBNull.Value),
                    new SqlParameter("@correo", request.Correo ?? (object)DBNull.Value),
                    new SqlParameter("@estado", request.Estado ? 1 : 0),
                    new SqlParameter("@password_hash", string.IsNullOrEmpty(request.Password) ? (object)DBNull.Value : passwordHash)
                };

                ClsConexion.EjecutarProcedimientoAccion("dbo.ActualizarUsuarios", parametros);
                return Ok(new { success = true, mensaje = "Usuario actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpDelete("Desactivar/{id}")]
        public IActionResult DesactivarUsuario(int id)
        {
            try
            {
                List<SqlParameter> parametros = new List<SqlParameter>
                {
                    new SqlParameter("@id_usuario", id)
                };

                ClsConexion.EjecutarProcedimientoAccion("dbo.DesactivarUsuarios", parametros);
                return Ok(new { success = true, mensaje = "Usuario desactivado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }
    }

    public class UsuarioInsertarRequest
    {
        public int IdRol { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Correo { get; set; }
    }

    public class UsuarioActualizarRequest : UsuarioInsertarRequest
    {
        public int IdUsuario { get; set; }
        public bool Estado { get; set; }
    }
}