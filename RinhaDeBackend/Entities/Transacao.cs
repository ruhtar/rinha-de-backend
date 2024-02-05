namespace RinhaDeBackend.Entities
{
    public class Transacao
    {
        public int Id { get; set; }
        public int IdCliente { get; set; }
        public int Valor { get; set; }
        public char Tipo { get; set; }
        public string Descricao { get; set; }
        public DateTime RealizadaEm { get; set; }

        // Relacionamento com clientes
        public Cliente Cliente { get; set; }
    }
}
