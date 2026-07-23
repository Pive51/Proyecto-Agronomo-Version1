using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using AgroSystemServer.Data;

namespace AgroSystemServer.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProveedoresController : ControllerBase
    {
        [HttpGet("Obtener")]
        public IActionResult ObtenerProveedores()
        {
            try
            {
                DataTable dt = ClsConexion.EjecutarProcedimientoConsulta("ObtenerProveedores");

                var proveedores = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                    }
                    proveedores.Add(dict);
                }

                return Ok(new { success = true, data = proveedores });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost("Guardar")]
        public IActionResult GuardarProveedor([FromBody] ProveedorRequest req)
        {
            try
            {
                List<SqlParameter> parametros = new List<SqlParameter>
                {
                    new SqlParameter("@ProveedorId", req.ProveedorId ?? (object)DBNull.Value),
                    new SqlParameter("@RazonSocial", req.RazonSocial),
                    new SqlParameter("@RUC", req.RUC),
                    new SqlParameter("@Direccion", req.Direccion ?? (object)DBNull.Value),
                    new SqlParameter("@Telefono", req.Telefono ?? (object)DBNull.Value),
                    new SqlParameter("@Email", req.Email ?? (object)DBNull.Value)
                };

                ClsConexion.EjecutarProcedimientoAccion("GuardarProveedor", parametros);

                return Ok(new { success = true, mensaje = "Proveedor guardado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpDelete("Desactivar/{id}")]
        public IActionResult Desactivar(int id)
        {
            try
            {
                var parametros = new List<SqlParameter> { new SqlParameter("@ProveedorId", id) };
                ClsConexion.EjecutarProcedimientoAccion("dbo.DesactivarProveedor", parametros);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }
    }

    public class ProveedorRequest
    {
        public int? ProveedorId { get; set; }
        public string? RazonSocial { get; set; }
        public string? RUC { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
    }
}