using AgroSystemServer.Data;
using AgroSystemServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Text.Json;


namespace AgroSystemServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public VentasController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // GET: api/Ventas/Categorias
        [HttpGet("Categorias")]
        public IActionResult ObtenerCategorias()
        {
            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("CategoriasListar");
                var categorias = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    categorias.Add(new
                    {
                        idCategoria = Convert.ToInt32(row["id_categoria"]),
                        nombre = row["nombre"].ToString(),
                        descripcion = row["descripcion"].ToString(),
                        idUnidadSugerida = row["id_unidad_sugerida"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["id_unidad_sugerida"])
                    });
                }
                return Ok(new { success = true, data = categorias });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
            finally
            {
                dt?.Dispose();
            }
        }

        // GET: api/Ventas/Productos
        [HttpGet("Productos")]
        public IActionResult ObtenerProductos([FromQuery] int? idCategoria = null)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@id_categoria", (object?)idCategoria ?? DBNull.Value)
            };
            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Productos_ListarPorCategoria", parametros);
                var productos = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    productos.Add(new
                    {
                        idProducto = Convert.ToInt32(row["id_producto"]),
                        nombre = row["nombre"].ToString(),
                        precioVenta = Convert.ToDecimal(row["precio_venta"]),
                        codigoBarras = row["codigo_barras"].ToString(),
                        stockActual = Convert.ToDecimal(row["stock_actual"]),
                        idCategoria = Convert.ToInt32(row["id_categoria"]),
                        categoria = row["categoria"].ToString(),
                        permiteDecimales = row["permite_decimales"] == DBNull.Value ? true : Convert.ToBoolean(row["permite_decimales"]),
                        idPromocion = row["id_promocion"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["id_promocion"]),
                        promocionNombre = row["promocion_nombre"] == DBNull.Value ? null : row["promocion_nombre"].ToString(),
                        valorDescuento = row["valor_descuento"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["valor_descuento"]),
                        precioConDescuento = row["precio_con_descuento"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["precio_con_descuento"]),
                        fechaVencimiento = row["fecha_vencimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["fecha_vencimiento"]),
                        diasRestantes = row["dias_restantes"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["dias_restantes"])
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

        // POST: api/Ventas/Registrar
        [HttpPost("Registrar")]
        public IActionResult RegistrarVenta([FromBody] VentaRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, mensaje = "Datos inválidos." });

            if (request.Detalle == null || request.Detalle.Count == 0)
                return BadRequest(new { success = false, mensaje = "El detalle de la venta está vacío." });

            var detalleJson = JsonSerializer.Serialize(request.Detalle.Select(d => new
            {
                id_producto = d.IdProducto,
                cantidad = d.Cantidad,
                precio_unitario = d.PrecioUnitario,
                porcentaje_iva = d.PorcentajeIva,
                descuento = d.Descuento,
                subtotal = d.Subtotal
            }));

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@id_cliente",      request.IdCliente),
                new SqlParameter("@id_usuario",      request.IdUsuario),
                new SqlParameter("@subtotal_iva_0",  request.SubtotalIva0),
                new SqlParameter("@subtotal_iva_15", request.SubtotalIva15),
                new SqlParameter("@monto_iva",       request.MontoIva),
                new SqlParameter("@total",           request.Total),
                new SqlParameter("@forma_pago",      request.FormaPago),
                new SqlParameter("@detalle",         detalleJson)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Ventas_Registrar", parametros);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return Ok(new
                    {
                        success = true,
                        mensaje = dt.Rows[0]["mensaje"].ToString(),
                        idVenta = Convert.ToInt32(dt.Rows[0]["id_venta"])
                    });
                }
                return Ok(new { success = true, mensaje = "Venta registrada." });
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

        // GET: api/Ventas/Clientes?busqueda=juan
        [HttpGet("Clientes")]
        public IActionResult ObtenerClientes([FromQuery] string? busqueda = null)
        {
            DataTable dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("ObtenerClientes");
                var clientes = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    var razonSocial = row["razon_social"].ToString();
                    var identificacion = row["identificacion"].ToString();

                    if (!string.IsNullOrEmpty(busqueda) &&
                        !razonSocial.Contains(busqueda, StringComparison.OrdinalIgnoreCase) &&
                        !identificacion.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (Convert.ToBoolean(row["estado"]))
                    {
                        clientes.Add(new
                        {
                            idCliente = Convert.ToInt32(row["id_cliente"]),
                            razonSocial = razonSocial,
                            identificacion = identificacion,
                            telefono = row["telefono"].ToString(),
                            email = row["correo_electronico"]?.ToString()
                        });
                    }
                }
                return Ok(new { success = true, data = clientes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
            finally
            {
                dt?.Dispose();
            }
        }

        public class AnularVentaRequest
        {
            public string Motivo { get; set; } = string.Empty;
            public int? IdUsuario { get; set; }
        }

        // POST: api/Ventas/5/anular
        [HttpPost("{idVenta}/anular")]
        public IActionResult AnularVenta(int idVenta, [FromBody] AnularVentaRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Motivo))
                return BadRequest(new { success = false, mensaje = "Debe especificar un motivo de anulación." });

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@id_venta", idVenta),
                new SqlParameter("@motivo", request.Motivo),
                new SqlParameter("@id_usuario", (object?)request.IdUsuario ?? DBNull.Value)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Ventas_Anular", parametros);
                string mensaje = dt != null && dt.Rows.Count > 0 ? dt.Rows[0]["mensaje"].ToString() : "Venta anulada.";
                return Ok(new { success = true, mensaje });
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

        public class EnviarComprobanteRequest
        {
            public string Email { get; set; } = string.Empty;
            public string PdfBase64 { get; set; } = string.Empty;
        }

        // POST: api/Ventas/5/enviar-comprobante
        [HttpPost("{idVenta}/enviar-comprobante")]
        public async Task<IActionResult> EnviarComprobante(int idVenta, [FromBody] EnviarComprobanteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.PdfBase64))
                return BadRequest(new { success = false, mensaje = "Faltan datos para enviar el comprobante." });

            try
            {
                byte[] pdfBytes = Convert.FromBase64String(request.PdfBase64);
                await _emailService.EnviarComprobanteVentaAsync(request.Email, idVenta, pdfBytes);
                return Ok(new { success = true, mensaje = "Comprobante enviado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Error al enviar el comprobante: " + ex.Message });
            }
        }

    }

    // Modelos de request
    public class VentaRequest
    {
        public int IdCliente { get; set; }
        public int IdUsuario { get; set; }
        public decimal SubtotalIva0 { get; set; }
        public decimal SubtotalIva15 { get; set; }
        public decimal MontoIva { get; set; }
        public decimal Total { get; set; }
        public string? FormaPago { get; set; }
        public List<DetalleVentaRequest>? Detalle { get; set; }
    }

    public class DetalleVentaRequest
    {
        public int IdProducto { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PorcentajeIva { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
    }
}