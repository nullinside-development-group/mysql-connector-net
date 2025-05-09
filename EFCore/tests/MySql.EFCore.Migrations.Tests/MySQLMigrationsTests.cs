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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using NUnit.Framework;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
  public class MySQLMigrationsTests
  {
    [Test]
    public void Can_generate_migration_from_initial_database_to_initial()
    {
      // create the context            
      var optionsBuilder = new DbContextOptionsBuilder();
      optionsBuilder.UseMySQL(MySQLTestStore.RootConnectionString + "database=test;");

      using (var mytestContext = new MyTestContext(optionsBuilder.Options))
      {
        var migrator = mytestContext.GetInfrastructure().GetRequiredService<IMigrator>();

        migrator.GenerateScript(fromMigration: Migration.InitialDatabase, toMigration: Migration.InitialDatabase);
      }
    }

    //Bug #37513445 Cannot Perform Database Migration using MySql.EntityFrameworkCore 9.0.0
    [Test]
    public void TryPerformMigration()
    {
      var optionsBuilder = new DbContextOptionsBuilder();
      optionsBuilder.UseMySQL(MySQLTestStore.RootConnectionString + "database=test;");

      using (var mytestContext = new MyTestContext(optionsBuilder.Options))
      {
        mytestContext.Database.EnsureCreated();
        mytestContext.Database.EnsureDeleted();
        mytestContext.Database.Migrate();

        Assert.That(mytestContext.Database.CanConnect(), Is.True);
        mytestContext.Database.EnsureDeleted();
      }
    }
  }
}
