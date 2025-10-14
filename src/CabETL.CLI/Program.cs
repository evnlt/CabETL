using CabETL.CLI;

// TODO - move to method
using var context = new AppDbContext();
context.Database.EnsureCreated();

Console.WriteLine("Hello, World!");