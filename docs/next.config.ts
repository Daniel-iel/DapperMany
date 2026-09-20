import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Static export only for production builds (GitHub Pages)
  // Development mode uses the Next.js dev server normally
  ...(process.env.NODE_ENV === 'production' && {
    output: 'export',
    basePath: '/DapperMany',
    assetPrefix: '/DapperMany/',
    images: {
      unoptimized: true,
    },
    trailingSlash: true,
  }),
};

export default nextConfig;
