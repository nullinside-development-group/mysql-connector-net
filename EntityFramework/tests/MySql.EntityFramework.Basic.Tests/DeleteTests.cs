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

using System.Linq;
using NUnit.Framework;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace MySql.Data.EntityFramework.Tests
{

  public class DeleteTests : DefaultFixture
  {
    public override void LoadData()
    {
      using (DefaultContext ctx = new DefaultContext(ConnectionString))
      {
        ctx.Products.Add(new Product() { Name = "Garbage Truck", MinAge = 8 });
        ctx.Products.Add(new Product() { Name = "Fire Truck", MinAge = 12 });
        ctx.Products.Add(new Product() { Name = "Hula Hoop", MinAge = 18 });
        ctx.SaveChanges();
      }
    }

    [Test]
    public void SimpleDeleteAllRows()
    {
      using (DefaultContext ctx = new DefaultContext(ConnectionString))
      {
        Assert.That(ctx.Products.Count() > 0);

        foreach (Product p in ctx.Products)
          ctx.Products.Remove(p);
        ctx.SaveChanges();

        Assert.That(ctx.Products.Count(), Is.EqualTo(0));
      }
      // set the flag that will cause the setup to happen again
      // since we just blew away a table
      NeedSetup = true;
    }

    [Test]
    public void SimpleDeleteRowByParameter()
    {
      using (DefaultContext ctx = new DefaultContext(ConnectionString))
      {
        int total = ctx.Products.Count();
        int cntLeft = ctx.Products.Where(b => b.MinAge >= 18).Count();
        // make sure the test is valid
        Assert.That(total > cntLeft);

        foreach (Product p in ctx.Products.Where(b => b.MinAge < 18).ToList())
          ctx.Products.Remove(p);
        ctx.SaveChanges();
        Assert.That(ctx.Products.Count(), Is.EqualTo(cntLeft));
      }
      // set the flag that will cause the setup to happen again
      // since we just blew away a table
      NeedSetup = true;
    }


    public class Widget
    {
      public int Id { get; set;  }
      public WidgetDetail Detail { get; set; }
    }

    public class WidgetDetail
    {
      public int Id { get; set; }
      public Widget Widget { get; set; }
    }

    public class WidgetContext : DbContext
    {
      public WidgetContext(string connStr) : base(connStr)
      {
        Database.SetInitializer<WidgetContext>(null);
      }

      protected override void OnModelCreating(DbModelBuilder modelBuilder)
      {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<WidgetDetail>()
          .HasRequired(b => b.Widget)
          .WithOptional(a => a.Detail)
          .WillCascadeOnDelete(true);
      }
    }

    /// <summary>
    /// Fix for bug Cascading delete using CreateDatabase in Entity Framework
    /// (http://bugs.mysql.com/bug.php?id=64779) using ModelFirst.
    /// </summary>
    [Test]
    public void XOnDeleteCascade()
    {
      using (WidgetContext ctx = new WidgetContext(ConnectionString))
      {
        var context = ((IObjectContextAdapter)ctx).ObjectContext;
        var sql = context.CreateDatabaseScript();
        CheckSqlContains(sql,
          @"ALTER TABLE `WidgetDetails` ADD CONSTRAINT WidgetDetail_Widget
	          FOREIGN KEY (Id)	REFERENCES `Widgets` (Id) ON DELETE Cascade ON UPDATE NO ACTION;");
      }
    }
  }
}
