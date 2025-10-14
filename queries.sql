-- Find out which `PULocationId` (Pick-up location ID) has the highest tip_amount on average.

-- basic query
SELECT TOP 1
    PickupLocationId,
    AVG(TipAmount) AS AverageTipAmount
FROM dbo.CabData
GROUP BY PickupLocationId
ORDER BY AverageTipAmount DESC;

-- view query
SELECT TOP 1
    PickupLocationId,
    CAST(TotalTipAmount / NULLIF(TripCount, 0) AS DECIMAL(10,4)) AS AverageTipAmount
FROM dbo.v_CabData_TipStatsByPickupLocation
ORDER BY AverageTipAmount DESC;

-- Find the top 100 longest fares in terms of `trip_distance`.

SELECT TOP 100
    PickupLocationId,
    DropoffLocationId,
    TripDistance
FROM dbo.CabData
ORDER BY TripDistance DESC;

-- Find the top 100 longest fares in terms of time spent traveling.

SELECT TOP 100
    PickupDatetime,
    DropoffDatetime,
    DATEDIFF(SECOND, PickupDatetime, DropoffDatetime) AS TripDurationInSeconds,
    *
FROM
    CabData
ORDER BY
    TripDurationInSeconds DESC;

-- Search, where part of the conditions is `PULocationId`.

SELECT *
FROM CabData
WHERE PickupLocationId = 260;

SELECT *
FROM CabData
WHERE PickupLocationId IN (260, 136, 42);