// Copyright © 2019, 2025, Oracle and/or its affiliates.
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
using MySqlX.Common;
using MySqlX.XDevAPI;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// Compression/decompression based unit tests.
  /// </summary>
  public class CompressionTests : BaseTest
  {
    private const string DEFLATE_STREAM = "DEFLATE_STREAM";
    public Client client = null;

    [TearDown]
    public void TearDown()
    {
      ExecuteSQL("drop database if exists compression");
      ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""ZSTD_STREAM,LZ4_MESSAGE,DEFLATE_STREAM"" ");
    }

    [Test]
    public void ConnectionOptionIsValidUsingBuilder()
    {
      var builder = new MySqlXConnectionStringBuilder(ConnectionString);
      builder.Compression = CompressionType.Preferred;
      Assert.That(builder.ToString(), Does.Contain("compression=Preferred"));

      builder.Compression = CompressionType.Required;
      Assert.That(builder.ToString(), Does.Contain("compression=Required"));

      builder.Compression = CompressionType.Disabled;
      Assert.That(builder.ToString(), Does.Contain("compression=Disabled"));
    }

    [Test]
    public void ConnectionOptionIsValidUsingConnectionUri()
    {
      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=PreFerRed"))
      {
        Assert.That(session.Settings.Compression, Is.EqualTo(CompressionType.Preferred));
        session.Close();
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=required"))
      {
        Assert.That(session.Settings.Compression, Is.EqualTo(CompressionType.Required));
        session.Close();
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=DISABLED"))
      {
        Assert.That(session.Settings.Compression, Is.EqualTo(CompressionType.Disabled));
        session.Close();
      }

      // Test whitespace
      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression= DISABLED"))
      {
        Assert.That(session.Settings.Compression, Is.EqualTo(CompressionType.Disabled));
        session.Close();
      }

      using (var session = MySQLX.GetSession($"{ConnectionStringUri}?compression= DISABLED  "))
      {
        Assert.That(session.Settings.Compression, Is.EqualTo(CompressionType.Disabled));
        session.Close();
      }
    }

    [Test]
    public void ConnectionOptionIsValidUsingAnonymousObject()
    {
      var connectionData = new
      {
        server = Host,
        user = "test",
        password = "test",
        port = UInt32.Parse(XPort),
        compression = CompressionType.Required
      };

      using (var session = MySQLX.GetSession(connectionData))
      {
        Assert.That(session.Settings.Compression, Is.EqualTo(CompressionType.Required));
        session.Close();
      }
    }

    [Test]
    public void ConnectionOptionIsValidUsingConnectionString()
    {
      var builder = new MySqlXConnectionStringBuilder($"server={Host};port={XPort};compression=PreFerRed");
      Assert.That(builder.Compression, Is.EqualTo(CompressionType.Preferred));

      builder = new MySqlXConnectionStringBuilder($"server={Host};port={XPort};compression=required");
      Assert.That(builder.Compression, Is.EqualTo(CompressionType.Required));

      builder = new MySqlXConnectionStringBuilder($"server={Host};port={XPort};compression=DISABLED");
      Assert.That(builder.Compression, Is.EqualTo(CompressionType.Disabled));

      // Test whitespace
      builder = new MySqlXConnectionStringBuilder($"server={Host};port={XPort};compression=  required");
      Assert.That(builder.Compression, Is.EqualTo(CompressionType.Required));

      builder = new MySqlXConnectionStringBuilder($"server={Host};port={XPort};compression=    required");
      Assert.That(builder.Compression, Is.EqualTo(CompressionType.Required));

      builder = new MySqlXConnectionStringBuilder($"server={Host};port={XPort};compression=  required  ");
      Assert.That(builder.Compression, Is.EqualTo(CompressionType.Required));
    }

    [Test]
    public void PreferredIsTheDefaultValue()
    {
      var builder = new MySqlXConnectionStringBuilder();
      Assert.That(builder.Compression, Is.EqualTo(CompressionType.Preferred));

      // Empty value is ignored.
      var updatedConnectionStringUri = ConnectionStringUri + "?compression=";
      using (var session = MySQLX.GetSession(updatedConnectionStringUri))
      {
        Assert.That(session.Settings.Compression, Is.EqualTo(CompressionType.Preferred));
        session.Close();
      }

      // Whitespace is ignored.
      updatedConnectionStringUri = ConnectionStringUri + "?compression= ";
      using (var session = MySQLX.GetSession(updatedConnectionStringUri))
      {
        Assert.That(session.Settings.Compression, Is.EqualTo(CompressionType.Preferred));
        session.Close();
      }
    }

    [Test]
    public void SettingAnInvalidCompressionTypeRaisesException()
    {
      string[] invalidValues = { "test", "true", "123" };
      foreach (var invalidValue in invalidValues)
      {
        var exception = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder($"server={Host};port={XPort};compression={invalidValue}"));
        Assert.That(exception.Message, Is.EqualTo($"The connection property 'compression' acceptable values are: 'preferred', 'required' or 'disabled'. The value '{invalidValue}' is not acceptable"));

        exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"server={Host};port={XPort};user=root;compression={invalidValue}"));
        Assert.That(exception.Message, Is.EqualTo($"The connection property 'compression' acceptable values are: 'preferred', 'required' or 'disabled'. The value '{invalidValue}' is not acceptable"));
      }
    }

    [Test]
    public void SessionRetainsTheSpecifiedCompressionType()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.That(session.Settings.Compression, Is.EqualTo(CompressionType.Preferred));
        session.Close();
      }

      var updatedConnectionStringUri = ConnectionStringUri + "?compression=Disabled";
      using (var session = MySQLX.GetSession(updatedConnectionStringUri))
      {
        Assert.That(session.Settings.Compression, Is.EqualTo(CompressionType.Disabled));
        session.Close();
      }
    }

    [Test]
    public void ValidateRequiredCompressionType()
    {
      // Compression supported starting server 8.0.19.
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 19))
      {
        var exception = Assert.Throws<NotSupportedException>(() => MySQLX.GetSession($"{ConnectionStringUri}?compression=Required"));
        Assert.That(exception.Message, Is.EqualTo("Compression requested but the server does not support it."));
      }
      else
      {
        using var session = MySQLX.GetSession($"{ConnectionStringUri}?compression=Required");
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
      }
    }

    [Test]
    public void NegotiationSucceedsWithExpectedCompressionAlgorithm()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 19), "This test is for MySql 8.0.19 or higher");

      // Validate zstd_stream is the default.
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.zstd_stream.ToString()));
        compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.zstd_stream.ToString()));
      }

      using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=lz4_message"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
        compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

