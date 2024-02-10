using System.ComponentModel.DataAnnotations;

namespace RinhaDeBackend.Dtos
{
    public class RequestTransacaoDto
    {
        public double Valor { get; set; } //o .NET por padrão retorna um 400 para requisições que não são parseáveis. 
        //Coloquei um double aqui somente para aceitar a requisição e retornar um 422 no caso de ser um valor com ponto flutuante.
        public char Tipo { get; set; }
        public string? Descricao { get; set; }
    }
}
