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
        public int Valor { get; set; }
        public char Tipo { get; set; }
        public string Descricao { get; set; }
        public DateTime realizada_em { get; set; }
    }
}
