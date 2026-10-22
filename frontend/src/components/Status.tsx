export function ErrorMessage({ message }: { message: string | null }) {
  return message ? (
    <p className="notice error" role="alert">
      {message}
    </p>
  ) : null;
}

export function Loading({ children }: { children: string }) {
  return (
    <p className="notice" role="status">
      <span className="spinner" aria-hidden="true" />
      {children}
    </p>
  );
}
