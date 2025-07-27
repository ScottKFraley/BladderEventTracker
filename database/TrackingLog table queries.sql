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