#if !NETFRAMEWORK 
      using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=deflate_stream"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.deflate_stream.ToString()));
        compressionAlgorithm = session.XSession.GetCompressionAlgorithm(false);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.deflate_stream.ToString()));
      }
#endif
    }

    [Test]
    public void NegotiationWithSpecificCompressionAlgorithm()
    {
      var updatedConnectionStringUri = ConnectionStringUri + "?compression=Required";

      if (Platform.IsWindows())
      {
        // Test with one of the supported compression algorithms.
        ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{CompressionAlgorithms.zstd_stream.ToString().ToUpperInvariant()}\"");
        using (var session = MySQLX.GetSession(updatedConnectionStringUri))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.zstd_stream.ToString()));
        }
      }

      ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{CompressionAlgorithms.lz4_message.ToString().ToUpperInvariant()}\"");
      using (var session = MySQLX.GetSession(updatedConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms ={DEFLATE_STREAM}");
#if NETFRAMEWORK
      var exception = Assert.Throws<NotSupportedException>(() => MySQLX.GetSession(updatedConnectionStringUri));
      Assert.That(exception.Message, Is.EqualTo("Compression requested but the compression algorithm negotiation failed."));
#else
      using (var session = MySQLX.GetSession(updatedConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.deflate_stream.ToString()));
      }
#endif

      // Test with a sublist of supported compression algorithms.
      ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{CompressionAlgorithms.lz4_message.ToString().ToUpperInvariant()},{CompressionAlgorithms.zstd_stream.ToString().ToUpperInvariant()}\"");
      using (var session = MySQLX.GetSession(updatedConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(Enum.TryParse<CompressionAlgorithms>(compressionAlgorithm, out var algorithm));
        Assert.That(algorithm == CompressionAlgorithms.lz4_message || algorithm == CompressionAlgorithms.zstd_stream);
      }
    }

    [Test]
    public void ValidateZstdAllocation()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        if (!(CompressionAlgorithms.zstd_stream.ToString() == compressionAlgorithm))
        {
          return;
        }
      }

      // Ensure resources are being released on each session.
      // If a memory allocation error is raised then a resource has not been released.
      for (int i = 0; i < 4000; i++)
      {
        var session = MySQLX.GetSession(ConnectionStringUri);
        session.Close();
      }
    }

    [Test]
    // WL-14001 XProtocol -- support for configurable compression algorithms
    public void ConfigurableCompressionAlgorithms()
    {
      // FR1_1 Create session with option compression-algorithms for URI, connectionstring, anonymous object, MySqlXConnectionStringBuilder.
      using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=lz4_message;"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms=lz4_message;"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

#if NETFRAMEWORK
      // No exception expected due to compression=preferred, no compression expected
      using (var session = MySQLX.GetSession(new { server = Host, port = XPort, uid = "test", password = "test", compressionalgorithms = "deflate_stream" }))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.Null);
      }
