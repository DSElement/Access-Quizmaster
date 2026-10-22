import {
  InteractionRequiredAuthError,
  PublicClientApplication,
} from '@azure/msal-browser';
import { config } from './config';

export const loginRequest = {
  scopes: [`api://${config.clientId}/access_as_user`],
};

export const msal = new PublicClientApplication({
  auth: {
    clientId: config.clientId,
    authority: 'https://login.microsoftonline.com/consumers',
    redirectUri: config.redirectUri,
    postLogoutRedirectUri: config.redirectUri,
  },
  cache: { cacheLocation: 'localStorage' },
});

export async function getAccessToken(): Promise<string> {
  const account = msal.getActiveAccount();
  if (!account) throw new Error('Sign in with Microsoft to continue.');
  try {
    const result = await msal.acquireTokenSilent({ ...loginRequest, account });
    return result.accessToken;
  } catch (error) {
    // MSAL requires an interactive flow when silent renewal cannot complete.
    if (!(error instanceof InteractionRequiredAuthError)) throw error;
    const result = await msal.acquireTokenPopup({ ...loginRequest, account });
    return result.accessToken;
  }
}
