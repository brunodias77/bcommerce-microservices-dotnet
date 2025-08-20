export interface CreateUserRequest {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  role?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface CreateUserResponse {
  message: string;
  userId?: string;
  username: string;
  email: string;
  role: string;
  timestamp: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  refreshToken: string;
  timestamp: string;
}

export interface UserProfile {
  id: string;
  keycloakUserId?: string;
  firstName: string;
  lastName: string;
  email: string;
  cpf?: string;
  dateOfBirth?: string;
  phone?: string;
  newsletterOptIn: boolean;
  status: string;
  role: string;
  failedLoginAttempts: number;
  accountLockedUntil?: string;
  emailVerifiedAt?: string;
  createdAt: string;
  updatedAt?: string;
  addresses: Address[];
  consents: Consent[];
  savedCards: SavedCard[];
  timestamp: string;
}

export interface Address {
  id: string;
  street: string;
  number: string;
  complement?: string;
  neighborhood: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  isDefault: boolean;
  type: string;
}

export interface Consent {
  id: string;
  type: string;
  isGranted: boolean;
  grantedAt: string;
  revokedAt?: string;
  ipAddress?: string;
  userAgent?: string;
}

export interface SavedCard {
  id: string;
  nickname: string;
  brand: string;
  lastFourDigits: string;
  expiryDate: string;
  isDefault: boolean;
}

export interface ApiErrorResponse {
  message: string;
  errors?: Array<{ code: string; message: string }>;
  timestamp: string;
}