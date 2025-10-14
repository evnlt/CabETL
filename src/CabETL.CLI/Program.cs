using CabETL.CLI.Services;

var connectionString =
    "Data Source=(local);Initial Catalog=CabData;User Id=sa;Password=Qwerty123$%;TrustServerCertificate=true";

// TODO - add an ability to type in file path 

var solutionRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName; // this looks bad
if (solutionRoot == null)
{
    throw new Exception("Could not determine solution root");   
}

var csvPath = Path.Combine(solutionRoot, "sample-cab-data.csv");
var service = new CsvToDbService(connectionString);

service.ImportCsvToDatabase(csvPath);