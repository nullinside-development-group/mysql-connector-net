// Copyright © 2017, 2025, Oracle and/or its affiliates.
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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// The implementation of the caching_sha2_password authentication plugin.
  /// </summary>
  internal class CachingSha2AuthenticationPlugin : Sha256AuthenticationPlugin
  {
    internal static AuthStage _authStage;

    public override string PluginName => "caching_sha2_password";

    protected override void SetAuthData(byte[] data)
    {
      // If the data given to us is a null terminated string, we need to trim off the trailing zero.
      if (data[data.Length - 1] == 0)
      {
        byte[] b = new byte[data.Length - 1];
        Buffer.BlockCopy(data, 0, b, 0, data.Length - 1);
        base.SetAuthData(b);
      }
      else base.SetAuthData(data);
    }

    protected override Task<byte[]> MoreDataAsync(byte[] data, bool execAsync)
    {
      rawPubkey = data;

      // Generate scramble.
      if (data == null)
      {
        byte[] scramble = GetPassword() as byte[];
        byte[] buffer = new byte[scramble.Length - 1];
        Array.Copy(scramble, 1, buffer, 0, scramble.Length - 1);
        return Task.FromResult<byte[]>(buffer);
      }
      // Fast authentication.
      else if (data[0] == 3)
      {
        _authStage = AuthStage.FAST_AUTH;
        return Task.FromResult<byte[]>(null);
      }
      else
        return Task.FromResult<byte[]>(GeneratePassword());
    }

    /// <summary>
    /// Generates a byte array set with the password of the user in the expected format based on the
    /// SSL settings of the current connection.
    /// </summary>
    /// <returns>A byte array that contains the password of the user in the expected format.</returns>
    protected byte[] GeneratePassword()
    {
      // If connection is secure perform full authentication.
      if (Settings.SslMode != MySqlSslMode.Disabled)
      {
        _authStage = AuthStage.FULL_AUTH;

        // Send as clear text since the channel is already encrypted.
        byte[] passBytes = Encoding.GetBytes(GetMFAPassword());
        byte[] buffer = new byte[passBytes.Length + 1];
        Array.Copy(passBytes, 0, buffer, 0, passBytes.Length);
        buffer[passBytes.Length] = 0;
        return buffer;
      }
      else
      {
        // Request RSA key from server.
        if (rawPubkey != null && rawPubkey[0] == 4)
        {
          _authStage = AuthStage.REQUEST_RSA_KEY;
          return new byte[] { 0x02 };
        }
        else if (!Settings.AllowPublicKeyRetrieval)
          throw new MySqlException(Resources.RSAPublicKeyRetrievalNotEnabled);
        // Full authentication.
        else
        {
          _authStage = AuthStage.FULL_AUTH;
          byte[] bytes = GetRsaPassword(GetMFAPassword(), AuthenticationData, rawPubkey);
          if (bytes != null && bytes.Length == 1 && bytes[0] == 0) return null;
          return bytes;
        }
      }
    }

    private byte[] GetRsaPassword(string password, byte[] seedBytes, byte[] rawPublicKey)
    {
      if (password.Length == 0) return new byte[1];
      if (rawPubkey == null) return null;
      // Obfuscate the plain text password with the session scramble.
      byte[] obfuscated = GetXor(Encoding.Default.GetBytes(password), seedBytes);

      // Encrypt the password and send it to the server.
      if (this.ServerVersion >= new Version("8.0.5"))
      {
        RSACryptoServiceProvider rsa = MySqlPemReader.ConvertPemToRSAProvider(rawPublicKey);
        if (rsa == null) throw new MySqlException(Resources.UnableToReadRSAKey);

        return rsa.Encrypt(obfuscated, true);
      }
      else
      {
        RSACryptoServiceProvider rsa = MySqlPemReader.ConvertPemToRSAProvider(rawPublicKey);
        if (rsa == null) throw new MySqlException(Resources.UnableToReadRSAKey);

        return rsa.Encrypt(obfuscated, false);
      }
    }

    public override object GetPassword()
    {
      _authStage = AuthStage.GENERATE_SCRAMBLE;

      // If we have no password then we just return 1 zero byte.
      if (GetMFAPassword().Length == 0) return new byte[1];

      SHA256 sha = SHA256.Create();

      byte[] firstHash = sha.ComputeHash(Encoding.Default.GetBytes(GetMFAPassword()));
      byte[] secondHash = sha.ComputeHash(firstHash);

      byte[] input = new byte[AuthenticationData.Length + secondHash.Length];
      Array.Copy(secondHash, 0, input, 0, secondHash.Length);
      Array.Copy(AuthenticationData, 0, input, secondHash.Length, AuthenticationData.Length);
      byte[] thirdHash = sha.ComputeHash(input);

      byte[] finalHash = new byte[thirdHash.Length];
      for (int i = 0; i < firstHash.Length; i++)
        finalHash[i] = (byte)(firstHash[i] ^ thirdHash[i]);

      byte[] buffer = new byte[finalHash.Length + 1];
      Array.Copy(finalHash, 0, buffer, 1, finalHash.Length);
      buffer[0] = 0x20;
      return buffer;
    }
  }

  /// <summary>
  /// Defines the stage of the authentication.
  /// </summary>
  internal enum AuthStage
  {
    GENERATE_SCRAMBLE = 0,
    REQUEST_RSA_KEY = 1,
    FAST_AUTH = 2,
    FULL_AUTH = 3
  }
}
