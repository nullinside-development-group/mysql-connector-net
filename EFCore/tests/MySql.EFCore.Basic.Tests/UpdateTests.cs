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
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using NUnit.Framework;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySql.EntityFrameworkCore.Basic.Tests
{

#region Bug113443

    [Table("Bug113443Table")]
    public record Bug113443Record
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }
        [Column("name")]
        public string? Name { get; set;}

        [Column("time_created", TypeName = "TIMESTAMP(6)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime TimeCreated { get; init; }

        [Column("time_updated", TypeName = "TIMESTAMP(6)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime TimeUpdated { get; init; }
    }

    public class Bug113443Context : MyTestContext
    {
        public DbSet<Bug113443Record> Bug113443 { get; set; }
    }

#endregion

  public class UpdateTests
  {
    [Test]
    public void Bug113443() {
        using var ctx = new Bug113443Context();
        ctx.Database.EnsureCreated();
        try
        {
            Bug113443Record data = new() { Name = "Sample1"};
            ctx.Bug113443.Add(data);
            ctx.SaveChanges();
            data.Name = "Changed!";
            ctx.SaveChanges();
        }
        finally
        {
            ctx.Database.EnsureDeleted();
        }
    }
  }
}
