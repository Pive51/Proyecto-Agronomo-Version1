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
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {
        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            DataTable dt = null;

            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("CategoriasListar", null);

                var lista = new List<object>();

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        lista.Add(new
                        {
                            idCategoria = Convert.ToInt32(row["id_categoria"]),
                            nombre = row["nombre"].ToString(),
                            descripcion = row["descripcion"].ToString(),
                            estado = Convert.ToBoolean(row["estado"])
                        });
                    }
                }

                return Ok(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CategoriasController.Listar] " + ex.Message);
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor." });
            }
            finally
            {
                dt?.Dispose();
            }
        }

        [HttpPost("Agregar")]
        public IActionResult Agregar([FromBody] CategoriaRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Nombre))
                return BadRequest(new { success = false, mensaje = "El nombre es obligatorio." });

            var resultado = new SqlParameter("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            var mensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 200)
            {
                Direction = ParameterDirection.Output
            };

            var idGenerado = new SqlParameter("@id_categoria_generada", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            List<SqlParameter> parametros = new()
            {
                new SqlParameter("@nombre", request.Nombre),
                new SqlParameter("@descripcion",(object?)request.Descripcion ?? DBNull.Value),
                new SqlParameter("@estado",request.Estado),

                resultado,
                mensaje,
                idGenerado
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("CategoriasAgregar", parametros);

                bool ok = Convert.ToBoolean(resultado.Value);
                string msj = mensaje.Value?.ToString() ?? "";

                if (ok)
                {
                    return Ok(new
                    {
                        success = true,
                        mensaje = msj,
                        idCategoria = Convert.ToInt32(idGenerado.Value)
                    });
                }

                return BadRequest(new { success = false, mensaje = msj });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CategoriasController.Agregar] " + ex.Message);
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor." });
            }
        }

        [HttpPut("Modificar")]
        public IActionResult Modificar([FromBody] CategoriaUpdateRequest request)
        {
            if (request == null || request.IdCategoria <= 0 || string.IsNullOrWhiteSpace(request.Nombre))
                return BadRequest(new { success = false, mensaje = "Datos inválidos." });

            var resultado = new SqlParameter("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            var mensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 200)
            {
                Direction = ParameterDirection.Output
            };

            List<SqlParameter> parametros = new()
            {
                new SqlParameter("@id_categoria",request.IdCategoria),
                new SqlParameter("@nombre",request.Nombre),
                new SqlParameter("@descripcion",(object?)request.Descripcion ?? DBNull.Value),
                new SqlParameter("@estado",request.Estado),

                resultado,
                mensaje
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("CategoriasUpdate", parametros);

                bool ok = Convert.ToBoolean(resultado.Value);
                string msj = mensaje.Value?.ToString() ?? "";

                if (ok)
                    return Ok(new { success = true, mensaje = msj });

                return BadRequest(new { success = false, mensaje = msj });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CategoriasController.Modificar] " + ex.Message);
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor." });
            }
        }

        [HttpDelete("Eliminar/{idCategoria}")]
        public IActionResult Eliminar(int idCategoria)
        {
            if (idCategoria <= 0)
                return BadRequest(new { success = false, mensaje = "Id inválido." });

            var resultado = new SqlParameter("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            var mensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 200)
            {
                Direction = ParameterDirection.Output
            };

            List<SqlParameter> parametros = new()
            {
                new SqlParameter("@id_categoria",idCategoria),
                resultado,
                mensaje
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("CategoriasDelete", parametros);

                bool ok = Convert.ToBoolean(resultado.Value);
                string msj = mensaje.Value?.ToString() ?? "";

                if (ok)
                    return Ok(new { success = true, mensaje = msj });

                return BadRequest(new { success = false, mensaje = msj });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CategoriasController.Eliminar] " + ex.Message);
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor." });
            }
        }
    }

    public class CategoriaRequest
    {
        public string? Nombre { get; set; }

        public string? Descripcion { get; set; }

        public bool Estado { get; set; } = true;
    }

    public class CategoriaUpdateRequest : CategoriaRequest
    {
        public int IdCategoria { get; set; }
    }
}