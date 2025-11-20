using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Aura.Tests.Configuration;

public class SqliteConnectionStringTests
{
    [Fact]
    public void SqliteConnectionString_Should_OnlyContainSupportedKeywords()
    {
        // Arrange - This is the connection string format from Program.cs
        var testPath = "Data Source=test.db";
        var connectionString = $"{testPath};Mode=ReadWriteCreate;Cache=Shared;Foreign Keys=True;";

        // Act & Assert - This should not throw an exception
        var exception = Record.Exception(() => 
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            connection.Close();
        });

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Data Source=test.db;Journal Mode=WAL;")]
    [InlineData("Data Source=test.db;Synchronous=NORMAL;")]
    [InlineData("Data Source=test.db;Page Size=4096;")]
    [InlineData("Data Source=test.db;Cache Size=-64000;")]
    [InlineData("Data Source=test.db;Temp Store=MEMORY;")]
    [InlineData("Data Source=test.db;Locking Mode=NORMAL;")]
    public void SqliteConnectionString_Should_RejectUnsupportedKeywords(string connectionString)
    {
        // Act & Assert - These should throw ArgumentException
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
        });

        Assert.Contains("keyword", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SqliteConnectionString_Should_SupportAllRequiredKeywords()
    {
        // Arrange - Test all supported keywords from Microsoft.Data.Sqlite
        var testPath = "Data Source=:memory:"; // Use in-memory database for test
        var connectionStrings = new[]
        {
            $"{testPath};Mode=ReadWriteCreate;",
            $"{testPath};Cache=Shared;",
            $"{testPath};Foreign Keys=True;",
            $"{testPath};Mode=ReadWriteCreate;Cache=Shared;Foreign Keys=True;",
        };

        // Act & Assert - All should work
        foreach (var connString in connectionStrings)
        {
            var exception = Record.Exception(() =>
            {
                using var connection = new SqliteConnection(connString);
                connection.Open();
                connection.Close();
            });

            Assert.Null(exception);
        }
    }
}
