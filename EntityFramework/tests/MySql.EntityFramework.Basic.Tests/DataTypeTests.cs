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
using System.Linq;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using System.Data;
using System.Globalization;
using System.Threading;

namespace MySql.Data.EntityFramework.Tests
{
  public class DataTypeTests : DefaultFixture
  {
    /// <summary>
    /// Bug #45457 DbType Time is not supported in entity framework
    /// </summary>
    [Test]
    public void TimeType()
    {
      using (DefaultContext ctx = GetDefaultContext())
      {
        TimeSpan birth = new TimeSpan(11, 3, 2);

        Child c = new Child();
        c.ChildId = "ABC";
        c.Name = "first";
        c.BirthTime = birth;
        c.Label = Guid.NewGuid();
        ctx.Children.Add(c);
        ctx.SaveChanges();

        Child d = ctx.Children.Where(x => x.ChildId == "ABC").Single();
        Assert.That(d.BirthTime, Is.EqualTo(birth));
      }
    }

    /// <summary>
    /// Bug #44455	insert and update error with entity framework
    /// </summary>
    [Test]
    public void DoubleValuesNonEnglish()
    {
      CultureInfo curCulture = Thread.CurrentThread.CurrentCulture;
      CultureInfo curUICulture = Thread.CurrentThread.CurrentUICulture;
      CultureInfo newCulture = new CultureInfo("da-DK");
      Thread.CurrentThread.CurrentCulture = newCulture;
      Thread.CurrentThread.CurrentUICulture = newCulture;

      try
      {
        using (DefaultContext ctx = GetDefaultContext())
        {
          Product p = new Product();
          p.Name = "New Product";
          p.Weight = 8.65f;
          p.CreatedDate = DateTime.Now;
          ctx.Products.Add(p);
          ctx.SaveChanges();
        }
      }
      finally
      {
        Thread.CurrentThread.CurrentCulture = curCulture;
        Thread.CurrentThread.CurrentUICulture = curUICulture;
      }
    }

    /// <summary>
    /// Bug #46311	TimeStamp table column Entity Framework issue.
    /// </summary>
    [Ignore("Fix Me")]
    public void TimestampColumn()
    {
      DateTime now = DateTime.Now;

      using (DefaultContext ctx = GetDefaultContext())
      {
        Product p = new Product() { Name = "My Product", MinAge = 7, Weight = 8.0f };
        ctx.Products.Add(p);
        ctx.SaveChanges();

        p = ctx.Products.First();
        p.CreatedDate = now;
        ctx.SaveChanges();

        p = ctx.Products.First();
        Assert.That(p.CreatedDate, Is.EqualTo(now));
      }
    }

    /// <summary>
    /// Bug #48417	Invalid cast from 'System.String' to 'System.Guid'
    /// </summary>
    [Test]
    public void GuidType()
    {
      using (DefaultContext ctx = GetDefaultContext())
      {
        TimeSpan birth = new TimeSpan(11, 3, 2);
        Guid g = Guid.NewGuid();
        
        Child c = new Child();
        c.ChildId = "GUID";
        c.Name = "first";
        c.BirthTime = birth;
        c.Label = g;
        ctx.Children.Add(c);
        ctx.SaveChanges();

        Child d = ctx.Children.Where(x => x.ChildId == "GUID").Single();
        Assert.That(d.Label, Is.EqualTo(g));

      }
    }

    /// <summary>
    /// Bug #62246	Connector/NET Incorrectly Maps Decimal To AnsiString
    /// </summary>
    [Test]
    public void CanSetDbTypeDecimalFromNewDecimalParameter()
    {
      MySqlParameter newDecimalParameter = new MySqlParameter
      {
        ParameterName = "TestNewDecimal",
        Size = 10,
        Scale = 2,
        MySqlDbType = MySqlDbType.NewDecimal,
        Value = 1111111.12,
        IsNullable = true
      };

      Assert.That(newDecimalParameter.DbType, Is.EqualTo(DbType.Decimal));
    }
  }
}
