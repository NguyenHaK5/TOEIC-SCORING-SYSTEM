export function FormError({ message }: { message?: string | null }) {
  if (!message) return null;
  return (
    <p role="alert" className="rounded-sm border border-danger/30 bg-danger/5 px-3.5 py-2.5 text-[13px] text-danger">
      {message}
    </p>
  );
}
