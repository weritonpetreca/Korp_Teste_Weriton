import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Produto, CadastrarProdutoRequest } from '../models/produto.model';

@Injectable({
  providedIn: 'root'
})
export class EstoqueService {
  // Base URL pointing to our backend Estoque API
  private apiUrl = 'http://localhost:5245/api/produtos';

  constructor(private http: HttpClient) {}

  listarProdutos(): Observable<Produto[]> {
    return this.http.get<Produto[]>(this.apiUrl);
  }

  cadastrarProduto(request: CadastrarProdutoRequest, idempotencyKey: string): Observable<any> {
    // Passando o header de idempotência que nosso backend exige rigorosamente!
    const headers = { 'X-Idempotency-Key': idempotencyKey };
    return this.http.post(this.apiUrl, request, { headers });
  }

  obterProduto(codigo: string): Observable<Produto> {
    return this.http.get<Produto>(`${this.apiUrl}/${codigo}`);
  }
}