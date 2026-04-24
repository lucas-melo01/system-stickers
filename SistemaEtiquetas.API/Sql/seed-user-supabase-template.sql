-- Modelo: copiar para seed-*.local.sql, preencher e-mail, palavra-passe e Nome
-- Correr no Supabase SQL Editor (não fazer commit de ficheiros com palavra-passe)
--
-- Perfil: 0 = Operador, 1 = Admin (ver enum UsuarioPerfil em SistemaEtiquetas.Domain)
--
-- CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO $$
DECLARE
  v_user_id uuid := gen_random_uuid();
  v_email   text := 'TEXTO_DO_EMAIL';
  v_pwd     text := $pwd$PALAVRA_PASSE_AQUI$pwd$;
  v_hash    text := crypt(v_pwd, gen_salt('bf'));
  v_instance_id uuid;
BEGIN
  v_instance_id := (SELECT id FROM auth.instances LIMIT 1);
  IF v_instance_id IS NULL THEN
    v_instance_id := '00000000-0000-0000-0000-000000000000'::uuid;
  END IF;

  IF EXISTS (SELECT 1 FROM auth.users WHERE email = v_email) THEN
    RAISE EXCEPTION 'E-mail % já existe em auth.users.', v_email;
  END IF;

  INSERT INTO auth.users (
    instance_id,
    id,
    aud,
    role,
    email,
    encrypted_password,
    email_confirmed_at,
    recovery_sent_at,
    last_sign_in_at,
    raw_app_meta_data,
    raw_user_meta_data,
    created_at,
    updated_at,
    confirmation_token,
    email_change,
    email_change_token_new,
    recovery_token
  ) VALUES (
    v_instance_id,
    v_user_id,
    'authenticated',
    'authenticated',
    v_email,
    v_hash,
    now(),
    now(),
    now(),
    '{"provider":"email","providers":["email"]}'::jsonb,
    jsonb_build_object('email', v_email),
    now(),
    now(),
    '',
    '',
    '',
    ''
  );

  INSERT INTO auth.identities (
    id,
    user_id,
    identity_data,
    provider,
    provider_id,
    last_sign_in_at,
    created_at,
    updated_at
  ) VALUES (
    gen_random_uuid(),
    v_user_id,
    format('{"sub":"%s","email":"%s","email_verified":true}', v_user_id::text, v_email)::jsonb,
    'email',
    v_user_id::text,
    now(),
    now(),
    now()
  );

  INSERT INTO public."Usuarios" ("Id", "Email", "Nome", "Perfil", "Ativo", "CriadoEm")
  VALUES (v_user_id, v_email, 'Nome exibido', 1, true, now())
  ON CONFLICT ("Id") DO UPDATE SET
    "Email" = EXCLUDED."Email",
    "Nome" = EXCLUDED."Nome",
    "Perfil" = EXCLUDED."Perfil",
    "Ativo" = EXCLUDED."Ativo";
END $$;
