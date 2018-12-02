using System;
using System.Collections.Generic;
using System.Text;

namespace FamiliadaClientForms
{
    public class TableCommand
    {
        public string CommandType { get; set; }

        public TableCommand(string commandType)
        {
            CommandType = commandType;
        }
    }
}
