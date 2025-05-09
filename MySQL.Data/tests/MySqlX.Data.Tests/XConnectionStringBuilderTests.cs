// Copyright © 2018, 2025, Oracle and/or its affiliates.
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
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Data;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// The purpose of this class is to incorporate MySqlBaseConnectionStringBuilder, MySqlConnectionStringBuilder and MySqlXConnectionStringBuilder
  /// tests that aren't affected by previously opened connections/sessions.
  /// </summary>
  public class XConnectionStringBuilderTests
  {
    private static string _connectionString;
    private static string _xConnectionURI;
    private static string _connectionStringWithSslMode;

    static XConnectionStringBuilderTests()
    {
      _connectionString = $"server={BaseTest.Host};user={BaseTest.RootUser};password={BaseTest.RootPassword};port={BaseTest.Port};";
      _xConnectionURI = $"mysqlx://{BaseTest.RootUser}:{BaseTest.RootPassword}@{BaseTest.Host}:{BaseTest.XPort}";
      _connectionStringWithSslMode = _connectionString + "sslmode=required;";
    }

    [Test]
    public void SessionCanBeOpened()
    {
      using (var session = MySQLX.GetSession(_xConnectionURI))
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
    }

    [Test]
    public void ConnectionAfterSessionCanBeOpened()
    {
      Assume.That(Platform.IsWindows(), "Check for Linux OS");

      using (var session = MySQLX.GetSession(_xConnectionURI))
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));

      using (var connection = new MySqlConnection(_connectionStringWithSslMode))
      {
        connection.Open();
        Assert.That(connection.State, Is.EqualTo(ConnectionState.Open));
      }

      using (var session = MySQLX.GetSession(_xConnectionURI + "?sslca=client.pfx&certificatepassword=pass"))
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
    }

#if !NET452
    [TestCase(";tls-version=TlSv1.3", "Tls13")]
    [TestCase(";tls-version=TlSv1.2, tLsV11, TLS13, tls1.0", "Tls12, Tls13")]
#endif
    [TestCase(";tls-version=TlSv1.2, tLsV11, tls1.0", "Tls12")]
    [TestCase(";tls-version=TlSv1.2, SsLv3", "Tls12")]
    public void ValidateTlsVersionOptionAsString(string options, string result)
    {
      MySqlXConnectionStringBuilder builder = new MySqlXConnectionStringBuilder(_connectionString + options);
      Assert.That(builder.TlsVersion, Is.EqualTo(result));
    }

#if !NET452
    [TestCase("TlSv1.2, tLsV11, TLS13, tls1.0", "Tls12, Tls13")]
    [TestCase("TlSv1.2, TLS13, SsLv3", "Tls12, Tls13")]
#endif
    [TestCase("TlSv1.2, tLsV11, tls1.0", "Tls12")]
    [TestCase("TlSv1.2, SsLv3", "Tls12")]
    public void ValidateTlsVersionOptionAsProperty(string options, string result)
    {
      MySqlXConnectionStringBuilder builder = new MySqlXConnectionStringBuilder(_connectionString);

      builder.TlsVersion = options;
      Assert.That(builder.TlsVersion, Is.EqualTo(result));
    }

#if !NET452
    [TestCase(MySqlSslMode.Prefered, "TlSv1.2, tLsV11, TLS13, tls1.0", "Tls12, Tls13")]
    [TestCase(MySqlSslMode.Disabled, "TlSv1.2, tLsV11, TLS13, tls1.0", "TLS12, TLS13")]
    [TestCase(null, "TlSv1.2, tLsV11, TLS13, tls1.0", "Tls12, Tls13")]
#endif
    [TestCase(MySqlSslMode.Prefered, "TlSv1.2, tLsV11, tls1.0", "Tls12")]
    [TestCase(MySqlSslMode.Disabled, "TlSv1.2, tLsV11, tls1.0", "Tls12")]
    [TestCase(null, "TlSv1.2, tLsV11, tls1.0", "Tls12")]
    public void ValidateTlsVersionOptionAndSslMode(MySqlSslMode? sslMode1, string options, string result)
    {
      MySqlXConnectionStringBuilder builder = new MySqlXConnectionStringBuilder(_connectionString);

      if (sslMode1.HasValue)
        builder.SslMode = sslMode1.Value;

      if (result != null)
      {
        builder.TlsVersion = options;
        Assert.That(builder.TlsVersion, Is.EqualTo(result).IgnoreCase);
      }
      else
        Assert.Throws<ArgumentException>(() => { builder.TlsVersion = options; });
    }

    [Test]
    public void CaseInsensitiveAuthOption()
    {
      string[,] values = new string[,] {
        { "PLAIN", "plain", "PLAin", "PlaIn" },
        { "MYSQL41", "MySQL41", "mysql41", "mYSqL41" },
        { "EXTERNAL", "external", "exterNAL", "eXtERNal" }
      };

      for (int i = 0; i < values.GetLength(0); i++)
      {
        for (int j = 0; j < values.GetLength(1); j++)
        {
          var builder = new MySqlXConnectionStringBuilder(String.Format("server=localhost;auth={0}", values[i, j]));
          Assert.That(builder.Auth, Is.EqualTo((MySqlAuthenticationMode)(i + 1)));
        }
      }
    }

    [Test]
    public void IncorrectAuthOptionThrowsArgumentException()
    {
      string[] values = { "OTHER", "Other", "MYSQL42", "PlaINs" };
      foreach (var value in values)
      {
        Exception ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder(String.Format("server=localhost;aUth={0}", value)));
        Assert.That(ex.Message, Is.EqualTo(String.Format("Value '{0}' is not of the correct type", value)));
      }
    }

    /// <summary>
    /// Bug #33351775 [MySqlConnectionStringBuilder.TryGetValue always returns false]
    /// TryGetValue() method of ConnectionStringBuilder object was not overrided.
    /// </summary>
    [Test]
    public void TryGetValue()
    {
      MySqlXConnectionStringBuilder connStringBuilder = new()
      {
        DnsSrv = true,
        CompressionAlgorithm = "deflate, lz4",
      };

      Assert.That(connStringBuilder.ContainsKey("dnssrv"));
      Assert.That(connStringBuilder.TryGetValue("dns-srv", out var dnssrv));
      Assert.That((bool)dnssrv, Is.EqualTo(connStringBuilder.DnsSrv));

      Assert.That(connStringBuilder.ContainsKey("compressionAlgorithms"));
      Assert.That(connStringBuilder.TryGetValue("Compression-Algorithms", out var compressionAlgorithm));
      Assert.That((string)compressionAlgorithm, Is.EqualTo(connStringBuilder.CompressionAlgorithm).IgnoreCase);

      // Default value
      Assert.That(connStringBuilder.TryGetValue("connection-attributes", out var connectionattributes));
      Assert.That(connectionattributes, Is.EqualTo(connStringBuilder.GetOption("connection-attributes").DefaultValue));

      // Non existing option
      Assert.That(connStringBuilder.TryGetValue("foo", out var nonexistingoption), Is.False);
      Assert.That(nonexistingoption, Is.Null);
    }
  }
}
