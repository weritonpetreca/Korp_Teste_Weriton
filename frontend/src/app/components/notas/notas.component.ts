import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { FaturamentoService } from '../../services/faturamento.service';
import { EstoqueService } from '../../services/estoque.service';
import { Produto } from '../../models/produto.model';
import { ItemNotaRequest, NotaFiscalResponse } from '../../models/nota-fiscal.model';

@Component({
  selector: 'app-notas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './notas.component.html',
  styleUrls: ['./notas.component.scss']
})
export class NotasComponent implements OnInit {
  produtosDisponiveis: Produto[] = [];
  itensDaNota: ItemNotaRequest[] = [];
  notasCriadas: NotaFiscalResponse[] = [];

  // Item temporário sendo montado antes de adicionar à nota
  itemAtual: ItemNotaRequest = { codigoProduto: '', quantidade: 1 };

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private faturamentoService: FaturamentoService,
    private estoqueService: EstoqueService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.carregarProdutosDisponiveis();
    this.carregarNotas();
  }

  carregarNotas(): void {
    this.faturamentoService.listarNotas().subscribe({
      next: (notas) => {
        this.notasCriadas = notas.sort((a, b) => {
          const dataA = a.dataCriacao ? new Date(a.dataCriacao).getTime() : 0;
          const dataB = b.dataCriacao ? new Date(b.dataCriacao).getTime() : 0;
          return dataB - dataA;
        });
        this.cdr.detectChanges();
      },
      error: (err: Error) => {
        // Se a API falhar silenciosamente ao listar vazias, tratamos aqui
        this.cdr.detectChanges();
      }
    });
  }

  carregarProdutosDisponiveis(): void {
    this.estoqueService.listarProdutos().subscribe({
      next: (data) => { 
        this.produtosDisponiveis = data;
        this.cdr.detectChanges(); 
      },
      error: (err: Error) => { 
        this.errorMessage = err.message;
        this.cdr.detectChanges();
      }
    });
  }

  // MÉTODO QUE ESTAVA FALTANDO: Calcula o saldo real disponível descontando o já adicionado na nota atual
  getSaldoDisponivel(codigo: string): number {
    const produto = this.produtosDisponiveis.find(p => p.codigo === codigo);
    if (!produto) return 0;

    const jaAdicionado = this.itensDaNota
      .filter(i => i.codigoProduto === codigo)
      .reduce((sum, i) => sum + i.quantidade, 0);

    return produto.saldo - jaAdicionado;
  }

  adicionarItem(): void {
    this.successMessage = '';
    this.errorMessage = '';

    if (!this.itemAtual.codigoProduto || this.itemAtual.quantidade <= 0) {
      this.errorMessage = 'Selecione um produto válido e quantidade maior que zero.';
      return;
    }

    const saldoRestante = this.getSaldoDisponivel(this.itemAtual.codigoProduto);
    if (this.itemAtual.quantidade > saldoRestante) {
      this.errorMessage = `Quantidade indisponível em estoque. Saldo restante: ${saldoRestante}`;
      return;
    }

    // Agregação: Se o produto já existe na lista temporária, soma a quantidade em vez de duplicar a linha
    const itemExistente = this.itensDaNota.find(i => i.codigoProduto === this.itemAtual.codigoProduto);
    if (itemExistente) {
      itemExistente.quantidade += Number(this.itemAtual.quantidade);
    } else {
      this.itensDaNota.push({ 
        codigoProduto: this.itemAtual.codigoProduto, 
        quantidade: Number(this.itemAtual.quantidade) 
      });
    }

    this.itemAtual = { codigoProduto: '', quantidade: 1 }; // Reseta seleção
  }

  removerItem(index: number): void {
    this.itensDaNota.splice(index, 1);
  }

  criarNotaFiscal(): void {
    this.successMessage = '';
    this.errorMessage = '';

    if (this.itensDaNota.length === 0) {
      this.errorMessage = 'Adicione pelo menos um item para criar a nota fiscal.';
      return;
    }

    const idempotencyKey = crypto.randomUUID();
    const request = { itens: this.itensDaNota };

    this.faturamentoService.criarNota(request, idempotencyKey).subscribe({
      next: (res) => {
        if (!res || !res.numero) {
          throw new Error('Contrato violado: O microsserviço de Faturamento não retornou o número da nota.');
        }
        this.successMessage = 'Nota Fiscal criada com sucesso (Status: Aberta)!';
        this.errorMessage = '';
        this.notasCriadas.unshift({
          numero: res.numero,
          status: 'Aberta',
          itens: [...request.itens],
          dataCriacao: new Date().toLocaleString()
        });
        this.itensDaNota = []; // Limpa os itens
        this.carregarProdutosDisponiveis(); // Atualiza os saldos gerais
        this.cdr.detectChanges();
      },
      error: (err: Error) => { 
        this.errorMessage = err.message;
        this.cdr.detectChanges();
      }
    });
  }

  imprimirNota(nota: NotaFiscalResponse): void {
    this.successMessage = '';
    this.errorMessage = '';

    if (nota.status !== 'Aberta') {
      this.errorMessage = 'Não é permitido imprimir notas com status diferente de Aberta.';
      return;
    }

    this.isLoading = true; // Ativa o indicador de processamento
    const idempotencyKey = crypto.randomUUID();

    this.faturamentoService.imprimirNota(nota.numero, idempotencyKey).pipe(
      finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: () => {
        nota.status = 'Fechada'; // Atualiza o status visualmente para Fechada
        nota.dataFechamento = new Date().toLocaleString();
        this.successMessage = `Nota ${nota.numero} impressa com sucesso e saldo atualizado!`;
        this.carregarProdutosDisponiveis(); // Atualiza a listagem de produtos locais e do estoque
      },
      error: (err: Error) => {
        this.errorMessage = err.message;
      }
    });
  }
}