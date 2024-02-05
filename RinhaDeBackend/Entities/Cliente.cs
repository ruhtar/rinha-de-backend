namespace RinhaDeBackend.Entities
{
    public class Cliente
    {
        public int Id { get; set; }
        public int Limite { get; set; }
        public string Nome { get; set; }
        public List<Transacao> Transacoes { get; set; }

        public int Saldo { get; set; }

    }
}
