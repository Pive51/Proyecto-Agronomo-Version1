using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using AgroSystemServer.Data;
using Microsoft.AspNetCore.Authorization;

namespace AgroSystemServer.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        [HttpGet("Obtener")]
        public IActionResult ObtenerClientes([FromQuery] bool solo_activos = false)
        {
            try
            {
                List<SqlParameter> parametros = new List<SqlParameter>
                {
                    new SqlParameter("@solo_activos", solo_activos ? 1 : 0)
                };

                DataTable dt = ClsConexion.EjecutarProcedimientoConsulta("dbo.ObtenerClientes", parametros);

                var clientes = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                    }
                    clientes.Add(dict);
                }

                return Ok(new { success = true, data = clientes });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost("Insertar")]
        public IActionResult InsertarCliente([FromBody] ClienteInsertarRequest request)
        {
            try
            {
                List<SqlParameter> parametros = new List<SqlParameter>
                {
                    new SqlParameter("@tipo_identificacionId", request.TipoIdentificacionId),
                    new SqlParameter("@identificacion", request.Identificacion ?? (object)DBNull.Value),
                    new SqlParameter("@razon_social", request.RazonSocial ?? (object)DBNull.Value),
                    new SqlParameter("@direccion", request.Direccion ?? (object)DBNull.Value),
                    new SqlParameter("@telefono", request.Telefono ?? (object)DBNull.Value),
                    new SqlParameter("@correo_electronico", request.CorreoElectronico ?? (object)DBNull.Value)};

                ClsConexion.EjecutarProcedimientoAccion("dbo.InsertarClientes", parametros);

                return Ok(new { success = true, mensaje = "Cliente registrado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPut("Actualizar")]
        public IActionResult ActualizarCliente([FromBody] ClienteActualizarRequest request)
        {
            try
            {
                List<SqlParameter> parametros = new List<SqlParameter>
                {
                    new SqlParameter("@id_cliente", request.IdCliente),
                    new SqlParameter("@tipo_identificacionId", request.TipoIdentificacionId), // Ahora es entero
                    new SqlParameter("@identificacion", request.Identificacion ?? (object)DBNull.Value),
                    new SqlParameter("@razon_social", request.RazonSocial ?? (object)DBNull.Value),
                    new SqlParameter("@direccion", request.Direccion ?? (object)DBNull.Value),
                    new SqlParameter("@telefono", request.Telefono ?? (object)DBNull.Value),
                    new SqlParameter("@correo_electronico", request.CorreoElectronico ?? (object)DBNull.Value),
                    new SqlParameter("@estado", request.Estado ? 1 : 0)
                };

                ClsConexion.EjecutarProcedimientoAccion("dbo.ActualizarClientes", parametros);

                return Ok(new { success = true, mensaje = "Cliente actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpDelete("Desactivar/{id}")]
        public IActionResult DesactivarCliente(int id)
        {
            try
            {
                List<SqlParameter> parametros = new List<SqlParameter>
                {
                    new SqlParameter("@id_cliente", id)
                };

                ClsConexion.EjecutarProcedimientoAccion("dbo.DesactivarClientes", parametros);

                return Ok(new { success = true, mensaje = "Cliente desactivado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }
    }

    public class ClienteInsertarRequest
    {
        public int TipoIdentificacionId { get; set; }
        public string? Identificacion { get; set; }
        public string? RazonSocial { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }
    }

    public class ClienteActualizarRequest : ClienteInsertarRequest
    {
        public int IdCliente { get; set; }
        public bool Estado { get; set; }
    }
}