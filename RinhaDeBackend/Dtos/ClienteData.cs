namespace RinhaDeBackend.Dtos
{
    public class ClienteData
    {
        public int ClienteId { get; set; }
        public List<UltimasTransacoes> Transacoes { get; set; }
        public int Saldo { get; set; }
    }
}
