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
using System;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class Syntax2 : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [Test]
    public void CommentsInSQL()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      string sql = "INSERT INTO Test /* my table */ VALUES (1 /* this is the id */, 'Test' );" +
        "/* These next inserts are just for testing \r\n" +
        "   comments */\r\n" +
        "INSERT INTO \r\n" +
        "  # This table is bogus\r\n" +
        "Test VALUES (2, 'Test2')";


      MySqlCommand cmd = new MySqlCommand(sql, Connection);
      cmd.ExecuteNonQuery();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable table = new DataTable();
      da.Fill(table);
      Assert.That(table.Rows[0]["id"], Is.EqualTo(1));
      Assert.That(table.Rows[0]["name"], Is.EqualTo("Test"));
      Assert.That(table.Rows.Count, Is.EqualTo(2));
      Assert.That(table.Rows[1]["id"], Is.EqualTo(2));
      Assert.That(table.Rows[1]["name"], Is.EqualTo("Test2"));
    }

    [Test]
    public void LastInsertid()
    {
      ExecuteSQL("CREATE TABLE Test(id int auto_increment, name varchar(20), primary key(id))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(NULL, 'test')", Connection);
      cmd.ExecuteNonQuery();
      Assert.That(cmd.LastInsertedId, Is.EqualTo(1));

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
      }
      Assert.That(cmd.LastInsertedId, Is.EqualTo(2));

      cmd.CommandText = "SELECT id FROM Test";
      cmd.ExecuteScalar();
      Assert.That(cmd.LastInsertedId, Is.EqualTo(-1));
    }

    [Test]
    public void ParsingBugTest()
    {
      ExecuteSQL(@"CREATE FUNCTION `TestFunction`(A INTEGER (11), B INTEGER (11), C VARCHAR (20)) 
          RETURNS int(11)
          RETURN 1");

      MySqlCommand command = new MySqlCommand("TestFunction", Connection);
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = "TestFunction";
      command.Parameters.AddWithValue("@A", 1);
      command.Parameters.AddWithValue("@B", 2);
      command.Parameters.AddWithValue("@C", "test");
      command.Parameters.Add("@return", MySqlDbType.Int32).Direction = ParameterDirection.ReturnValue;
      command.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug #44960	backslash in string - connector return exeption
    /// </summary>
    [Test]
    public void EscapedBackslash()
    {
      ExecuteSQL("CREATE TABLE Test(id INT, name VARCHAR(20))");

      MySqlCommand cmd = new MySqlCommand(@"INSERT INTO Test VALUES (1, '\\=\\')", Connection);
      cmd.ExecuteNonQuery();
    }
  }
}
