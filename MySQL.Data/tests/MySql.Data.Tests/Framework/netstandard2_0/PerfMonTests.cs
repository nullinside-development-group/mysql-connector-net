// Copyright © 2013, 2025, Oracle and/or its affiliates.
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

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MySql.Data.MySqlClient.Tests
{
  public class PerfMonTests : TestBase
  {
    internal override void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
      settings.UsePerformanceMonitor = true;
    }

    //[Fact(Skip = "Fix This")]
    //public void ProcedureFromCache()
    //{
    //  executeSQL("DROP PROCEDURE IF EXISTS spTest");
    //  executeSQL("CREATE PROCEDURE spTest(id int) BEGIN END");

    //  PerformanceCounter hardQuery = new PerformanceCounter(
    //     ".NET Data Provider for MySQL", "HardProcedureQueries", true);
    //  PerformanceCounter softQuery = new PerformanceCounter(
    //     ".NET Data Provider for MySQL", "SoftProcedureQueries", true);
    //  long hardCount = hardQuery.RawValue;
    //  long softCount = softQuery.RawValue;

    //  MySqlCommand cmd = new MySqlCommand("spTest", Connection);
    //  cmd.CommandType = CommandType.StoredProcedure;
    //  cmd.Parameters.AddWithValue("?id", 1);
    //  cmd.ExecuteScalar();

    //  Assert.AreEqual(hardCount + 1, hardQuery.RawValue);
    //  Assert.AreEqual(softCount, softQuery.RawValue);
    //  hardCount = hardQuery.RawValue;

    //  MySqlCommand cmd2 = new MySqlCommand("spTest", Connection);
    //  cmd2.CommandType = CommandType.StoredProcedure;
    //  cmd2.Parameters.AddWithValue("?id", 1);
    //  cmd2.ExecuteScalar();

    //  Assert.AreEqual(hardCount, hardQuery.RawValue);
    //  Assert.AreEqual(softCount + 1, softQuery.RawValue);
    //}
  }
}
