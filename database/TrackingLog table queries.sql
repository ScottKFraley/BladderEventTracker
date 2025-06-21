-- truncate table public."TrackingLog";
-- SELECT Count(*) FROM public."TrackingLog";

-- To see the date range we're querying:
SELECT 
    "EventDate", 
    "EventDate" AT TIME ZONE 'UTC' AS utc_time,
    "EventDate" AT TIME ZONE 'UTC' AT TIME ZONE 'America/Los_Angeles' AS pacific_time
FROM public."TrackingLog"
LIMIT 10;

--

SELECT 
    "EventDate", 
    "EventDate" AT TIME ZONE 'UTC' AS utc_time,
    "EventDate" AT TIME ZONE 'UTC' AT TIME ZONE 'America/Los_Angeles' AS pacific_time
FROM public."TrackingLog"
WHERE "EventDate"::date >= '2025-05-31'::date
ORDER BY "EventDate" DESC;

-- 

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- UPDATE public."Users" SET "PasswordHash" = crypt('new_pass', gen_salt('bf'))
-- WHERE "Id"='8e3ddf21-4153-4838-abc6-47d553a5d905';

-- SELECT * FROM public."Users";