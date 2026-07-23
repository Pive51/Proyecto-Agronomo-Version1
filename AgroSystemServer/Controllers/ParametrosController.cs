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
    public class ParametrosController : ControllerBase
    {
        [HttpGet("Obtener")]
        public IActionResult Obtener()
        {
            try
            {
                DataTable dt = ClsConexion.EjecutarProcedimientoConsulta("dbo.ObtenerParametrosEmpresa", new List<SqlParameter>());

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    var parametros = new
                    {
                        ruc = row["ruc"].ToString(),
                        razonSocial = row["razon_social"].ToString(),
                        nombreComercial = row["nombre_comercial"] == DBNull.Value ? "" : row["nombre_comercial"].ToString(),
                        direccion = row["direccion"].ToString(),
                        telefono = row["telefono"] == DBNull.Value ? "" : row["telefono"].ToString(),
                        correo = row["correo"] == DBNull.Value ? "" : row["correo"].ToString(),
                        ambienteSri = row["ambiente_sri"].ToString(),
                        obligadoContabilidad = row["obligado_contabilidad"].ToString(),
                        porcentajeIva = Convert.ToInt32(row["porcentaje_iva"]),
                        moneda = row["moneda"].ToString(),
                        impresora = row["impresora"].ToString()
                    };

                    return Ok(new { success = true, data = parametros });
                }

                return NotFound(new { success = false, mensaje = "No se encontraron parámetros de configuración." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPut("Actualizar")]
        public IActionResult Actualizar([FromBody] ParametrosActualizarRequest request)
        {
            try
            {
                List<SqlParameter> parametros = new List<SqlParameter>
                {
                    new SqlParameter("@ruc", request.Ruc ?? (object)DBNull.Value),
                    new SqlParameter("@razon_social", request.RazonSocial ?? (object)DBNull.Value),
                    new SqlParameter("@nombre_comercial", request.NombreComercial ?? (object)DBNull.Value),
                    new SqlParameter("@direccion", request.Direccion ?? (object)DBNull.Value),
                    new SqlParameter("@telefono", request.Telefono ?? (object)DBNull.Value),
                    new SqlParameter("@correo", request.Correo ?? (object)DBNull.Value),
                    new SqlParameter("@ambiente_sri", request.AmbienteSri ?? "1"),
                    new SqlParameter("@obligado_contabilidad", request.ObligadoContabilidad ?? "NO"),
                    new SqlParameter("@porcentaje_iva", request.PorcentajeIva),
                    new SqlParameter("@moneda", request.Moneda ?? "USD"),
                    new SqlParameter("@impresora", request.Impresora ?? "Termica80mm")
                };

                ClsConexion.EjecutarProcedimientoAccion("dbo.ActualizarParametrosEmpresa", parametros);

                return Ok(new { success = true, mensaje = "Configuración del establecimiento actualizada correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }
    }

    public class ParametrosActualizarRequest
    {
        public string? Ruc { get; set; }
        public string? RazonSocial { get; set; }
        public string? NombreComercial { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? AmbienteSri { get; set; }
        public string? ObligadoContabilidad { get; set; }
        public int PorcentajeIva { get; set; }
        public string? Moneda { get; set; }
        public string? Impresora { get; set; }
    }
}