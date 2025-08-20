import { AuthConfig } from 'angular-oauth2-oidc';

export const authConfig: AuthConfig = {
  issuer: 'http://localhost:8080/realms/b-commerce-realm',
  redirectUri: window.location.origin,
  clientId: 'frontend',
  responseType: 'code',
  scope: 'openid profile email',
  showDebugInformation: true,
  requireHttps: false,
  strictDiscoveryDocumentValidation: false,
};