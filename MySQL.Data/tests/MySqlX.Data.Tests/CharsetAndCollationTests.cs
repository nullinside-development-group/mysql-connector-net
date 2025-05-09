// Copyright © 2017, 2025, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// Charset and collation related tests.
  /// </summary>
  public class CharsetAndCollationTests : BaseTest
  {
    private static DBVersion _serverVersion;

    static CharsetAndCollationTests()
    {
      using (var connection = new MySqlConnection(ConnectionStringRoot))
      {
        connection.Open();
        _serverVersion = connection.driver.Version;
      }
    }

    [Test]
    public void DefaultCharSet()
    {
      if (!_serverVersion.isAtLeast(8, 0, 1)) return;

      using (var session = MySQLX.GetSession(ConnectionString))
      {
        Assert.That(session.Settings.CharacterSet, Is.EqualTo("utf8mb4"));
      }

      using (var connection = new MySqlConnection(ConnectionStringRoot))
      {
        connection.Open();
        MySqlCommand cmd = new MySqlCommand("SHOW VARIABLES LIKE 'character_set_connection'", connection);
        MySqlDataReader reader = cmd.ExecuteReader();
        reader.Read();
        Assert.That(reader.GetString("Value"), Is.EqualTo("utf8mb4"));
        reader.Close();

        cmd.CommandText = "SHOW VARIABLES LIKE 'character_set_database'";
        reader = cmd.ExecuteReader();
        reader.Read();
        Assert.That(reader.GetString("Value"), Is.EqualTo("utf8mb4"));
        reader.Close();

        cmd.CommandText = "SHOW VARIABLES LIKE 'character_set_server'";
        reader = cmd.ExecuteReader();
        reader.Read();
        Assert.That(reader.GetString("Value"), Is.EqualTo("utf8mb4"));
        reader.Close();

        cmd.CommandText = "SHOW VARIABLES LIKE 'collation_%'";
        reader = cmd.ExecuteReader();
        while (reader.Read())
        {
          Assert.That(reader.GetString("Value"), Is.EqualTo("utf8mb4_0900_ai_ci"));
        }
        reader.Close();
      }
    }

    [Test]
    public void ValidateCollationMapList()
    {
      if (!_serverVersion.isAtLeast(8, 0, 1)) return;

      using (var connection = new MySqlConnection(ConnectionStringRoot))
      {
        connection.Open();
        var command = new MySqlCommand("SELECT id, collation_name FROM INFORMATION_SCHEMA.COLLATIONS", connection);
        var reader = command.ExecuteReader();
        Assert.That(reader.HasRows);

        while (reader.Read())
        {
          var id = reader.GetInt32("id");
          var collationName = reader.GetString("collation_name");

          if (!_serverVersion.isAtLeast(8, 0, 30) && collationName.Contains("utf8_"))
            collationName = collationName.Replace("utf8_", "utf8mb3_");

          Assert.That(collationName, Is.EqualTo(CollationMap.GetCollationName(id)));
        }
      }
    }

    /// <summary>
    /// Bug #26163694 SELECT WITH/WO PARAMS(DIFF COMB) N PROC CALL FAIL WITH KEY NOT FOUND EX-WL#10561
    /// </summary>
    [Test]
    public void Utf8mb4CharsetExists()
    {
      if (!_serverVersion.isAtLeast(8, 0, 1)) return;

      using (Session session = MySQLX.GetSession(ConnectionString))
      {
        // Search utf8mb4 database.
        var result = ExecuteSQLStatement(session.SQL("SHOW COLLATION WHERE id = 255"));
        Assert.That(result.HasData);
        var data = result.FetchOne();
        Assert.That(data.GetString("Collation"), Is.EqualTo("utf8mb4_0900_ai_ci"));

        // Check in CollationMap.
        Assert.That(CollationMap.GetCollationName(255), Is.EqualTo("utf8mb4_0900_ai_ci"));
      }
    }

    /// <summary>
    /// Bug #26163703 SHOW COLLATION FAILS WITH MYSQL SERVER 8.0-WL#10561
    /// </summary>
    [Test]
    public void IllegalMixCollations()
    {
      using (Session session = MySQLX.GetSession(ConnectionString))
      {
        var result = ExecuteSQLStatement(session.SQL("SHOW COLLATION WHERE `Default` ='Yes';"));
        Assert.That(result.HasData);
      }

      using (Session session = MySQLX.GetSession(ConnectionString + ";charset=latin1"))
      {
        var result = ExecuteSQLStatement(session.SQL("SHOW COLLATION WHERE `Default` ='Yes';"));
        Assert.That(result.HasData);
      }

      using (Session session = MySQLX.GetSession(ConnectionString + ";charset=utf8mb4"))
      {
        var result = ExecuteSQLStatement(session.SQL("SHOW COLLATION WHERE `Default` ='Yes';"));
        Assert.That(result.HasData);
      }

      using (Session session = MySQLX.GetSession(ConnectionString + ";charset=utf-8"))
      {
        var result = ExecuteSQLStatement(session.SQL("SHOW COLLATION WHERE `Default` ='Yes';"));
        Assert.That(result.HasData);
      }
    }

    /// <summary>
    /// Bug #26163678 VIEW.SELECT RETURNS TYPE INSTEAD OF THE TABLE-WL#10561
    /// Bug #26163667 COLLECTIONS.NAME RETURNS TYPE INSTEAD OF THE NAME OF THE COLLECTION-WL#10561
    /// </summary>
    [Test]
    public void NamesAreReturnedAsStrings()
    {
      session.SQL("DROP DATABASE IF EXISTS test").Execute();
      session.SQL("CREATE DATABASE test").Execute();
      using (Session mySession = new Session(ConnectionString))
      {
        Schema test = mySession.GetSchema("test");
        ExecuteSQL("CREATE TABLE test1(id1 int,firstname varchar(20))");
        ExecuteSQL("INSERT INTO test1 values ('1','Rob')");
        ExecuteSQL("INSERT INTO test1 values ('2','Steve')");
        ExecuteSQL("CREATE TABLE test2(id2 int,lastname varchar(20))");
        ExecuteSQL("INSERT INTO test2 values ('1','Williams')");
        ExecuteSQL("INSERT INTO test2 values ('2','Waugh')");
        ExecuteSQL("CREATE VIEW view1 AS select * from test.test1");
        ExecuteSQL("SELECT * FROM view1");
        ExecuteSQL("CREATE VIEW view2 AS select * from test.test2");
        ExecuteSQL("SELECT * FROM view2");

        List<Table> tables = test.GetTables();
        Assert.That(tables.Count, Is.EqualTo(4));
        Assert.That(tables.FindAll(i => !i.IsView).Count, Is.EqualTo(2));
        Assert.That(tables.FindAll(i => i.IsView).Count, Is.EqualTo(2));
        ExecuteSelectStatement(tables[0].Select());
        ExecuteSelectStatement(tables[1].Select());
        ExecuteSelectStatement(tables[2].Select());
        ExecuteSelectStatement(tables[3].Select());
        Assert.That(tables[0].Name, Is.EqualTo("test1"));
        Assert.That(tables[1].Name, Is.EqualTo("test2"));
        Assert.That(tables[2].Name, Is.EqualTo("view1"));
        Assert.That(tables[3].Name, Is.EqualTo("view2"));

        Table table = test.GetTable("test2");
        Assert.That(table.Name, Is.EqualTo("test2"));

        Collection c = test.CreateCollection("coll");

        List<Collection> collections = test.GetCollections();
        Assert.That(collections, Has.One.Items);
        Assert.That(collections[0].Name, Is.EqualTo("coll"));

        Collection collection = test.GetCollection("coll");
        Assert.That(collection.Name, Is.EqualTo("coll"));
      }
    }

    #region WL14389

    [Test, Description("Column Default Datatypes")]
    public void ColumnDefaultDatatypes()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      session.SQL($"USE {schemaName}").Execute();
      session.SQL("Drop table if exists address").Execute();
      session.SQL("CREATE TABLE address" +
                   "(address_number1  TINYINT NOT NULL AUTO_INCREMENT, " +
                   "address_number2  SMALLINT NOT NULL, " +
                   "address_number3  MEDIUMINT NOT NULL, " +
                   "address_number4  INT NOT NULL, " +
                   "address_number5  BIGINT NOT NULL, " +
                   "address_number6  FLOAT(10) NOT NULL, " +
                   "address_number7  FLOAT(10,2) NOT NULL, " +
                   "address_number8  DOUBLE(10,2) NOT NULL, " +
                   "address_number9  DECIMAL(6,4) NOT NULL, " +
                   "address_number10  BIT NOT NULL, " +
                   "building_name1  CHAR(100) NOT NULL, " +
                   "building_name2  VARCHAR(100) NOT NULL, " +
                   "building_name3  TINYTEXT NOT NULL, " +
                   "building_name4  MEDIUMTEXT NOT NULL, " +
                   "building_name5  LONGTEXT NOT NULL, " +
                   "building_name6  BINARY NOT NULL, " +
                   "building_name7  VARBINARY(120) NOT NULL, " +
                   "building_name8  BLOB NOT NULL, " +
                   "building_name9  MEDIUMBLOB NOT NULL, " +
                   "building_name10  LONGBLOB NOT NULL, " +
                   "building_name11 ENUM('x-small', 'small', 'medium', 'large', 'x-large') NOT NULL, " +
                   "building_name12  SET('x-small', 'small', 'medium', 'large', 'x-large') NOT NULL, " +
                   "building_name13  DATE NOT NULL, " +
                   "building_name14  DATETIME(6) NOT NULL, " +
                   "building_name15  TIME NOT NULL, " +
                   "building_name16  TIMESTAMP NOT NULL, " +
                   "building_name17  YEAR NOT NULL, " +
                   "PRIMARY KEY (address_number1)" + ");").Execute();
      session.SQL("INSERT INTO address" +
                  "(address_number1,address_number2,address_number3,address_number4,address_number5,address_number6,address_number7,address_number8,address_number9,address_number10,building_name1,building_name2,building_name3,building_name4,building_name5,building_name6,building_name7,building_name8,building_name9,building_name10,building_name11,building_name12,building_name13,building_name14,building_name15,building_name16,building_name17)" +
                  " VALUES " +
                  "(1,1000,100,500,1000000000000,22.7,81.80,10.9,10,0,'BGL','TEST','ABCDEFGHIJKLMNOPQRSTUVWXYZ' ,'A','CHECK',0,256,0,128,256,'large','medium','1000-01-01','2012-11-12 13:54:00.12345678','838:59:59','20160316153000','2100');").Execute();
      string[] columns = new string[] { "address_number1", "address_number2", "address_number3",
                    "address_number4","address_number5","address_number6","address_number7","address_number8","address_number9","address_number10",
                    "building_name1","building_name2","building_name3","building_name4","building_name5","building_name6","building_name7",
                    "building_name8","building_name9","building_name10","building_name11","building_name12","building_name13","building_name14",
                    "building_name15","building_name16","building_name17"};
      uint[] Length = new uint[] { 4, 6, 9, 11, 20, 12, 10, 10, 8, 1, 100, 100, 255, 16777215, 4294967295, 1, 120, 65535, 16777215, 4294967295, 7, 34, 10, 26, 10, 19, 4 };
      string[] columnTypeMatch = new string[] { "Tinyint", "Smallint", "Mediumint", "Int", "Bigint", "Float", "Float",
                        "Double", "Decimal", "Bit", "String", "String", "String", "String", "String", "Bytes", "Bytes", "Bytes",
                        "Bytes", "Bytes", "Enum", "Set", "Date", "DateTime", "Time", "Timestamp", "Smallint" };
      uint[] FDLength = new uint[] { 0, 0, 0, 0, 0, 31, 2, 2, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
      string[] columnIsSignedMatch = new string[] { "True", "True", "True", "True", "True", "False", "False",
                        "False", "False", "False", "False", "False", "False", "False", "False", "False", "False", "False", "False",
                        "False", "False", "False", "False", "False", "False", "False", "False" };
      string[] columnIsPaddedMatch = new string[] { "False", "False", "False", "False", "False", "False", "False",
                        "False", "False", "False", "True", "False", "False", "False", "False", "True", "False", "False", "False",
                        "False", "False", "False", "False", "False", "False", "False", "False" };

      string[] clrTypeMatch = new string[] { "System.SByte", "System.Int16", "System.Int32", "System.Int32", "System.Int64", "System.Single", "System.Single",
                        "System.Double", "System.Decimal", "System.UInt64", "System.String", "System.String", "System.String", "System.String", "System.String", "System.Byte[]", "System.Byte[]", "System.Byte[]",
                        "System.Byte[]", "System.Byte[]", "System.String", "System.String", "System.DateTime", "System.DateTime", "System.TimeSpan", "System.DateTime", "System.UInt16" };
      RowResult result = session.GetSchema(schemaName).GetTable("address").Select(columns).Execute();

      for (int i = 0; i < columns.Length; i++)
      {
        var tableType = result.Columns[i].Type;
        Assert.That(tableType.ToString(), Is.EqualTo(columnTypeMatch[i]), "Matching the table Type");
        string tableLabel = result.Columns[i].TableLabel;
        Assert.That(tableLabel, Is.EqualTo("address"), "Matching the table label");
        string columnName = result.Columns[i].ColumnName;
        Assert.That(columnName, Is.EqualTo(columns[i].ToString()), "Matching the Column Name");
        string columnLabel = result.Columns[i].ColumnLabel;
        Assert.That(columnLabel, Is.EqualTo(columns[i].ToString()), "Matching the Column Label");
        uint columnLength = result.Columns[i].Length;
        Assert.That(columnLength, Is.EqualTo(columnLength), "Matching the Column Length");
        var columnType = result.Columns[i].Type;
        Assert.That(columnType.ToString(), Is.EqualTo(columnTypeMatch[i]), "Matching the Column Type");
        var columnFD = result.Columns[i].FractionalDigits;
        Assert.That(columnFD, Is.EqualTo(FDLength[i]), "Matching the Column FD");
        var columnIsSigned = result.Columns[i].IsNumberSigned;
        Assert.That(columnIsSigned.ToString(), Is.EqualTo(columnIsSignedMatch[i]), "Matching whether column is signed or not");
        string columnCollation = result.Columns[i].CollationName;
        if (i == 10 || i == 11 || i == 12 || i == 13 || i == 14 || i == 20 || i == 21)
          Assert.That(columnCollation, Is.EqualTo(columnCollation), "Matching the Collation Name for default characters");
        else if (i == 15 || i == 16 || i == 17 || i == 18 || i == 19)
          Assert.That(columnCollation, Is.EqualTo("binary"), "Matching the Collation Name for default characters");
        else
          Assert.That(columnCollation, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        string columnCharacterSet = result.Columns[i].CharacterSetName;
        if (i == 10 || i == 11 || i == 12 || i == 13 || i == 14 || i == 20 || i == 21)
          Assert.That(columnCharacterSet, Is.EqualTo(columnCharacterSet), "Matching the CharacterSet Name for default characters");
        else if (i == 15 || i == 16 || i == 17 || i == 18 || i == 19)
          Assert.That(columnCharacterSet, Is.EqualTo("binary"), "Matching the Collation Name for default characters");
        else
          Assert.That(columnCharacterSet, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        var columnIsPadded = result.Columns[i].IsPadded;
        Assert.That(columnIsPadded.ToString(), Is.EqualTo(columnIsPaddedMatch[i]), "Matching whether column is padded or not");
        var columnClrType = result.Columns[i].ClrType;
        Assert.That(columnClrType.ToString(), Is.EqualTo(clrTypeMatch[i]), "Matching whether column CLR Type");
      }
    }

    [Test, Description("Column Custom Datatypes(Unsigned,Padded,CharacterSet,Collation)")]
    public void ColumnCustomDatatypes()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 14), "This test is for MySql 8.0.14 or higher");
      var defaultCharset = "utf8mb4";
      session.SQL($"USE {schemaName}").Execute();
      session.SQL("CREATE TABLE IF NOT EXISTS address" +
                    "(address_number1  TINYINT UNSIGNED NOT NULL AUTO_INCREMENT, " +
                    "address_number2  SMALLINT ZEROFILL NOT NULL, " +
                    "address_number3  MEDIUMINT NOT NULL, " +
                    "address_number4  INT UNSIGNED NOT NULL, " +
                    "address_number5  BIGINT NOT NULL, " +
                    "address_number6  FLOAT(10) NOT NULL, " +
                    "address_number7  FLOAT(10,2) NOT NULL, " +
                    "address_number8  DOUBLE(10,2) NOT NULL, " +
                    "address_number9  DECIMAL(6,4) NOT NULL, " +
                    "address_number10  BIT NOT NULL, " +
                    "building_name1  CHAR(100) NOT NULL, " +
                    "building_name2  VARCHAR(100) NOT NULL, " +
                    "building_name3  TINYTEXT NOT NULL, " +
                    "building_name4  MEDIUMTEXT NOT NULL, " +
                    "building_name5  LONGTEXT NOT NULL, " +
                    "building_name6  BINARY NOT NULL, " +
                    "building_name7  VARBINARY(120) NOT NULL, " +
                    "building_name8  BLOB NOT NULL, " +
                    "building_name9  MEDIUMBLOB NOT NULL, " +
                    "building_name10  LONGBLOB NOT NULL, " +
                    "building_name11 ENUM('x-small', 'small', 'medium', 'large', 'x-large') NOT NULL, " +
                    "building_name12  SET('x-small', 'small', 'medium', 'large', 'x-large') NOT NULL, " +
                    "building_name13  DATE NOT NULL, " +
                    "building_name14  DATETIME(6) NOT NULL, " +
                    "building_name15  TIME NOT NULL, " +
                    "building_name16  TIMESTAMP NOT NULL, " +
                    "building_name17  YEAR NOT NULL, " +
                    "PRIMARY KEY (address_number1)" + ")CHARACTER SET latin1 COLLATE latin1_danish_ci;").Execute();
      session.SQL("INSERT INTO address" +
                  "(address_number1,address_number2,address_number3,address_number4,address_number5,address_number6,address_number7,address_number8,address_number9,address_number10,building_name1,building_name2,building_name3,building_name4,building_name5,building_name6,building_name7,building_name8,building_name9,building_name10,building_name11,building_name12,building_name13,building_name14,building_name15,building_name16,building_name17)" +
                  " VALUES " +
                  "(0,1000,100,0,1000000000000,22.7,81.8,10.9,10,0,'BGL','TEST','ABCDEFGHIJKLMNOPQRSTUVWXYZ' ,'A','CHECK',0,256,0,128,256,'large','medium','1000-01-01','2012-11-12 13:54:00.12345678','838:59:59','20160316153000','2100');").Execute();
      string[] columns = new string[] { "address_number1", "address_number2", "address_number3",
                    "address_number4","address_number5","address_number6","address_number7","address_number8","address_number9","address_number10",
                    "building_name1","building_name2","building_name3","building_name4","building_name5","building_name6","building_name7",
                    "building_name8","building_name9","building_name10","building_name11","building_name12","building_name13","building_name14",
                    "building_name15","building_name16","building_name17"};
      uint[] Length = new uint[] { 3, 5, 9, 10, 20, 12, 10, 10, 8, 1, 100, 100, 255, 16777215, 4294967295, 1, 120, 65535, 16777215, 4294967295, 0, 0, 10, 26, 10, 19, 4 };
      string[] columnTypeMatch = new string[] { "Tinyint", "Smallint", "Mediumint", "Int", "Bigint", "Float", "Float",
                        "Double", "Decimal", "Bit", "String", "String", "String", "String", "String", "Bytes", "Bytes", "Bytes",
                        "Bytes", "Bytes", "Enum", "Set", "Date", "DateTime", "Time", "Timestamp", "Smallint" };
      uint[] FDLength = new uint[] { 0, 0, 0, 0, 0, 31, 2, 2, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
      string[] columnIsSignedMatch = new string[] { "False", "False", "True", "False", "True", "False", "False",
                        "False", "False", "False", "False", "False", "False", "False", "False", "False", "False", "False", "False",
                        "False", "False", "False", "False", "False", "False", "False", "False" };
      string[] columnIsPaddedMatch = new string[] { "False", "True", "False", "False", "False", "False", "False",
                        "False", "False", "False", "True", "False", "False", "False", "False", "True", "False", "False", "False",
                        "False", "False", "False", "False", "False", "False", "False", "False" };

      string[] clrTypeMatch = new string[] { "System.Byte", "System.UInt16", "System.Int32", "System.UInt32", "System.Int64", "System.Single", "System.Single",
                        "System.Double", "System.Decimal", "System.UInt64", "System.String", "System.String", "System.String", "System.String", "System.String", "System.Byte[]", "System.Byte[]", "System.Byte[]",
                        "System.Byte[]", "System.Byte[]", "System.String", "System.String", "System.DateTime", "System.DateTime", "System.TimeSpan", "System.DateTime", "System.UInt16" };
      RowResult result = session.GetSchema(schemaName).GetTable("address").Select(columns).Execute();

      for (int i = 0; i < columns.Length; i++)
      {
        string tableLabel = result.Columns[i].TableLabel;
        Assert.That(tableLabel, Is.EqualTo("address"), "Matching the table label");
        string columnName = result.Columns[i].ColumnName;
        Assert.That(columnName, Is.EqualTo(columns[i].ToString()), "Matching the Column Name");
        string columnLabel = result.Columns[i].ColumnLabel;
        Assert.That(columnLabel, Is.EqualTo(columns[i].ToString()), "Matching the Column Label");
        uint columnLength = result.Columns[i].Length;
        Assert.That(columnLength, Is.EqualTo(Length[i]), "Matching the Column Length");
        var columnType = result.Columns[i].Type;
        Assert.That(columnType.ToString(), Is.EqualTo(columnTypeMatch[i]), "Matching the Column Type");
        var columnFD = result.Columns[i].FractionalDigits;
        Assert.That(columnFD, Is.EqualTo(FDLength[i]), "Matching the Column FD");
        var columnIsSigned = result.Columns[i].IsNumberSigned;
        Assert.That(columnIsSigned.ToString(), Is.EqualTo(columnIsSignedMatch[i]), "Matching whether column is signed or not");
        string columnCollation = result.Columns[i].CollationName;
        if (i == 10 || i == 11 || i == 12 || i == 13 || i == 14 || i == 20 || i == 21)
          Assert.That(columnCollation, Is.EqualTo(columnCollation), "Matching the Collation Name for default characters");
        else if (i == 15 || i == 16 || i == 17 || i == 18 || i == 19)
          Assert.That(columnCollation, Is.EqualTo("binary"), "Matching the Collation Name for default characters");
        else
          Assert.That(columnCollation, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        string columnCharacterSet = result.Columns[i].CharacterSetName;
        if (i == 10 || i == 11 || i == 12 || i == 13 || i == 14 || i == 20 || i == 21)
          Assert.That(columnCharacterSet, Is.EqualTo(defaultCharset).IgnoreCase, "Matching the CharacterSet Name for default characters");
        else if (i == 15 || i == 16 || i == 17 || i == 18 || i == 19)
          Assert.That(columnCharacterSet, Is.EqualTo("binary"), "Matching the Collation Name for default characters");
        else
          Assert.That(columnCharacterSet, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        var columnIsPadded = result.Columns[i].IsPadded;
        Assert.That(columnIsPadded.ToString(), Is.EqualTo(columnIsPaddedMatch[i]), "Matching whether column is padded or not");
        var columnClrType = result.Columns[i].ClrType;
        Assert.That(columnClrType.ToString(), Is.EqualTo(clrTypeMatch[i]), "Matching whether column CLR Type");
      }
      session.SQL($"drop table if exists address").Execute();
    }

    [Test, Description("Column Join Two tables")]
    public void ColumnJoin()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 14), "This test is for MySql 8.0.14 or higher");
      var defaultCharset = "utf8mb4";
      session.SQL($"USE {schemaName}").Execute();
      session.SQL("CREATE TABLE address1" +
                  "(address_number1  TINYINT UNSIGNED NOT NULL AUTO_INCREMENT, " +
                  "address_number2  SMALLINT ZEROFILL NOT NULL, " +
                  "address_number3   CHAR(100) NOT NULL, " +
                  "PRIMARY KEY (address_number1)" + ");").Execute();
      session.SQL("CREATE TABLE address2" +
                 "(address_number4  INT UNSIGNED NOT NULL AUTO_INCREMENT, " +
                 "address_number5  BIGINT ZEROFILL NOT NULL, " +
                 "address_number6   CHAR(100) NOT NULL, " +
                 "PRIMARY KEY (address_number4)" + ");").Execute();
      session.SQL("INSERT INTO address1" +
                  "(address_number1,address_number2,address_number3)" +
                  " VALUES " +
                  "(1,1000,'ABCDEFGHIJKLMNOPQRSTUVWXYZ');").Execute();
      session.SQL("INSERT INTO address1" +
                  "(address_number1,address_number2,address_number3)" +
                  " VALUES " +
                  "(2,4000,'AEIOU');").Execute();
      session.SQL("INSERT INTO address2" +
                  "(address_number4,address_number5,address_number6)" +
                  " VALUES " +
                  "(1,2000,'TEST1ABCDEFGHIJKLMNOPQRSTUVWXYZ');").Execute();
      session.SQL("INSERT INTO address2" +
                  "(address_number4,address_number5,address_number6)" +
                  " VALUES " +
                  "(3,6000,'TEST1AEIOU');").Execute();
      session.SQL("CREATE TABLE result1 AS SELECT " +
                 "address1.address_number1, address1.address_number2, address1.address_number3, " +
                 "address2.address_number4, address2.address_number5,address2.address_number6 " +
                 "FROM address1,address2 " +
                 "WHERE address1.address_number1 = address2.address_number4;").Execute();
      session.SQL("CREATE TABLE result2 AS SELECT * FROM address1;").Execute();
      string[] columns1 = new string[] { "address_number1", "address_number2", "address_number3", "address_number4", "address_number5", "address_number6" };
      string[] columns2 = new string[] { "address_number1", "address_number2", "address_number3" };
      uint[] Length1 = new uint[] { 3, 5, 100, 10, 20, 100 };
      uint[] Length2 = new uint[] { 3, 5, 400 };
      string[] columnTypeMatch1 = new string[] { "Tinyint", "Smallint", "String", "Int", "Bigint", "String" };
      string[] columnTypeMatch2 = new string[] { "Tinyint", "Smallint", "String" };
      uint[] FDLength1 = new uint[] { 0, 0, 0, 0, 0, 0 };
      uint[] FDLength2 = new uint[] { 0, 0, 0 };
      string[] columnIsSignedMatch1 = new string[] { "False", "False", "False", "False", "False", "False" };
      string[] columnIsSignedMatch2 = new string[] { "False", "False", "False" };
      string[] columnIsPaddedMatch1 = new string[] { "False", "True", "True", "False", "True", "True" };
      string[] columnIsPaddedMatch2 = new string[] { "False", "True", "True" };
      string[] clrTypeMatch1 = new string[] { "System.Byte", "System.UInt16", "System.String", "System.UInt32", "System.UInt64", "System.String" };
      string[] clrTypeMatch2 = new string[] { "System.Byte", "System.UInt16", "System.String" };

      RowResult result1 = session.GetSchema(schemaName).GetTable("result1").Select(columns1).Execute();
      RowResult result2 = session.GetSchema(schemaName).GetTable("result2").Select(columns2).Execute();
      for (int i = 0; i < columns1.Length; i++)
      {
        string tableName = result1.Columns[i].TableName;
        Assert.That(tableName, Is.EqualTo("result1"), "Matching the table name");
        string tableLabel = result1.Columns[i].TableLabel;
        Assert.That(tableLabel, Is.EqualTo("result1"), "Matching the table label");
        string columnName = result1.Columns[i].ColumnName;
        Assert.That(columnName, Is.EqualTo(columns1[i].ToString()), "Matching the Column Name");
        string columnLabel = result1.Columns[i].ColumnLabel;
        Assert.That(columnLabel, Is.EqualTo(columns1[i].ToString()), "Matching the Column Label");
        uint columnLength = result1.Columns[i].Length;
        Assert.That(Length1[i], Is.EqualTo(Length1[i]), "Matching the Column Length");
        var columnType = result1.Columns[i].Type;
        Assert.That(columnType.ToString(), Is.EqualTo(columnTypeMatch1[i]), "Matching the Column Type");
        var columnFD = result1.Columns[i].FractionalDigits;
        Assert.That(columnFD, Is.EqualTo(FDLength1[i]), "Matching the Column FD");
        var columnIsSigned = result1.Columns[i].IsNumberSigned;
        Assert.That(columnIsSigned.ToString(), Is.EqualTo(columnIsSignedMatch1[i]), "Matching whether column is signed or not");
        string columnCollation = result1.Columns[i].CollationName;
        if (i == 2 || i == 5)
        {
          Assert.That(columnCollation, Does.Contain(defaultCharset), "Matching the Collation Name for default characters");
        }
        else
        {
          Assert.That(columnCollation, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        }
        string columnCharacterSet = result1.Columns[i].CharacterSetName;
        if (i == 2 || i == 5)
          Assert.That(columnCharacterSet, Is.EqualTo(defaultCharset).IgnoreCase, "Matching the CharacterSet Name for default characters");
        else
          Assert.That(columnCharacterSet, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        var columnIsPadded = result1.Columns[i].IsPadded;
        Assert.That(columnIsPadded.ToString(), Is.EqualTo(columnIsPaddedMatch1[i]), "Matching whether column is padded or not");
        var columnClrType = result1.Columns[i].ClrType;
        Assert.That(columnClrType.ToString(), Is.EqualTo(clrTypeMatch1[i]), "Matching whether column CLR Type");
      }

      for (int i = 0; i < columns2.Length; i++)
      {
        string tableName = result2.Columns[i].TableName;
        Assert.That(tableName, Is.EqualTo("result2"), "Matching the table name");
        string tableLabel = result2.Columns[i].TableLabel;
        Assert.That(tableLabel, Is.EqualTo("result2"), "Matching the table label");
        string columnName = result2.Columns[i].ColumnName;
        Assert.That(columnName, Is.EqualTo(columns2[i].ToString()), "Matching the Column Name");
        string columnLabel = result2.Columns[i].ColumnLabel;
        Assert.That(columnLabel, Is.EqualTo(columns2[i].ToString()), "Matching the Column Label");
        uint columnLength = result2.Columns[i].Length;
        Assert.That(columnLength, Is.EqualTo(Length2[i]), "Matching the Column Length");
        var columnType = result2.Columns[i].Type;
        Assert.That(columnType.ToString(), Is.EqualTo(columnTypeMatch2[i]), "Matching the Column Type");
        var columnFD = result2.Columns[i].FractionalDigits;
        Assert.That(columnFD, Is.EqualTo(FDLength2[i]), "Matching the Column FD");
        var columnIsSigned = result2.Columns[i].IsNumberSigned;
        Assert.That(columnIsSigned.ToString(), Is.EqualTo(columnIsSignedMatch2[i]), "Matching whether column is signed or not");
        string columnCollation = result2.Columns[i].CollationName;
        if (i == 2)
          Assert.That(columnCollation, Does.Contain(defaultCharset), "Matching the Collation Name for default characters");
        else
          Assert.That(columnCollation, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        string columnCharacterSet = result2.Columns[i].CharacterSetName;
        if (i == 2)
          Assert.That(columnCharacterSet, Is.EqualTo(defaultCharset).IgnoreCase, "Matching the CharacterSet Name for default characters");
        else
          Assert.That(columnCharacterSet, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        var columnIsPadded = result2.Columns[i].IsPadded;
        Assert.That(columnIsPadded.ToString(), Is.EqualTo(columnIsPaddedMatch2[i]), "Matching whether column is padded or not");
        var columnClrType = result2.Columns[i].ClrType;
        Assert.That(columnClrType.ToString(), Is.EqualTo(clrTypeMatch2[i]), "Matching whether column CLR Type");
      }

      session.SQL("DROP TABLE if exists test").Execute();
      session.SQL("CREATE TABLE test(b VARCHAR(255) )").Execute();
      session.SQL("INSERT INTO test VALUES('Bob')").Execute();
      result2 = session.GetSchema(schemaName).GetTable("test").Select("1 + 1 as a", "b").Execute();
      var rows = result2.FetchAll();
      Assert.That(result2.Columns.Count, Is.EqualTo(2), "Matching Column Count");

      Assert.That(result2.Columns[0].SchemaName, Is.EqualTo(null), "Matching Column Schema Name");
      Assert.That(result2.Columns[0].TableName, Is.EqualTo(null), "Matching Column Table Name");
      Assert.That(result2.Columns[0].TableLabel, Is.EqualTo(null), "Matching Column Table Label");
      Assert.That(result2.Columns[0].ColumnName, Is.EqualTo(null), "Matching Column Name");
      Assert.That(result2.Columns[0].ColumnLabel, Is.EqualTo("a"), "Matching Column Label");
      Assert.That(result2.Columns[0].Type.ToString(), Is.EqualTo("Tinyint"), "Matching Column Type");
      Assert.That(result2.Columns[0].Length, Is.EqualTo(3u), "Matching Column Length");
      Assert.That(result2.Columns[0].FractionalDigits, Is.EqualTo(0u), "Matching Column FD");
      Assert.That(result2.Columns[0].IsNumberSigned, Is.EqualTo(true), "Matching Column Is Signed");
      Assert.That(result2.Columns[0].CharacterSetName, Is.EqualTo(null), "Matching Character Set Name");
      Assert.That(result2.Columns[0].CollationName, Is.EqualTo(null), "Matching Collation Name");
      Assert.That(result2.Columns[0].IsPadded, Is.EqualTo(false), "Matching Column Padded");

      Assert.That(result2.Columns[1].SchemaName, Is.EqualTo(schemaName), "Matching Column Schema Name");
      Assert.That(result2.Columns[1].TableName, Is.EqualTo("test"), "Matching Column Table Name");
      Assert.That(result2.Columns[1].TableLabel, Is.EqualTo("test"), "Matching Column Table Label");
      Assert.That(result2.Columns[1].ColumnName, Is.EqualTo("b"), "Matching Column Name");
      Assert.That(result2.Columns[1].ColumnLabel, Is.EqualTo("b"), "Matching Column Label");
      Assert.That(result2.Columns[1].Type.ToString(), Is.EqualTo("String"), "Matching Column Type");
      Assert.That(result2.Columns[1].Length, Is.EqualTo(1020u), "Matching Column Length");
      Assert.That(result2.Columns[1].FractionalDigits, Is.EqualTo(0u), "Matching Column FD");
      Assert.That(result2.Columns[1].IsNumberSigned, Is.EqualTo(false), "Matching Column Is Signed");
      Assert.That(result2.Columns[1].CharacterSetName, Is.EqualTo(defaultCharset), "Matching Character Set Name");
      Assert.That(result2.Columns[1].CollationName, Does.Contain(defaultCharset), "Matching Collation Name");
      Assert.That(result2.Columns[1].IsPadded, Is.EqualTo(false), "Matching Column Padded");

      session.SQL("create table test1(c1 int,c2 double GENERATED ALWAYS AS (c1*101/102) Stored COMMENT 'First Gen Col',c3 Json GENERATED ALWAYS AS (concat('{\"F1\":',c1,'}')) VIRTUAL COMMENT 'Second Gen /**/Col', c4 bigint GENERATED ALWAYS as (c1*10000) VIRTUAL UNIQUE KEY Comment '3rd Col' NOT NULL)").Execute();
      session.SQL("insert into test1(c1) values(1000)").Execute();
      result2 = session.GetSchema(schemaName).GetTable("test1").Select("c1").Execute();

      Assert.That(result2.Columns[0].SchemaName, Is.EqualTo(schemaName), "Matching Column Schema Name");
      Assert.That(result2.Columns[0].TableName, Is.EqualTo("test1"), "Matching Column Table Name");
      Assert.That(result2.Columns[0].TableLabel, Is.EqualTo("test1"), "Matching Column Table Label");
      Assert.That(result2.Columns[0].ColumnName, Is.EqualTo("c1"), "Matching Column Name");
      Assert.That(result2.Columns[0].ColumnLabel, Is.EqualTo("c1"), "Matching Column Label");
      Assert.That(result2.Columns[0].Type.ToString(), Is.EqualTo("Int"), "Matching Column Type");
      Assert.That(result2.Columns[0].Length, Is.EqualTo(11), "Matching Column Length");
      Assert.That(result2.Columns[0].FractionalDigits, Is.EqualTo(0u), "Matching Column FD");
      Assert.That(result2.Columns[0].IsNumberSigned, Is.EqualTo(true), "Matching Column Is Signed");
      Assert.That(result2.Columns[0].CharacterSetName, Is.EqualTo(null), "Matching Character Set Name");
      Assert.That(result2.Columns[0].CollationName, Is.EqualTo(null), "Matching Collation Name");
      Assert.That(result2.Columns[0].IsPadded, Is.EqualTo(false), "Matching Column Padded");

      result2 = session.GetSchema(schemaName).GetTable("test1").Select("c2").Execute();

      Assert.That(result2.Columns[0].SchemaName, Is.EqualTo(schemaName), "Matching Column Schema Name");
      Assert.That(result2.Columns[0].TableName, Is.EqualTo("test1"), "Matching Column Table Name");
      Assert.That(result2.Columns[0].TableLabel, Is.EqualTo("test1"), "Matching Column Table Label");
      Assert.That(result2.Columns[0].ColumnName, Is.EqualTo("c2"), "Matching Column Name");
      Assert.That(result2.Columns[0].ColumnLabel, Is.EqualTo("c2"), "Matching Column Label");
      Assert.That(result2.Columns[0].Type.ToString(), Is.EqualTo("Double"), "Matching Column Type");
      Assert.That(result2.Columns[0].Length, Is.EqualTo(22), "Matching Column Length");
      Assert.That(result2.Columns[0].FractionalDigits, Is.EqualTo(31), "Matching Column FD");
      Assert.That(result2.Columns[0].IsNumberSigned, Is.EqualTo(false), "Matching Column Is Signed");
      Assert.That(result2.Columns[0].CharacterSetName, Is.EqualTo(null), "Matching Character Set Name");
      Assert.That(result2.Columns[0].CollationName, Is.EqualTo(null), "Matching Collation Name");
      Assert.That(result2.Columns[0].IsPadded, Is.EqualTo(false), "Matching Column Padded");

      result2 = session.GetSchema(schemaName).GetTable("test1").Select("c3").Execute();

      Assert.That(result2.Columns[0].SchemaName, Is.EqualTo(schemaName), "Matching Column Schema Name");
      Assert.That(result2.Columns[0].TableName, Is.EqualTo("test1"), "Matching Column Table Name");
      Assert.That(result2.Columns[0].TableLabel, Is.EqualTo("test1"), "Matching Column Table Label");
      Assert.That(result2.Columns[0].ColumnName, Is.EqualTo("c3"), "Matching Column Name");
      Assert.That(result2.Columns[0].ColumnLabel, Is.EqualTo("c3"), "Matching Column Label");
      Assert.That(result2.Columns[0].Type.ToString(), Is.EqualTo("Json"), "Matching Column Type");
      Assert.That(result2.Columns[0].Length, Is.EqualTo(4294967295), "Matching Column Length");
      Assert.That(result2.Columns[0].FractionalDigits, Is.EqualTo(0u), "Matching Column FD");
      Assert.That(result2.Columns[0].IsNumberSigned, Is.EqualTo(false), "Matching Column Is Signed");
      Assert.That(result2.Columns[0].CharacterSetName, Is.EqualTo("binary"), "Matching Character Set Name");
      Assert.That(result2.Columns[0].CollationName, Is.EqualTo("binary"), "Matching Collation Name");
      Assert.That(result2.Columns[0].IsPadded, Is.EqualTo(false), "Matching Column Padded");

      result2 = session.GetSchema(schemaName).GetTable("test1").Select("c4").Execute();

      Assert.That(result2.Columns[0].SchemaName, Is.EqualTo(schemaName), "Matching Column Schema Name");
      Assert.That(result2.Columns[0].TableName, Is.EqualTo("test1"), "Matching Column Table Name");
      Assert.That(result2.Columns[0].TableLabel, Is.EqualTo("test1"), "Matching Column Table Label");
      Assert.That(result2.Columns[0].ColumnName, Is.EqualTo("c4"), "Matching Column Name");
      Assert.That(result2.Columns[0].ColumnLabel, Is.EqualTo("c4"), "Matching Column Label");
      Assert.That(result2.Columns[0].Type.ToString(), Is.EqualTo("Bigint"), "Matching Column Type");
      Assert.That(result2.Columns[0].Length, Is.EqualTo(20), "Matching Column Length");
      Assert.That(result2.Columns[0].FractionalDigits, Is.EqualTo(0u), "Matching Column FD");
      Assert.That(result2.Columns[0].IsNumberSigned, Is.EqualTo(true), "Matching Column Is Signed");
      Assert.That(result2.Columns[0].CharacterSetName, Is.EqualTo(null), "Matching Character Set Name");
      Assert.That(result2.Columns[0].CollationName, Is.EqualTo(null), "Matching Collation Name");
      Assert.That(result2.Columns[0].IsPadded, Is.EqualTo(false), "Matching Column Padded");

      session.SQL("DROP TABLE if exists test").Execute();
      session.SQL("DROP TABLE if exists test1").Execute();
      session.SQL("DROP TABLE if exists result1").Execute();
      session.SQL("DROP TABLE if exists result2").Execute();
      session.SQL("DROP TABLE if exists address1").Execute();
      session.SQL("DROP TABLE if exists address2").Execute();
    }

    [Test, Description("Column Character Default Datatype")]
    public void ColumnCharacterDefaultDatatype()
    {
      if (!_serverVersion.isAtLeast(5, 7, 0)) return;
      session.SQL($"USE {schemaName}").Execute();
      session.SQL("Drop table if exists address").Execute();
      session.SQL("CREATE TABLE address" +
                  "(address_number1  TINYINT UNSIGNED NOT NULL AUTO_INCREMENT, " +
                  "building_name1  CHAR(100) NOT NULL, " +
                  "building_name2  VARCHAR(100) NOT NULL, " +
                  "building_name3  TINYTEXT NOT NULL, " +
                  "building_name4  MEDIUMTEXT NOT NULL, " +
                  "building_name5  LONGTEXT NOT NULL, " +
                  "PRIMARY KEY (address_number1)" + ");").Execute();
      session.SQL("INSERT INTO address" +
                  "(address_number1,building_name1,building_name2,building_name3,building_name4,building_name5)" +
                  " VALUES " +
                  "(1,'MYSQLDEVTEAM','ORACLETECHPARK1','SURVEYNUMBER1','TEST','TEST123');").Execute();
      string[] columns = new string[] { "address_number1",
                    "building_name1","building_name2","building_name3","building_name4","building_name5"};
      uint[] Length = new uint[] { 3, 100, 100, 255, 16777215, 4294967295 };
      string[] columnTypeMatch = new string[] { "Tinyint", "String", "String", "String", "String", "String" };
      uint[] FDLength = new uint[] { 0, 0, 0, 0, 0, 0 };
      string[] columnIsSignedMatch = new string[] { "False", "False", "False", "False", "False", "False" };
      string[] columnIsPaddedMatch = new string[] { "False", "True", "False", "False", "False", "False" };
      string[] clrTypeMatch = new string[] { "System.Byte", "System.String", "System.String", "System.String", "System.String", "System.String" };
      RowResult result = session.GetSchema(schemaName).GetTable("address").Select(columns).Execute();

      for (int i = 0; i < columns.Length; i++)
      {
        string tableLabel = result.Columns[i].TableLabel;
        Assert.That(tableLabel, Is.EqualTo("address"), "Matching the table label");
        string columnName = result.Columns[i].ColumnName;
        Assert.That(columnName, Is.EqualTo(columns[i].ToString()), "Matching the Column Name");
        string columnLabel = result.Columns[i].ColumnLabel;
        Assert.That(columnLabel, Is.EqualTo(columns[i].ToString()), "Matching the Column Label");
        uint columnLength = result.Columns[i].Length;
        Assert.That(columnLength, Is.EqualTo(columnLength), "Matching the Column Length");
        var columnType = result.Columns[i].Type;
        Assert.That(columnType.ToString(), Is.EqualTo(columnTypeMatch[i]), "Matching the Column Type");
        var columnFD = result.Columns[i].FractionalDigits;
        Assert.That(columnFD, Is.EqualTo(FDLength[i]), "Matching the Column FD");
        var columnIsSigned = result.Columns[i].IsNumberSigned;
        Assert.That(columnIsSigned.ToString(), Is.EqualTo(columnIsSignedMatch[i]), "Matching whether column is signed or not");
        string columnCollation = result.Columns[i].CollationName;
        if (i == 1 || i == 2 || i == 3 || i == 4 || i == 5)
          Assert.That(columnCollation, Is.EqualTo(columnCollation), "Matching the Collation Name for default characters");
        else
          Assert.That(columnCollation, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        string columnCharacterSet = result.Columns[i].CharacterSetName;
        if (i == 1 || i == 2 || i == 3 || i == 4 || i == 5)
          Assert.That(columnCharacterSet, Is.EqualTo(columnCharacterSet), "Matching the CharacterSet Name for default characters");
        else
          Assert.That(columnCharacterSet, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        var columnIsPadded = result.Columns[i].IsPadded;
        Assert.That(columnIsPadded.ToString(), Is.EqualTo(columnIsPaddedMatch[i]), "Matching whether column is padded or not");
        var columnClrType = result.Columns[i].ClrType;
        Assert.That(columnClrType.ToString(), Is.EqualTo(clrTypeMatch[i]), "Matching whether column CLR Type");
      }
      session.SQL("DROP TABLE if exists address").Execute();
    }

    [Test, Description("Column Character Custom Datatype")]
    public void ColumnCharacterCustomDatatype()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 14), "This test is for MySql 8.0.14 or higher");
      var defaultCharset = "utf8mb4";
      session.SQL($"USE {schemaName}").Execute();
      session.SQL("Drop table if exists address").Execute();
      session.SQL("CREATE TABLE address" +
                    "(address_number1  TINYINT UNSIGNED NOT NULL AUTO_INCREMENT, " +
                    "building_name1  CHAR(100) NOT NULL, " +
                    "building_name2  VARCHAR(100) NOT NULL, " +
                    "building_name3  TINYTEXT NOT NULL, " +
                    "building_name4  MEDIUMTEXT NOT NULL, " +
                    "building_name5  LONGTEXT NOT NULL, " +
                    "PRIMARY KEY (address_number1)" + ")CHARACTER SET big5 COLLATE big5_chinese_ci;").Execute();
      session.SQL("INSERT INTO address" +
                  "(address_number1,building_name1,building_name2,building_name3,building_name4,building_name5)" +
                  " VALUES " +
                  "(1,'MYSQLDEVTEAM','ORACLETECHPARK1','SURVEYNUMBER1','TEST','TEST123');").Execute();
      string[] columns = new string[] { "address_number1",
                    "building_name1","building_name2","building_name3","building_name4","building_name5"};
      uint[] Length = new uint[] { 3, 200, 200, 255, 16777215, 4294967295 };
      string[] columnTypeMatch = new string[] { "Tinyint", "String", "String", "String", "String", "String" };
      uint[] FDLength = new uint[] { 0, 0, 0, 0, 0, 0 };
      string[] columnIsSignedMatch = new string[] { "False", "False", "False", "False", "False", "False" };
      string[] columnIsPaddedMatch = new string[] { "False", "True", "False", "False", "False", "False" };
      string[] clrTypeMatch = new string[] { "System.Byte", "System.String", "System.String", "System.String", "System.String", "System.String" };
      RowResult result = session.GetSchema(schemaName).GetTable("address").Select(columns).Execute();

      for (int i = 0; i < columns.Length; i++)
      {
        string tableLabel = result.Columns[i].TableLabel;
        Assert.That(tableLabel, Is.EqualTo("address"), "Matching the table label");
        string columnName = result.Columns[i].ColumnName;
        Assert.That(columnName, Is.EqualTo(columns[i].ToString()), "Matching the Column Name");
        string columnLabel = result.Columns[i].ColumnLabel;
        Assert.That(columnLabel, Is.EqualTo(columns[i].ToString()), "Matching the Column Label");
        uint columnLength = result.Columns[i].Length;
        Assert.That(columnLength, Is.EqualTo(Length[i]), "Matching the Column Length");
        var columnType = result.Columns[i].Type;
        Assert.That(columnType.ToString(), Is.EqualTo(columnTypeMatch[i]), "Matching the Column Type");
        var columnFD = result.Columns[i].FractionalDigits;
        Assert.That(columnFD, Is.EqualTo(FDLength[i]), "Matching the Column FD");
        var columnIsSigned = result.Columns[i].IsNumberSigned;
        Assert.That(columnIsSigned.ToString(), Is.EqualTo(columnIsSignedMatch[i]), "Matching whether column is signed or not");
        string columnCollation = result.Columns[i].CollationName;
        if (i == 1 || i == 2 || i == 3 || i == 4 || i == 5)
          Assert.That(columnCollation, Does.Contain(defaultCharset), "Matching the Collation Name for big5_chinese_ci characters");
        else
          Assert.That(columnCollation, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        string columnCharacterSet = result.Columns[i].CharacterSetName;
        if (i == 1 || i == 2 || i == 3 || i == 4 || i == 5)
          Assert.That(columnCharacterSet, Is.EqualTo(defaultCharset).IgnoreCase, "Matching the CharacterSet Name for big5 characters");
        else
          Assert.That(columnCharacterSet, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        var columnIsPadded = result.Columns[i].IsPadded;
        Assert.That(columnIsPadded.ToString(), Is.EqualTo(columnIsPaddedMatch[i]), "Matching whether column is padded or not");
        var columnClrType = result.Columns[i].ClrType;
        Assert.That(columnClrType.ToString(), Is.EqualTo(clrTypeMatch[i]), "Matching whether column CLR Type");
      }
      session.SQL("Drop table if exists address").Execute();
    }

    [Test, Description("Column Geometric Datatypes")]
    public void ColumnCharacterGeometricDatatype()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      session.SQL($"USE {schemaName}").Execute();
      session.SQL($"drop table if exists geotest").Execute();
      session.SQL("CREATE TABLE geotest (g GEOMETRY,p POINT,l LINESTRING,po POLYGON,mp MULTIPOINT,ml MULTILINESTRING,mpo MULTIPOLYGON,gc GEOMETRYCOLLECTION);").Execute();
      string[] columns = new string[] { "g", "p", "l", "po", "mp", "ml", "mpo", "gc" };
      RowResult result = session.GetSchema(schemaName).GetTable("geotest").Select(columns).Execute();
      for (int i = 0; i < columns.Length; i++)
      {
        string tableName = result.Columns[i].TableName;
        Assert.That(tableName, Is.EqualTo("geotest"), "Matching the table name");
        string tableLabel = result.Columns[i].TableLabel;
        Assert.That(tableLabel, Is.EqualTo("geotest"), "Matching the table label");
        string columnName = result.Columns[i].ColumnName;
        Assert.That(columnName, Is.EqualTo(columns[i].ToString()), "Matching the Column Name");
        string columnLabel = result.Columns[i].ColumnLabel;
        Assert.That(columnLabel, Is.EqualTo(columns[i].ToString()), "Matching the Column Label");
        uint columnLength = result.Columns[i].Length;
        Assert.That(columnLength, Is.EqualTo(0), "Matching the Column Length");
        var columnType = result.Columns[i].Type;
        Assert.That(columnType.ToString(), Is.EqualTo("Geometry"), "Matching the Column Type");
        var columnFD = result.Columns[i].FractionalDigits;
        Assert.That(columnFD, Is.EqualTo(0), "Matching the Column FD");
        var columnIsSigned = result.Columns[i].IsNumberSigned;
        Assert.That(columnIsSigned, Is.EqualTo(false), "Matching whether column is signed or not");
        string columnCollation = result.Columns[i].CollationName;
        Assert.That(columnCollation, Is.EqualTo(null), "Matching the Collation Name for default characters");
        string columnCharacterSet = result.Columns[i].CharacterSetName;
        Assert.That(columnCharacterSet, Is.EqualTo(null), "Matching the Collation Name as null for data types other than characters");
        var columnIsPadded = result.Columns[i].IsPadded;
        Assert.That(columnIsPadded, Is.EqualTo(false), "Matching whether column is padded or not");
        var columnClrType = result.Columns[i].ClrType;
        Assert.That(columnClrType.ToString(), Is.EqualTo("System.Byte[]"), "Matching whether column CLR Type");
      }
    }

    [Test, Description("Column Blob Datatype")]
    public void ColumnCharacterBlobDatatype()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      session.SQL($"USE {schemaName}").Execute();
      session.SQL($"drop table if exists geotest").Execute();
      session.SQL("CREATE TABLE geotest (g TINYBLOB,p BLOB,l MEDIUMBLOB,po LONGBLOB);").Execute();

      string[] columns = new string[] { "g", "p", "l", "po" };
      string[] columnTypeMatch = new string[] { "Bytes", "Bytes", "Bytes", "Bytes" };
      uint[] ColumnLength = new uint[] { 255, 65535, 16777215, 4294967295 };
      RowResult result = session.GetSchema(schemaName).GetTable("geotest").Select(columns).Execute();
      for (int i = 0; i < columns.Length; i++)
      {
        string tableName = result.Columns[i].TableName;
        Assert.That(tableName, Is.EqualTo("geotest"), "Matching the table name");
        string tableLabel = result.Columns[i].TableLabel;
        Assert.That(tableLabel, Is.EqualTo("geotest"), "Matching the table label");
        string columnName = result.Columns[i].ColumnName;
        Assert.That(columnName, Is.EqualTo(columns[i].ToString()), "Matching the Column Name");
        string columnLabel = result.Columns[i].ColumnLabel;
        Assert.That(columnLabel, Is.EqualTo(columns[i].ToString()), "Matching the Column Label");
        uint columnLength = result.Columns[i].Length;
        Assert.That(columnLength, Is.EqualTo(ColumnLength[i]), "Matching the Column Length");
        var columnType = result.Columns[i].Type;
        Assert.That(columnType.ToString(), Is.EqualTo(columnTypeMatch[i]), "Matching the Column Type");
        var columnFD = result.Columns[i].FractionalDigits;
        Assert.That(columnFD, Is.EqualTo(0), "Matching the Column FD");
        var columnIsSigned = result.Columns[i].IsNumberSigned;
        Assert.That(columnIsSigned.ToString(), Is.EqualTo("False"), "Matching whether column is signed or not");
        string columnCollation = result.Columns[i].CollationName;
        Assert.That(columnCollation, Is.EqualTo("binary"), "Matching the Collation Name for default characters");
        string columnCharacterSet = result.Columns[i].CharacterSetName;
        //Character name returns binary for blob
        Assert.That(columnCharacterSet, Is.EqualTo("binary"), "Matching the Collation Name as null for data types other than characters");
        var columnIsPadded = result.Columns[i].IsPadded;
        Assert.That(columnIsPadded.ToString(), Is.EqualTo("False"), "Matching whether column is padded or not");
        var columnClrType = result.Columns[i].ClrType;
        Assert.That(columnClrType.ToString(), Is.EqualTo("System.Byte[]"), "Matching whether column CLR Type");
      }
    }

    [Test, Description("Verify that different language specific collations are availabe for charset utf8mb4 when server version is 8.0 or greater")]
    public void LanguageSpecificCollations()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");
      var charset = "utf8mb4";
      string[] collationname =
      {
          "utf8mb4_0900_ai_ci", "utf8mb4_de_pb_0900_ai_ci", "utf8mb4_is_0900_ai_ci", "utf8mb4_lv_0900_ai_ci",
          "utf8mb4_ro_0900_ai_ci", "utf8mb4_sl_0900_ai_ci", "utf8mb4_pl_0900_ai_ci", "utf8mb4_et_0900_ai_ci",
          "utf8mb4_es_0900_ai_ci", "utf8mb4_sv_0900_ai_ci", "utf8mb4_tr_0900_ai_ci", "utf8mb4_cs_0900_ai_ci",
          "utf8mb4_da_0900_ai_ci", "utf8mb4_lt_0900_ai_ci", "utf8mb4_sk_0900_ai_ci", "utf8mb4_es_trad_0900_ai_ci",
          "utf8mb4_la_0900_ai_ci", "utf8mb4_eo_0900_ai_ci", "utf8mb4_hu_0900_ai_ci", "utf8mb4_hr_0900_ai_ci",
          "utf8mb4_vi_0900_ai_ci"
      };
      var database_name = "collation_test";
      session.DropSchema(database_name);
      var CommandText1 = "SHOW VARIABLES LIKE 'collation_%';";
      Assert.That(session.Settings.CharacterSet, Is.EqualTo(charset), "Matching the character set of the session");

      for (var i = 0; i < collationname.Length; i++)
      {
        CommandText1 = "CREATE DATABASE " + database_name + " CHARACTER SET " + charset + " COLLATE " +
                       collationname[i];
        var sqlRes = session.SQL(CommandText1).Execute();
        session.SQL("USE " + database_name).Execute();
        session.SQL("create table x(id int,name char(25));").Execute();
        var res = session.SQL("insert into x values(10,'AXTREF');").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
        session.SQL("insert into x values(20,'Trädgårdsvägen');").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
        session.SQL("insert into x values(30,'foo𝌆bar');").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
        session.SQL("insert into x values(40,'Dolphin:🐬');").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));

        var dbCharset = session.SQL("select @@character_set_database;").Execute().FirstOrDefault();
        var dbCollation = session.SQL("select @@collation_database").Execute().FirstOrDefault();
        Assert.That(charset, Is.EqualTo(dbCharset[0]));
        Assert.That(collationname[i], Is.EqualTo(dbCollation[0]));
        session.DropSchema(database_name);
      }
    }

    /// <summary>
    /// Bug#34156197 - Update utf8 mappings
    /// </summary>
    [Test]
    public void VerifyRenamedCollations()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      var charset = "utf8mb3";
      var collation = session.Version.isAtLeast(8, 0, 30) ? "utf8mb3" : "utf8";
      string[] collationname =
      {
        $"{collation}_general_ci", $"{collation}_tolower_ci", $"{collation}_bin", $"{collation}_unicode_ci",
        $"{collation}_icelandic_ci", $"{collation}_latvian_ci", $"{collation}_romanian_ci", $"{collation}_slovenian_ci", $"{collation}_polish_ci",
        $"{collation}_estonian_ci", $"{collation}_spanish_ci", $"{collation}_swedish_ci", $"{collation}_turkish_ci", $"{collation}_czech_ci",
        $"{collation}_danish_ci", $"{collation}_lithuanian_ci", $"{collation}_slovak_ci", $"{collation}_spanish2_ci", $"{collation}_roman_ci",
        $"{collation}_persian_ci", $"{collation}_esperanto_ci", $"{collation}_hungarian_ci", $"{collation}_sinhala_ci", $"{collation}_german2_ci",
        $"{collation}_croatian_ci", $"{collation}_unicode_520_ci", $"{collation}_vietnamese_ci", $"{collation}_general_mysql500_ci"
      };

      using var sessionX = MySQLX.GetSession(session.Settings.ConnectionString + $";charset={charset}");

      var database_name = "collation_test";
      session.DropSchema(database_name);
      Assert.That(sessionX.Settings.CharacterSet, Is.EqualTo(charset), "Matching the character set of the session");

      for (var i = 0; i < collationname.Length; i++)
      {
        string cmdText = $"CREATE DATABASE {database_name} CHARACTER SET {charset} COLLATE {collationname[i]}";
        var sqlRes = session.SQL(cmdText).Execute();
        sessionX.SQL("USE " + database_name).Execute();
        sessionX.SQL("CREATE TABLE x(id int,name char(25));").Execute();
        var res = sessionX.SQL("insert into x values(10,'AXTREF');").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
        sessionX.SQL("insert into x values(20,'Trädgårdsvägen');").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
        sessionX.SQL("insert into x values(30,'foo𝌆bar');").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
        sessionX.SQL("insert into x values(40,'Dolphin:🐬');").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));

        var dbCharset = sessionX.SQL("select @@character_set_database;").Execute().FirstOrDefault();
        var dbCollation = sessionX.SQL("select @@collation_database").Execute().FirstOrDefault();
        Assert.That(charset, Is.EqualTo(dbCharset[0]));
        Assert.That(collationname[i], Is.EqualTo(dbCollation[0]));
        sessionX.DropSchema(database_name);
      }
    }

    /// <summary>
    /// Bug #33116709	WRONG CHARSET RETURNED USING XPLUGIN
    /// </summary>
    [Test, Description("Verify default charset and collation")]
    public void VerifyLatinCharsetAndCollation()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 14), "This test is for MySql 8.0.14 or higher");
      var database_name = "collation_test";
      var charset = "latin1";
      var collationname = "latin1_danish_ci";
      var defaultCharset = "utf8mb4";
      Assert.That(session.Settings.CharacterSet, Is.EqualTo(defaultCharset), "Matching the character set of the session");
      session.DropSchema(database_name);
      var CommandText1 = $"CREATE DATABASE {database_name} CHARACTER SET {charset} COLLATE {collationname}";
      session.SQL(CommandText1).Execute();
      session.SQL("USE " + database_name).Execute();
      session.SQL("create table x(id int,name char(25));").Execute();
      session.SQL("insert into x values(10,'AXTREF');").Execute();
      RowResult result1 = session.GetSchema(database_name).GetTable("x").Select("id").Execute();
      Assert.That(result1.Columns[0].CharacterSetName, Is.EqualTo(null), "id-charset");
      Assert.That(result1.Columns[0].CollationName, Is.EqualTo(null), "id-collation");
      result1 = session.GetSchema(database_name).GetTable("x").Select("name").Execute();
      Assert.That(result1.Columns[0].CharacterSetName, Is.EqualTo(defaultCharset).IgnoreCase, "name-charset");
      Assert.That(result1.Columns[0].CollationName, Does.Contain(defaultCharset), "name-collation");
      session.DropSchema(database_name);
    }

    [Test, Description("Create table/db with collation utf8mb4_0900_bin and insert non ascii characters and fetch data")]
    public void Utf8mb4BinaryNopadCollationTable()
    {
      Assume.That(Platform.IsWindows(), "This test is for Windows OS only.");
      Assume.That(session.Version.isAtLeast(8, 0, 17), "This test is for MySql 8.0.17 or higher");
      char t_char;
      var charset = "utf8mb4";
      var collation = "utf8mb4_0900_ai_ci";
      string foo = "\x00000281\x00000282\x00000283\x00000284\x00000285\x00000286";
      var conn = new MySqlConnectionStringBuilder();
      var collationname = new List<string>();

      var CommandText1 = "SHOW COLLATION WHERE Charset = 'utf8mb4'";
      var collation_res = session.SQL(CommandText1).Execute().FetchAll();
      for (var i = 0; i < collation_res.Count; i++)
        collationname.Add(collation_res[i][0].ToString());

      var database_name = "collation_test";

      Assert.That(session.Settings.CharacterSet, Is.EqualTo(charset), "Matching the character set of the session");
      session.DropSchema(database_name);
      CommandText1 = "CREATE DATABASE " + database_name;
      var sqlRes = session.SQL(CommandText1).Execute();
      session.SQL("USE " + database_name).Execute();
      session.SQL("CREATE TABLE t(a TEXT)").Execute();
      Table t = session.GetSchema(database_name).GetTable("t");
      t.Insert().Values(foo).Execute();
      Row r = t.Select().Limit(1).Execute().FetchOne();
      Assert.That(r[0].ToString(), Is.EqualTo(foo), "Compare extracted string");

      session.DropSchema(database_name);
      CommandText1 = "SHOW VARIABLES LIKE 'collation_%';";
      sqlRes = session.SQL(CommandText1).Execute();
      while (sqlRes.Next()) ;
      Assert.That(sqlRes.Rows.ToArray()[0][1].ToString(), Is.EqualTo(collation), "Matching the collation");

      for (var i = 0; i < collationname.Count; i++)
      {
        CommandText1 = $"CREATE DATABASE {database_name} CHARACTER SET {charset} COLLATE {collationname[i]}";
        sqlRes = session.SQL(CommandText1).Execute();
        session.SQL("USE " + database_name).Execute();
        session.SQL("create table x(id int,name char(25));").Execute();
        session.SQL("insert into x values(10,'AXTREF');").Execute();
        session.SQL("insert into x values(20,'Trädgårdsvägen');").Execute();
        session.SQL("insert into x values(30,'foo𝌆bar');").Execute();
        session.SQL("insert into x values(40,'Dolphin:🐬');").Execute();
        t = session.GetSchema(database_name).GetTable("x");
        var res = t.Select().Execute().FetchAll();
        Assert.That(res[0][1].ToString(), Is.EqualTo("AXTREF"), "Matching the data");
        Assert.That(res[1][1].ToString(), Is.EqualTo("Trädgårdsvägen"), "Matching the data");
        Assert.That(res[2][1].ToString(), Is.EqualTo("foo𝌆bar"), "Matching the data");
        Assert.That(res[3][1].ToString(), Is.EqualTo("Dolphin:🐬"), "Matching the data");
        RowResult result_collation = t.Select("name").Execute();
        var collationName = result_collation.Columns[0].CollationName;
        Assert.That(collationName, Is.EqualTo(collationname[i]), "Matching the collation");
        var characterName = result_collation.Columns[0].CharacterSetName;
        Assert.That(characterName, Is.EqualTo(charset), "Matching the charset");
        session.DropSchema(database_name);
      }

      for (var i = 0; i < collationname.Count; i++)
      {
        CommandText1 = "CREATE DATABASE " + database_name;
        sqlRes = session.SQL(CommandText1).Execute();
        session.SQL("USE " + database_name).Execute();
        session.SQL($"create table x(id int,name char(25)) CHARACTER SET {charset} COLLATE {collationname[i]};").Execute();
        session.SQL("insert into x values(10,'AXTREF');").Execute();
        session.SQL("insert into x values(20,'Trädgårdsvägen');").Execute();
        session.SQL("insert into x values(30,'foo𝌆bar');").Execute();
        session.SQL("insert into x values(40,'Dolphin:🐬');").Execute();
        t = session.GetSchema(database_name).GetTable("x");
        var res = t.Select().Execute().FetchAll();
        Assert.That(res[0][1].ToString(), Is.EqualTo("AXTREF"), "Matching the data");
        Assert.That(res[1][1].ToString(), Is.EqualTo("Trädgårdsvägen"), "Matching the data");
        Assert.That(res[2][1].ToString(), Is.EqualTo("foo𝌆bar"), "Matching the data");
        Assert.That(res[3][1].ToString(), Is.EqualTo("Dolphin:🐬"), "Matching the data");
        RowResult result_collation = t.Select("name").Execute();
        var collationName = result_collation.Columns[0].CollationName;
        Assert.That(collationName, Is.EqualTo(collationname[i]), "Matching the collation");
        var characterName = result_collation.Columns[0].CharacterSetName;
        Assert.That(characterName, Is.EqualTo(charset), "Matching the charset");
        session.DropSchema(database_name);
        //ALTER
        CommandText1 = "CREATE DATABASE " + database_name;
        sqlRes = session.SQL(CommandText1).Execute();
        session.SQL("USE " + database_name).Execute();
        session.SQL("create table x(id int,name char(25))").Execute();
        session.SQL($"ALTER TABLE x MODIFY name CHAR(25) COLLATE {collationname[i]};").Execute();
        session.SQL("insert into x values(10,'AXTREF');").Execute();
        session.SQL("insert into x values(20,'Trädgårdsvägen');").Execute();
        session.SQL("insert into x values(30,'foo𝌆bar');").Execute();
        session.SQL("insert into x values(40,'Dolphin:🐬');").Execute();
        t = session.GetSchema(database_name).GetTable("x");
        res = t.Select().Execute().FetchAll();
        Assert.That(res[0][1].ToString(), Is.EqualTo("AXTREF"), "Matching the data");
        Assert.That(res[1][1].ToString(), Is.EqualTo("Trädgårdsvägen"), "Matching the data");
        Assert.That(res[2][1].ToString(), Is.EqualTo("foo𝌆bar"), "Matching the data");
        Assert.That(res[3][1].ToString(), Is.EqualTo("Dolphin:🐬"), "Matching the data");
        result_collation = t.Select("name").Execute();
        collationName = result_collation.Columns[0].CollationName;
        Assert.That(collationName, Is.EqualTo(collationname[i]), "Matching the collation");
        characterName = result_collation.Columns[0].CharacterSetName;
        Assert.That(characterName, Is.EqualTo(charset), "Matching the charset");
        session.DropSchema(database_name);
      }

      for (int k = 0; k <= 255; k++)
      {
        CommandText1 = $"CREATE DATABASE {database_name} CHARACTER SET {charset} COLLATE {collationname[0]}";
        sqlRes = session.SQL(CommandText1).Execute();
        session.SQL("USE " + database_name).Execute();
        session.SQL("create table x(id int,name char(25));").Execute();

        t_char = (char)k;

        if (k != 39 && k != 92 && k != 32)
        {
          session.SQL($"insert into x values({k},'{t_char}')").Execute();
          t = session.GetSchema(database_name).GetTable("x");
          r = t.Select().Limit(1).Execute().FetchOne();
          Assert.That(r[1].ToString(), Is.EqualTo(t_char.ToString()), "Compare extracted string");
        }
        session.DropSchema(database_name);

        CommandText1 = $"CREATE DATABASE {database_name} CHARACTER SET {charset} COLLATE {collationname[21]}";
        sqlRes = session.SQL(CommandText1).Execute();
        session.SQL("USE " + database_name).Execute();
        session.SQL("create table x(id int,name char(25));").Execute();
        t_char = (char)k;
        if (k != 39 && k != 92 && k != 32)
        {
          session.SQL($"insert into x values({k},'{t_char}')").Execute();
          t = session.GetSchema(database_name).GetTable("x");
          r = t.Select().Limit(1).Execute().FetchOne();
          Assert.That(r[1].ToString(), Is.EqualTo(t_char.ToString()), "Compare extracted string");
        }
        session.DropSchema(database_name);
      }
      session.DropSchema(database_name);

      MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder();
      mysqlx0.Server = Host;
      mysqlx0.Database = schemaName;
      mysqlx0.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysqlx0.Port = Convert.ToUInt32(XPort);
      mysqlx0.UserID = session.Settings.UserID;
      mysqlx0.Password = session.Settings.Password;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 1000;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";

      using (var sessiontest = MySQLX.GetSession(mysqlx0.ConnectionString))
      {
        Assert.That(sessiontest.Settings.CharacterSet, Is.EqualTo(charset), "Matching the character set of the session");
        sessiontest.DropSchema(database_name);
        CommandText1 = "CREATE DATABASE " + database_name;
        sqlRes = sessiontest.SQL(CommandText1).Execute();
        sessiontest.SQL("USE " + database_name).Execute();
        sessiontest.SQL("CREATE TABLE t(a TEXT)").Execute();
        t = sessiontest.GetSchema(database_name).GetTable("t");
        t.Insert().Values(foo).Execute();
        r = t.Select().Limit(1).Execute().FetchOne();
        Assert.That(r[0].ToString(), Is.EqualTo(foo), "Compare extracted string");
        sessiontest.DropSchema(database_name);
      }
    }

    #endregion WL14389
  }
}
