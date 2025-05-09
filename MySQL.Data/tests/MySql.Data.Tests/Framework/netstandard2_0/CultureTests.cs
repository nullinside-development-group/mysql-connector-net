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
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Threading;
using System.Globalization;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class CultureTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [Test]
    public void TestFloats()
    {
      InternalTestFloats(false);
    }

    [Test]
    public void TestFloatsPrepared()
    {
      InternalTestFloats(true);
    }


    private void InternalTestFloats(bool prepared)
    {
      CultureInfo curCulture = Thread.CurrentThread.CurrentCulture;
      CultureInfo curUICulture = Thread.CurrentThread.CurrentUICulture;
      CultureInfo c = new CultureInfo("de-DE");
      Thread.CurrentThread.CurrentCulture = c;
      Thread.CurrentThread.CurrentUICulture = c;

      ExecuteSQL("CREATE TABLE Test (fl FLOAT, db DOUBLE, dec1 DECIMAL(5,2))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?fl, ?db, ?dec)", Connection);
      cmd.Parameters.Add("?fl", MySqlDbType.Float);
      cmd.Parameters.Add("?db", MySqlDbType.Double);
      cmd.Parameters.Add("?dec", MySqlDbType.Decimal);
      cmd.Parameters[0].Value = 2.3;
      cmd.Parameters[1].Value = 4.6;
      cmd.Parameters[2].Value = 23.82;
      if (prepared)
        cmd.Prepare();
      int count = cmd.ExecuteNonQuery();
      Assert.That(count, Is.EqualTo(1));

      try
      {
        cmd.CommandText = "SELECT * FROM Test";
        if (prepared) cmd.Prepare();
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          Assert.That((decimal)reader.GetFloat(0), Is.EqualTo((decimal)2.3));
          Assert.That(reader.GetDouble(1), Is.EqualTo(4.6));
          Assert.That(reader.GetDecimal(2), Is.EqualTo((decimal)23.82));
        }
      }
      finally
      {
        Thread.CurrentThread.CurrentCulture = curCulture;
        Thread.CurrentThread.CurrentUICulture = curUICulture;
      }
    }

    /// <summary>
    /// Bug #8228  	turkish character set causing the error
    /// </summary>
    [Test]
    public void Turkish()
    {
      CultureInfo curCulture = Thread.CurrentThread.CurrentCulture;
      CultureInfo curUICulture = Thread.CurrentThread.CurrentUICulture;
      CultureInfo c = new CultureInfo("tr-TR");
      Thread.CurrentThread.CurrentCulture = c;
      Thread.CurrentThread.CurrentUICulture = c;

      using (MySqlConnection newConn = new MySqlConnection(Root.ConnectionString))
      {
        newConn.Open();
      }

      Thread.CurrentThread.CurrentCulture = curCulture;
      Thread.CurrentThread.CurrentUICulture = curUICulture;
    }

    /// <summary>
    /// Bug #29931  	Connector/NET does not handle Saudi Hijri calendar correctly
    /// </summary>
    [Test]
    public void ArabicCalendars()
    {
      ExecuteSQL("CREATE TABLE test(dt DATETIME)");
      ExecuteSQL("INSERT INTO test VALUES ('2007-01-01 12:30:45')");

      CultureInfo curCulture = Thread.CurrentThread.CurrentCulture;
      CultureInfo curUICulture = Thread.CurrentThread.CurrentUICulture;
      CultureInfo c = new CultureInfo("ar-SA");
      Thread.CurrentThread.CurrentCulture = c;
      Thread.CurrentThread.CurrentUICulture = c;

      MySqlCommand cmd = new MySqlCommand("SELECT dt FROM test", Connection);
      DateTime dt = (DateTime)cmd.ExecuteScalar();
      Assert.That(dt.Year, Is.EqualTo(2007));
      Assert.That(dt.Month, Is.EqualTo(1));
      Assert.That(dt.Day, Is.EqualTo(1));
      Assert.That(dt.Hour, Is.EqualTo(12));
      Assert.That(dt.Minute, Is.EqualTo(30));
      Assert.That(dt.Second, Is.EqualTo(45));

      Thread.CurrentThread.CurrentCulture = curCulture;
      Thread.CurrentThread.CurrentUICulture = curUICulture;
    }

    /// <summary>
    /// Bug #52187	FunctionsReturnString=true messes up decimal separator
    /// </summary>
    [Test]
    public void FunctionsReturnStringAndDecimal()
    {
      ExecuteSQL("CREATE TABLE bug52187a (a decimal(5,2) not null)");
      ExecuteSQL("CREATE TABLE bug52187b (b decimal(5,2) not null)");
      ExecuteSQL("insert into bug52187a values (1.25)");
      ExecuteSQL("insert into bug52187b values (5.99)");

      CultureInfo curCulture = Thread.CurrentThread.CurrentCulture;
      CultureInfo curUICulture = Thread.CurrentThread.CurrentUICulture;
      CultureInfo c = new CultureInfo("pt-PT");
      Thread.CurrentThread.CurrentCulture = c;
      Thread.CurrentThread.CurrentUICulture = c;

      string connStr = Connection.ConnectionString + ";functions return string=true";
      try
      {
        using (MySqlConnection con = new MySqlConnection(connStr))
        {
          con.Open();
          MySqlDataAdapter da = new MySqlDataAdapter(
            "select *,(select b from bug52187b) as field_b from bug52187a", con);
          DataTable dt = new DataTable();
          da.Fill(dt);
          Assert.That(dt.Rows.Count, Is.EqualTo(1));
          Assert.That((decimal)dt.Rows[0][0], Is.EqualTo((decimal)1.25));
          Assert.That((decimal)dt.Rows[0][1], Is.EqualTo((decimal)5.99));
        }
      }
      finally
      {
        Thread.CurrentThread.CurrentCulture = curCulture;
        Thread.CurrentThread.CurrentUICulture = curUICulture;
      }

    }
  }
}
