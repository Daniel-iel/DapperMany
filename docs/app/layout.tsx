import type { Metadata } from "next";
import { LanguageProvider } from "@/lib/LanguageContext";
import { CONTENT } from "@/content";
import "./globals.css";

export const metadata: Metadata = {
  title: CONTENT.en.misc.metaTitle,
  description: CONTENT.en.misc.metaDescription,
  icons: {
    icon: "/icon.png",
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <LanguageProvider>{children}</LanguageProvider>
      </body>
    </html>
  );
}
