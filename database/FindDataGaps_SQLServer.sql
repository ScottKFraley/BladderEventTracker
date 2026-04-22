/* 
SELECT TOP 5
	EventDate
FROM
	dbo.TrackingLog
ORDER BY
	EventDate DESC

-- 
*/

SELECT 
    curr.EventDate AS GapStart,
    next.EventDate AS GapEnd,
    DATEDIFF(day, curr.EventDate, next.EventDate) AS GapDays
FROM (
    SELECT 
        EventDate,
        LEAD(EventDate) OVER (ORDER BY EventDate) AS NextEventDate
    FROM [dbo].[TrackingLog]
) AS curr
CROSS APPLY (SELECT curr.NextEventDate AS EventDate) AS next
WHERE DATEDIFF(day, curr.EventDate, next.EventDate) > 1
ORDER BY curr.EventDate;
