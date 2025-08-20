import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { 
  LoginRequest, 
  LoginResponse, 
  CreateUserRequest, 
  CreateUserResponse, 
  UserProfile 
} from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiUrl = 'http://localhost:5000'; // API Gateway URL

  // Signals for authentication state
  private _isAuthenticated = signal<boolean>(false);
  private _currentUser = signal<UserProfile | null>(null);
  private _isLoading = signal<boolean>(false);

  // Public computed signals
  public isAuthenticated = computed(() => this._isAuthenticated());
  public currentUser = computed(() => this._currentUser());
  public isLoading = computed(() => this._isLoading());
  public userName = computed(() => {
    const user = this._currentUser();
    return user ? `${user.firstName} ${user.lastName}` : '';
  });
  public userEmail = computed(() => this._currentUser()?.email || '');

  constructor() {
    this.initializeAuth();
  }

  private initializeAuth(): void {
    // Check if user is already authenticated
    const token = this.getStoredToken();
    if (token) {
      this._isAuthenticated.set(true);
      this.loadUserProfile();
    }
  }

  async login(loginData: LoginRequest): Promise<LoginResponse> {
    this._isLoading.set(true);
    try {
      const response = await firstValueFrom(
        this.http.post<LoginResponse>(`${this.apiUrl}/api/client/login`, loginData)
      );
      
      // Store tokens
      this.setTokens(response.accessToken, response.refreshToken);
      
      // Load user profile
      await this.loadUserProfile();
      
      this._isAuthenticated.set(true);
      return response;
    } catch (error) {
      this.clearTokens();
      throw error;
    } finally {
      this._isLoading.set(false);
    }
  }

  async register(registerData: CreateUserRequest): Promise<CreateUserResponse> {
    this._isLoading.set(true);
    try {
      const response = await firstValueFrom(
        this.http.post<CreateUserResponse>(`${this.apiUrl}/api/client/create-user`, registerData)
      );
      
      return response;
    } catch (error) {
      throw error;
    } finally {
      this._isLoading.set(false);
    }
  }

  async loadUserProfile(): Promise<void> {
    try {
      const profile = await firstValueFrom(
        this.http.get<UserProfile>(`${this.apiUrl}/api/client/profile`)
      );
      this._currentUser.set(profile);
    } catch (error) {
      console.error('Error loading user profile:', error);
      this.logout();
    }
  }

  logout(): void {
    this.clearTokens();
    this._isAuthenticated.set(false);
    this._currentUser.set(null);
    this.router.navigate(['/login']);
  }

  private setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', refreshToken);
  }

  private clearTokens(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }

  private getStoredToken(): string | null {
    return localStorage.getItem('access_token');
  }

  public getAccessToken(): string | null {
    return this.getStoredToken();
  }

  public hasValidToken(): boolean {
    const token = this.getStoredToken();
    if (!token) return false;
    
    try {
      // Simple JWT expiration check
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      return payload.exp > currentTime;
    } catch {
      return false;
    }
  }
}