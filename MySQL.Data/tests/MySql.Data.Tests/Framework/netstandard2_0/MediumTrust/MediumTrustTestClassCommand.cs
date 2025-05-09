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
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient.Tests.Xunit.MediumTrust;
using Xunit.Sdk;

namespace MySql.Data.MySqlClient.Tests.Xunit
{
  class MediumTrustTestClassCommand : ITestClassCommand
  {

    readonly TestClassCommand _cmd = new TestClassCommand();
    Random randomizer = new Random();

    #region ITestClassCommand Members
    public object ObjectUnderTest
    {
      get { return _cmd.ObjectUnderTest; }
    }

    public ITypeInfo TypeUnderTest
    {
      get { return _cmd.TypeUnderTest; }
      set { _cmd.TypeUnderTest = value; }
    }

    public int ChooseNextTest(ICollection<IMethodInfo> testsLeftToRun)
    {
      return randomizer.Next(testsLeftToRun.Count);
    }

    public Exception ClassFinish()
    {
      return _cmd.ClassFinish();
    }

    public Exception ClassStart()
    {
      return _cmd.ClassStart();
    }

    public IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo testMethod)
    {
      foreach (var testCommand in _cmd.EnumerateTestCommands(testMethod))
      {
        if (testCommand is MediumTrustTestCommand)
        {
          yield return testCommand;
          continue;
        }

        yield return new MediumTrustTestCommand(testCommand, null);
      }
    }

    public bool IsTestMethod(IMethodInfo testMethod)
    {
      return _cmd.IsTestMethod(testMethod);
    }

    public IEnumerable<IMethodInfo> EnumerateTestMethods()
    {
      return _cmd.EnumerateTestMethods();
    }
    #endregion
  }
}
