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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MySql.Data.MySqlClient.Tests
{
  public class ParameterTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [Test]
    public void TestQuoting()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("", Connection);
      cmd.CommandText = "INSERT INTO Test VALUES (?id, ?name, NULL,NULL,NULL)";
      cmd.Parameters.Add(new MySqlParameter("?id", 1));
      cmd.Parameters.Add(new MySqlParameter("?name", "my ' value"));
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = 2;
      cmd.Parameters[1].Value = @"my "" value";
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = 3;
      cmd.Parameters[1].Value = @"my ` value";
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = 4;
      cmd.Parameters[1].Value = @"my ´ value";
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = 5;
      cmd.Parameters[1].Value = @"my \ value";
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      MySqlDataReader reader = null;
      try
      {
        reader = cmd.ExecuteReader();
        reader.Read();
        Assert.That(reader.GetString(1), Is.EqualTo("my ' value"));
        reader.Read();
        Assert.That(reader.GetString(1), Is.EqualTo(@"my "" value"));
        reader.Read();
        Assert.That(reader.GetString(1), Is.EqualTo("my ` value"));
        reader.Read();
        Assert.That(reader.GetString(1), Is.EqualTo("my ´ value"));
        reader.Read();
        Assert.That(reader.GetString(1), Is.EqualTo(@"my \ value"));
      }
      catch (Exception ex)
      {
        Assert.That(ex.Message == String.Empty, Is.False, ex.Message);
      }
      finally
      {
        if (reader != null) reader.Close();
      }
    }

    [Test]
    public void TestDateTimeParameter()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("", Connection);

      TimeSpan time = new TimeSpan(0, 1, 2, 3);
      DateTime dt = new DateTime(2003, 11, 11, 1, 2, 3);
      cmd.CommandText = "INSERT INTO Test VALUES (1, 'test', ?dt, ?time, CURRENT_TIMESTAMP)";
      cmd.Parameters.Add(new MySqlParameter("?time", time));
      cmd.Parameters.Add(new MySqlParameter("?dt", dt));
      int cnt = cmd.ExecuteNonQuery();
      Assert.That(cnt == 1, "Insert count");

      cmd = new MySqlCommand("SELECT tm, dt, ts FROM Test WHERE id=1", Connection);
      MySqlDataReader reader = cmd.ExecuteReader();
      reader.Read();
      TimeSpan time2 = (TimeSpan)reader.GetValue(0);
      Assert.That(time2, Is.EqualTo(time));

      DateTime dt2 = reader.GetDateTime(1);
      Assert.That(dt2, Is.EqualTo(dt));

      DateTime ts2 = reader.GetDateTime(2);
      reader.Close();

      // now check the timestamp column.  We won't check the minute or second for obvious reasons
      DateTime now = DateTime.Now;
      Assert.That(ts2.Year, Is.EqualTo(now.Year));
      Assert.That(ts2.Month, Is.EqualTo(now.Month));
      Assert.That(ts2.Day, Is.EqualTo(now.Day));
      Assert.That(ts2.Hour, Is.EqualTo(now.Hour));

      // now we'll set some nulls and see how they are handled
      cmd = new MySqlCommand("UPDATE Test SET tm=?ts, dt=?dt WHERE id=1", Connection);
      cmd.Parameters.Add(new MySqlParameter("?ts", DBNull.Value));
      cmd.Parameters.Add(new MySqlParameter("?dt", DBNull.Value));
      cnt = cmd.ExecuteNonQuery();
      Assert.That(cnt == 1, "Update null count");

      cmd = new MySqlCommand("SELECT tm, dt FROM Test WHERE id=1", Connection);
      reader = cmd.ExecuteReader();
      reader.Read();
      object tso = reader.GetValue(0);
      object dto = reader.GetValue(1);
      Assert.That(tso == DBNull.Value, "Time column");
      Assert.That(dto == DBNull.Value, "DateTime column");

      reader.Close();

      cmd.CommandText = "DELETE FROM Test WHERE id=1";
      cmd.ExecuteNonQuery();
    }

    [Test]
    public void NestedQuoting()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) " +
        "VALUES(1, 'this is ?\"my value\"')", Connection);
      int count = cmd.ExecuteNonQuery();
      Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void SetDbType()
    {
      IDbCommand cmd = Connection.CreateCommand();
      IDbDataParameter prm = cmd.CreateParameter();
      prm.DbType = DbType.Int64;
      Assert.That(prm.DbType, Is.EqualTo(DbType.Int64));
      prm.Value = 3;
      Assert.That(prm.DbType, Is.EqualTo(DbType.Int64));

      MySqlParameter p = new MySqlParameter("name", MySqlDbType.Int64);
      Assert.That(p.DbType, Is.EqualTo(DbType.Int64));
      Assert.That(p.MySqlDbType, Is.EqualTo(MySqlDbType.Int64));
      p.Value = 3;
      Assert.That(p.DbType, Is.EqualTo(DbType.Int64));
      Assert.That(p.MySqlDbType, Is.EqualTo(MySqlDbType.Int64));
    }

    [Test]
    public void NullParameterObject()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (1, ?name)", Connection);
      try
      {
        cmd.Parameters.Add(null);
      }
      catch (ArgumentException)
      {
      }
    }

    /// <summary>
    /// Bug #28980952  	MySqlParameterCollection.Add precondition check isn't consistent
    /// </summary>
    [TestCase("testParam", "testParam")]
    [TestCase("testParam", "@testParam")]
    [TestCase("@testParam", "@testParam")]
    [TestCase("@testParam", "testParam")]
    [TestCase("@testParam", "?testParam")]
    [TestCase("?testParam", "@testParam")]
    public void AddParameterCheck(string param1, string param2)
    {
      using (MySqlCommand cmd = new MySqlCommand())
      {
        cmd.Parameters.AddWithValue(param1, 1);
        Assert.Throws<MySqlException>(() => cmd.Parameters.AddWithValue(param2, 2));
      }
    }

    /// <summary>
    /// Bug #32506736  	Can't use MemoryStream as MySqlParameter value
    /// </summary>
    [Test]
    public void MemoryStreamAsParameterValue()
    {
      ExecuteSQL("CREATE TABLE Test(str TEXT, blb BLOB,num INT); ");

      using (MySqlCommand cmd = new MySqlCommand("INSERT INTO Test(str, blb, num) VALUES(@str, @blb, @num); ", Connection))
      {
        using var streamString = new MemoryStream(new byte[] { 97, 98, 99, 100 });//abcd
        cmd.Parameters.AddWithValue("@str", streamString);
        using var streamBlob = new MemoryStream(new byte[] { 101, 102, 103, 104 });//efgh
        cmd.Parameters.AddWithValue("@blb", streamBlob);
        using var streamnumber = new MemoryStream(new byte[] { 53, 54, 55, 56 });//5678
        cmd.Parameters.AddWithValue("@num", streamnumber);
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";

        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          Assert.That(Convert.ToString(reader[0]), Is.EqualTo("abcd"));
          Assert.That(Encoding.Default.GetString((byte[])reader[1]), Is.EqualTo("efgh"));
          Assert.That(Convert.ToString(reader[2]), Is.EqualTo("5678"));
        }
      }
    }

    /// <summary>
    /// Bug #34993796  	MySqlParameter.Clone loses specific MySqlDbType
    /// </summary>
    [Test]
    public void ParameterClone2()
    {
      var param = new MySqlParameter("@param", MySqlDbType.MediumText);
      var clone = param.Clone();
      Assert.That(param.MySqlDbType, Is.EqualTo(MySqlDbType.MediumText)); // Prints "MediumText"
      Assert.That(clone.MySqlDbType, Is.EqualTo(MySqlDbType.MediumText)); // Prints "VarChar"
    }


    /// <summary>
    /// Bug #28777779  	MySqlParameter.Clone doesn't clone all properties
    /// </summary>
    [Test]
    public void ParameterClone()
    {
      var param = new MySqlParameter()
      {
        DbType = DbType.Int32,
        Direction = ParameterDirection.Output,
        Encoding = System.Text.Encoding.UTF8,
        IsNullable = true,
        MySqlDbType = MySqlDbType.Int32,
        ParameterName = "test",
        Precision = 3,
        Scale = 2,
        Size = 1,
        SourceColumnNullMapping = true,
        Value = 1
      };

      var clonedparam = param.Clone();

      Assert.That(clonedparam.DbType, Is.EqualTo(DbType.Int32));
      Assert.That(clonedparam.Direction, Is.EqualTo(ParameterDirection.Output));
      Assert.That(clonedparam.Encoding, Is.EqualTo(System.Text.Encoding.UTF8));
      Assert.That(clonedparam.IsNullable);
      Assert.That(clonedparam.MySqlDbType, Is.EqualTo(MySqlDbType.Int32));
      Assert.That(clonedparam.ParameterName, Is.EqualTo("test"));
      Assert.That(clonedparam.Precision, Is.EqualTo((byte)3));
      Assert.That(clonedparam.Scale, Is.EqualTo((byte)2));
      Assert.That(clonedparam.Size, Is.EqualTo(1));
      Assert.That(clonedparam.SourceColumnNullMapping);
      Assert.That(clonedparam.Value, Is.EqualTo(1));
    }

    /// <summary>
    /// Bug #20259756 MysqlParameter direction output does not work for text commands
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public void ParameterDirectionOutputTextCommand(bool preparedCommand)
    {
      using (MySqlCommand cmd = Connection.CreateCommand())
      {
        cmd.CommandText = "set @outputParam=1234;";
        cmd.CommandType = CommandType.Text;
        MySqlParameter outParam = new MySqlParameter();
        outParam.ParameterName = "@outputParam";
        outParam.MySqlDbType = MySqlDbType.Int32;
        outParam.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(outParam);
        if (preparedCommand) cmd.Prepare();

        cmd.ExecuteNonQuery();
        Assert.That(cmd.Parameters["@outputParam"].Value, Is.EqualTo(1234));
      }
    }

    /// <summary>
    /// Bug #7398  	MySqlParameterCollection doesn't allow parameters without filled in names
    /// </summary>
    [Test]
    public void AllowUnnamedParameters()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (?id, ?name)", Connection);

      MySqlParameter p = new MySqlParameter();
      p.Value = 1;
      cmd.Parameters.Add(p);
      cmd.Parameters[0].ParameterName = "?id";

      p = new MySqlParameter();
      p.Value = "test";
      cmd.Parameters.Add(p);
      p.ParameterName = "?name";

      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT id FROM Test";
      Assert.That(cmd.ExecuteScalar(), Is.EqualTo(1));

      cmd.CommandText = "SELECT name FROM Test";
      Assert.That(cmd.ExecuteScalar(), Is.EqualTo("test"));
    }

    [Test]
    public void NullParameterValue()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (1, ?name)", Connection);
      cmd.Parameters.Add(new MySqlParameter("?name", null));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT name FROM Test WHERE id=1";
      object name = cmd.ExecuteScalar();
      Assert.That(name, Is.EqualTo(DBNull.Value));
    }

    /// <summary>
    /// Bug #12646  	Parameters are defaulted to Decimal
    /// </summary>
    [Test]
    public void DefaultType()
    {
      IDbCommand cmd = Connection.CreateCommand();
      IDbDataParameter p = cmd.CreateParameter();
      p.ParameterName = "?boo";
      p.Value = "test";
      MySqlParameter mp = (MySqlParameter)p;
      Assert.That(mp.MySqlDbType, Is.EqualTo(MySqlDbType.VarChar));
    }

    [Test]
    public void OddCharsInParameterNames()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (1, ?nam$es)", Connection);
      cmd.Parameters.Add(new MySqlParameter("?nam$es", "Test"));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test (id, name) VALUES (2, ?nam_es)";
      cmd.Parameters.Clear();
      cmd.Parameters.Add(new MySqlParameter("?nam_es", "Test2"));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test (id, name) VALUES (3, ?nam.es)";
      cmd.Parameters.Clear();
      cmd.Parameters.Add(new MySqlParameter("?nam.es", "Test3"));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT name FROM Test WHERE id=1";
      object name = cmd.ExecuteScalar();
      Assert.That(name, Is.EqualTo("Test"));

      cmd.CommandText = "SELECT name FROM Test WHERE id=2";
      name = cmd.ExecuteScalar();
      Assert.That(name, Is.EqualTo("Test2"));

      cmd.CommandText = "SELECT name FROM Test WHERE id=3";
      name = cmd.ExecuteScalar();
      Assert.That(name, Is.EqualTo("Test3"));
    }

    /// <summary>
    /// Bug #24565 Inferring DbType fails when reusing commands and the first time the value is nul 
    /// </summary>
    [Test]
    public void UnTypedParameterBeingReused()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, dt) VALUES (?id, ?dt)", Connection);
      cmd.Parameters.AddWithValue("?id", 1);
      MySqlParameter p = cmd.CreateParameter();
      p.ParameterName = "?dt";
      p.Value = DBNull.Value;
      cmd.Parameters.Add(p);
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = 2;
      p.Value = DateTime.Now;
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Parameters.Clear();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.That(reader.IsDBNull(2));
        reader.Read();
        Assert.That(reader.IsDBNull(2), Is.False);
        Assert.That(reader.Read(), Is.False);
      }
    }

    [Test]
    public void ParameterCacheNotClearing()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (?id, ?name)", Connection);
      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Parameters.AddWithValue("?name", "test");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test (id, name, dt) VALUES (?id1, ?name1, ?id)";
      cmd.Parameters[0].ParameterName = "?id1";
      cmd.Parameters[0].Value = 2;
      cmd.Parameters[1].ParameterName = "?name1";
      cmd.Parameters.AddWithValue("?id", DateTime.Now);
      cmd.ExecuteNonQuery();
    }

    [Test]
    public void WithAndWithoutMarker()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (?id, ?name)", Connection);
      cmd.Parameters.AddWithValue("id", 1);
      Assert.That(cmd.Parameters.IndexOf("?id"), Is.EqualTo(-1));
      cmd.Parameters.AddWithValue("name", "test");
      cmd.ExecuteNonQuery();

      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("?id", 2);
      Assert.That(cmd.Parameters.IndexOf("id"), Is.EqualTo(-1));
      cmd.Parameters.AddWithValue("?name", "test2");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT COUNT(*) FROM Test";
      object count = cmd.ExecuteScalar();
      Assert.That(Convert.ToInt32(count), Is.EqualTo(2));
    }

    [Test]
    public void DoubleAddingParameters()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (?id, ?name)", Connection);
      cmd.Parameters.AddWithValue("id", 1);
      Assert.That(cmd.Parameters.IndexOf("?id"), Is.EqualTo(-1));
      Assert.That(cmd.Parameters.IndexOf("@id"), Is.EqualTo(-1));
      cmd.Parameters.AddWithValue("name", "test");
      Exception ex = Assert.Throws<MySqlException>(() => cmd.Parameters.AddWithValue("?id", 2));
      Assert.That(ex.Message, Is.EqualTo("Parameter '?id' has already been defined"));
    }

    /// <summary>
    /// Bug #26904 MySqlParameterCollection fails to add MySqlParameter that previously removed 
    /// </summary>
    [Test]
    public void AddingParameterPreviouslyRemoved()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new
      MySqlCommand("Insert into sometable(s1, s2) values(?p1, ?p2)");

      MySqlParameter param1 = cmd.CreateParameter();
      param1.ParameterName = "?p1";
      param1.DbType = DbType.String;
      param1.Value = "Ali Gel";

      cmd.Parameters.Add(param1);
      cmd.Parameters.RemoveAt(0);
      cmd.Parameters.Add(param1);
    }

    /// <summary>
    /// Bug #27135 MySqlParameterCollection and parameters added with Insert Method 
    /// </summary>
    [Test]
    public void AddingParametersUsingInsert()
    {
      MySqlCommand cmd = new MySqlCommand();
      cmd.Parameters.Insert(0, new MySqlParameter("?id", MySqlDbType.Int32));
      MySqlParameter p = cmd.Parameters["?id"];
      Assert.That(p.ParameterName, Is.EqualTo("?id"));
    }

    /// <summary>
    /// Bug #27187 cmd.Parameters.RemoveAt("Id") will cause an error if the last item is requested 
    /// </summary>
    [Test]
    public void FindParameterAfterRemoval()
    {
      MySqlCommand cmd = new MySqlCommand();

      cmd.Parameters.Add("?id1", MySqlDbType.Int32);
      cmd.Parameters.Add("?id2", MySqlDbType.Int32);
      cmd.Parameters.Add("?id3", MySqlDbType.Int32);
      cmd.Parameters.Add("?id4", MySqlDbType.Int32);
      cmd.Parameters.Add("?id5", MySqlDbType.Int32);
      cmd.Parameters.Add("?id6", MySqlDbType.Int32);
      cmd.Parameters.RemoveAt("?id1");
      MySqlParameter p = cmd.Parameters["?id6"];
      Assert.That(p.ParameterName, Is.EqualTo("?id6"));
    }

    /// <summary>
    /// Bug #29312  	System.FormatException if parameter not found
    /// </summary>
    [Test]
    public void MissingParameter()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test(id) VALUES (?id)", Connection);
      try
      {
        cmd.ExecuteNonQuery();
      }
      catch (MySqlException)
      {
      }
    }

    /// <summary>
    /// Bug #32094 Size property on string parameter throws an exception 
    /// </summary>
    [Test]
    public void StringParameterSizeSetAfterValue()
    {
      ExecuteSQL("CREATE TABLE Test (v VARCHAR(10))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?p1)", Connection);
      cmd.Parameters.Add("?p1", MySqlDbType.VarChar);
      cmd.Parameters[0].Value = "123";
      cmd.Parameters[0].Size = 10;
      cmd.ExecuteNonQuery();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.That(dt.Rows[0][0], Is.EqualTo("123"));

      cmd.Parameters.Clear();
      cmd.Parameters.Add("?p1", MySqlDbType.VarChar);
      cmd.Parameters[0].Value = "123456789012345";
      cmd.Parameters[0].Size = 10;
      cmd.ExecuteNonQuery();

      dt.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows[1][0], Is.EqualTo("1234567890"));
    }

    /// <summary>
    /// Bug #32093 MySqlParameter Constructor does not allow Direction of anything other than Input 
    /// </summary>
    [Test]
    public void NonInputParametersToCtor()
    {
      MySqlParameter p = new MySqlParameter("?p1", MySqlDbType.VarChar, 20,
          ParameterDirection.InputOutput, true, 0, 0, "id", DataRowVersion.Current, 0);
      Assert.That(p.Direction, Is.EqualTo(ParameterDirection.InputOutput));

      MySqlParameter p1 = new MySqlParameter("?p1", MySqlDbType.VarChar, 20,
          ParameterDirection.Output, true, 0, 0, "id", DataRowVersion.Current, 0);
      Assert.That(p1.Direction, Is.EqualTo(ParameterDirection.Output));
    }

    [Test]
    public void UseAtSignForParameters()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (@id, @name)", Connection);
      cmd.Parameters.AddWithValue("@id", 33);
      cmd.Parameters.AddWithValue("@name", "Test");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.That(reader.GetInt32(0), Is.EqualTo(33));
        Assert.That(reader.GetString(1), Is.EqualTo("Test"));
      }
    }

    /// <summary>
    /// Bug #62194	MySQL Parameter constructor doesn't set
    /// all properties: IsNullable, Precision and Scale
    /// </summary>
    [Test]
    public void CanCreateMySQLParameterWithNullability()
    {

      MySqlParameter p = new MySqlParameter("?id", MySqlDbType.Decimal, 2,
                                          ParameterDirection.Input, true, 1, 1, "sourceColumn", DataRowVersion.Default, 1);

      Assert.That(p.IsNullable);
    }

    /// <summary>
    /// Bug #62194	MySQL Parameter constructor doesn't set
    /// all properties: IsNullable, Precision and Scale
    /// </summary>
    [Test]
    public void CanCreateMySQLParameterWithPrecision()
    {
      MySqlParameter p = new MySqlParameter("?id", MySqlDbType.Decimal, 2,
                                          ParameterDirection.Input, true, Byte.MaxValue, 1, "sourceColumn", DataRowVersion.Default, 1);

      Assert.That(Byte.MaxValue, Is.EqualTo(p.Precision));
    }


    /// <summary>
    /// Bug #62194	MySQL Parameter constructor doesn't set
    /// all properties: IsNullable, Precision and Scale
    /// </summary>
    [Test]
    public void CanCreateMySQLParameterWithScale()
    {

      MySqlParameter p = new MySqlParameter("?id", MySqlDbType.Decimal, 2,
                                          ParameterDirection.Input, true, 1, Byte.MaxValue, "sourceColumn", DataRowVersion.Default, 1);

      Assert.That(Byte.MaxValue, Is.EqualTo(p.Scale));
    }

    /// <summary>
    /// Bug #66060 #14499549 "Parameter '?' must be defined" error, when using unnamed parameters
    /// </summary>
    [Test]
    public void CanIdentifyParameterWithOutName()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (?, ?)", Connection);

      cmd.Parameters.AddWithValue("", 1);
      cmd.Parameters.AddWithValue("", "test");

      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT id FROM Test";
      Assert.That(cmd.ExecuteScalar(), Is.EqualTo(1));

      cmd.CommandText = "SELECT name FROM Test";
      Assert.That(cmd.ExecuteScalar(), Is.EqualTo("test"));
    }

    /// <summary>
    /// Bug #66060  #14499549  "Parameter '?' must be defined" error, when using unnamed parameters
    /// </summary>
    [Test]
    public void CanThrowAnExceptionWhenMixingParameterNaming()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (?Id, ?name, ?)", Connection);
      cmd.Parameters.AddWithValue("?Id", 1);
      cmd.Parameters.AddWithValue("?name", "test");
      Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      Assert.That(ex.Message, Is.EqualTo("Fatal error encountered during command execution"));
    }

    /// <summary>
    /// Bug #22101727 CONNECTOR MODIFIES RESULT TYPE AFTER PARENT TINYINT VALUE IS NULL
    /// </summary>
    [Test]
    public void TreatTinyAsBooleanWhenNull()
    {
      ExecuteSQL("CREATE TABLE testbool (id INT (10) UNSIGNED NOT NULL AUTO_INCREMENT, testcol TINYINT(1) DEFAULT NULL, PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO testbool(testcol) VALUES(0),(1),(1),(NULL),(0),(0),(1)");

      using (var conn = new MySqlConnection(Settings.ConnectionString))
      {
        conn.Open();
        MySqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testbool";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            if (!(reader["testcol"] is DBNull))
              Assert.That(reader["testcol"] is bool);
          }
        }
      }
    }

    /// <summary>
    /// Bug #27113566 MYSQLCOMMAND.PREPARE STOPS TINYINT(1) FROM BEING TREATED AS A .NET BOOL
    /// </summary>
    [Test]
    public void TreatTinyAsBooleanWhenCallingPrepare()
    {
      ExecuteSQL("CREATE TABLE `mysql_bug_test` (`test_key` varchar(10) NOT NULL, `test_val` tinyint(1) NOT NULL, PRIMARY KEY(`test_key`)) ENGINE = InnoDB DEFAULT CHARSET = utf8; ");
      ExecuteSQL("LOCK TABLES `mysql_bug_test` WRITE;");
      ExecuteSQL("INSERT INTO `mysql_bug_test` VALUES ('mykey',0);");
      ExecuteSQL("UNLOCK TABLES;");

      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.CharacterSet = "utf8";
      builder.UseCompression = true;
      builder.TreatTinyAsBoolean = false;

      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        using (var cmd = new MySqlCommand("SELECT * FROM mysql_bug_test WHERE test_key = @TestKey", connection))
        {
          cmd.Parameters.AddWithValue("@TestKey", "mykey").MySqlDbType = MySqlDbType.VarChar;
          cmd.Prepare();
          using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
          {
            reader.Read();
            Assert.That(reader["test_val"] is bool, Is.False);
          }
        }

        connection.Close();
      }

      builder.TreatTinyAsBoolean = true;

      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        using (var cmd = new MySqlCommand("SELECT * FROM mysql_bug_test WHERE test_key = @TestKey", connection))
        {
          cmd.Parameters.AddWithValue("@TestKey", "mykey").MySqlDbType = MySqlDbType.VarChar;
          cmd.Prepare();
          using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
          {
            reader.Read();
            Assert.That(reader["test_val"] is bool);
          }
        }

        connection.Close();
      }
    }

    /// <summary >
    /// Bug #25573071 MYSQLPARAMETER INT ZERO EVALUATED TO NULL
    /// This fix was reverted since it was wrong implemented. See https://mybug.mysql.oraclecorp.com/orabugs/site/bug.php?id=32050204 for more details
    /// </summary>
    [Test]
    public void ZeroParameterAsNull()
    {
      ExecuteSQL(@"DROP TABLE IF EXISTS `audit`");
      ExecuteSQL(@"CREATE TABLE `audit` (`ProviderId` int(11) NOT NULL,`Permanent` tinyint(4) NOT NULL DEFAULT '1')");
      ExecuteSQL(@"insert into `audit` values (1,0);");
      var query = "SELECT * FROM audit t1 WHERE t1.Permanent = ?IsFalse";
      MySqlParameter[] parameters = { new MySqlParameter() { ParameterName = "IsFalse", Value = 0 } };
      var ds = MySqlHelper.ExecuteDataset(Connection.ConnectionString, query, parameters);
      Assert.That(1, Is.EqualTo(ds.Tables[0].Rows.Count));
    }

    /// <summary>
    /// Bug #32050204 - DEFAULT VALUE FOR MYSQLPARAMETER.VALUE CHANGED FROM NULL TO 0
    /// This fix reverted the work done in Bug#25573071
    /// </summary>
    [Test]
    public void DefaultNullValue()
    {
      ExecuteSQL("CREATE TABLE Test (data INT NULL)");
      string cmdString = "INSERT INTO Test(data) VALUES(@Data)";
      using (var cmd = new MySqlCommand(cmdString, Connection))
      {
        cmd.Parameters.Add(new MySqlParameter("@Data", MySqlDbType.Int32));
        cmd.ExecuteNonQuery();
      }

      using (var command = new MySqlCommand("SELECT data FROM Test", Connection))
      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          Assert.That(reader.IsDBNull(0));
        }
      }
    }

    /// <summary >
    /// Bug #25467610	OVERFLOW EXCEPTION - 64 BIT ENUM VALUE AS PARAMETER CAST TO INT32
    /// </summary>
    public enum TestEnum : ulong
    {
      Value = ulong.MaxValue
    }

    public enum TestEnumDefault
    {
      Value = int.MaxValue
    }

    public enum TestEnumByte : byte
    {
      Value = byte.MaxValue
    }


    [TestCase(TestEnum.Value, "serial")]
    [TestCase(TestEnumDefault.Value, "int")]
    [TestCase(TestEnumByte.Value, "TINYINT UNSIGNED")]
    public void CastingEnum(Enum name, string typeName)
    {
      ExecuteSQL(@"DROP TABLE IF EXISTS `test`");
      ExecuteSQL($"CREATE TABLE `test` (`id` {typeName})");
      using (var conn = new MySqlConnection(Settings.ConnectionString))
      {
        conn.Open();
        string sql = "select * from test where id = @ID;";
        MySqlCommand command = new MySqlCommand(sql, conn);
        command.Parameters.AddWithValue("@ID", name);
        MySqlDataReader rdr = command.ExecuteReader();
        Assert.That(rdr, Is.Not.Null);
      }
    }

    /// <summary>
    /// Bug #31754599 - MYSQLCOMMAND.PARAMETERS.INSERT(-1) SUCCEEDS BUT SHOULD FAIL
    /// </summary>
    [Test]
    public void InvalidParameterIndex()
    {
      var cmd = new MySqlCommand();
      cmd.Connection = Connection;
      cmd.Parameters.Insert(0, new MySqlParameter("test0", "test0"));
      Assert.Throws<ArgumentOutOfRangeException>(() => cmd.Parameters.Insert(-1, new MySqlParameter("test-1", "test-1")));
      Assert.Throws<ArgumentOutOfRangeException>(() => cmd.Parameters.Insert(-2, new MySqlParameter("test-1", "test-1")));
      cmd.Parameters.Insert(1, new MySqlParameter("test1", "test1"));
      cmd.Parameters.Insert(0, new MySqlParameter("testNew0", "test2"));

      Assert.That(cmd.Parameters.IndexOf("testNew0") == 0);
      Assert.That(cmd.Parameters.IndexOf("test0") == 1);
      Assert.That(cmd.Parameters.IndexOf("test1") == 2);

      cmd.Parameters.AddWithValue("", "test3");
      Assert.That(cmd.Parameters.Count == 4);
      Assert.That(cmd.Parameters.IndexOf("Parameter4") == 3);

      cmd.Parameters.Insert(1, new MySqlParameter("lastTest", "test4"));
      Assert.That(cmd.Parameters.IndexOf("lastTest") == 1);
    }

    /// <summary>
    /// Bug #13276 Exception on serialize after inserting null value
    /// </summary>
    [Test]
    public void InsertValueAfterNull()
    {
      ExecuteSQL("CREATE TABLE Test (id int auto_increment primary key, foo int)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommand c = new MySqlCommand("INSERT INTO Test (foo) values (?foo)", Connection);
      c.Parameters.Add("?foo", MySqlDbType.Int32, 0, "foo");

      da.InsertCommand = c;
      DataTable dt = new DataTable();
      da.Fill(dt);
      DataRow row = dt.NewRow();
      dt.Rows.Add(row);
      row = dt.NewRow();
      row["foo"] = 2;
      dt.Rows.Add(row);
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(2));
      Assert.That(dt.Rows[1]["foo"], Is.EqualTo(2));
    }

    /// <summary>
    /// Bug #20056757	- MYSQLPARAMETER.CLONE MISSED ASSIGN VALUE TO PROPERTY SOURCECOLUMNNULLMAPPING
    /// At the moment of cloning the parameters, the SourceColumnNullMapping property was missing to copy hence the exception
    /// </summary>
    [Test]
    public void CloneParameterAssignSourceColumnNullMapping()
    {
      ExecuteSQL("CREATE TABLE Test (id INT AUTO_INCREMENT, name VARCHAR(10) NULL, PRIMARY KEY(id)); INSERT INTO Test VALUES (1, null)");
      string query = "SELECT * FROM Test";

      MySqlDataAdapter dataAdapter = new MySqlDataAdapter(query, Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(dataAdapter);
      dataAdapter.DeleteCommand = (MySqlCommand)cb.GetDeleteCommand().Clone();
      DataTable dataTable = new DataTable();
      dataAdapter.Fill(dataTable);

      using (var cmd = new MySqlCommand(query, Connection))
        Assert.That(cmd.ExecuteScalar(), Is.EqualTo(1));
      Assert.That(dataTable.Rows.Count, Is.EqualTo(1));

      dataTable.Rows[0].Delete();
      dataAdapter.Update(dataTable);

      using (var cmd = new MySqlCommand(query, Connection))
        Assert.That(cmd.ExecuteScalar(), Is.Null);
      Assert.That(dataTable.Rows.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// Bug #23343947 - .NET BUG WRITE NULLABLE VALUES
    /// Initializing of the parameter changed to match same type for DbType and MySqlDbType, String.
    /// </summary>
    [Test]
    public void InitializeParameter()
    {
      var cmd = new MySqlCommand();
      var newParam = cmd.CreateParameter();
      var newIntParam = new MySqlParameter("newIntParam", 3);

      Assert.That(newParam.MySqlDbType, Is.EqualTo(MySqlDbType.VarChar));
      Assert.That(newParam.DbType, Is.EqualTo(DbType.String));
      Assert.That(newIntParam.MySqlDbType, Is.EqualTo(MySqlDbType.Int32));
      Assert.That(newIntParam.DbType, Is.EqualTo(DbType.Int32));
    }

    /// <summary>
    /// Bug #33710643 [Poor performance when adding parameters with "Add(Object)"]
    /// Before the fix, the method Add(object value) was above the 8-10 seconds.
    /// </summary>
    [Test]
    public void AddObjectPerformance()
    {
      int paramCount = 50000;
      var cmd = new MySqlCommand();
      var sw1 = new Stopwatch();
      var sw2 = new Stopwatch();

      sw1.Start();
      for (int i = 0; i < paramCount; i++)
      {
        IDbDataParameter p = cmd.CreateParameter();
        p.ParameterName = $"?param_{i}";
        p.DbType = DbType.String;
        cmd.Parameters.AddWithValue(p.ParameterName, p);
      }
      sw1.Stop();
      cmd.Parameters.Clear();

      sw2.Start();
      for (int i = 0; i < paramCount; i++)
      {
        IDbDataParameter p = cmd.CreateParameter();
        p.ParameterName = $"?param_{i}";
        p.DbType = DbType.String;
        cmd.Parameters.Add(p);
      }
      sw2.Stop();
      Console.Write(sw2.Elapsed);

      Assert.That(sw1.Elapsed.TotalSeconds < 1);
      Assert.That(sw2.Elapsed.TotalSeconds < 1);
    }
  }
}
