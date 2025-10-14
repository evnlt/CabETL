using CabETL.CLI.Services;

// for testing purposes
await DbSchemaService.DeleteDatabase();

var connectionString =
    "Data Source=(local);Database=CabDataDb;User Id=sa;Password=Qwerty123$%;TrustServerCertificate=true";

await DbSchemaService.EnsureDbSchemaExists(connectionString);

var solutionRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.Parent
    ?.FullName; // path to solution root

if (solutionRoot == null)
{
    throw new Exception("Could not determine solution root");
}

var csvPath = Path.Combine(solutionRoot, "sample-cab-data.csv");
var service = new CsvToDbService(connectionString);

var totalRowsInserted = service.ImportCsvToDatabase(csvPath);
Console.WriteLine("Total inserted - " + totalRowsInserted);

var duplicatesCsvPath = Path.Combine(solutionRoot, "duplicates.csv");
var deletedCount = await service.DeleteDuplicates(duplicatesCsvPath);
Console.WriteLine("Deleted count - " + deletedCount);

var recordsLeft = totalRowsInserted - deletedCount;
Console.WriteLine("Records left in db - " + recordsLeft);

await DbSchemaService.CreateIndexes(connectionString);

// Assume your program will be used on much larger data files. Describe in a few sentences what you would change if you knew it would be used for a 10GB CSV input file.
// 1. Play with batch size to see what number works best
// 2. Process file in parallel
// 3. Implement producer-consumer pattern