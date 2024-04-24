DO $$ BEGIN
    CREATE ROLE "${APP-USER}";
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;