#else
      using (var session = MySQLX.GetSession(new { server = Host, port = XPort, uid = "test", password = "test", compressionalgorithms = "deflate_stream" }))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.deflate_stream.ToString()).IgnoreCase);
      }
#endif

      var sb = new MySqlXConnectionStringBuilder($"server={Host};port={XPort};uid=test;password=test;compression-algorithms=lz4_message");
      using (var session = MySQLX.GetSession(sb.GetConnectionString(true)))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      // FR1_2 Create session with option compression-algorithms and set the option with no value either by not including the property in the connection string 
      // or by setting it with an empty value.
      using (var session = MySQLX.GetSession($"server={Host};port={XPort};uid=test;password=test;"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(Enum.TryParse<CompressionAlgorithms>(compressionAlgorithm, out var result));
      }

      using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms="))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(Enum.TryParse<CompressionAlgorithms>(compressionAlgorithm, out var result));
      }

      // FR2_1,FR2_2 Create session with option compression-algorithms and set the value with multiple compression algorithms for 
      // URI,connectionstring,anonymous object,MySqlXConnectionStringBuilder.check that the negotiation happens in the order provided in the connection string
      using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=lz4_message,zstd_stream,deflate_stream;"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms=lz4_message,zstd_stream,deflate_stream;"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

#if NETFRAMEWORK
      // No exception expected due to compression=preferred, lz4_message compression expected
      using (var session = MySQLX.GetSession(new { server = Host, port = XPort, uid = "test", password = "test", compressionalgorithms = "deflate_stream,lz4_message,zstd_stream" }))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }
#else
      using (var session = MySQLX.GetSession(new { server = Host, port = XPort, uid = "test", password = "test", compressionalgorithms = "deflate_stream,lz4_message,zstd_stream" }))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.deflate_stream.ToString()).IgnoreCase);
      }
