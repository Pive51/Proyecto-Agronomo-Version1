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
    public class DashboardController : ControllerBase
    {
        // GET: api/Dashboard/ProductosMasVendidos?fechaInicio=2026-07-01&fechaFin=2026-07-09&top=5
        [HttpGet("ProductosMasVendidos")]
        public IActionResult ObtenerProductosMasVendidos(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] int top = 5)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@FechaInicio", (object?)fechaInicio?.Date ?? DBNull.Value),
                new SqlParameter("@FechaFin",    (object?)fechaFin?.Date ?? DBNull.Value),
                new SqlParameter("@Top",         top)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Productos_MasVendidos", parametros);
                var lista = new List<object>();

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        lista.Add(new
                        {
                            idProducto = row["id_producto"] == DBNull.Value ? 0 : Convert.ToInt32(row["id_producto"]),
                            nombre = row["nombre"]?.ToString() ?? string.Empty,
                            codigoBarras = row["codigo_barras"]?.ToString() ?? string.Empty,
                            cantidadVendida = row["cantidad_vendida"] == DBNull.Value ? 0m : Convert.ToDecimal(row["cantidad_vendida"]),
                            totalVendido = row["total_vendido"] == DBNull.Value ? 0m : Convert.ToDecimal(row["total_vendido"]),
                            numeroTransacciones = row["numero_transacciones"] == DBNull.Value ? 0 : Convert.ToInt32(row["numero_transacciones"])
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
                parametros?.Clear();
                dt?.Dispose();
            }
        }

        // GET: api/Dashboard/VentasDia?fecha=2026-07-09&idCliente=3
        [HttpGet("VentasDia")]
        public IActionResult ObtenerVentasDia(
            [FromQuery] DateTime? fecha = null,
            [FromQuery] int? idCliente = null)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@Fecha",     (object?)fecha?.Date ?? DBNull.Value),
                new SqlParameter("@IdCliente", (object?)idCliente ?? DBNull.Value)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Ventas_Dia", parametros);

                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var resultado = new
                    {
                        cantidadVentas = row["cantidad_ventas"] == DBNull.Value ? 0 : Convert.ToInt32(row["cantidad_ventas"]),
                        totalVentas = row["total_ventas"] == DBNull.Value ? 0m : Convert.ToDecimal(row["total_ventas"]),
                        ticketPromedio = row["ticket_promedio"] == DBNull.Value ? 0m : Convert.ToDecimal(row["ticket_promedio"])
                    };
                    return Ok(new { success = true, data = resultado });
                }

                return Ok(new { success = true, data = new { cantidadVentas = 0, totalVentas = 0m, ticketPromedio = 0m } });
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

        // GET: api/Dashboard/Margenes?fechaInicio=2026-07-01&fechaFin=2026-07-09
        [HttpGet("Margenes")]
        public IActionResult ObtenerMargenes(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@FechaInicio", (object?)fechaInicio?.Date ?? DBNull.Value),
                new SqlParameter("@FechaFin",    (object?)fechaFin?.Date ?? DBNull.Value)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Ventas_Margenes", parametros);

                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var resultado = new
                    {
                        totalVentas = row["total_ventas"] == DBNull.Value ? 0m : Convert.ToDecimal(row["total_ventas"]),
                        totalCosto = row["total_costo"] == DBNull.Value ? 0m : Convert.ToDecimal(row["total_costo"]),
                        utilidadBruta = row["utilidad_bruta"] == DBNull.Value ? 0m : Convert.ToDecimal(row["utilidad_bruta"]),
                        margenGananciaPct = row["margen_ganancia_pct"] == DBNull.Value ? 0m : Convert.ToDecimal(row["margen_ganancia_pct"]),
                        margenUtilidadBrutaPct = row["margen_utilidad_bruta_pct"] == DBNull.Value ? 0m : Convert.ToDecimal(row["margen_utilidad_bruta_pct"])
                    };
                    return Ok(new { success = true, data = resultado });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalVentas = 0m,
                        totalCosto = 0m,
                        utilidadBruta = 0m,
                        margenGananciaPct = 0m,
                        margenUtilidadBrutaPct = 0m
                    }
                });
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

        // GET: api/Dashboard/ValorTotalInventario
        [HttpGet("ValorTotalInventario")]
        public IActionResult ObtenerValorTotalInventario()
        {
            List<SqlParameter> parametros = new List<SqlParameter>();
            DataTable? dt = null;

            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Productos_ValorTotalInventario", parametros);

                decimal totalInventario = 0m;

                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["total"] != DBNull.Value)
                {
                    totalInventario = Convert.ToDecimal(dt.Rows[0]["total"]);
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        valorTotalInventario = totalInventario
                    }
                });
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

        // GET: api/Dashboard/ProductosVencimientoProximo?diasLimite=30&top=5
        [HttpGet("ProductosVencimientoProximo")]
        public IActionResult ObtenerProductosVencimientoProximo(
            [FromQuery] int diasLimite = 30,
            [FromQuery] int top = 5)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@DiasLimite", diasLimite),
                new SqlParameter("@Top",        top)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Productos_VencimientoProximo", parametros);
                var lista = new List<object>();

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        lista.Add(new
                        {
                            idProducto = row["id_producto"] == DBNull.Value ? 0 : Convert.ToInt32(row["id_producto"]),
                            nombre = row["nombre"]?.ToString() ?? string.Empty,
                            codigoBarras = row["codigo_barras"]?.ToString() ?? string.Empty,
                            fechaVencimiento = row["fecha_vencimiento"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["fecha_vencimiento"]),
                            diasRestantes = row["dias_restantes"] == DBNull.Value ? 0 : Convert.ToInt32(row["dias_restantes"]),
                            stockRestante = row["stock_restante"] == DBNull.Value ? 0m : Convert.ToDecimal(row["stock_restante"])
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
                parametros?.Clear();
                dt?.Dispose();
            }
        }

        // GET: api/Dashboard/VentasPorPeriodo?tipoPeriodo=mes&fechaInicio=2026-01-01&fechaFin=2026-07-10
        // tipoPeriodo acepta: dia | semana | mes | trimestre | semestre | anio
        [HttpGet("VentasPorPeriodo")]
        public IActionResult ObtenerVentasPorPeriodo(
            [FromQuery] string tipoPeriodo = "mes",
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@TipoPeriodo", tipoPeriodo ?? "mes"),
                new SqlParameter("@FechaInicio", (object?)fechaInicio?.Date ?? DBNull.Value),
                new SqlParameter("@FechaFin",    (object?)fechaFin?.Date ?? DBNull.Value)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Ventas_PorPeriodo", parametros);
                var lista = new List<object>();

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        lista.Add(new
                        {
                            periodo = row["periodo"]?.ToString() ?? string.Empty,
                            fechaOrden = row["fecha_orden"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["fecha_orden"]),
                            totalVentas = row["total_ventas"] == DBNull.Value ? 0m : Convert.ToDecimal(row["total_ventas"]),
                            cantidadVentas = row["cantidad_ventas"] == DBNull.Value ? 0 : Convert.ToInt32(row["cantidad_ventas"])
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
                parametros?.Clear();
                dt?.Dispose();
            }
        }
    }
}