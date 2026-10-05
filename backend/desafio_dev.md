**DESAFIO**

1. Considerando que o json abaixo tem registros de vendas de um time comercial, faça um programa que leia os dados e calcule a comissão de cada vendedor, seguindo a seguinte regra para cada venda:

- Vendas abaixo de R$100,00 não gera comissão

- Vendas abaixo de R$500,00 gera 1% de comissão

- A partir de R$500,00 gera 5% de comissão

**{**

  **"vendas": [**

    **{ "vendedor": "João Silva", "valor": 1200.50 },**

    **{ "vendedor": "João Silva", "valor": 950.75 },**

    **{ "vendedor": "João Silva", "valor": 1800.00 },**

    **{ "vendedor": "João Silva", "valor": 1400.30 },**

    **{ "vendedor": "João Silva", "valor": 1100.90 },**

    **{ "vendedor": "João Silva", "valor": 1550.00 },**

    **{ "vendedor": "João Silva", "valor": 1700.80 },**

    **{ "vendedor": "João Silva", "valor": 250.30 },**

    **{ "vendedor": "João Silva", "valor": 480.75 },**

    **{ "vendedor": "João Silva", "valor": 320.40 },**

    ****

    **{ "vendedor": "Maria Souza", "valor": 2100.40 },**

    **{ "vendedor": "Maria Souza", "valor": 1350.60 },**

    **{ "vendedor": "Maria Souza", "valor": 950.20 },**

    **{ "vendedor": "Maria Souza", "valor": 1600.75 },**

    **{ "vendedor": "Maria Souza", "valor": 1750.00 },**

    **{ "vendedor": "Maria Souza", "valor": 1450.90 },**

    **{ "vendedor": "Maria Souza", "valor": 400.50 },**

    **{ "vendedor": "Maria Souza", "valor": 180.20 },**

    **{ "vendedor": "Maria Souza", "valor": 90.75 },**

    ****

    **{ "vendedor": "Carlos Oliveira", "valor": 800.50 },**

    **{ "vendedor": "Carlos Oliveira", "valor": 1200.00 },**

    **{ "vendedor": "Carlos Oliveira", "valor": 1950.30 },**

    **{ "vendedor": "Carlos Oliveira", "valor": 1750.80 },**

    **{ "vendedor": "Carlos Oliveira", "valor": 1300.60 },**

    **{ "vendedor": "Carlos Oliveira", "valor": 300.40 },**

    **{ "vendedor": "Carlos Oliveira", "valor": 500.00 },**

    **{ "vendedor": "Carlos Oliveira", "valor": 125.75 },**

    ****

    **{ "vendedor": "Ana Lima", "valor": 1000.00 },**

    **{ "vendedor": "Ana Lima", "valor": 1100.50 },**

    **{ "vendedor": "Ana Lima", "valor": 1250.75 },**

    **{ "vendedor": "Ana Lima", "valor": 1400.20 },**

    **{ "vendedor": "Ana Lima", "valor": 1550.90 },**

    **{ "vendedor": "Ana Lima", "valor": 1650.00 },**

    **{ "vendedor": "Ana Lima", "valor": 75.30 },**

    **{ "vendedor": "Ana Lima", "valor": 420.90 },**

    **{ "vendedor": "Ana Lima", "valor": 315.40 }**

  **]**

**}**

2. Faça um programa onde eu possa lançar movimentações de estoque dos produtos que estão no json abaixo, dando entrada ou saída da mercadoria no meu depósito, onde cada movimentação deve ter:

- Um número identificador único.

- Uma descrição para identificar o tipo da movimentação realizada

E que ao final da movimentação me retorne a qtde final do estoque do produto movimentado.

**{**

**"estoque":**

**[**

  **{**

**"codigoProduto": 101,**

**"descricaoProduto": "Caneta Azul",**

**"estoque": 150**

  **},**

  **{**

**"codigoProduto": 102,**

**"descricaoProduto": "Caderno Universitário",**

**"estoque": 75**

  **},**

  **{**

**"codigoProduto": 103,**

**"descricaoProduto": "Borracha Branca",**

**"estoque": 200**

  **},**

  **{**

**"codigoProduto": 104,**

**"descricaoProduto": "Lápis Preto HB",**

**"estoque": 320**

  **},**

  **{**

**"codigoProduto": 105,**

**"descricaoProduto": "Marcador de Texto Amarelo",**

**"estoque": 90**

  **}**

**]**

**}**

3. Faça um programa que a partir de um valor e de uma data de vencimento, calcule o valor dos juros na data de hoje considerando que a multa seja de 2,5% ao dia.
