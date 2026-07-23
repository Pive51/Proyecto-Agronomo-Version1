using AgroSystemServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AgroSystemServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MarcaController : ControllerBase
    {
        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            try
            {
                DataTable dt = ClsConexion.EjecutarProcedimientoConsulta("ObtenerMarca");
                var lista = dt.AsEnumerable().Select(row => new
                {
                    idMarca = row["id_marca"],
                    nombre = row["nombre"],
                    descripcion = row["descripcion"]
                });
                return Ok(new { success = true, data = lista });
            }
            catch (Exception ex) { return StatusCode(500, new { success = false, mensaje = ex.Message }); }
        }

        [HttpPost("Guardar")]
        public IActionResult Guardar([FromBody] MarcaRequest req)
        {
            var p = new List<SqlParameter> {
            new SqlParameter("@id_marca", req.IdMarca ?? (object)DBNull.Value),
            new SqlParameter("@nombre", req.Nombre),
            new SqlParameter("@descripcion", req.Descripcion ?? (object)DBNull.Value)
        };
            ClsConexion.EjecutarProcedimientoAccion("InsertarMarca", p);
            return Ok(new { success = true });
        }
    }

    public class MarcaRequest
    {
        public int? IdMarca { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
    }
}
