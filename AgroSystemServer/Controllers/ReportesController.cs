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
    public class ReportesController : ControllerBase
    {
        // GET: api/Reportes/Mermas?fechaDesde=2026-01-01&fechaHasta=2026-12-31
        [HttpGet("Mermas")]
        public IActionResult Mermas([FromQuery] DateTime? fechaDesde = null, [FromQuery] DateTime? fechaHasta = null)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@fecha_desde", (object?)fechaDesde ?? DBNull.Value),
                new SqlParameter("@fecha_hasta", (object?)fechaHasta ?? DBNull.Value)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Reportes_MermasPorCaducidad", parametros);
                var mermas = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    mermas.Add(new
                    {
                        idLote = Convert.ToInt32(row["id_lote"]),
                        producto = row["producto"].ToString(),
                        codigoLote = row["codigo_lote"].ToString(),
                        fechaVencimiento = Convert.ToDateTime(row["fecha_vencimiento"]),
                        stockRestante = Convert.ToDecimal(row["stock_restante"]),
                        precioCompra = Convert.ToDecimal(row["precio_compra"]),
                        valorPerdida = Convert.ToDecimal(row["valor_perdida"])
                    });
                }
                return Ok(new { success = true, data = mermas });
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

        // GET: api/Reportes/UtilidadPorProducto?fechaDesde=2026-01-01&fechaHasta=2026-12-31
        [HttpGet("UtilidadPorProducto")]
        public IActionResult UtilidadPorProducto([FromQuery] DateTime? fechaDesde = null, [FromQuery] DateTime? fechaHasta = null)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@fecha_desde", (object?)fechaDesde ?? DBNull.Value),
                new SqlParameter("@fecha_hasta", (object?)fechaHasta ?? DBNull.Value)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Reportes_UtilidadPorProducto", parametros);
                var utilidad = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    utilidad.Add(new
                    {
                        idProducto = Convert.ToInt32(row["id_producto"]),
                        producto = row["producto"].ToString(),
                        cantidadVendida = Convert.ToDecimal(row["cantidad_vendida"]),
                        totalVentas = Convert.ToDecimal(row["total_ventas"]),
                        costoTotal = Convert.ToDecimal(row["costo_total"]),
                        utilidad = Convert.ToDecimal(row["utilidad"]),
                        margenPorcentaje = Convert.ToDecimal(row["margen_porcentaje"])
                    });
                }
                return Ok(new { success = true, data = utilidad });
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

        // GET: api/Reportes/VentasPorPeriodo?fechaDesde=2026-07-01&fechaHasta=2026-07-15&formaPago=EFECTIVO
        [HttpGet("VentasPorPeriodo")]
        public IActionResult VentasPorPeriodo([FromQuery] DateTime? fechaDesde, [FromQuery] DateTime? fechaHasta, [FromQuery] string? formaPago = null)
        {
            // Por defecto, si no mandan fechas, usamos los últimos 30 días.
            DateTime desde = fechaDesde ?? DateTime.Today.AddDays(-29);
            DateTime hasta = fechaHasta ?? DateTime.Today;

            try
            {
                var diasActuales = ObtenerVentasPorDia(desde, hasta, formaPago);

                // Período anterior de igual duración, para comparar (ej. estos 15 días vs los 15 anteriores).
                int duracionDias = (hasta.Date - desde.Date).Days + 1;
                DateTime desdeAnterior = desde.Date.AddDays(-duracionDias);
                DateTime hastaAnterior = desde.Date.AddDays(-1);
                var diasAnteriores = ObtenerVentasPorDia(desdeAnterior, hastaAnterior, formaPago);

                decimal totalActual = diasActuales.Sum(d => d.TotalVentas);
                decimal totalAnterior = diasAnteriores.Sum(d => d.TotalVentas);
                int cantidadVentasActual = diasActuales.Sum(d => d.CantidadVentas);

                decimal? variacionPorcentaje = totalAnterior > 0
                    ? Math.Round(((totalActual - totalAnterior) / totalAnterior) * 100, 2)
                    : (decimal?)null;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        dias = diasActuales,
                        resumen = new
                        {
                            totalActual,
                            totalAnterior,
                            variacionPorcentaje,
                            cantidadVentas = cantidadVentasActual,
                            ticketPromedio = cantidadVentasActual > 0 ? Math.Round(totalActual / cantidadVentasActual, 2) : 0
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
        }

        private List<VentaPorDia> ObtenerVentasPorDia(DateTime desde, DateTime hasta, string? formaPago)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
    {
        new SqlParameter("@fecha_desde", desde.Date),
        new SqlParameter("@fecha_hasta", hasta.Date),
        new SqlParameter("@forma_pago", (object?)formaPago ?? DBNull.Value)
    };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Reporte_VentasPorPeriodo", parametros);
                var lista = new List<VentaPorDia>();
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        lista.Add(new VentaPorDia
                        {
                            Fecha = Convert.ToDateTime(row["fecha"]),
                            CantidadVentas = Convert.ToInt32(row["cantidad_ventas"]),
                            TotalVentas = Convert.ToDecimal(row["total_ventas"]),
                            TicketPromedio = Convert.ToDecimal(row["ticket_promedio"])
                        });
                    }
                }
                return lista;
            }
            finally
            {
                parametros.Clear();
                dt?.Dispose();
            }
        }

        // GET: api/Reportes/ProductosMasVendidos?fechaDesde=2026-07-01&fechaHasta=2026-07-15&idCategoria=1&top=20
        [HttpGet("ProductosMasVendidos")]
        public IActionResult ProductosMasVendidos(
            [FromQuery] DateTime? fechaDesde,
            [FromQuery] DateTime? fechaHasta,
            [FromQuery] int? idCategoria = null,
            [FromQuery] int top = 20)
        {
            DateTime desde = fechaDesde ?? DateTime.Today.AddDays(-29);
            DateTime hasta = fechaHasta ?? DateTime.Today;

            List<SqlParameter> parametros = new List<SqlParameter>
    {
        new SqlParameter("@fecha_desde", desde.Date),
        new SqlParameter("@fecha_hasta", hasta.Date),
        new SqlParameter("@id_categoria", (object?)idCategoria ?? DBNull.Value),
        new SqlParameter("@top", top)
    };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Reporte_ProductosMasVendidos", parametros);
                var lista = new List<object>();
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        lista.Add(new
                        {
                            idProducto = Convert.ToInt32(row["id_producto"]),
                            producto = row["producto"].ToString(),
                            idCategoria = row["id_categoria"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["id_categoria"]),
                            categoria = row["categoria"]?.ToString(),
                            cantidadVendida = Convert.ToDecimal(row["cantidad_vendida"]),
                            totalVentas = Convert.ToDecimal(row["total_ventas"])
                        });
                    }
                }
                return Ok(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
            finally
            {
                parametros.Clear();
                dt?.Dispose();
            }
        }

        public class VentaPorDia
        {
            public DateTime Fecha { get; set; }
            public int CantidadVentas { get; set; }
            public decimal TotalVentas { get; set; }
            public decimal TicketPromedio { get; set; }
        }
    }
}