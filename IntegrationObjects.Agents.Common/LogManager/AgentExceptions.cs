using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationObjects.Agents.Common
{
   public static class AgentExceptions
    {
        public static string GetExceptionMessage(Exception ex)
        {
            int linenum = -1;
            try
            {
                //linenum = new System.Diagnostics.StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                linenum = Convert.ToInt32(ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(' ')));
            }
            catch
            {
                //Stack trace is not available!
            }

            return ex.Message + "(" + linenum.ToString() + ")";
        }
    }
}
