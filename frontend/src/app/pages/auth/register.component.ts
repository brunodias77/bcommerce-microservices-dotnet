import { Component, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CreateUserRequest } from '../../core/models/auth.models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div class="max-w-md w-full space-y-8">
        <div>
          <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Criar nova conta
          </h2>
          <p class="mt-2 text-center text-sm text-gray-600">
            Ou
            <a routerLink="/login" class="font-medium text-indigo-600 hover:text-indigo-500">
              faça login em sua conta existente
            </a>
          </p>
        </div>
        
        <form class="mt-8 space-y-6" [formGroup]="registerForm" (ngSubmit)="onSubmit()">
          <div class="rounded-md shadow-sm -space-y-px">
            <!-- Nome -->
            <div>
              <label for="firstName" class="sr-only">Nome</label>
              <input
                id="firstName"
                name="firstName"
                type="text"
                formControlName="firstName"
                class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-t-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                [class.border-red-500]="isFieldInvalid('firstName')"
                placeholder="Nome"
              />
              @if (isFieldInvalid('firstName')) {
                <p class="mt-1 text-sm text-red-600">Nome é obrigatório</p>
              }
            </div>
            
            <!-- Sobrenome -->
            <div>
              <label for="lastName" class="sr-only">Sobrenome</label>
              <input
                id="lastName"
                name="lastName"
                type="text"
                formControlName="lastName"
                class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                [class.border-red-500]="isFieldInvalid('lastName')"
                placeholder="Sobrenome"
              />
              @if (isFieldInvalid('lastName')) {
                <p class="mt-1 text-sm text-red-600">Sobrenome é obrigatório</p>
              }
            </div>
            
            <!-- Username -->
            <div>
              <label for="username" class="sr-only">Nome de usuário</label>
              <input
                id="username"
                name="username"
                type="text"
                formControlName="username"
                class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                [class.border-red-500]="isFieldInvalid('username')"
                placeholder="Nome de usuário"
              />
              @if (isFieldInvalid('username')) {
                <p class="mt-1 text-sm text-red-600">
                  @if (registerForm.get('username')?.errors?.['required']) {
                    Nome de usuário é obrigatório
                  }
                  @if (registerForm.get('username')?.errors?.['minlength']) {
                    Nome de usuário deve ter pelo menos 3 caracteres
                  }
                </p>
              }
            </div>
            
            <!-- Email -->
            <div>
              <label for="email" class="sr-only">Email</label>
              <input
                id="email"
                name="email"
                type="email"
                formControlName="email"
                class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                [class.border-red-500]="isFieldInvalid('email')"
                placeholder="Email"
              />
              @if (isFieldInvalid('email')) {
                <p class="mt-1 text-sm text-red-600">
                  @if (registerForm.get('email')?.errors?.['required']) {
                    Email é obrigatório
                  }
                  @if (registerForm.get('email')?.errors?.['email']) {
                    Email deve ter um formato válido
                  }
                </p>
              }
            </div>
            
            <!-- Senha -->
            <div>
              <label for="password" class="sr-only">Senha</label>
              <input
                id="password"
                name="password"
                type="password"
                formControlName="password"
                class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                [class.border-red-500]="isFieldInvalid('password')"
                placeholder="Senha"
              />
              @if (isFieldInvalid('password')) {
                <p class="mt-1 text-sm text-red-600">
                  @if (registerForm.get('password')?.errors?.['required']) {
                    Senha é obrigatória
                  }
                  @if (registerForm.get('password')?.errors?.['minlength']) {
                    Senha deve ter pelo menos 6 caracteres
                  }
                </p>
              }
            </div>
            
            <!-- Confirmar Senha -->
            <div>
              <label for="confirmPassword" class="sr-only">Confirmar Senha</label>
              <input
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                formControlName="confirmPassword"
                class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-b-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                [class.border-red-500]="isFieldInvalid('confirmPassword') || passwordMismatch()"
                placeholder="Confirmar Senha"
              />
              @if (isFieldInvalid('confirmPassword')) {
                <p class="mt-1 text-sm text-red-600">Confirmação de senha é obrigatória</p>
              }
              @if (passwordMismatch()) {
                <p class="mt-1 text-sm text-red-600">As senhas não coincidem</p>
              }
            </div>
          </div>
          
          <!-- Mensagem de erro -->
          @if (errorMessage()) {
            <div class="rounded-md bg-red-50 p-4">
              <div class="flex">
                <div class="ml-3">
                  <h3 class="text-sm font-medium text-red-800">
                    Erro no registro
                  </h3>
                  <div class="mt-2 text-sm text-red-700">
                    <p>{{ errorMessage() }}</p>
                  </div>
                </div>
              </div>
            </div>
          }
          
          <div>
            <button
              type="submit"
              [disabled]="!registerForm.valid || isLoading() || passwordMismatch()"
              class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              @if (isLoading()) {
                <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Criando conta...
              } @else {
                Criar conta
              }
            </button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  
  // Signals para gerenciar estado
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  
  registerForm: FormGroup;
  
  constructor() {
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      username: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    });
  }
  
  // Computed para verificar se as senhas coincidem
  passwordMismatch = computed(() => {
    const password = this.registerForm?.get('password')?.value;
    const confirmPassword = this.registerForm?.get('confirmPassword')?.value;
    return password && confirmPassword && password !== confirmPassword;
  });
  
  isFieldInvalid(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }
  
  async onSubmit() {
    if (this.registerForm.valid && !this.passwordMismatch()) {
      this.isLoading.set(true);
      this.errorMessage.set(null);
      
      const formValue = this.registerForm.value;
      const registerData: CreateUserRequest = {
        username: formValue.username,
        email: formValue.email,
        firstName: formValue.firstName,
        lastName: formValue.lastName,
        password: formValue.password,
        role: 'Customer' // Papel padrão
      };
      
      try {
        const response = await this.authService.register(registerData);
        
        // Redirecionar para login após registro bem-sucedido
        this.router.navigate(['/login'], {
          queryParams: { message: 'Conta criada com sucesso! Faça login para continuar.' }
        });
      } catch (error: any) {
        this.errorMessage.set(
          error.error?.message || 'Erro ao criar conta. Tente novamente.'
        );
      } finally {
        this.isLoading.set(false);
      }
    }
  }
}