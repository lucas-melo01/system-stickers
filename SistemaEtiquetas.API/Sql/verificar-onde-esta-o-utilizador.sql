-- 1) O seed manual NAO corre automaticamente com commit/deploy. Tens de colar
--    o conteúdo de seed-lucas-manual.local.sql (ou o template preenchido) no
--    Supabase -> SQL e clicar Run.
--
-- 2) O utilizador de login fica SEMPRE em auth.users. A app .NET lê
--    public."Usuarios" (o Id tem de ser o mesmo id em auth.users = claim "sub").

-- Auth (deve retornar 1 linha se o e-mail tiver sido criado):
SELECT id, email, created_at, email_confirmed_at IS NOT NULL AS email_ok
FROM auth.users
WHERE email = 'lucas_santosmelo@hotmail.com';

-- Tabela da aplicação (EF usa aspas: "Usuarios", "Id", "Email" ...):
SELECT "Id", "Email", "Nome", "Perfil", "Ativo", "CriadoEm"
FROM public."Usuarios"
WHERE "Email" = 'lucas_santosmelo@hotmail.com';

-- Se o SELECT acima der erro "relation does not exist", lista nomes reais:
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND LOWER(table_name) LIKE '%usuario%';
