import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { EstoqueService } from '../../services/estoque.service';
import { Produto } from '../../models/produto.model';

@Component({
  selector: 'app-produtos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './produtos.component.html',
  styleUrls: ['./produtos.component.scss']
})
export class ProdutosComponent implements OnInit {
  produtos: Produto[] = [];
  
  novoProduto = {
    codigo: '',
    descricao: '',
    saldo: 1
  };

  errorMessage = '';
  successMessage = '';
  
  // Variáveis para a Inteligência Artificial Real (Gemini)
  aiInsight: string = '';
  isAnalyzing: boolean = false;

  constructor(
    private estoqueService: EstoqueService,
    private http: HttpClient, // Injetamos o HttpClient para chamar o endpoint de IA
    private cdr: ChangeDetectorRef 
  ) {}

  ngOnInit(): void {
    this.carregarProdutos();
  }

  carregarProdutos(): void {
    this.estoqueService.listarProdutos().subscribe({
      next: (data) => {
        this.produtos = data;
        this.cdr.detectChanges();
      },
      error: (err: Error) => {
        this.errorMessage = err.message;
        this.cdr.detectChanges();
      }
    });
  }

  cadastrarProduto(): void {
    this.successMessage = '';
    this.errorMessage = '';

    if (!this.novoProduto.codigo || !this.novoProduto.descricao) {
      this.errorMessage = 'Preencha todos os campos obrigatórios do produto.';
      return;
    }
    
    const idempotencyKey = crypto.randomUUID();

    this.estoqueService.cadastrarProduto(this.novoProduto, idempotencyKey).subscribe({
      next: () => {
        this.successMessage = 'Produto cadastrado com sucesso!';
        this.errorMessage = '';
        this.novoProduto = { codigo: '', descricao: '', saldo: 1 }; 
        this.carregarProdutos(); 
      },
      error: (err: Error) => {
        this.errorMessage = err.message;
        this.cdr.detectChanges();
      }
    });
  }

  // Método que consome a API real do Gemini no backend
  analisarEstoqueComIA(): void {
    this.isAnalyzing = true;
    this.aiInsight = '';

    this.http.get<{ insight: string }>('http://localhost:5245/api/produtos/ia-insight').subscribe({
      next: (res) => {
        let textoFormatado = res.insight
        ? res.insight
              .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
              .replace(/\*(.*?)\*/g, '<em>$1</em>')
          : '';
          
        this.aiInsight = textoFormatado;
        this.isAnalyzing = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.aiInsight = 'Erro ao conectar com o serviço de Inteligência Artificial.';
        this.isAnalyzing = false;
        this.cdr.detectChanges();
      }
    });
  }
}