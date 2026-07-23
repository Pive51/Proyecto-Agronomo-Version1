using AgroSystemServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace AgroSystemServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LotesController : ControllerBase
    {
        [HttpGet("Listar/{idProducto?}")]
        public IActionResult Listar(int? idProducto)
        {
            var parametros = new List<SqlParameter> { new SqlParameter("@id_producto", (object?)idProducto ?? DBNull.Value) };
            DataTable dt = ClsConexion.EjecutarProcedimientoConsulta("ObtenerLotesListar", parametros);

            var lista = dt.AsEnumerable().Select(row => new
            {
                idLote = row["id_lote"],
                idProducto = row["id_producto"],
                nombreProducto = row["nombre_producto"],
                codigoLote = row["codigo_lote"],
                fechaElaboracion = row["fecha_elaboracion"],
                fechaVencimiento = row["fecha_vencimiento"],
                cantidadInicial = row["cantidad_inicial"],
                stockRestante = row["stock_restante"]
            });
            return Ok(new { success = true, data = lista });
        }

        [HttpPost("Agregar")]
        public IActionResult Agregar([FromBody] LoteRequest req)
        {
            var p = new List<SqlParameter> {
            new SqlParameter("@id_producto", req.IdProducto),
            new SqlParameter("@codigo_lote", req.CodigoLote),
            new SqlParameter("@fecha_elaboracion", (object?)req.FechaElaboracion ?? DBNull.Value),
            new SqlParameter("@fecha_vencimiento", req.FechaVencimiento),
            new SqlParameter("@cantidad_inicial", req.CantidadInicial)
        };
            ClsConexion.EjecutarProcedimientoAccion("Lotes_Agregar", p);
            return Ok(new { success = true });
        }
    }

    public class LoteRequest
    {
        public int IdProducto { get; set; }
        public string CodigoLote { get; set; }
        public DateTime? FechaElaboracion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal CantidadInicial { get; set; }
    }
}
