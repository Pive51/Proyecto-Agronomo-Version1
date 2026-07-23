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

    public class PromocionesController : ControllerBase
    {
        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            DataTable dt = null;
            try
            {
                dt = ClsConexion.EjecutarProcedimientoConsulta("ObtenerPromociones", null);
                var lista = new List<object>();

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        // 1. Extraer Lista de Productos
                        string prodIdsStr = row["productos_ids"] != DBNull.Value ? row["productos_ids"].ToString() : "";
                        List<int> listaProductosIds = new List<int>();
                        if (!string.IsNullOrEmpty(prodIdsStr))
                        {
                            listaProductosIds = prodIdsStr.Split(',').Select(int.Parse).ToList();
                        }

                        // 2. Extraer Lista de Categorías (NUEVO)
                        string catIdsStr = row["categorias_ids"] != DBNull.Value ? row["categorias_ids"].ToString() : "";
                        List<int> listaCategoriasIds = new List<int>();
                        if (!string.IsNullOrEmpty(catIdsStr))
                        {
                            listaCategoriasIds = catIdsStr.Split(',').Select(int.Parse).ToList();
                        }

                        // 3. Mapear el objeto final
                        lista.Add(new
                        {
                            idPromocion = Convert.ToInt32(row["id_promocion"]),
                            nombre = row["nombre"].ToString(),
                            porcentaje = Convert.ToDecimal(row["valor_descuento"]),
                            fechaInicio = Convert.ToDateTime(row["fecha_inicio"]),
                            fechaFin = Convert.ToDateTime(row["fecha_fin"]),
                            estado = Convert.ToBoolean(row["estado"]),

                            productosIds = listaProductosIds,
                            categoriasIds = listaCategoriasIds 
                        });
                    }
                }
                return Ok(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = ex.Message });
            }
            finally { dt?.Dispose(); }
        }

        [HttpPost("Guardar")]
        public IActionResult Guardar([FromBody] PromocionRequest req)
        {
            try
            {
                int idPromo = 0;

                if (req.IdPromocion == 0)
                {
                    // --- MODO CREAR ---
                    SqlParameter idOut = new SqlParameter("@id_promocion", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    List<SqlParameter> p = new List<SqlParameter>
                    {
                        new SqlParameter("@nombre", req.Nombre),
                        new SqlParameter("@descuento", req.Porcentaje),
                        new SqlParameter("@f_inicio", req.FechaInicio),
                        new SqlParameter("@f_fin", req.FechaFin),
                        // Ya no usamos la columna id_categoria de la tabla principal
                        new SqlParameter("@id_categoria", DBNull.Value),
                        idOut
                    };

                    ClsConexion.EjecutarProcedimientoAccion("AgregarPromocion", p);
                    idPromo = (int)idOut.Value;
                }
                else
                {
                    // --- MODO ACTUALIZAR ---
                    idPromo = req.IdPromocion ?? 0;

                    List<SqlParameter> p = new List<SqlParameter>
                    {
                        new SqlParameter("@id_promocion", idPromo),
                        new SqlParameter("@nombre", req.Nombre),
                        new SqlParameter("@descuento", req.Porcentaje),
                        new SqlParameter("@f_inicio", req.FechaInicio),
                        new SqlParameter("@f_fin", req.FechaFin),
                    };

                    ClsConexion.EjecutarProcedimientoAccion("ActualizarPromocion", p);

                    // LIMPIEZA TOTAL: Borramos cualquier relación vieja para empezar de cero
                    List<SqlParameter> pDelProd = new List<SqlParameter> { new SqlParameter("@id_promocion", idPromo) };
                    ClsConexion.EjecutarProcedimientoAccion("LimpiarProductosPromocion", pDelProd);

                    List<SqlParameter> pDelCat = new List<SqlParameter> { new SqlParameter("@id_promocion", idPromo) };
                    ClsConexion.EjecutarProcedimientoAccion("LimpiarCategoriasPromocion", pDelCat);
                }

                if (req.ProductosIds != null && req.ProductosIds.Count > 0)
                {
                    // Insertamos la lista de productos
                    foreach (int idProd in req.ProductosIds)
                    {
                        List<SqlParameter> pProd = new List<SqlParameter> {
                            new SqlParameter("@id_promocion", idPromo),
                            new SqlParameter("@id_producto", idProd)
                        };
                        ClsConexion.EjecutarProcedimientoAccion("AsignarProductoPromocion", pProd);
                    }
                }
                else if (req.CategoriasIds != null && req.CategoriasIds.Count > 0)
                {
                    // Usamos "else if" para garantizar que SOLO sea por categorías o SOLO por productos
                    foreach (int idCat in req.CategoriasIds)
                    {
                        List<SqlParameter> pCat = new List<SqlParameter> {
                            new SqlParameter("@id_promocion", idPromo),
                            new SqlParameter("@id_categoria", idCat)
                        };
                        ClsConexion.EjecutarProcedimientoAccion("AsignarCategoriaPromocion", pCat);
                    }
                }

                string mensajeAccion = req.IdPromocion == 0 ? "Promoción creada correctamente" : "Promoción actualizada correctamente";
                return Ok(new { success = true, mensaje = mensajeAccion });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpDelete("Desactivar/{id}")]
        public IActionResult Desactivar(int id)
        {
            try
            {
                SqlParameter p = new SqlParameter("@id_promocion", id);
                ClsConexion.EjecutarProcedimientoAccion("DesactivarPromocion", new List<SqlParameter> { p });
                return Ok(new { success = true });
            }
            catch (Exception ex) { return BadRequest(new { success = false, mensaje = ex.Message }); }
        }
    }
}

public class PromocionRequest
{
    public int? IdPromocion { get; set; } // Agregado para edición
    public string Nombre { get; set; }
    public decimal Porcentaje { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public List<int> ProductosIds { get; set; }
    public List<int> CategoriasIds { get; set; }
}

