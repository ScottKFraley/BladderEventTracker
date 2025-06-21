DO $$
BEGIN
    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'Users') THEN
        DROP TABLE "Users" CASCADE;
    ELSE
        RAISE NOTICE 'Table "Users" does not exist';
    END IF;

    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = '__EFMigrationsHistory') THEN
        DROP TABLE "__EFMigrationsHistory" CASCADE;
    ELSE
        RAISE NOTICE 'Table "__EFMigrationsHistory" does not exist';
    END IF;

    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'TrackingLog') THEN
        TRUNCATE TABLE "TrackingLog";
    ELSE
        RAISE NOTICE 'Table "TrackingLog" does not exist';
    END IF;
END $$;
