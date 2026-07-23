using AgroSystemServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AgroSystemServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AlertasController : ControllerBase
    {
        // GET: api/Alertas/proximos-vencer?dias=30
        [HttpGet("proximos-vencer")]
        public IActionResult ProximosAVencer([FromQuery] int dias = 30)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@dias_alerta", dias)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Lotes_ProximosAVencer", parametros);
                var lotes = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    lotes.Add(new
                    {
                        idLote = Convert.ToInt32(row["id_lote"]),
                        producto = row["producto"].ToString(),
                        codigoLote = row["codigo_lote"].ToString(),
                        fechaVencimiento = Convert.ToDateTime(row["fecha_vencimiento"]),
                        stockRestante = Convert.ToDecimal(row["stock_restante"]),
                        diasRestantes = Convert.ToInt32(row["dias_restantes"])
                    });
                }
                return Ok(new { success = true, data = lotes });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Error interno: " + ex.Message });
            }
            finally
            {
                parametros?.Clear();
                dt?.Dispose();
            }
        }

        // GET: api/Alertas/stock-bajo
        [HttpGet("stock-bajo")]
        public IActionResult StockBajo()
        {
            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Productos_StockBajo");
                var productos = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    productos.Add(new
                    {
                        idProducto = Convert.ToInt32(row["id_producto"]),
                        nombre = row["nombre"].ToString(),
                        stockActual = Convert.ToDecimal(row["stock_actual"]),
                        stockMinimo = Convert.ToDecimal(row["stock_minimo"]),
                        cantidadFaltante = Convert.ToDecimal(row["cantidad_faltante"])
                    });
                }
                return Ok(new { success = true, data = productos });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Error interno: " + ex.Message });
            }
            finally
            {
                dt?.Dispose();
            }
        }
    }
}