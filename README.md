## Rinha de Backend - 2ª edição

### Descrição do Projeto

A rinha é uma competição organizada pelo Francisco Zanfranceschi, Software Engineering Specialist no Nubank. Ela tem como objetivo desenvolver uma API para realizar transações financeiras entre usuários, integrada a um sistema composto por um banco de dados, um load balancer e 2 instâncias dessa API. Todo o sistema deve ser dockerizado e é possível subir a aplicação utilizando o docker-compose. 

O desafio pede restrições específicas quanto aos recursos dos contêineres, com um limite de 550MB de RAM e 1.5 unidades de CPU para todos os componentes da aplicação. Além disso, existe uma etapa de teste de carga com mais de 60 mil requisições, incluindo validações para o controle de concorrência entre inserções e leituras, assegurando a consistência dos dados.

Você pode conferir a postagem do evento no link abaixo:

[Repositório da Rinha](https://github.com/zanfranceschi/rinha-de-backend-2024-q1)

### Tecnologias utilizadas

- **Linguagem:** C#
- **Framework:** .NET 8
- **Banco de Dados:** Postgres
- **Micro ORM:** Dapper
- **Load Balancer:** Nginx

### Teste de Carga

O desafio incluiu um teste de carga envolvendo mais de 60 mil requisições. Implementei algumas medidas, como locks pessimistas, para controlar a concorrência durante as operações de inserção e leitura de dados, assegurando que a aplicação mantenha a consistência dos dados mesmo sob condições de alta carga.

![TesteDeCarga](https://github.com/ruhtar/rinha-de-backend/assets/83853014/d4d93494-51d1-46ee-a8df-c0a3f0e54735)


### Implementação

Para garantir a eficiência e consistência dos dados, optei por utilizar índices, locks pessimistas, transações, Functions e um cache. Essas escolhas visam melhorar a performance da aplicação e assegurar que as operações de transação ocorram de maneira consistente.

### Estrutura do Projeto

O projeto está organizado em containers, cada um cumprindo um papel específico no sistema. O arquivo `docker-compose.yml` descreve a configuração dos serviços, incluindo o banco de dados, as instâncias da API e o load balancer. A arquitetura foi pensada para respeitar as restrições de recursos impostas pelo desafio.




### Como Rodar o Projeto

Para rodar o projeto, certifique-se de ter o Docker e o docker-compose instalados em sua máquina. Em seguida, execute o seguinte comando na raiz do projeto:

```bash
docker-compose up -d
