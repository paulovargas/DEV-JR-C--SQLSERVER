export interface Venda {
  vendedor: string;
  valor: number;
}

export interface CalculoComissaoRequest {
  vendas: Venda[];
}

export interface ResumoComissao {
  vendedor: string;
  quantidadeVendas: number;
  valorTotalVendas: number;
  comissaoTotal: number;
}

export interface CalculoComissaoResponse {
  vendedores: ResumoComissao[];
  comissaoTotalGeral: number;
}

export interface ProdutoEstoque {
  codigoProduto: number;
  descricaoProduto: string;
  estoque: number;
}

export type TipoMovimentacao = 'entrada' | 'saida';

export interface MovimentacaoEstoqueRequest {
  codigoProduto: number;
  tipo: TipoMovimentacao;
  quantidade: number;
  descricao: string;
}

export interface MovimentacaoEstoque {
  id: string;
  codigoProduto: number;
  descricaoProduto: string;
  tipo: TipoMovimentacao;
  quantidade: number;
  descricao: string;
  estoqueAnterior: number;
  estoqueFinal: number;
  realizadaEm: string;
}

export interface CalculoJurosRequest {
  valor: number;
  dataVencimento: string;
}

export interface CalculoJurosResponse {
  valorOriginal: number;
  dataVencimento: string;
  dataCalculo: string;
  diasAtraso: number;
  taxaDiariaPercentual: number;
  valorJuros: number;
  valorAtualizado: number;
}
