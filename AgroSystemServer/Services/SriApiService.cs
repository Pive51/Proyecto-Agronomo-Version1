using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgroSystemServer.Services
{
    public class SriApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://api-facturacion-terceros.ec/v1/facturas"; // URL de la API que contrates
        private readonly string _apiToken = "TU_TOKEN_SECRETO_DE_LA_API";

        public SriApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<RespuestaSriDto> EnviarFacturaSincrona(FacturaSriRequest factura)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiToken);

                string json = JsonSerializer.Serialize(factura);
                var contenido = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(_apiUrl, contenido);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // La API procesó, firmó y autorizó la factura
                    return JsonSerializer.Deserialize<RespuestaSriDto>(responseBody);
                }
                else
                {
                    // Hubo un error de validación (ej. RUC incorrecto)
                    return new RespuestaSriDto
                    {
                        Estado = "RECHAZADO",
                        Mensaje = $"Error API: {response.StatusCode} - {responseBody}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new RespuestaSriDto { Estado = "ERROR_CONEXION", Mensaje = ex.Message };
            }
        }
    }

    // --- MODELOS DE DATOS (DTOs) PARA EL JSON ---
    public class RespuestaSriDto
    {
        public string Estado { get; set; } // "AUTORIZADO", "RECHAZADO"
        public string ClaveAcceso { get; set; }
        public string Mensaje { get; set; }
        public string UrlPdf { get; set; } // Link al PDF del RIDE
        public string UrlXml { get; set; }
    }

    public class FacturaSriRequest
    {
        public string Ambiente { get; set; } // 1: Pruebas, 2: Producción
        public ClienteSri Cliente { get; set; }
        public List<DetalleSri> Detalles { get; set; }
        // Aquí agregarías los datos de tu empresa (Emisor), impuestos, formas de pago, etc.
        // según la documentación exacta de la API que contrates.
    }

    public class ClienteSri
    {
        public string TipoIdentificacion { get; set; } // 04=RUC, 05=Cedula
        public string Identificacion { get; set; }
        public string RazonSocial { get; set; }
        public string Correo { get; set; }
    }

    public class DetalleSri
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
        public int CodigoPorcentajeIva { get; set; } // 0 = 0%, 2 = 12%, 4 = 15%
    }
}