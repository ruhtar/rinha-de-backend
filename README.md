A proposta feita é criar uma API que faz transações financeiras entre usuários e subir um sistema composto por um banco de dados, um load balancer e 2 instâncias dessa API. Todo o sistema deve ser dockerizado e subir por um docker-compose. Além disso, existem restrições quanto aos recursos do container, como 550MB de ram e 1.5 unidades de CPU para todos os componentes da aplicação.



O desafio consiste em um teste de carga com mais de 60 mil requisições, além de validações para o controle de concorrência entre as várias inserções e leituras dos dados, a fim de garantir a consistência dos dados.



Minha implementação foi feita usando C#, .NET 8, Dapper, Postgres e Nginx como load balancer. Optei por usar índices, locks pessimistas, transações, Functions e um cache para melhorar a perfomance e garantir a consistência dos dados.
