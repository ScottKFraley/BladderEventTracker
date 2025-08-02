-- truncate table public."TrackingLog";
-- SELECT Count(*) FROM public."TrackingLog";

-- To see the date range we're querying:
SELECT 
    "EventDate", 
    "EventDate" AT TIME ZONE 'UTC' AS utc_time,
    "EventDate" AT TIME ZONE 'UTC' AT TIME ZONE 'America/Los_Angeles' AS pacific_time
FROM 
	dbo.TrackingLog
-- was a LIMIT 10; here for the Postgres db, so perhaps I need to find out how to do that in T-SQL

--

SELECT 
    "EventDate", 
    "EventDate" AT TIME ZONE 'UTC' AS utc_time,
    "EventDate" AT TIME ZONE 'UTC' AT TIME ZONE 'America/Los_Angeles' AS pacific_time
FROM 
	[dbo].[TrackingLog]
WHERE 
	"EventDate"::date >= '2025-05-31'::date
ORDER BY 
	"EventDate" DESC;

-- 

SELECT * FROM Users



-- EOF
