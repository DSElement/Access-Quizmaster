import React from 'react';
import ReactDOM from 'react-dom/client';
import { EventType, type AuthenticationResult } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { msal } from './auth';
import App from './App';
import './styles.css';

msal.addEventCallback((event) => {
  if (event.eventType === EventType.LOGIN_SUCCESS) {
    msal.setActiveAccount((event.payload as AuthenticationResult).account);
  }
});

async function start() {
  await msal.initialize();
  await msal.handleRedirectPromise();
  const accounts = msal.getAllAccounts();
  if (!msal.getActiveAccount() && accounts.length > 0)
    msal.setActiveAccount(accounts[0]);
  ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
      <MsalProvider instance={msal}>
        <App />
      </MsalProvider>
    </React.StrictMode>,
  );
}

start().catch((error: Error) => {
  const root = document.getElementById('root')!;
  root.textContent = `QuizMaster could not start: ${error.message}`;
  root.setAttribute('role', 'alert');
});
