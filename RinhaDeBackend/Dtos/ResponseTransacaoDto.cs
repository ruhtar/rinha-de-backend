using System.Text.Json.Serialization;

namespace RinhaDeBackend.Dtos
{
    public class ResponseTransacaoDto
    {
        [JsonPropertyName("limite")]
        public int Limite { get; set; }
        [JsonPropertyName("saldo")]
        public int Saldo { get; set; }
    }
}
