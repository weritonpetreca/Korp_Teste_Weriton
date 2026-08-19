export interface ItemNotaRequest {
  codigoProduto: string;
  quantidade: number;
}

export interface CriarNotaFiscalRequest {
  itens: ItemNotaRequest[];
}

export interface NotaFiscalResponse {
  numero: string;
  status: 'Aberta' | 'Fechada';
  itens: ItemNotaRequest[];
  dataCriacao?: string;
  dataFechamento?: string;
}