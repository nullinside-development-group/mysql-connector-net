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

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MySql.Data.MySqlClient.Tests
{
  public class StressTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [Test]
    [Ignore("Fix this")]
    public void TestMultiPacket()
    {
      int len = 20000000;

      ExecuteSQL(@"CREATE TABLE Test (id INT NOT NULL, name varchar(100), blob1 LONGBLOB, text1 TEXT, 
                  PRIMARY KEY(id))");
      ExecuteSQL("SET GLOBAL max_allowed_packet=64000000", true);

      // currently do not test this with compression
      if (Connection.UseCompression) return;

      using (MySqlConnection c = GetConnection())
      {
        c.Open();
        byte[] dataIn = MySql.Data.MySqlClient.Tests.Utils.CreateBlob(len);
        byte[] dataIn2 = MySql.Data.MySqlClient.Tests.Utils.CreateBlob(len);

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, NULL, ?blob, NULL )", c);
        cmd.CommandTimeout = 0;
        cmd.Parameters.Add(new MySqlParameter("?id", 1));
        cmd.Parameters.Add(new MySqlParameter("?blob", dataIn));
        cmd.ExecuteNonQuery();

        cmd.Parameters[0].Value = 2;
        cmd.Parameters[1].Value = dataIn2;
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";

        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          byte[] dataOut = new byte[len];
          long count = reader.GetBytes(2, 0, dataOut, 0, len);
          Assert.That(count, Is.EqualTo(len));
          int i = 0;
          try
          {
            for (; i < len; i++)
              Assert.That(dataOut[i], Is.EqualTo(dataIn[i]));
          }
          catch (Exception)
          {
            int z = i;
          }

          reader.Read();
          count = reader.GetBytes(2, 0, dataOut, 0, len);
          Assert.That(count, Is.EqualTo(len));

          for (int x = 0; x < len; x++)
            Assert.That(dataOut[x], Is.EqualTo(dataIn2[x]));
        }
      }
    }

    [Test]
    public void TestSequence()
    {
      if (Version > new Version(5, 6, 6))
        ExecuteSQL("SET GLOBAL innodb_lru_scan_depth=256");

      ExecuteSQL(@"CREATE TABLE Test (id INT NOT NULL, name varchar(100), blob1 LONGBLOB, text1 TEXT, 
                  PRIMARY KEY(id))");
      MySqlCommand cmd = new MySqlCommand("insert into Test (id, name) values (?id, 'test')", Connection);
      cmd.Parameters.Add(new MySqlParameter("?id", 1));

      for (int i = 1; i <= 8000; i++)
      {
        cmd.Parameters[0].Value = i;
        cmd.ExecuteNonQuery();
      }

      int i2 = 0;
      cmd = new MySqlCommand("select * from Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read())
        {
          Assert.That(i2 + 1 == reader.GetInt32(0), "Sequence out of order");
          i2++;
        }
        reader.Close();

        Assert.That(i2, Is.EqualTo(8000));
        cmd = new MySqlCommand("delete from Test where id >= 100", Connection);
        cmd.ExecuteNonQuery();
      }
    }

    #region WL14389
    [Test, Description("Command Async Stress ")]
    public async Task CommandAsyncStress()
    {
      for (var i = 0; i < 1000; i++)
      {
        using (var dbConn = new MySqlConnection($"server={Host};user={Settings.UserID};database={Settings.Database};port={Port};password={Settings.Password};sslmode=none"))
        using (var cmd = new MySqlCommand("DROP DATABASE IF EXISTS code_first_2", dbConn))
        {
          await dbConn.OpenAsync();
          await cmd.ExecuteNonQueryAsync();
          await dbConn.ChangeDatabaseAsync(Settings.Database);
          await dbConn.CloseAsync();
        }
      }
    }
    #endregion WL14389

  }
}
