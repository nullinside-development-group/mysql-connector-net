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
  public class VehicleDbContext : DbContext
  {
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Manufacturer> Manufacturers { get; set; }
    public DbSet<Distributor> Distributors { get; set; }

    public VehicleDbContext() : base(CodeFirstFixture.GetEFConnectionString<VehicleDbContext>())
    {
      Database.SetInitializer<VehicleDbContext>(new VehicleDBInitializer());
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      //modelBuilder.Entity<Vehicle>()
      //    .Map<Car>(o => o.ToTable("Cars"))
      //    .Map<Bike>(o => o.ToTable("Bikes"));
      modelBuilder.Entity<Car>().ToTable("Cars");
      modelBuilder.Entity<Bike>().ToTable("Bikes");
    }
  }

  public class VehicleDBInitializer : DropCreateDatabaseReallyAlways<VehicleDbContext>
  {
  }

  [DbConfigurationType(typeof(MySqlEFConfiguration))]
  public class VehicleDbContext2 : DbContext
  {
    public DbSet<Vehicle2> Vehicles { get; set; }

    public VehicleDbContext2() : base(CodeFirstFixture.GetEFConnectionString<VehicleDbContext2>())
    {
      Database.SetInitializer<VehicleDbContext2>(new VehicleDBInitializer2());
    }
  }

  public class VehicleDBInitializer2 : DropCreateDatabaseReallyAlways<VehicleDbContext2>
  {
  }

  /// <summary>
  /// This initializer really drops the database, not just once per AppDomain (like the DropCreateDatabaseAlways).
  /// </summary>
  /// <typeparam name="TContext"></typeparam>
  public class DropCreateDatabaseReallyAlways<TContext> : IDatabaseInitializer<TContext> where TContext : DbContext
  {
    public void InitializeDatabase(TContext context)
    {
      context.Database.Delete();
      context.Database.CreateIfNotExists();
      this.Seed(context);
      context.SaveChanges();
    }

    protected virtual void Seed(TContext context)
    {
    }
  }

  public class Vehicle
  {
    public int Id { get; set; }
    public int Year { get; set; }

    [MaxLength(1024)]
    public string Name { get; set; }
  }

  [DbConfigurationType(typeof(MySqlEFConfiguration))]
  public class VehicleDbContext3 : DbContext
  {
    public DbSet<Accessory> Accessories { get; set; }

    public VehicleDbContext3() : base(CodeFirstFixture.GetEFConnectionString<VehicleDbContext3>())
    {

  }
  }

  public class Car4 : Vehicle4
  {
    public string CarProperty { get; set; }
  }

  public class Bike4 : Vehicle4
  {
    public string BikeProperty { get; set; }
  }
  public class Manufacturer4
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid ManufacturerId { get; set; }
    public string Name { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid GroupIdentifier { get; set; }
    public virtual ICollection<Vehicle4> Vehicles { get; set; }
  }
  public class Vehicle4
  {
    public int Id { get; set; }
    public int Year { get; set; }

    [MaxLength(1024)]
    public string Name { get; set; }
    public Guid ManufacturerId { get; set; }
    [ForeignKey(nameof(ManufacturerId))]
    public virtual Manufacturer4 Manufacturer { get; set; }
  }
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
  public class VehicleDbContext4 : DbContext
  {
    public DbSet<Vehicle4> Vehicles { get; set; }
    public DbSet<Manufacturer4> Manufacturers { get; set; }

    public VehicleDbContext4() : base(CodeFirstFixture.GetEFConnectionString<VehicleDbContext4>())
    {
      Database.SetInitializer<VehicleDbContext4>(new VehicleDBInitializer4());

    }
    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Vehicle4>().ToTable("Vehicles");
      modelBuilder.Entity<Car4>().ToTable("Cars");
      modelBuilder.Entity<Bike4>().ToTable("Bikes");
    }
  }
  public class VehicleDBInitializer4 : DropCreateDatabaseReallyAlways<VehicleDbContext4>
  {
  }

  public class Accessory
  {
    [Key]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    [MaxLength(80000)]
    public string Description { get; set; }

    [Required]
    [MaxLength(16777216)]
    public string LongDescription { get; set; }

  }

  public class Car : Vehicle
  {
    public string CarProperty { get; set; }
  }

  public class Bike : Vehicle
  {
    public string BikeProperty { get; set; }
  }
  public class Vehicle2
  {
    public int Id { get; set; }
    public int Year { get; set; }
    [MaxLength(1024)]
    public string Name { get; set; }
  }

  public class Car2 : Vehicle2
  {
    public string CarProperty { get; set; }
  }

  public class Bike2 : Vehicle2
  {
    public string BikeProperty { get; set; }
  }

  public class Manufacturer
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid ManufacturerId { get; set; }
    public string Name { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid GroupIdentifier { get; set; }
  }

  public class Distributor
  {
    public int DistributorId { get; set; }
    public string Name { get; set; }
  }


  public class Product
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime DateCreated { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "timestamp")]
    public DateTime Timestamp { get; set; }

    public DateTime DateTimeWithPrecision { get; set; }

    [Column(TypeName = "TimeStamp")]
    public DateTime TimeStampWithPrecision { get; set; }

  }

  [DbConfigurationType(typeof(MySqlEFConfiguration))]
  public class ProductsDbContext : DbContext
  {
    public DbSet<Product> Products { get; set; }

    public ProductsDbContext() : base(CodeFirstFixture.GetEFConnectionString<ProductsDbContext>())
    {

    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      Database.SetInitializer<ProductsDbContext>(new ProductDBInitializer());

      modelBuilder.Entity<Product>()
    .Property(f => f.DateTimeWithPrecision)
    .HasColumnType("DateTime")
    .HasPrecision(3);

      modelBuilder.Entity<Product>()
    .Property(f => f.TimeStampWithPrecision)
    .HasColumnType("Timestamp")
    .HasPrecision(3);

      Database.SetInitializer<ProductsDbContext>(new MigrateDatabaseToLatestVersion<ProductsDbContext, Configuration<ProductsDbContext>>());
    }
  }

  public class ProductDBInitializer : DropCreateDatabaseReallyAlways<ProductsDbContext>
  {
  }

  public class Names
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime DateCreated { get; set; }
  }

  [DbConfigurationType(typeof(MySqlEFConfiguration))]
  public class ShortDbContext : DbContext
  {
    public DbSet<Names> Names { get; set; }

    public ShortDbContext() : base(CodeFirstFixture.GetEFConnectionString<ShortDbContext>())
    {

    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Names>()
    .Property(f => f.DateCreated)
    .HasColumnType("DateTime")
    .HasPrecision(9);
    }
  }

  [DbConfigurationType(typeof(MySqlEFConfiguration))]
  public class AutoIncrementBugContext : DbContext
  {
    public DbSet<AutoIncrementBug> AutoIncrementBug { get; set; }

    public AutoIncrementBugContext() : base(CodeFirstFixture.GetEFConnectionString<AutoIncrementBugContext>())
    {
      Database.SetInitializer<AutoIncrementBugContext>(new AutoIncrementBugInitialize<AutoIncrementBugContext>());
      Database.SetInitializer<AutoIncrementBugContext>(new DropCreateDatabaseAlways<AutoIncrementBugContext>());
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
    }
  }

  public class AutoIncrementBugInitialize<TContext> : IDatabaseInitializer<TContext> where TContext : DbContext
  {
    public void InitializeDatabase(TContext context)
    {
      context.Database.Delete();
      context.Database.CreateIfNotExists();
      this.Seed(context);
      context.SaveChanges();
    }

    protected virtual void Seed(TContext context)
    {
    }
  }

  public class AutoIncrementBug
  {
    [Key]
    public short MyKey { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AutoIncrementBugId { get; set; }
    public string Description { get; set; }
  }

  public class AutoIncrementConfiguration<TContext> : System.Data.Entity.Migrations.DbMigrationsConfiguration<TContext> where TContext : DbContext
  {
    public AutoIncrementConfiguration()
    {
      AutomaticMigrationsEnabled = true;
      //CodeGenerator = new MySqlMigrationCodeGenerator();
      SetSqlGenerator("MySql.Data.MySqlClient", new MySql.Data.EntityFramework.MySqlMigrationSqlGenerator());
    }
  }


  [Table("client")]
  public class Client
  {
    [Key]
    public int Id { get; set; }
    public ICollection<Order> Orders { get; set; }
  }

  [Table("order")]
  public class Order
  {
    [Key]
    public int Id { get; set; }
    public ICollection<Item> Items { get; set; }
    public ICollection<Discount> Discounts { get; set; }
  }

  [Table("item")]
  public class Item
  {
    [Key]
    public int Id { get; set; }
  }

  [Table("discount")]
  public class Discount
  {
    [Key]
    public int Id { get; set; }
  }


  [DbConfigurationType(typeof(MySqlEFConfiguration))]
  public class UsingUnionContext : DbContext
  {
    public DbSet<Client> Clients { get; set; }
    public DbSet<Item> Items { get; set; }

    public DbSet<Order> Orders { get; set; }

    public DbSet<Discount> Discounts { get; set; }

    public UsingUnionContext() : base(CodeFirstFixture.GetEFConnectionString<UsingUnionContext>())
    {

  }
  }



}
