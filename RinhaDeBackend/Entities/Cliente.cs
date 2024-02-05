using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;

namespace RinhaDeBackend.Entities
{
    [Table("clientes")]
    public class Cliente
    {
        public int Id { get; set; }
        public int Limite { get; set; }
        public string Nome { get; set; }
        public ICollection<Transacao> Transacoes { get; set; } = new List<Transacao>();

        public int Saldo { get; set; }
    }
}
