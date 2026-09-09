import type { Metadata } from "next";
import { Toaster } from "sonner";
import { AuthProvider } from "@/components/providers/auth-provider";
import { AppQueryProvider } from "@/components/providers/query-provider";
import "./globals.css";

export const metadata: Metadata = {
  title: "TOEIC Scoring Support System",
  description: "Hệ thống hỗ trợ tổ chức thi và chấm điểm TOEIC trực tuyến.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="vi">
      <body className="font-sans antialiased">
        <AppQueryProvider>
          <AuthProvider>{children}</AuthProvider>
          <Toaster
            position="top-right"
            toastOptions={{
              style: {
                background: "var(--color-paper-raised)",
                color: "var(--color-ink)",
                border: "1px solid var(--color-border)",
                fontFamily: "var(--font-sans)",
              },
            }}
          />
        </AppQueryProvider>
      </body>
    </html>
  );
}
