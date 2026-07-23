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
    public class ComprasController : ControllerBase
    {
        // POST: api/Compras
        [HttpPost]
        public IActionResult Registrar([FromBody] RegistrarCompraRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, mensaje = "Datos inválidos." });

            var paramCompraId = new SqlParameter("@CompraId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@ProveedorId", request.ProveedorId),
                new SqlParameter("@NumeroFactura", request.NumeroFactura),
                new SqlParameter("@UsuarioId", request.UsuarioId),
                new SqlParameter("@Observaciones", (object?)request.Observaciones ?? DBNull.Value),
                paramCompraId
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("sp_Compras_Registrar", parametros);
                int compraId = (int)paramCompraId.Value;

                return Ok(new { success = true, mensaje = "Compra registrada correctamente.", data = new { compraId } });
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
            }
        }

        // POST: api/Compras/detalle
        [HttpPost("detalle")]
        public IActionResult AgregarDetalle([FromBody] AgregarDetalleCompraRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, mensaje = "Datos inválidos." });

            List<SqlParameter> parametros = new List<SqlParameter>
    {
        new SqlParameter("@CompraId", request.CompraId),
        new SqlParameter("@ProductoId", request.ProductoId),
        new SqlParameter("@Cantidad", request.Cantidad),
        new SqlParameter("@CostoUnitario", request.CostoUnitario)
    };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Compras_AgregarDetalle", parametros);
                int compraDetalleId = dt != null && dt.Rows.Count > 0
                    ? Convert.ToInt32(dt.Rows[0]["CompraDetalleId"])
                    : 0;

                return Ok(new { success = true, mensaje = "Detalle agregado correctamente.", data = new { compraDetalleId } });
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

        // POST: api/Compras/recibir-lote
        [HttpPost("recibir-lote")]
        public IActionResult RecibirLote([FromBody] RecibirLoteRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, mensaje = "Datos inválidos." });

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@CompraDetalleId", request.CompraDetalleId),
                new SqlParameter("@codigo_lote", request.CodigoLote),
                new SqlParameter("@fecha_elaboracion", (object?)request.FechaElaboracion ?? DBNull.Value),
                new SqlParameter("@fecha_vencimiento", request.FechaVencimiento),
                new SqlParameter("@cantidad_recibida", request.CantidadRecibida)
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("sp_Compras_RecibirLote", parametros);
                return Ok(new { success = true, mensaje = "Lote recibido correctamente." });
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
            }
        }

        [HttpGet("productos")]
        public IActionResult ListarProductosParaCompra([FromQuery] int? idCategoria = null)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
    {
        new SqlParameter("@id_categoria", (object?)idCategoria ?? DBNull.Value)
    };
            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Productos_ListarParaCompra", parametros);
                var productos = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    productos.Add(new
                    {
                        idProducto = Convert.ToInt32(row["id_producto"]),
                        nombre = row["nombre"].ToString(),
                        precioCompra = Convert.ToDecimal(row["precio_compra"]),
                        precioVenta = Convert.ToDecimal(row["precio_venta"]),
                        stockActual = Convert.ToDecimal(row["stock_actual"]),
                        idCategoria = Convert.ToInt32(row["id_categoria"]),
                        categoria = row["categoria"].ToString(),
                        permiteDecimales = row["permite_decimales"] == DBNull.Value ? true : Convert.ToBoolean(row["permite_decimales"])
                    });
                }
                return Ok(new { success = true, data = productos });
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

        // POST: api/Compras/5/finalizar
        [HttpPost("{compraId}/finalizar")]
        public IActionResult Finalizar(int compraId)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@CompraId", compraId)
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("sp_Compras_Finalizar", parametros);
                return Ok(new { success = true, mensaje = "Compra finalizada correctamente." });
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
            }
        }
        [HttpGet("{compraId}/detalle")]
        public IActionResult ObtenerDetalle(int compraId)
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
                        codigoLote = row["codigo_lote"]?.ToString(),
                        recibido = row["codigo_lote"] != DBNull.Value
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

        [HttpGet("CodigoLoteSugerido")]
        public IActionResult CodigoLoteSugerido()
        {
            DataTable dt = null;

            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_ObtenerCodigoLoteSugerido", null);

                string codigo = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    codigo = dt.Rows[0]["CodigoLote"].ToString();
                }

                if (string.IsNullOrEmpty(codigo))
                {
                    return StatusCode(500, new { success = false, mensaje = "No se pudo generar el código de lote." });
                }

                return Ok(new { success = true, data = new { codigoLote = codigo } });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ComprasController.CodigoLoteSugerido] " + ex.Message);
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor." });
            }
            finally
            {
                dt?.Dispose();
            }
        }
    }

    // Modelos de request
    public class RegistrarCompraRequest
    {
        public int ProveedorId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string? Observaciones { get; set; }
    }

    public class AgregarDetalleCompraRequest
    {
        public int CompraId { get; set; }
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    public class RecibirLoteRequest
    {
        public int CompraDetalleId { get; set; }
        public string CodigoLote { get; set; } = string.Empty;
        public DateTime? FechaElaboracion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal CantidadRecibida { get; set; }
    }
}