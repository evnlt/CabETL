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

await DbSchemaService.CreateIndexes(connectionString);