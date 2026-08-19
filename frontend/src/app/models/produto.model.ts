export interface Produto {
  codigo: string;
  descricao: string;
  saldo: number;
}

export interface CadastrarProdutoRequest {
  codigo: string;
  descricao: string;
  saldo: number;
}