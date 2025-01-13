// Copyright © 2015, 2025, Oracle and/or its affiliates.
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

using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class RowBufferingTests : BaseTest
  {
    [Test]
    public void SmartBuffering()
    {
      ExecuteSQL("CREATE TABLE test1(id INT)");
      ExecuteSQL("INSERT INTO test1 VALUES (1),(2),(3),(4)");
      ExecuteSQL("CREATE TABLE test2(id INT, val INT)");
      ExecuteSQL("INSERT INTO test2 VALUES (1,0)");

      var rowResult = ExecuteSelectStatement(testSchema.GetTable("test1").Select("id"));
      Assert.That(rowResult.IndexOf("id"), Is.EqualTo(0));
      foreach (var row in rowResult)
      {
        var result = ExecuteUpdateStatement(testSchema.GetTable("test2").Update().Where("id=1").Set("val", row["id"]));
        Assert.That(result.AffectedItemsCount, Is.EqualTo(1));
      }

      Row valRow = ExecuteSelectStatement(testSchema.GetTable("test2").Select("val")).FetchOne();
      Assert.That(valRow[0], Is.EqualTo(4));
    }
  }
}
