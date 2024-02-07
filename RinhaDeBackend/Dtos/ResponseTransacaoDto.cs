using System.Text.Json.Serialization;

namespace RinhaDeBackend.Dtos
{
    public class ResponseTransacaoDto
    {
        public int Limite { get; set; }
        public int Saldo { get; set; }
    }
}
