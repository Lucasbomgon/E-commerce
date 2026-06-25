# E-commerce API

Projeto de estudos para construir um sistema de e-commerce com ASP.NET Core Web API.

## Tecnologias

- ASP.NET Core Web API
- Entity Framework Core
- MySQL
- JWT

## Funcionalidades planejadas

- Cadastro e login de usuarios com JWT
- Cadastro e consulta de produtos e categorias
- Carrinho de compras
- Finalizacao de pedidos
- Historico de pedidos por usuario

## Como configurar o banco

Atualize a connection string em `appsettings.json` conforme seu MySQL local:

```json
"DefaultConnection": "server=localhost;port=3306;database=ecommerce_db;user=root;password=;"
```

## Status

Parte 1: estrutura inicial da API criada.
Parte 2: entidades e contexto do Entity Framework Core configurados.
Parte 3: cadastro e login com JWT implementados.
Parte 4: endpoints de produtos e categorias implementados.
Parte 5: carrinho de compras autenticado implementado.
