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
    public class HistorialController : ControllerBase
    {
        // GET: api/Historial/Compras?fechaDesde=2026-06-01&fechaHasta=2026-07-08
        [HttpGet("Compras")]
        public IActionResult HistorialCompras([FromQuery] DateTime? fechaDesde = null, [FromQuery] DateTime? fechaHasta = null)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@fecha_desde", (object?)fechaDesde ?? DBNull.Value),
                new SqlParameter("@fecha_hasta", (object?)fechaHasta ?? DBNull.Value)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Compras_Historial", parametros);
                var compras = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    compras.Add(new
                    {
                        compraId = Convert.ToInt32(row["CompraId"]),
                        numeroFactura = row["NumeroFactura"].ToString(),
                        fechaCompra = Convert.ToDateTime(row["FechaCompra"]),
                        proveedor = row["Proveedor"].ToString(),
                        subtotal = Convert.ToDecimal(row["Subtotal"]),
                        iva = Convert.ToDecimal(row["Iva"]),
                        total = Convert.ToDecimal(row["Total"]),
                        estado = row["Estado"].ToString(),
                        observaciones = row["Observaciones"]?.ToString()
                    });
                }
                return Ok(new { success = true, data = compras });
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

        // GET: api/Historial/Compras/5/Detalle
        [HttpGet("Compras/{compraId}/Detalle")]
        public IActionResult DetalleCompra(int compraId)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@CompraId", compraId)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Compras_DetalleHistorial", parametros);
                var detalle = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    detalle.Add(new
                    {
                        compraDetalleId = Convert.ToInt32(row["CompraDetalleId"]),
                        producto = row["Producto"].ToString(),
                        cantidad = Convert.ToDecimal(row["Cantidad"]),
                        costoUnitario = Convert.ToDecimal(row["CostoUnitario"]),
                        subtotal = Convert.ToDecimal(row["Subtotal"]),
                        codigoLote = row["codigo_lote"]?.ToString(),
                        fechaElaboracion = row["fecha_elaboracion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["fecha_elaboracion"]),
                        fechaVencimiento = row["fecha_vencimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["fecha_vencimiento"]),
                        cantidadInicial = row["cantidad_inicial"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["cantidad_inicial"]),
                        stockRestante = row["stock_restante"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["stock_restante"])
                    });
                }
                return Ok(new { success = true, data = detalle });
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

        [HttpGet("Ventas")]
        public IActionResult HistorialVentas([FromQuery] DateTime? fechaDesde = null, [FromQuery] DateTime? fechaHasta = null, [FromQuery] bool soloAnuladas = false)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
    {
        new SqlParameter("@fecha_desde", (object?)fechaDesde ?? DBNull.Value),
        new SqlParameter("@fecha_hasta", (object?)fechaHasta ?? DBNull.Value),
        new SqlParameter("@solo_anuladas", soloAnuladas ? 1 : 0)
    };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Ventas_Historial", parametros);
                var ventas = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    ventas.Add(new
                    {
                        idVenta = Convert.ToInt32(row["id_venta"]),
                        fechaEmision = Convert.ToDateTime(row["fecha_emision"]),
                        cliente = row["Cliente"].ToString(),
                        identificacionCliente = row["identificacion"]?.ToString(),
                        emailCliente = row["correo_electronico"]?.ToString(),
                        subtotalIva0 = Convert.ToDecimal(row["subtotal_iva_0"]),
                        subtotalIva15 = Convert.ToDecimal(row["subtotal_iva_15"]),
                        montoIva = Convert.ToDecimal(row["monto_iva"]),
                        total = Convert.ToDecimal(row["total"]),
                        formaPago = row["forma_pago"].ToString(),
                        estadoSri = row["estado_sri"].ToString(),
                        motivoAnulacion = row["motivo_anulacion"]?.ToString(),
                        fechaAnulacion = row["fecha_anulacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["fecha_anulacion"])
                    });
                }
                return Ok(new { success = true, data = ventas });
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

        // GET: api/Historial/Ventas/5/Detalle
        [HttpGet("Ventas/{idVenta}/Detalle")]
        public IActionResult DetalleVenta(int idVenta)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@id_venta", idVenta)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Ventas_DetalleHistorial", parametros);
                var detalle = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    detalle.Add(new
                    {
                        idDetalle = Convert.ToInt32(row["id_detalle"]),
                        producto = row["Producto"].ToString(),
                        cantidad = Convert.ToDecimal(row["cantidad"]),
                        precioUnitario = Convert.ToDecimal(row["precio_unitario"]),
                        porcentajeIva = Convert.ToDecimal(row["porcentaje_iva"]),
                        descuento = Convert.ToDecimal(row["descuento"]),
                        subtotal = Convert.ToDecimal(row["subtotal"]),
                        codigoLote = row["codigo_lote"]?.ToString(),
                        fechaVencimiento = row["fecha_vencimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["fecha_vencimiento"])
                    });
                }
                return Ok(new { success = true, data = detalle });
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
    }
}