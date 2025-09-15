-- Idempotent migration: create users table if not exists and add index if missing
BEGIN;

CREATE TABLE IF NOT EXISTS users (
	id SERIAL PRIMARY KEY,
	email TEXT NOT NULL UNIQUE,
	is_active BOOLEAN NOT NULL DEFAULT TRUE,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Add index only if it doesn't exist (Postgres)
DO $$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_class c
		JOIN pg_namespace n ON n.oid = c.relnamespace
		WHERE c.relname = 'idx_users_is_active'
		AND n.nspname = 'public'
	) THEN
		CREATE INDEX idx_users_is_active ON public.users (is_active);
	END IF;
END
$$;

COMMIT;