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
  public class MySqlCommandBuilderTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [Test]
    public void MultiWord()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);

      DataRow row = dt.NewRow();
      row["id"] = 1;
      row["name"] = "Name";
      row["dt"] = DBNull.Value;
      row["tm"] = DBNull.Value;
      row["multi word"] = 2;
      dt.Rows.Add(row);
      da.Update(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["multi word"], Is.EqualTo(2));

      dt.Rows[0]["multi word"] = 3;
      da.Update(dt);
      cb.Dispose();
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["multi word"], Is.EqualTo(3));
    }

    [Test]
    public void LastOneWins()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (1, 'Test')");

      MySqlCommandBuilder cb = new MySqlCommandBuilder(
          new MySqlDataAdapter("SELECT * FROM Test", Connection));
      MySqlDataAdapter da = cb.DataAdapter;
      cb.ConflictOption = ConflictOption.OverwriteChanges;
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));

      ExecuteSQL("UPDATE Test SET name='Test2' WHERE id=1");

      dt.Rows[0]["name"] = "Test3";
      Assert.That(da.Update(dt), Is.EqualTo(1));

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("Test3"));
    }

    [Test]
    public void NotLastOneWins()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (1, 'Test')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ConflictOption = ConflictOption.CompareAllSearchableValues;
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));

      ExecuteSQL("UPDATE Test SET name='Test2' WHERE id=1");
      dt.Rows[0]["name"] = "Test3";
      void Update() { da.Update(dt); }
      Exception ex = Assert.Throws<DBConcurrencyException>(() => Update());
      Assert.That(ex.Message, Is.EqualTo("Concurrency violation: the UpdateCommand affected 0 of the expected 1 records."));

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("Test2"));
    }

    /// <summary>
    /// Bug #8574 - MySqlCommandBuilder unable to support sub-queries
    /// Bug #11947 - MySQLCommandBuilder mishandling CONCAT() aliased column
    /// </summary>
    [Test]
    public void UsingFunctions()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (1,'test1')");
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (2,'test2')");
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (3,'test3')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT id, name, now() as ServerTime FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["id"] = 4;
      da.Update(dt);

      da.SelectCommand.CommandText = "SELECT id, name, CONCAT(name, '  boo') as newname from Test where id=4";
      dt.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("test1"));
      Assert.That(dt.Rows[0]["newname"], Is.EqualTo("test1  boo"));

      dt.Rows[0]["id"] = 5;
      da.Update(dt);

      dt.Clear();
      da.SelectCommand.CommandText = "SELECT * FROM Test WHERE id=5";
      da.Fill(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("test1"));

      da.SelectCommand.CommandText = "SELECT *, now() as stime FROM Test WHERE id<4";
      cb = new MySqlCommandBuilder(da);
      cb.ConflictOption = ConflictOption.OverwriteChanges;
      da.InsertCommand = cb.GetInsertCommand();
    }

    /// <summary>
    /// Bug #8382  	Commandbuilder does not handle queries to other databases than the default one-
    /// </summary>
    [Test]
    public void DifferentDatabase()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (1,'test1')");
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (2,'test2')");
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (3,'test3')");

      string oldDb = Connection.Database;
      string newDb = CreateDatabase("1");
      Connection.ChangeDatabase(newDb);

      MySqlDataAdapter da = new MySqlDataAdapter(
          String.Format("SELECT id, name FROM `{0}`.Test", oldDb), Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataSet ds = new DataSet();
      da.Fill(ds);

      ds.Tables[0].Rows[0]["id"] = 4;
      DataSet changes = ds.GetChanges();
      da.Update(changes);
      ds.Merge(changes);
      ds.AcceptChanges();
      cb.Dispose();

      Connection.ChangeDatabase(oldDb);
    }

    /// <summary>
    /// Bug #13036  	Returns error when field names contain any of the following chars %<>()/ etc
    /// </summary>
    [Test]
    public void SpecialCharactersInFieldNames()
    {
      ExecuteSQL("CREATE TABLE Test (`col%1` int PRIMARY KEY, `col()2` int, `col<>3` int, `col/4` int)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ToString();  // keep the compiler happy
      DataTable dt = new DataTable();
      da.Fill(dt);
      DataRow row = dt.NewRow();
      row[0] = 1;
      row[1] = 2;
      row[2] = 3;
      row[3] = 4;
      dt.Rows.Add(row);
      da.Update(dt);
    }

    /// <summary>
    /// Bug #14631  	"#42000Query was empty"
    /// </summary>
    [Test]
    public void SemicolonAtEndOfSQL()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO Test VALUES(1, 'Data')");

      DataSet ds = new DataSet();
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM `Test`;", Connection);
      da.FillSchema(ds, SchemaType.Source, "Test");

      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);
      dt.Rows[0]["id"] = 2;
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      cb.Dispose();
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["id"], Is.EqualTo(2));
    }

    /// <summary>
    /// Bug #23862 Problem with CommandBuilder 'GetInsertCommand' method 
    /// </summary>
    [Test]
    public void AutoIncrementColumnsOnInsert()
    {
      ExecuteSQL("CREATE TABLE Test (id INT UNSIGNED NOT NULL AUTO_INCREMENT, " +
          "name VARCHAR(100), PRIMARY KEY(id))");
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);

      da.InsertCommand = cb.GetInsertCommand();
      da.InsertCommand.CommandText += "; SELECT last_insert_id()";
      da.InsertCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;

      DataTable dt = new DataTable();
      da.Fill(dt);
      dt.Columns[0].AutoIncrement = true;
      Assert.That(dt.Columns[0].AutoIncrement);
      dt.Columns[0].AutoIncrementSeed = -1;
      dt.Columns[0].AutoIncrementStep = -1;
      DataRow row = dt.NewRow();
      row["name"] = "Test";

      dt.Rows.Add(row);
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["id"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("Test"));
      cb.Dispose();
    }

    /// <summary>
    /// Bug #25569 UpdateRowSource.FirstReturnedRecord does not work 
    /// </summary>
    [Test]
    public void AutoIncrementColumnsOnInsert2()
    {
      ExecuteSQL("CREATE TABLE Test (id INT UNSIGNED NOT NULL " +
          "AUTO_INCREMENT PRIMARY KEY, name VARCHAR(20))");
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);

      MySqlCommand cmd = (MySqlCommand)(cb.GetInsertCommand() as ICloneable).Clone();
      cmd.CommandText += "; SELECT last_insert_id() as id";
      cmd.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
      da.InsertCommand = cmd;

      DataTable dt = new DataTable();
      da.Fill(dt);
      dt.Rows.Clear();

      DataRow row = dt.NewRow();
      row["name"] = "Test";
      dt.Rows.Add(row);
      da.Update(dt);
      Assert.That(Convert.ToInt32(dt.Rows[0]["id"]), Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("Test"));

      row = dt.NewRow();
      row["name"] = "Test2";
      dt.Rows.Add(row);
      da.Update(dt);
      Assert.That(Convert.ToInt32(dt.Rows[1]["id"]), Is.EqualTo(2));
      Assert.That(dt.Rows[1]["name"], Is.EqualTo("Test2"));

      Assert.That(Convert.ToInt32(dt.Rows[0]["id"]), Is.EqualTo(1));
    }

    [Test]
    public void MultiUpdate()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO  Test (id, name) VALUES (1, 'test1')");
      ExecuteSQL("INSERT INTO  Test (id, name) VALUES (2, 'test2')");
      ExecuteSQL("INSERT INTO  Test (id, name) VALUES (3, 'test3')");
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["id"] = 4;
      dt.Rows[0]["name"] = "test4";
      dt.Rows[1]["id"] = 5;
      dt.Rows[1]["name"] = "test5";
      dt.Rows[2]["id"] = 6;
      dt.Rows[2]["name"] = "test6";
      DataTable changes = dt.GetChanges();
      da.Update(changes);
      dt.AcceptChanges();

      dt.Rows[0]["id"] = 7;
      dt.Rows[0]["name"] = "test7";
      dt.Rows[1]["id"] = 8;
      dt.Rows[1]["name"] = "test8";
      dt.Rows[2]["id"] = 9;
      dt.Rows[2]["name"] = "test9";
      changes = dt.GetChanges();
      da.Update(changes);
      dt.AcceptChanges();
      cb.Dispose();
    }

    /// <summary>
    /// Bug #30077  	MySqlDataAdapter.Update() exception due to date field format
    /// </summary>
    [Test]
    public void UpdatingWithDateInKey()
    {
      ExecuteSQL("CREATE TABLE Test (cod INT, dt DATE, PRIMARY KEY(cod, dt))");

      ExecuteSQL("INSERT INTO Test (cod, dt) VALUES (1, '2006-1-1')");
      ExecuteSQL("INSERT INTO Test (cod, dt) VALUES (2, '2006-1-2')");
      ExecuteSQL("INSERT INTO Test (cod, dt) VALUES (3, '2006-1-3')");
      ExecuteSQL("INSERT INTO Test (cod, dt) VALUES (4, '2006-1-4')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test ORDER BY cod", Connection);
      MySqlCommandBuilder bld = new MySqlCommandBuilder(da);
      bld.ConflictOption = ConflictOption.OverwriteChanges;
      DataTable dt = new DataTable();
      da.Fill(dt);
      dt.Rows[0]["cod"] = 6;
      da.Update(dt);

      dt.Clear();
      da.SelectCommand.CommandText = "SELECT * FROM Test WHERE cod=6";
      da.Fill(dt);
      Assert.That(dt.Rows[0]["cod"], Is.EqualTo(6));
    }

    /// <summary>
    /// Bug #35492 Please implement DbCommandBuilder.QuoteIdentifier 
    /// </summary>
    [Test]
    public void QuoteAndUnquoteIdentifiers()
    {
      MySqlCommandBuilder cb = new MySqlCommandBuilder();
      Assert.That(cb.QuoteIdentifier("boo"), Is.EqualTo("`boo`"));
      Assert.That(cb.QuoteIdentifier("bo`o"), Is.EqualTo("`bo``o`"));
      Assert.That(cb.QuoteIdentifier("`boo`"), Is.EqualTo("`boo`"));

      // now do the unquoting
      Assert.That(cb.UnquoteIdentifier("`boo`"), Is.EqualTo("boo"));
      Assert.That(cb.UnquoteIdentifier("`boo"), Is.EqualTo("`boo"));
      Assert.That(cb.UnquoteIdentifier("`bo``o`"), Is.EqualTo("bo`o"));
    }

    /// <summary>
    /// Bug #33650097 - MySqlCommandBuilder doesn't support tables with a bigint unsigned as primary key
    /// This bug was introduced in the attempt to fix Bug#29802379, which is not a C/NET bug per se.
    /// </summary>
    [Test]
    public void BigintUnsignedAsPK()
    {
      ExecuteSQL(@"CREATE TABLE `Test` (`id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT, 
        `field1` VARCHAR(45) NOT NULL DEFAULT '', PRIMARY KEY(`id`)); ");

      var adapter = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      var commandBuilder = new MySqlCommandBuilder(adapter);
      var myCommand = commandBuilder.GetUpdateCommand();

      Assert.That(myCommand.CommandText, Is.EqualTo($"UPDATE `test` SET `field1` = @p1 WHERE ((`id` = @p2) AND (`field1` = @p3))").IgnoreCase);
    }
  }
}
