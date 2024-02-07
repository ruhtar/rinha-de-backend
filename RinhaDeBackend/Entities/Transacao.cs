using System.ComponentModel.DataAnnotations.Schema;

namespace RinhaDeBackend.Entities
{
    [Table("transacoes")]
    public class Transacao
    {
        public int Id { get; set; }
        public int Cliente_Id { get; set; }
        public int Valor { get; set; }
        public char Tipo { get; set; }
        public string Descricao { get; set; }
        public DateTime Realizada_Em { get; set; }

    }
}
