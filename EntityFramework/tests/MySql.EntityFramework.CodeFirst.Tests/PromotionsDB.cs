// Copyright © 2014, 2025, Oracle and/or its affiliates.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySql.Data.EntityFramework.CodeFirst.Tests
{
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
  public class PromotionsDB : DbContext
  {
    private bool disposed = false;

    public virtual DbSet<HomePromo> HomePromoes { get; set; }

    public PromotionsDB() : base(CodeFirstFixture.GetEFConnectionString<PromotionsDB>())
    {
      Database.SetInitializer<PromotionsDB>(new PromotionsDBInitializer());
    }

    protected override void Dispose(bool disposing)
    {
      if (disposed)
        return;

      if (disposing)
      {
        Database.Delete();
  }

      base.Dispose(disposing);
      disposed = true;
    }
  }

  public class PromotionsDBInitializer : DropCreateDatabaseReallyAlways<PromotionsDB>
  {
  }

  public class HomePromo
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int ID { get; set; }

    public string Image { get; set; }

    public string Url { get; set; }

    public int DisplayOrder { get; set; }

    [Column("Active")]
    public bool Active { get; set; }
    [Column("ActiveFrom")]
    public DateTime? ActiveFrom { get; set; }
    [Column("ActiveTo")]
    public DateTime? ActiveTo { get; set; }
  }
}
