function requiredSetting(name: string): string {
  const value: unknown = import.meta.env[name];
  if (typeof value !== 'string' || value.trim() === '') {
    throw new Error(
      `Missing ${name}. Copy frontend/.env.example to frontend/.env and restart Vite.`,
    );
  }
  return value;
}

export const config = {
  apiBaseUrl: requiredSetting('VITE_API_BASE_URL'),
  clientId: requiredSetting('VITE_ENTRA_CLIENT_ID'),
  redirectUri: requiredSetting('VITE_REDIRECT_URI'),
};
