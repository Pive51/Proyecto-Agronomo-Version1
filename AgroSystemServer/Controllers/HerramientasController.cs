using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using AgroSystemServer.Data; 

namespace AgroSystemServer.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HerramientasController : ControllerBase
    {
        [HttpGet("UltimoRespaldo")]
        public IActionResult ObtenerUltimoRespaldo()
        {
            try
            {
                DataTable dt = ClsConexion.EjecutarProcedimientoConsulta("dbo.ObtenerUltimoRespaldo", new List<SqlParameter>());

                if (dt.Rows.Count > 0)
                {
                    DateTime fecha = Convert.ToDateTime(dt.Rows[0]["fecha_respaldo"]);
                    return Ok(new { success = true, fecha = fecha.ToString("dd MMM yyyy, HH:mm") });
                }

                return Ok(new { success = true, fecha = "Nunca se ha respaldado" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpGet("Backup")]
        public IActionResult GenerarBackup()
        {
            try
            {
                string backupFolder = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                string fileName = $"AgroSystemBackup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                string fullPath = Path.Combine(backupFolder, fileName);

                List<SqlParameter> parametros = new List<SqlParameter>
                {
                    new SqlParameter("@RutaArchivo", fullPath),
                    new SqlParameter("@NombreArchivo", fileName)
                };

                ClsConexion.EjecutarProcedimientoAccion("dbo.GenerarRespaldoBaseDatos", parametros);

                byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);

                System.IO.File.Delete(fullPath);

                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = $"Error al generar el respaldo: {ex.Message}" });
            }
        }
    }
}