[CmdletBinding()]
param(
    [string]$DatabaseContainer = 'arcworks-landing-preview-db-1',
    [string]$ExpectedInstance = 'arcworks-landing-preview'
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker is required to seed the isolated preview catalog.'
}

$containerId = docker inspect --format '{{.Id}}' $DatabaseContainer 2>$null
if (-not $containerId) {
    throw "Preview database container '$DatabaseContainer' was not found."
}

$composeProject = docker inspect --format '{{index .Config.Labels "com.docker.compose.project"}}' $DatabaseContainer
if ($composeProject -ne $ExpectedInstance) {
    throw "Refusing to seed '$DatabaseContainer': Compose project '$composeProject' does not match isolated preview '$ExpectedInstance'."
}

$environment = docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' $DatabaseContainer
$rootPasswordLine = $environment | Select-String '^MARIADB_ROOT_PASSWORD=' | Select-Object -First 1
if (-not $rootPasswordLine) {
    throw 'The preview database root credential is not available inside the container configuration.'
}
$rootPassword = $rootPasswordLine.Line.Split('=', 2)[1]

$sql = @'
START TRANSACTION;

INSERT INTO roms.RestaurantTables (Id, Number, IsActive, SortOrder)
SELECT UUID(), seed.Number, 1, seed.SortOrder
FROM (
    SELECT '1' Number, 1 SortOrder UNION ALL SELECT '2', 2 UNION ALL
    SELECT '3', 3 UNION ALL SELECT '4', 4 UNION ALL SELECT '5', 5 UNION ALL
    SELECT '6', 6 UNION ALL SELECT '7', 7 UNION ALL SELECT '8', 8 UNION ALL
    SELECT '9', 9 UNION ALL SELECT '10', 10 UNION ALL SELECT '11', 11 UNION ALL
    SELECT '12', 12
) seed
WHERE NOT EXISTS (
    SELECT 1 FROM roms.RestaurantTables existing WHERE existing.Number = seed.Number
);

INSERT INTO roms.MenuCategories (Id, Name, SortOrder, IsActive)
SELECT UUID(), seed.Name, seed.SortOrder, 1
FROM (
    SELECT 'Mains' Name, 1 SortOrder UNION ALL
    SELECT 'Drinks', 2 UNION ALL
    SELECT 'Sides', 3 UNION ALL
    SELECT 'Desserts', 4
) seed
WHERE NOT EXISTS (
    SELECT 1 FROM roms.MenuCategories existing WHERE existing.Name = seed.Name
);

SET @mains = (SELECT Id FROM roms.MenuCategories WHERE Name = 'Mains' ORDER BY SortOrder, Id LIMIT 1);
SET @drinks = (SELECT Id FROM roms.MenuCategories WHERE Name = 'Drinks' ORDER BY SortOrder, Id LIMIT 1);
SET @sides = (SELECT Id FROM roms.MenuCategories WHERE Name = 'Sides' ORDER BY SortOrder, Id LIMIT 1);
SET @desserts = (SELECT Id FROM roms.MenuCategories WHERE Name = 'Desserts' ORDER BY SortOrder, Id LIMIT 1);

INSERT INTO roms.MenuItems
    (Id, CategoryId, Name, Description, Price, IsActive, IsAvailable, PreparationMinutes)
SELECT UUID(), seed.CategoryId, seed.Name, seed.Description, seed.Price, 1, 1, seed.PreparationMinutes
FROM (
    SELECT @mains CategoryId, 'Beef Pares' Name, 'Braised beef served with garlic rice' Description, 220.00 Price, 12 PreparationMinutes UNION ALL
    SELECT @mains, 'Cheeseburger', 'Beef patty, cheese, and house sauce', 185.00, 8 UNION ALL
    SELECT @mains, 'Chicken Rice', 'Grilled chicken served with steamed rice', 165.00, 10 UNION ALL
    SELECT @mains, 'Pancit Canton', 'Stir-fried noodles with vegetables', 200.00, 15 UNION ALL
    SELECT @drinks, 'Iced Tea', 'House-brewed iced tea', 55.00, 2 UNION ALL
    SELECT @drinks, 'Bottled Water', 'Chilled bottled water', 35.00, 1 UNION ALL
    SELECT @drinks, 'Calamansi Juice', 'Fresh local calamansi juice', 65.00, 3 UNION ALL
    SELECT @sides, 'French Fries', 'Crisp golden potato fries', 95.00, 7 UNION ALL
    SELECT @sides, 'Lumpia', 'Crisp Filipino spring rolls', 150.00, 10 UNION ALL
    SELECT @desserts, 'Halo-Halo', 'Classic Filipino shaved-ice dessert', 120.00, 5 UNION ALL
    SELECT @desserts, 'Leche Flan', 'Silky caramel custard', 110.00, 5 UNION ALL
    SELECT @desserts, 'Cheesecake', 'Creamy baked cheesecake', 145.00, 5
) seed
WHERE NOT EXISTS (
    SELECT 1 FROM roms.MenuItems existing WHERE existing.Name = seed.Name
);

COMMIT;

SELECT 'Restaurant tables' Entity, COUNT(*) ItemCount
FROM roms.RestaurantTables WHERE IsActive = 1
UNION ALL
SELECT 'Menu categories', COUNT(*) FROM roms.MenuCategories WHERE IsActive = 1
UNION ALL
SELECT 'Available menu items', COUNT(*) FROM roms.MenuItems WHERE IsActive = 1 AND IsAvailable = 1;

SELECT category.Name Category, COUNT(*) ItemCount
FROM roms.MenuCategories category
JOIN roms.MenuItems item ON item.CategoryId = category.Id
WHERE category.IsActive = 1 AND item.IsActive = 1
GROUP BY category.Id, category.Name, category.SortOrder
ORDER BY category.SortOrder;
'@

$sql | docker exec -i $DatabaseContainer mariadb -u root "-p$rootPassword" --batch
if ($LASTEXITCODE -ne 0) {
    throw "Preview catalog seed failed with exit code $LASTEXITCODE."
}

Write-Host 'Isolated preview catalog is populated. No live database was modified.' -ForegroundColor Green