#endif

      sb = new MySqlXConnectionStringBuilder(ConnectionString + ";compression-algorithms=lz4_message,zstd_stream,deflate_stream");
      using (var session = MySQLX.GetSession(sb.GetConnectionString(true)))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      // FR3 Create session with option compression-algorithms and set the option with Algorithm aliases lz4, zstd, and deflate.
      using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms=lz4,zstd,deflate;"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=lz4,deflate_stream"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      // FR4_1 Create session with option compression-algorithms.Set the option with unsupported and supported algorithms by client.
      using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=NotSupported,lz4,SomethingElse;"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=lz4,NotSupported"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      sb = new MySqlXConnectionStringBuilder($"server={Host};port={XPort};uid=test;password=test;compression-algorithms=[NotValid,INVALID,NOTSUPPORTED,lz4]");
      using (var session = MySQLX.GetSession(sb.GetConnectionString(true)))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      // FR4_2 Create session and set invalid values to the compression-algorithm option to check if the connection is uncompressed when 
      // compression option is either not set or set to preferred or disabled.
      using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms=NotSupported,SomethingElse;"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.Null);
      }

      using (var session = MySQLX.GetSession(ConnectionString + ";compression=disabled;compression-algorithms=lz4,NotSupported,SomethingElse;"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.Null);
      }

      using (var session = MySQLX.GetSession(ConnectionString + ";compression=preferred;compression-algorithms=[NotSupported,SomethingElse];"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.Null);
      }

      // FR4_3 Create session and set invalid values to the compression-algorithm option.
      // The connection should terminate with an error when compression option is set to required.

      Exception ex = Assert.Throws<System.NotSupportedException>(() => MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=NotSupported,SomethingElse;"));
      Assert.That(ex.Message, Is.EqualTo("Compression requested but the compression algorithm negotiation failed"));

      // FR4_4 Start server with specific compression algorithm and create session with option 
      // compression-algorithms.Set the option with multiple compression algorithms.
      ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{CompressionAlgorithms.lz4_message.ToString().ToUpperInvariant()}\"");
      using (var session = MySQLX.GetSession(ConnectionString + ";compression=preferred;compression-algorithms=[lz4_message,deflate,zstd];"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      // FR4_5 Start the server with a specific compression algorithm and use some other in the client and when compression option is either 
      // not set or set to preferred or disabled.Verify that the connection is uncompressed.
      ExecuteSqlAsRoot($"SET GLOBAL mysqlx_compression_algorithms = \"{CompressionAlgorithms.zstd_stream.ToString().ToUpperInvariant()}\"");
      using (var session = MySQLX.GetSession(ConnectionString + ";compression-algorithms=[lz4_message]"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.Null);
      }

      using (var session = MySQLX.GetSession(ConnectionString + ";compression=preferred;compression-algorithms=[lz4_message]"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.Null);
      }

      using (var session = MySQLX.GetSession(ConnectionString + ";compression=disabled;compression-algorithms=[lz4_message]"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.Null);
      }

      //FR4_6,FR_5 Start the server with a specific compression algorithm and use some other in the client and when compression option is set to required.Verify the behaviour
      ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""LZ4_MESSAGE"" ");
      using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
        var ele = new List<object>();
        for (int i = 0; i < 1000; i++)
        {
          ele.Add(new { id = $"{i}", title = $"Book {i}" });
        }
        //Verify compression is being done
        Collection coll = CreateCollection("testcompress1");
        var result = ExecuteAddStatement(coll.Add(ele.ToArray()));
        var result1 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
        var result2 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(int.Parse(result1.ToString()), Is.GreaterThan(int.Parse(result2.ToString())));
        var result3 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
        var result4 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(int.Parse(result3.ToString()), Is.GreaterThan(int.Parse(result4.ToString())));
      }

      using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=[lz4_message]"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.lz4_message.ToString()));
      }

      // Server algorithm not contain user defined algorithm, with compression preferred
      using (var session = MySQLX.GetSession(ConnectionStringUri + "?compression-algorithms=[zstd];"))
      {
        var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
        Assert.That(compressionAlgorithm, Is.Null);

        var ele = new List<object>();
        for (int i = 0; i < 1000; i++)
        {
          ele.Add(new { id = $"{i}", title = $"Book {i}" });
        }
        //Verify there is no compression 
        Collection coll = CreateCollection("testcompress2");
        var result = ExecuteAddStatement(coll.Add(ele.ToArray()));
        var result1 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
        var result2 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result2, Is.EqualTo(result1));
        var result3 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
        var result4 = session.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result4, Is.EqualTo(result3));
      }

      Exception ex_args = Assert.Throws<System.ArgumentException>(() => MySQLX.GetSession(ConnectionString + ";compression=required;compression_algorithms=[lz4_message]"));
      Assert.That(ex_args.Message, Does.Contain("Option not supported"));
    }

    [Test]
    public void CompressionAlgorithms_Bugs()
    {
      bool success = true;
      try
      {
        // Bug #31544072
#if NETFRAMEWORK
        // Different algorithms available in server hence default compression expected
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=[];"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.That(compressionAlgorithm, Is.Not.Null);
        }
        // With only deflate available,Exeption expected 
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""DEFLATE_STREAM"" ");
        Exception ex_bug1 = Assert.Throws<System.NotSupportedException>(() => MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=[];"));
        Assert.That(ex_bug1.Message, Does.Contain("Compression requested but the compression algorithm negotiation failed"));
#else
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=[];"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.That(compressionAlgorithm, Is.Not.Null);
        }
        // With only deflate available,compression is expected 
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""DEFLATE_STREAM"" ");
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=[];"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.deflate_stream.ToString()));
        }
