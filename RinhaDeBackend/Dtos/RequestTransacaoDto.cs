using System.ComponentModel.DataAnnotations;

namespace RinhaDeBackend.Dtos
{
    public class RequestTransacaoDto
    {
        public double Valor { get; set; } //o .NET por padrão retorna um 400 para requisições que não são parseáveis. 
        //Coloquei um double aqui somente para aceitar a requisição com Valor = 1.2 e retornar um 422 por ser um valor com ponto flutuante e não um inteiro.
        public char Tipo { get; set; }
        public string? Descricao { get; set; }
    }
}
