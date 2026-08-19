import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotaFiscalResponse, CriarNotaFiscalRequest } from '../models/nota-fiscal.model';

@Injectable({
  providedIn: 'root'
})
export class FaturamentoService {
  private apiUrl = 'http://localhost:5259/api/notas';

  constructor(private http: HttpClient) {}

  listarNotas(): Observable<NotaFiscalResponse[]> {
    return this.http.get<NotaFiscalResponse[]>(this.apiUrl);
  }

  criarNota(request: CriarNotaFiscalRequest, idempotencyKey: string): Observable<any> {
    return this.http.post<any>(this.apiUrl, request, {
      headers: { 'X-Idempotency-Key': idempotencyKey }
    });
  }

  imprimirNota(numero: string, idempotencyKey: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${numero}/imprimir`, {}, {
      headers: { 'X-Idempotency-Key': idempotencyKey }
    });
  }
}