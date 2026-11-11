
export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthTokens {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
}

export interface CurrentUser {
  id: string;
  email: string;
  createdAt: string;
}
