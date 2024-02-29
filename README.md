## Rinha de Backend - 2ª edição

### Descrição do Projeto

A rinha tem como objetivo desenvolver uma API para realizar transações financeiras entre usuários, integrado a um sistema composto por um banco de dados, um load balancer e 2 instâncias dessa API. Todo o sistema é dockerizado e é possível subir a aplicação utilizando o docker-compose. 

O desafio pede restrições específicas quanto aos recursos do container, com um limite de 550MB de RAM e 1.5 unidades de CPU para todos os componentes da aplicação. Além disso, enfrentamos um teste de carga com mais de 60 mil requisições, incluindo validações para o controle de concorrência entre inserções e leituras, assegurando a consistência dos dados.

### Tecnologias Utilizadas

- **Linguagem:** C#
- **Framework:** .NET 8
- **Banco de Dados:** Postgres
- **ORM:** Dapper
- **Load Balancer:** Nginx

### Implementação

Para garantir a eficiência e consistência dos dados, optei por utilizar índices, locks pessimistas, transações, Functions e um cache. Essas escolhas visam melhorar a performance da aplicação e assegurar que as operações de transação ocorram de maneira consistente.

### Estrutura do Projeto

O projeto está organizado em containers, cada um cumprindo um papel específico no sistema. O arquivo `docker-compose.yml` descreve a configuração dos serviços, incluindo o banco de dados, as instâncias da API e o load balancer. A arquitetura foi pensada para respeitar as restrições de recursos impostas pelo desafio.

### Teste de Carga

O desafio incluiu um teste de carga envolvendo mais de 60 mil requisições. Implementei validações para controlar a concorrência durante as operações de inserção e leitura de dados, assegurando que a aplicação mantenha a consistência dos dados mesmo sob condições de alta carga.

### Como Rodar o Projeto

Para rodar o projeto, certifique-se de ter o Docker e o docker-compose instalados em sua máquina. Em seguida, execute o seguinte comando na raiz do projeto:

```bash
docker-compose up -d