#endif

        // Bug #31541819
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""DEFLATE_STREAM"" ");
#if NETFRAMEWORK
        // Exeption expected due to compression=required
        Exception ex_bug2 = Assert.Throws<System.NotSupportedException>(() => MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=deflate_stream;"));
        Assert.That(ex_bug2.Message, Does.Contain("is not supported in .NET Framework"));
#else
        using (var session = MySQLX.GetSession(ConnectionString + ";compression=required;compression-algorithms=deflate_stream;"))
        {
          var compressionAlgorithm = session.XSession.GetCompressionAlgorithm(true);
          Assert.That(compressionAlgorithm, Is.EqualTo(CompressionAlgorithms.deflate_stream.ToString()));
        }
#endif
      }
      catch (Exception ex)
      {
        TestContext.WriteLine("Exception: " + ex.Message);
      }
      finally
      {
        // This line ensures that the list of supported compression algorithms is set to its default value.
        ExecuteSqlAsRoot(@"SET GLOBAL mysqlx_compression_algorithms = ""ZSTD_STREAM,LZ4_MESSAGE,DEFLATE_STREAM"" ");
        Assert.That(success);
      }
    }

    #region WL14389

    public static Session session1 = null;
    public static Session session2 = null;
    public static Session session3 = null;
    public static Session session4 = null;
    public CompressionType[] compressValue = { CompressionType.Required, CompressionType.Preferred, CompressionType.Disabled };
    public MySqlSslMode[] modes = { MySqlSslMode.Required, MySqlSslMode.VerifyCA, MySqlSslMode.VerifyFull, MySqlSslMode.Preferred };
    public static object connObject = new { server = Host, port = XPort, user = "test", password = "test" };

    [Test, Description("Connection Compression tests to verify the values of compress option with connection string, uri, anonymous object, string builder")]
    public void ConnectionStringCombinations()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 19), "This test is for MySql 8.0.19 or higher");

      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      sb.SslCa = sslCa;
      sb.SslCert = sslCert;
      sb.SslKey = sslKey;
      for (int j = 0; j < 3; j++)
      {
        for (int i = 0; i < 3; i++)
        {
          //ConnectionString
          session1 = MySQLX.GetSession(ConnectionStringUserWithSSLPEM + " ;Auth=AUTO;sslmode=" + modes[j] + ";compression=" + compressValue[i]);
          Assert.That(session1.XSession.SessionState, Is.EqualTo(SessionState.Open));
          session1.Close();

          //Uri
          session2 = MySQLX.GetSession(connSSLURI + "&sslmode=" + modes[j] + "&compression=" + compressValue[i]);
          Assert.That(session2.XSession.SessionState, Is.EqualTo(SessionState.Open));
          session2.Close();

          //Anonymous Object
          session3 = MySQLX.GetSession(new { server = sb.Server, user = sb.UserID, port = sb.Port, password = sb.Password, SslCa = sslCa, SslCert = sslCert, SslKey = sslKey, Auth = MySqlAuthenticationMode.AUTO, sslmode = modes[j], compression = compressValue[i] });
          Assert.That(session3.XSession.SessionState, Is.EqualTo(SessionState.Open));
          session3.Close();

          //MySqlXConnectionStringBuilder
          sb.SslMode = modes[j];
          sb.Auth = MySqlAuthenticationMode.AUTO;
          sb.Compression = compressValue[i];
          session4 = MySQLX.GetSession(sb.ConnectionString);
          Assert.That(session4.XSession.SessionState, Is.EqualTo(SessionState.Open));
          session4.Close();
        }
      }

      sb = new MySqlXConnectionStringBuilder(ConnectionString);
      for (int i = 0; i < 3; i++)
      {
        session1 = MySQLX.GetSession(ConnectionString + ";auth=AUTO;compression=" + compressValue[i]);
        Assert.That(session1.XSession.SessionState, Is.EqualTo(SessionState.Open));
        session1.Close();

        session2 = MySQLX.GetSession(ConnectionStringUri + "?compression=" + compressValue[i]);
        Assert.That(session2.XSession.SessionState, Is.EqualTo(SessionState.Open));
        session2.Close();

        session3 = MySQLX.GetSession(new { server = sb.Server, user = sb.UserID, port = sb.Port, password = sb.Password, compression = compressValue[i] });
        Assert.That(session3.XSession.SessionState, Is.EqualTo(SessionState.Open));
        session3.Close();

        sb.Compression = compressValue[i];
        session4 = MySQLX.GetSession(sb.ConnectionString);
        Assert.That(session4.XSession.SessionState, Is.EqualTo(SessionState.Open));
        session4.Close();
      }
    }


    [Test, Description("Verifying the connection pooling with compression option")]
    public void CompressionWithPolling()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 19), "This test is for MySql 8.0.19 or higher");
      for (int i = 0; i < 3; i++)
      {
        client = MySQLX.GetClient(ConnectionString + ";compression=" + compressValue[i], new { pooling = new { maxSize = 2, queueTimeout = 2000 } });

        session1 = client.GetSession();
        Assert.That(session1.XSession.SessionState, Is.EqualTo(SessionState.Open));
        session1.Close();

        session2 = client.GetSession();
        Assert.That(session2.XSession.SessionState, Is.EqualTo(SessionState.Open));
        session2.Close();

        session1 = client.GetSession();
        Assert.That(session1.XSession.SessionState, Is.EqualTo(SessionState.Open));
        session2 = client.GetSession();
        Assert.That(session2.XSession.SessionState, Is.EqualTo(SessionState.Open));

        Assert.Throws<TimeoutException>(() => client.GetSession());
        session1.Close();
        session2.Close();
      }
    }

    [Test, Description("Verify if data sent is compressed")]
    public void VerifyDataSentCompression()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 19), "This test is for MySql 8.0.19 or higher");
      int BYTESIZE = 20000;
      string[] compressValue1 = new string[] { "preferred", "required", "required" };
      string[] compressValue2 = new string[] { "disabled", "disabled", "preferred" };
      for (int i = 0; i < 3; i++)
      {
        session1 = MySQLX.GetSession(ConnectionString + ";compression=" + compressValue1[i]);
        session1.SQL("DROP database if exists compression").Execute();
        session1.SQL("create database compression").Execute();
        session1.SQL("use compression").Execute();
        Schema schema = session1.GetSchema("compression");
        var collection = schema.CreateCollection("compressed");

        string text = GenerateDummyText("Wiki Loves Monuments ", BYTESIZE);
        var doc = new[] { new { _id = 1, summary = text } };
        collection.Add(doc).Execute();
        schema.GetCollection("compressed");

        session2 = MySQLX.GetSession(ConnectionString + ";compression=" + compressValue2[i]);
        session2.SQL("use compression").Execute();
        schema = session2.GetSchema("compression");

        schema.GetCollection("compressed");
        var reader = session2.SQL("Select count(*) from compressed").Execute().FetchOne()[0];
        var reader2 = session2.SQL("Select * from compressed").Execute().FetchAll();
        Assert.That(reader.ToString(), Is.EqualTo("1"));

        // Results of compression when its value for session1 is: compressValue1[i] and for session2 is: compressValue2[i]
        var result1 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result1, Is.Not.Null);
        var result2 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result2, Is.Not.Null);
        var result3 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result3, Is.Not.Null);
        var result4 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result4, Is.Not.Null);
        if (Convert.ToInt32(result4) == 0 || Convert.ToInt32(result2) == 0)
        {
          Assert.Fail("Compression failed");
        }

        // Results of compression when its value for session2 is: compressValue1[i] and for session2 is: compressValue2[i]
        var result21 = session2.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result21, Is.Not.Null);
        var result22 = session2.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result22, Is.Not.Null);
        var result23 = session2.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result23, Is.Not.Null);
        var result24 = session2.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result24, Is.Not.Null);
        session1.Close();
        session2.Close();
      }
    }

    [Test, Description("Verify if data read is compressed")]
    public void VerifyDataReadCompression()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 19), "This test is for MySql 8.0.19 or higher");
      const int BYTESIZE = 20000;
      string[] compressValue1 = new string[] { "preferred", "required" };
      for (int i = 0; i < 2; i++)
      {
        session1 = MySQLX.GetSession(ConnectionString + ";compression=disabled");
        session1.SQL("DROP database if exists compression").Execute();
        session1.SQL("create database compression").Execute();
        session1.SQL("use compression").Execute();
        Schema schema = session1.GetSchema("compression");
        var collection = schema.CreateCollection("compressed");
        string text = GenerateDummyText("Wiki Loves Monuments ", BYTESIZE);
        var doc = new[] { new { _id = 1, summary = text } };
        collection.Add(doc).Execute();
        schema.GetCollection("compressed");

        session2 = MySQLX.GetSession(ConnectionString + ";compression=" + compressValue1[i]);
        session2.SQL("use compression").Execute();
        schema = session2.GetSchema("compression");

        schema.GetCollection("compressed");
        var reader = session2.SQL("Select count(*) from compressed").Execute().FetchOne()[0];
        var reader2 = session2.SQL("Select * from compressed").Execute().FetchAll();
        Assert.That(reader.ToString(), Is.EqualTo("1"));

        var result1 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result1, Is.Not.Null);
        var result2 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result2, Is.Not.Null);
        var result3 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result3, Is.Not.Null);
        var result4 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result4, Is.Not.Null);

        var result21 = session2.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result21, Is.Not.Null);
        var result22 = session2.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result22, Is.Not.Null);
        var result23 = session2.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result23, Is.Not.Null);
        var result24 = session2.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result24, Is.Not.Null);
        session1.Close();
        session2.Close();
      }
    }


    [Test, Description("Verifying the threshold for compression")]
    public void CompressionThreshold()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 19), "This test is for MySql 8.0.19 or higher");
      using (session1 = MySQLX.GetSession(ConnectionString + ";compression=required"))
      {
        session1.SQL("DROP database if exists compression").Execute();
        session1.SQL("create database compression").Execute();
        session1.SQL("use compression").Execute();
        Schema schema = session1.GetSchema("compression");
        var collection = schema.CreateCollection("compressed");
        string text1 = GenerateDummyText("Wiki Loves Monuments ", 47).Substring(0, 917);
        var doc1 = new[] { new { _id = 1, summary = text1 } };

        collection.Add(doc1).Execute();
        var result1 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result1, Is.Not.Null);
        var result2 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result2, Is.Not.Null);
        var result3 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result3, Is.Not.Null);
        var result4 = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result4, Is.Not.Null);

        if (!Platform.IsWindows())
          if (Convert.ToInt32(result2) != 0 || Convert.ToInt32(result4) != 0)
            Assert.Fail("Compression failed");

        var collection2 = schema.CreateCollection("compressed2");
        string text2 = GenerateDummyText("Wiki Loves Monuments ", 48).Substring(0, 1000);
        var doc2 = new[] { new { _id = 1, summary = text2 } };

        collection2.Add(doc2).Execute();
        var result1b = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result1b, Is.Not.Null);
        var result2b = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result2b, Is.Not.Null);
        var result3b = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result3b, Is.Not.Null);
        var result4b = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result4b, Is.Not.Null);

        if (Convert.ToInt32(result4b) == 0 || Convert.ToInt32(result2b) == 0)
          Assert.Fail("Compression failed");

        var collection3 = schema.CreateCollection("compressed3");
        string text3 = GenerateDummyText("Wiki Loves Monuments ", 48).Substring(0, 1002);
        var doc3 = new[] { new { _id = 1, summary = text3 } };

        collection3.Add(doc3).Execute();
        var result1c = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result1c, Is.Not.Null);
        var result2c = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_uncompressed_frame' ").Execute().FetchOne()[1];
        Assert.That(result2c, Is.Not.Null);
        var result3c = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_sent_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result3c, Is.Not.Null);
        var result4c = session1.SQL("select * from performance_schema.session_status where variable_name='Mysqlx_bytes_received_compressed_payload' ").Execute().FetchOne()[1];
        Assert.That(result4c, Is.Not.Null);

        if (Convert.ToInt32(result4c) == 0 || Convert.ToInt32(result2c) == 0)
          Assert.Fail("Compression failed");
      }
    }

    [Test, Description("Checking the network latency")]
    public void NetworkLatency()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 19), "This test is for MySql 8.0.19 or higher");
      const int BYTESIZE = 20000;
      Stopwatch watch1 = new Stopwatch();
      Stopwatch watch2 = new Stopwatch();
      Collection collection;
      string dummyText = GenerateDummyText("Wiki Loves Monuments ", BYTESIZE);
      var doc = new[] { new { _id = 1, summary = dummyText } };

      using (session1 = MySQLX.GetSession(ConnectionString + ";compression=required"))
      {
        session1.SQL("DROP DATABASE IF EXISTS compression").Execute();
        session1.SQL("CREATE DATABASE compression").Execute();
        session1.SQL("USE compression").Execute();
        Schema schema = session1.GetSchema("compression");
        collection = schema.CreateCollection("compressed");
        watch1.Start();
        collection.Add(doc).Execute();
        schema.GetCollection("compressed");
        watch1.Stop();
      }

      using (session2 = MySQLX.GetSession(ConnectionString + ";compression=disabled"))
      {
        session2.SQL("DROP DATABASE IF EXISTS compression").Execute();
        session2.SQL("CREATE DATABASE compression").Execute();
        Schema schema = session2.GetSchema("compression");
        collection = schema.CreateCollection("compressed2");
        watch2.Start();
        collection.Add(doc).Execute();
        schema.GetCollection("compressed2");
        watch2.Stop();
      }

      Assert.That(watch1.ElapsedTicks != watch2.ElapsedTicks,
        $"Watch1: {watch1.ElapsedMilliseconds}, Watch2: {watch2.ElapsedMilliseconds}");
    }
    #endregion

    #region Methods
    /// <summary>
    /// Repeat the string <paramref name="textToRepeat"/> an specific number of times <paramref name="timesToRepeat"/>
    /// </summary>
    /// <param name="textToRepeat"></param>
    /// <param name="timesToRepeat"></param>
    /// <returns></returns>
    protected string GenerateDummyText(string textToRepeat, int timesToRepeat)
    {
      if (string.IsNullOrEmpty(textToRepeat) || timesToRepeat <= 0) return string.Empty;

      return new StringBuilder(textToRepeat.Length * timesToRepeat).Insert(0, textToRepeat, timesToRepeat).ToString();
    }
    #endregion Methods

  }
}
