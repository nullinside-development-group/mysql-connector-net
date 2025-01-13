// Copyright © 2021, 2025, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Basic.Tests.DbContextClasses;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Diagnostics.Internal;
using MySql.EntityFrameworkCore.Internal;
using MySql.EntityFrameworkCore.Storage.Internal;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Diagnostics;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class ConnectionTests
  {
    private static MySQLRelationalConnection CreateConnection(DbContextOptions options)
    {
      var dependencies = CreateDependencies(options);

      return new MySQLRelationalConnection(dependencies);
    }

#if NET6_0
    public static RelationalConnectionDependencies CreateDependencies(DbContextOptions options = null)
    {
      options ??= new DbContextOptionsBuilder()
          .UseMySQL(MySQLTestStore.BaseConnectionString + "database=test;")
          .Options;

      return new RelationalConnectionDependencies(
          options,
          new DiagnosticsLogger<DbLoggerCategory.Database.Transaction>(
              new LoggerFactory(),
              new LoggingOptions(),
              new DiagnosticListener("FakeDiagnosticListener"),
              new MySQLLoggingDefinitions(), null),
          new RelationalConnectionDiagnosticsLogger(
                        new LoggerFactory(),
                        new LoggingOptions(),
                        new DiagnosticListener("FakeDiagnosticListener"),
                        new TestRelationalLoggingDefinitions(),
                        new NullDbContextLogger(),
                        CreateOptions()),
          new NamedConnectionStringResolver(options),
          new RelationalTransactionFactory(new RelationalTransactionFactoryDependencies(
            new RelationalSqlGenerationHelper(new RelationalSqlGenerationHelperDependencies()))),
          new CurrentDbContext(new FakeDbContext()),
          new RelationalCommandBuilderFactory(new RelationalCommandBuilderDependencies(
            new MySQLTypeMappingSource(
              TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
              TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
              new MySQLOptions()))));
    }
#elif NET8_0_OR_GREATER
    public static RelationalConnectionDependencies CreateDependencies(DbContextOptions? options = null)
    {
      options ??= new DbContextOptionsBuilder()
          .UseMySQL(MySQLTestStore.BaseConnectionString + "database=test;")
          .Options;

      return new RelationalConnectionDependencies(
          options,
          new DiagnosticsLogger<DbLoggerCategory.Database.Transaction>(
              new LoggerFactory(),
              new LoggingOptions(),
              new DiagnosticListener("FakeDiagnosticListener"),
              new MySQLLoggingDefinitions(), new NullDbContextLogger()),
          new RelationalConnectionDiagnosticsLogger(
                        new LoggerFactory(),
                        new LoggingOptions(),
                        new DiagnosticListener("FakeDiagnosticListener"),
                        new TestRelationalLoggingDefinitions(),
                        new NullDbContextLogger(),
                        CreateOptions()),
          new NamedConnectionStringResolver(options),
          new RelationalTransactionFactory(new RelationalTransactionFactoryDependencies(
            new RelationalSqlGenerationHelper(new RelationalSqlGenerationHelperDependencies()))),
          new CurrentDbContext(new FakeDbContext()),
          new RelationalCommandBuilderFactory(new RelationalCommandBuilderDependencies(
            new MySQLTypeMappingSource(
              TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
              TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
              new MySQLOptions()), new ExceptionDetector())));
    }
#endif


    [TearDown]
    public void TearDown()
    {
      using (var context = new SakilaLiteUpdateContext())
      {
        context.DropContext();
      }
    }

    private class FakeDbContext : DbContext
    {
    }

    [Test]
    public void CanCreateConnectionString()
    {
      using (var connection = CreateConnection(CreateOptions()))
      {
        Assert.That(connection.DbConnection, Is.InstanceOf<MySqlConnection>());
      }
    }

    [Test]
    public void CanCreateMainConnection()
    {
      using (var connection = CreateConnection(CreateOptions()))
      {
        using (var source = connection.CreateSourceConnection())
        {
          var csb = new MySqlConnectionStringBuilder(source.ConnectionString);
          var csb1 = new MySqlConnectionStringBuilder(MySQLTestStore.BaseConnectionString);
          Assert.That(csb.Database, Is.EqualTo(csb1.Database));
          Assert.That(csb.Port, Is.EqualTo(csb1.Port));
          Assert.That(csb.Server, Is.EqualTo(csb1.Server));
          Assert.That(csb.UserID, Is.EqualTo(csb1.UserID));
        }
      }
    }

    public static DbContextOptions CreateOptions()
    {
      var optionsBuilder = new DbContextOptionsBuilder();
      optionsBuilder.UseMySQL(MySQLTestStore.BaseConnectionString + "database=test;");
      return optionsBuilder.Options;
    }

    [Test]
    public void TransactionTest()
    {
      using (var context = new SakilaLiteUpdateContext())
      {
        context.InitContext(false);
        MySqlTrace.LogInformation(9966, "EF Model CREATED");
      }

      using (MySqlConnection connection = new MySqlConnection(MySQLTestStore.GetContextConnectionString<SakilaLiteUpdateContext>()))
      {
        connection.Open();

        using (MySqlTransaction transaction = connection.BeginTransaction())
        {

          MySqlCommand command = connection.CreateCommand();
          command.CommandText = "DELETE FROM actor";
          command.ExecuteNonQuery();

          var options = new DbContextOptionsBuilder<SakilaLiteUpdateContext>()
            .UseMySQL(connection)
            .Options;

          using (var context = new SakilaLiteUpdateContext(options))
          {
            context.Database.UseTransaction(transaction);
            context.Actor.Add(new Actor
            {
              FirstName = "PENELOPE",
              LastName = "GUINESS"
            });
            context.SaveChanges();
          }

          transaction.Commit();
        }
      }
    }
  }
}
