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
using MySql.EntityFrameworkCore.Basic.Tests.DbContextClasses;
using MySql.EntityFrameworkCore.Extensions;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Linq;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class DbFunctionsMySQLTest
  {
    DateTime lastUpdate = new DateTime(2006, 02, 15, 04, 34, 33); // same date for all data (taken from ActorData)

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      using (var context = new SakilaLiteContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.PopulateData();
      }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      using (var context = new SakilaLiteContext())
      {
        context.Database.EnsureDeleted();
      }
    }

    [Test]
    public void DateDiffYear()
    {
      using (var context = new SakilaLiteContext())
      {
        var yearsDiff = DateTime.Now.Year - lastUpdate.Year;
        var count = context.Actor.Count(a => EF.Functions.DateDiffYear(a.LastUpdate, DateTime.Now) > 0);

        Assert.That(count, Is.EqualTo(200));
      }
    }

    [Test]
    public void DateDiffMonth()
    {
      using (var context = new SakilaLiteContext())
      {
        var monthsDiff = ((DateTime.Now.Year - lastUpdate.Year) * 12) + DateTime.Now.Month - lastUpdate.Month;
        var count = context.Actor
            .Count(a => EF.Functions.DateDiffMonth(a.LastUpdate, DateTime.Now) >= monthsDiff - 1);

        Assert.That(count, Is.EqualTo(200));
      }
    }

    [Test]
    public void DateDiffDay()
    {
      using (var context = new SakilaLiteContext())
      {
        var daysDiff = (int)(DateTime.Now - lastUpdate).TotalDays;
        var count = context.Actor
            .Count(a => EF.Functions.DateDiffDay(a.LastUpdate, DateTime.Now) == daysDiff);

        Assert.That(count, Is.EqualTo(200), "TotalDays: " + daysDiff);
      }
    }

    [Test]
    public void DateDiffHour()
    {
      using (var context = new SakilaLiteContext())
      {
        var hoursDiff = (int)(DateTime.Now - lastUpdate).TotalHours;
        var count = context.Actor
            .Count(a => EF.Functions.DateDiffHour(a.LastUpdate, DateTime.Now) == hoursDiff);

        Assert.That(count, Is.EqualTo(200), "TotalHours: " + hoursDiff);
      }
    }

    [Test]
    public void DateDiffMinute()
    {
      using (var context = new SakilaLiteContext())
      {
        var minutesDiff = (int)(DateTime.Now - lastUpdate).TotalMinutes;
        var count = context.Actor
            .Count(a => EF.Functions.DateDiffMinute(a.LastUpdate, DateTime.Now) == minutesDiff);

        Assert.That(count, Is.EqualTo(200), "TotalMinutes: " + minutesDiff);
      }
    }

    [Test]
    public void DateDiffSecond()
    {
      using (var context = new SakilaLiteContext())
      {
        var secondsDiff = (int)(DateTime.Now - lastUpdate).TotalSeconds;
        var count = context.Actor
            .Count(a => EF.Functions.DateDiffSecond(a.LastUpdate, DateTime.Now) >= secondsDiff);

        Assert.That(count, Is.EqualTo(200), "TotalSeconds: " + secondsDiff);
      }
    }

    [Test]
    public void DateDiffMicrosecond()
    {
      using (var context = new SakilaLiteContext())
      {
        var count = context.Actor
            .Count(a => EF.Functions.DateDiffMicrosecond(a.LastUpdate, DateTime.Now) == 0);

        Assert.That(count, Is.EqualTo(0));
      }
    }

    [Test]
    public void LikeIntLiteral()
    {
      using (var context = new SakilaLiteContext())
      {
        var count = context.Actor.Count(o => EF.Functions.Like(o.ActorId, "%A%"));

        Assert.That(count, Is.EqualTo(0));
      }
    }

    [Test]
    public void LikeDateTimeLiteral()
    {
      using (var context = new SakilaLiteContext())
      {
        var count = context.Actor.Count(o => EF.Functions.Like(o.LastUpdate, "%A%"));

        Assert.That(count, Is.EqualTo(0));
      }
    }

    [Test]
    public void LikeIntLiteralWithEscape()
    {
      using (var context = new SakilaLiteContext())
      {
        var count = context.Actor.Count(o => EF.Functions.Like(o.ActorId, "!%", "!"));

        Assert.That(count, Is.EqualTo(0));
      }
    }
  }
}
