namespace RinhaDeBackend.Dtos
{
    public class ResponseExtratoDto
    {
        public SaldoInfo Saldo { get; set; }
        public List<UltimasTransacoes> ultimas_transacoes { get; set; }
    }

    public class SaldoInfo
    {
        public int Total { get; set; }
        public DateTime data_extrato { get; set; }
        public int Limite { get; set; }
    }

    public class UltimasTransacoes
    {
        public int valor { get; set; }
        public char tipo { get; set; }
        public string descricao { get; set; }
        public DateTime realizada_em { get; set; }
    }
}
