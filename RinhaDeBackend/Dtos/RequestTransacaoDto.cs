using System.ComponentModel.DataAnnotations;

namespace RinhaDeBackend.Dtos
{
    public class RequestTransacaoDto
    {
        [Required]
        public int Valor { get; set; }
        [Required]
        public char Tipo { get; set; }
        [Required]
        public string Descricao { get; set; }
    }
}
