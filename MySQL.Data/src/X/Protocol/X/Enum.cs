// Copyright © 2015, 2025, Oracle and/or its affiliates.
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
using System.Threading.Tasks;

namespace MySqlX.Protocol.X
{
  internal enum NoticeType : int
  {
    Warning = 1,
    SessionVariableChanged = 2,
    SessionStateChanged = 3,
  }

  internal enum RowLock : int
  {
    SharedLock = 1,
    ExclusiveLock = 2
  }

  internal static class XpluginStatementCommand
  {
    public static readonly string XPLUGIN_STMT_CREATE_COLLECTION = "create_collection";
    public static readonly string XPLUGIN_STMT_CREATE_COLLECTION_INDEX = "create_collection_index";
    public static readonly string XPLUGIN_STMT_DROP_COLLECTION =
                 "drop_collection";
    //Added to support schema validation, store the name of the command used by MySQL Server to modify collections
    public static readonly string XPLUGIN_STMT_MODIFY_COLLECTION = "modify_collection_options";

    public static readonly string XPLUGIN_STMT_DROP_COLLECTION_INDEX = "drop_collection_index";
    public static readonly string XPLUGIN_STMT_PING = "ping";
    public static readonly string XPLUGIN_STMT_LIST_OBJECTS =
                 "list_objects";
    public static readonly string XPLUGIN_STMT_ENABLE_NOTICES = "enable_notices";
    public static readonly string XPLUGIN_STMT_DISABLE_NOTICES = "disable_notices";
    public static readonly string XPLUGIN_STMT_LIST_NOTICES =
                 "list_notices";
  }
}
