This is a [Next.js](https://nextjs.org) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).

## Variáveis de ambiente (gestão de utilizadores e API)

| Variável | Onde | Função |
|----------|------|--------|
| `NEXT_PUBLIC_API_URL` | Vercel / `.env.local` | URL pública do backend .NET (sem barra no fim). Usada nas rotas BFF (`/api/...`) que encaminham para a API, incl. `GET/PATCH /api/admin/usuarios` e criação de convites. |
| `NEXT_PUBLIC_SUPABASE_URL` + chave anónima | Já usadas pelo cliente Supabase | Login na app. |
| `SUPABASE_SERVICE_ROLE_KEY` | Só no servidor (Vercel, nunca no cliente) | Necessária em `POST /api/invite-user` para criar utilizadores no Supabase Auth (formulário em **Utilizadores**). |
| `ADMIN_INVITE_EMAILS` | Opcional (servidor) | Lista de e-mails separada por vírgula que podem convidar novos users se ainda não forem `Admin` na tabela (bootstrap de permissões). |

**Na API .NET (Render, etc.):** `SUPABASE_URL` (ou `Auth:SupabaseUrl`) e `SUPABASE_JWT_SECRET` (ou `Auth:SupabaseJwtSecret`) — sem isto o JWT não valida (rotas devolvem 501). Para o **primeiro administrador** na tabela `Usuarios`, definir `Auth:BootstrapAdminEmails` ou a variável `BOOTSTRAP_ADMIN_EMAILS` com o e-mail; no primeiro login com esse e-mail, o `GET /api/auth/sync` cria a linha com perfil `Admin`. Depois disso, a página **Utilizadores** consegue listar e aprovisionar (desde que `SUPABASE_SERVICE_ROLE_KEY` exista no Next).

## Getting Started

First, run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load [Geist](https://vercel.com/font), a new font family for Vercel.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.
