using AgroSystemServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace AgroSystemServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CotizacionesController : ControllerBase
    {
        // POST: api/Cotizaciones/Registrar
        [HttpPost("Registrar")]
        public IActionResult Registrar([FromBody] CotizacionRequest request)
        {
            if (request == null || request.Detalle == null || request.Detalle.Count == 0)
                return BadRequest(new { success = false, mensaje = "La cotización no tiene productos." });

            if (request.IdCliente == null && string.IsNullOrWhiteSpace(request.NombreProspecto))
                return BadRequest(new { success = false, mensaje = "Debes seleccionar un cliente o ingresar el nombre del prospecto." });

            var detalleJson = JsonSerializer.Serialize(request.Detalle.Select(d => new
            {
                id_producto = d.IdProducto,
                nombre_producto = d.NombreProducto,
                cantidad = d.Cantidad,
                precio_unitario = d.PrecioUnitario,
                subtotal = d.Subtotal
            }));

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@id_cliente", (object?)request.IdCliente ?? DBNull.Value),
                new SqlParameter("@nombre_prospecto", (object?)request.NombreProspecto ?? DBNull.Value),
                new SqlParameter("@telefono_prospecto", (object?)request.TelefonoProspecto ?? DBNull.Value),
                new SqlParameter("@id_usuario", request.IdUsuario),
                new SqlParameter("@subtotal", request.Subtotal),
                new SqlParameter("@descuento_promociones", request.DescuentoPromociones),
                new SqlParameter("@total", request.Total),
                new SqlParameter("@detalle", detalleJson)
            };

            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Cotizaciones_Registrar", parametros);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return Ok(new
                    {
                        success = true,
                        mensaje = dt.Rows[0]["mensaje"].ToString(),
                        idCotizacion = Convert.ToInt32(dt.Rows[0]["id_cotizacion"])
                    });
                }
                return Ok(new { success = true, mensaje = "Cotización registrada." });
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

        // GET: api/Cotizaciones/Listar
        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            DataTable? dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Cotizaciones_Listar", null);
                var lista = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new
                    {
                        idCotizacion = Convert.ToInt32(row["id_cotizacion"]),
                        fecha = Convert.ToDateTime(row["fecha"]),
                        nombreCliente = row["nombre_cliente"].ToString(),
                        telefonoCliente = row["telefono_cliente"].ToString(),
                        total = Convert.ToDecimal(row["total"]),
                        estado = row["estado"].ToString(),
                        idVentaGenerada = row["id_venta_generada"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["id_venta_generada"])
                    });
                }
                return Ok(new { success = true, data = lista });
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

        // GET: api/Cotizaciones/5/Detalle
        [HttpGet("{idCotizacion}/Detalle")]
        public IActionResult Detalle(int idCotizacion)
        {
            DataTable? cabecera = null;
            DataTable? items = null;

            try
            {
                cabecera = ClsConexion.EjecutarProcedimientoConsulta("sp_Cotizaciones_Cabecera",
                    new List<SqlParameter> { new SqlParameter("@id_cotizacion", idCotizacion) });

                if (cabecera == null || cabecera.Rows.Count == 0)
                    return NotFound(new { success = false, mensaje = "Cotización no encontrada." });

                items = ClsConexion.EjecutarProcedimientoConsulta("sp_Cotizaciones_ItemsDetalle",
                    new List<SqlParameter> { new SqlParameter("@id_cotizacion", idCotizacion) });

                var c = cabecera.Rows[0];
                var detalle = new List<object>();
                foreach (DataRow row in items.Rows)
                {
                    detalle.Add(new
                    {
                        idProducto = Convert.ToInt32(row["id_producto"]),
                        nombreProducto = row["nombre_producto"].ToString(),
                        cantidad = Convert.ToDecimal(row["cantidad"]),
                        precioUnitario = Convert.ToDecimal(row["precio_unitario"]),
                        subtotal = Convert.ToDecimal(row["subtotal"])
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        idCotizacion = Convert.ToInt32(c["id_cotizacion"]),
                        fecha = Convert.ToDateTime(c["fecha"]),
                        idCliente = c["id_cliente"] == DBNull.Value ? (int?)null : Convert.ToInt32(c["id_cliente"]),
                        nombreCliente = c["nombre_cliente"].ToString(),
                        identificacionCliente = c["identificacion_cliente"]?.ToString(),
                        emailCliente = c["email_cliente"]?.ToString(),
                        telefonoCliente = c["telefono_cliente"].ToString(),
                        subtotal = Convert.ToDecimal(c["subtotal"]),
                        descuentoPromociones = Convert.ToDecimal(c["descuento_promociones"]),
                        total = Convert.ToDecimal(c["total"]),
                        estado = c["estado"].ToString(),
                        idVentaGenerada = c["id_venta_generada"] == DBNull.Value ? (int?)null : Convert.ToInt32(c["id_venta_generada"]),
                        detalle
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
            finally
            {
                cabecera?.Dispose();
                items?.Dispose();
            }
        }

        // POST: api/Cotizaciones/5/ConvertirAVenta
        // Reutiliza sp_Ventas_Registrar con los mismos datos de la cotización,
        // luego marca la cotización como Convertida y la enlaza a la venta generada.
        [HttpPost("{idCotizacion}/ConvertirAVenta")]
        public IActionResult ConvertirAVenta(int idCotizacion, [FromBody] ConvertirAVentaRequest request)
        {
            DataTable? cabecera = null;
            DataTable? items = null;

            try
            {
                cabecera = ClsConexion.EjecutarProcedimientoConsulta("sp_Cotizaciones_Cabecera",
                    new List<SqlParameter> { new SqlParameter("@id_cotizacion", idCotizacion) });

                if (cabecera == null || cabecera.Rows.Count == 0)
                    return NotFound(new { success = false, mensaje = "Cotización no encontrada." });

                var c = cabecera.Rows[0];

                if (c["estado"].ToString() == "Convertida")
                    return BadRequest(new { success = false, mensaje = "Esta cotización ya fue convertida a venta." });

                if (c["id_cliente"] == DBNull.Value)
                    return BadRequest(new { success = false, mensaje = "Para convertir a venta, la cotización debe tener un cliente registrado (no un prospecto sin cuenta)." });

                items = ClsConexion.EjecutarProcedimientoConsulta("sp_Cotizaciones_ItemsDetalle",
                    new List<SqlParameter> { new SqlParameter("@id_cotizacion", idCotizacion) });

                int idCliente = Convert.ToInt32(c["id_cliente"]);

                var detalleJson = JsonSerializer.Serialize(items.Rows.Cast<DataRow>().Select(row => new
                {
                    id_producto = Convert.ToInt32(row["id_producto"]),
                    cantidad = Convert.ToDecimal(row["cantidad"]),
                    precio_unitario = Convert.ToDecimal(row["precio_unitario"]),
                    porcentaje_iva = 0,
                    descuento = 0,
                    subtotal = Convert.ToDecimal(row["subtotal"])
                }));

                List<SqlParameter> parametrosVenta = new List<SqlParameter>
                {
                    new SqlParameter("@id_cliente", idCliente),
                    new SqlParameter("@id_usuario", request.IdUsuario),
                    new SqlParameter("@subtotal_iva_0", Convert.ToDecimal(c["total"])),
                    new SqlParameter("@subtotal_iva_15", 0),
                    new SqlParameter("@monto_iva", 0),
                    new SqlParameter("@total", Convert.ToDecimal(c["total"])),
                    new SqlParameter("@forma_pago", request.FormaPago ?? "EFECTIVO"),
                    new SqlParameter("@detalle", detalleJson)
                };

                DataTable? dtVenta = null;
                int idVentaGenerada;
                try
                {
                    dtVenta = ClsConexion.EjecutarProcedimientoConsulta("sp_Ventas_Registrar", parametrosVenta);
                    if (dtVenta == null || dtVenta.Rows.Count == 0)
                        return StatusCode(500, new { success = false, mensaje = "No se pudo registrar la venta a partir de la cotización." });

                    idVentaGenerada = Convert.ToInt32(dtVenta.Rows[0]["id_venta"]);
                }
                finally
                {
                    parametrosVenta.Clear();
                    dtVenta?.Dispose();
                }

                List<SqlParameter> parametrosMarcar = new List<SqlParameter>
                {
                    new SqlParameter("@id_cotizacion", idCotizacion),
                    new SqlParameter("@id_venta_generada", idVentaGenerada)
                };
                try
                {
                    ClsConexion.EjecutarProcedimientoConsulta("sp_Cotizaciones_MarcarConvertida", parametrosMarcar);
                }
                finally
                {
                    parametrosMarcar.Clear();
                }

                return Ok(new { success = true, mensaje = "Cotización convertida a venta correctamente.", idVenta = idVentaGenerada });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Error al convertir la cotización: " + ex.Message });
            }
            finally
            {
                cabecera?.Dispose();
                items?.Dispose();
            }
        }
    }

    public class DetalleCotizacionRequest
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class CotizacionRequest
    {
        public int? IdCliente { get; set; }
        public string? NombreProspecto { get; set; }
        public string? TelefonoProspecto { get; set; }
        public int IdUsuario { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DescuentoPromociones { get; set; }
        public decimal Total { get; set; }
        public List<DetalleCotizacionRequest>? Detalle { get; set; }
    }

    public class ConvertirAVentaRequest
    {
        public int IdUsuario { get; set; }
        public string? FormaPago { get; set; }
    }
}