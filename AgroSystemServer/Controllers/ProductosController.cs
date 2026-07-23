using AgroSystemServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace AgroSystemServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("multipart/form-data")]
    public class ProductosController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        // Constructor con inyección de dependencias para manejar rutas dinámicas
        public ProductosController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("Listar")]
        public IActionResult Listar(string? nombre = null, int? idCategoria = null, bool? estado = null)
        {
            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@nombre", (object?)nombre ?? DBNull.Value),
                new SqlParameter("@id_categoria", (object?)idCategoria ?? DBNull.Value),
                new SqlParameter("@estado", (object?)estado ?? DBNull.Value)
            };

            DataTable dt = null;

            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("sp_Productos_Listar", parametros);

                var lista = new List<object>();

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        lista.Add(new
                        {
                            idProducto = Convert.ToInt32(row["id_producto"]),
                            idCategoria = row["id_categoria"] != DBNull.Value ? Convert.ToInt32(row["id_categoria"]) : (int?)null,
                            idUnidad = row["id_unidad"] != DBNull.Value ? Convert.ToInt32(row["id_unidad"]) : (int?)null,
                            codigoBarras = row["codigo_barras"].ToString(),
                            nombre = row["nombre"].ToString(),
                            precioCompra = Convert.ToDecimal(row["precio_compra"]),
                            precioVenta = Convert.ToDecimal(row["precio_venta"]),
                            codigoImpuestoSri = row["codigo_impuesto_sri"].ToString(),
                            stockActual = Convert.ToInt32(row["stock_actual"]),
                            stockMinimo = Convert.ToInt32(row["stock_minimo"]),
                            estado = Convert.ToBoolean(row["estado"]),
                            imagen = row["imagen"].ToString(),
                            idPromocion = row["id_promocion"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["id_promocion"]),
                            promocionNombre = row["promocion_nombre"] == DBNull.Value ? null : row["promocion_nombre"].ToString(),
                            valorDescuento = row["valor_descuento"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["valor_descuento"]),
                            precioConDescuento = row["precio_con_descuento"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["precio_con_descuento"]),
                            fechaVencimiento = row["fecha_vencimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["fecha_vencimiento"]),
                            diasRestantes = row["dias_restantes"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["dias_restantes"])
                        });
                    }
                }

                return Ok(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ProductosController.Listar] " + ex.Message);
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor." });
            }
            finally
            {
                parametros?.Clear();
                dt?.Dispose();
            }
        }

        [HttpPost("Agregar")]
        [Consumes("multipart/form-data")]
        public IActionResult Agregar([FromForm] ProductoRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.CodigoBarras))
            {
                return BadRequest(new { success = false, mensaje = "El nombre y el código de barras son obligatorios." });
            }

            string urlImagen = string.Empty;

            if (request.ImagenArchivo != null && request.ImagenArchivo.Length > 0)
            {
                try
                {
                    // Construcción de ruta portable interna en el proyecto
                    string carpetaDestino = Path.Combine(_env.ContentRootPath, "Imagenes", "Productos");

                    if (!Directory.Exists(carpetaDestino))
                    {
                        Directory.CreateDirectory(carpetaDestino);
                    }

                    string nombreArchivoUnico = Guid.NewGuid().ToString() + Path.GetExtension(request.ImagenArchivo.FileName);
                    string rutaCompletaFisica = Path.Combine(carpetaDestino, nombreArchivoUnico);

                    using (var stream = new FileStream(rutaCompletaFisica, FileMode.Create))
                    {
                        request.ImagenArchivo.CopyTo(stream);
                    }

                    urlImagen = "/Imagenes/Productos/" + nombreArchivoUnico;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, mensaje = "Error al procesar y guardar la imagen: " + ex.Message });
                }
            }

            var resultadoParam = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };
            var idProductoGeneradoParam = new SqlParameter("@id_producto_generado", SqlDbType.Int) { Direction = ParameterDirection.Output };

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@id_categoria", request.IdCategoria == 0 ? DBNull.Value : request.IdCategoria),
                new SqlParameter("@id_unidad", request.IdUnidad == 0 ? DBNull.Value : request.IdUnidad),
                new SqlParameter("@codigo_barras", request.CodigoBarras),
                new SqlParameter("@nombre", request.Nombre),
                new SqlParameter("@precio_compra", request.PrecioCompra),
                new SqlParameter("@precio_venta", request.PrecioVenta),
                new SqlParameter("@codigo_impuesto_sri", string.IsNullOrEmpty(request.CodigoImpuestoSri) ? DBNull.Value : request.CodigoImpuestoSri),
                new SqlParameter("@stock_actual", request.StockActual),
                new SqlParameter("@stock_minimo", request.StockMinimo),
                new SqlParameter("@estado", request.Estado),
                new SqlParameter("@imagen", string.IsNullOrEmpty(urlImagen) ? DBNull.Value : urlImagen),

                resultadoParam,
                mensajeParam,
                idProductoGeneradoParam
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("sp_Productos_Agregar", parametros);

                bool exito = resultadoParam.Value != DBNull.Value && (bool)resultadoParam.Value;
                string mensaje = mensajeParam.Value?.ToString() ?? "Sin mensaje.";
                int idGenerado = idProductoGeneradoParam.Value != DBNull.Value ? Convert.ToInt32(idProductoGeneradoParam.Value) : 0;

                if (exito)
                    return Ok(new { success = true, mensaje, idProducto = idGenerado, urlImagen });

                return BadRequest(new { success = false, mensaje });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ProductosController.Agregar] " + ex.Message);
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor." });
            }
            finally
            {
                parametros?.Clear();
            }
        }

        [HttpPut("Modificar")]
        [Consumes("multipart/form-data")]
        public IActionResult Modificar([FromForm] ProductoUpdateRequest request) // Corregido a FromForm para adjuntar imágenes
        {
            if (request == null || request.IdProducto <= 0 || string.IsNullOrWhiteSpace(request.Nombre))
            {
                return BadRequest(new { success = false, mensaje = "Datos inválidos para actualizar el producto." });
            }

            string urlImagen = string.Empty;

            // Procesar la nueva imagen si se subió un archivo durante la edición
            if (request.ImagenArchivo != null && request.ImagenArchivo.Length > 0)
            {
                try
                {
                    string carpetaDestino = Path.Combine(_env.ContentRootPath, "Imagenes", "Productos");

                    if (!Directory.Exists(carpetaDestino))
                    {
                        Directory.CreateDirectory(carpetaDestino);
                    }

                    string nombreArchivoUnico = Guid.NewGuid().ToString() + Path.GetExtension(request.ImagenArchivo.FileName);
                    string rutaCompletaFisica = Path.Combine(carpetaDestino, nombreArchivoUnico);

                    using (var stream = new FileStream(rutaCompletaFisica, FileMode.Create))
                    {
                        request.ImagenArchivo.CopyTo(stream);
                    }

                    urlImagen = "/Imagenes/Productos/" + nombreArchivoUnico;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, mensaje = "Error al procesar la nueva imagen: " + ex.Message });
                }
            }

            var resultadoParam = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@id_producto", request.IdProducto),
                new SqlParameter("@id_categoria", request.IdCategoria == 0 ? DBNull.Value : request.IdCategoria),
                new SqlParameter("@id_unidad", request.IdUnidad == 0 ? DBNull.Value : request.IdUnidad),
                new SqlParameter("@codigo_barras", request.CodigoBarras),
                new SqlParameter("@nombre", request.Nombre),
                new SqlParameter("@precio_compra", request.PrecioCompra),
                new SqlParameter("@precio_venta", request.PrecioVenta),
                new SqlParameter("@codigo_impuesto_sri", string.IsNullOrEmpty(request.CodigoImpuestoSri) ? DBNull.Value : request.CodigoImpuestoSri),
                new SqlParameter("@stock_actual", request.StockActual),
                new SqlParameter("@stock_minimo", request.StockMinimo),
                new SqlParameter("@estado", request.Estado),
                // Si urlImagen está vacío, pasamos DBNull.Value (el SP debería estar preparado para mantener la imagen anterior si recibe NULL)
                new SqlParameter("@imagen", string.IsNullOrEmpty(urlImagen) ? DBNull.Value : urlImagen),
                resultadoParam,
                mensajeParam
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("sp_Productos_Update", parametros);

                bool exito = resultadoParam.Value != DBNull.Value && (bool)resultadoParam.Value;
                string mensaje = mensajeParam.Value?.ToString() ?? "Sin mensaje.";

                if (exito)
                    return Ok(new { success = true, mensaje, urlImagen = string.IsNullOrEmpty(urlImagen) ? null : urlImagen });

                return BadRequest(new { success = false, mensaje });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ProductosController.Modificar] " + ex.Message);
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor." });
            }
            finally
            {
                parametros?.Clear();
            }
        }

        [HttpDelete("Eliminar/{idProducto}")]
        [Consumes("multipart/form-data")]
        public IActionResult Eliminar(int idProducto)
        {
            if (idProducto <= 0)
            {
                return BadRequest(new { success = false, mensaje = "El id del producto no es válido." });
            }

            var resultadoParam = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };

            List<SqlParameter> parametros = new List<SqlParameter>
            {
                new SqlParameter("@id_producto", idProducto),
                resultadoParam,
                mensajeParam
            };

            try
            {
                ClsConexion.EjecutarProcedimientoAccion("sp_Productos_Delete", parametros);

                bool exito = resultadoParam.Value != DBNull.Value && (bool)resultadoParam.Value;
                string mensaje = mensajeParam.Value?.ToString() ?? "Sin mensaje.";

                if (exito)
                    return Ok(new { success = true, mensaje });

                return BadRequest(new { success = false, mensaje });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ProductosController.Eliminar] " + ex.Message);
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor." });
            }
            finally
            {
                parametros?.Clear();
            }
        }

        [HttpGet("ListarUnidades")]
        public IActionResult Listar()
        {
            DataTable dt = null;

            try
            {
                // Ejecutamos tu SP "UnidadesMedidaListar" sin enviarle parámetros
                dt = ClsConexion.EjecutarProcedimientoConsulta("UnidadesMedidaListar", null);

                var lista = new List<object>();

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        lista.Add(new
                        {
                            // Mapeamos manteniendo CamelCase para el JSON
                            idUnidad = Convert.ToInt32(row["id_unidad"]),
                            nombre = row["nombre"].ToString(),
                            abreviatura = row["abreviatura"].ToString(),
                            permiteDecimales = Convert.ToBoolean(row["permite_decimales"])
                        });
                    }
                }

                return Ok(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[UnidadesController.Listar] " + ex.Message);
                return StatusCode(500, new { success = false, mensaje = "Error interno del servidor al listar unidades." });
            }
            finally
            {
                dt?.Dispose();
            }
        }
    }

    public class ProductoRequest
    {
        public int IdCategoria { get; set; }
        public int IdUnidad { get; set; }
        public string? CodigoBarras { get; set; }
        public string? Nombre { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public string? CodigoImpuestoSri { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public bool Estado { get; set; } = true;
        public IFormFile? ImagenArchivo { get; set; }
    }

    public class ProductoUpdateRequest : ProductoRequest
    {
        public int IdProducto { get; set; }
    }
}