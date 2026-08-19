import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'Ocorreu um erro desconhecido no sistema.';

      // O backend .NET retorna o padrão RFC 7807 (ProblemDetails)
      if (error.error && typeof error.error === 'object') {
        // Se o backend enviou uma mensagem específica de erro ou título de problema
        errorMessage = error.error.detail || error.error.title || error.message;
      } else if (error.status === 0) {
        errorMessage = 'Falha de conexão: O microsserviço está inacessível ou fora do ar.';
      } else if (error.status === 503) {
        errorMessage = 'Serviço indisponível (Resiliência acionada): Falha na comunicação entre os microsserviços.';
      }

      console.error('[Global Error Interceptor intercepted an error]:', {
        status: error.status,
        url: error.url,
        message: errorMessage,
        fullError: error
      });

      // Retorna o erro tratável para o componente exibir ao usuário se necessário
      return throwError(() => new Error(errorMessage));
    })
  );
};