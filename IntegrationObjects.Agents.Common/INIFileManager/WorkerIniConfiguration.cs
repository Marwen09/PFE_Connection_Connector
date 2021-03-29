using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace IntegrationObjects.Agents.Common
{
   public class WorkerIniConfiguration
    {
        /// <summary>
        /// Read from and Write to INI files
        /// </summary>
       
        #region Variable's declaration

        /// <summary>
        /// The path of the INI file
        /// </summary>
        private readonly string _strPath;

        #endregion

        #region External functions

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string strSection, string strKey, string strVal, string strFilePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string strSection, string strKey, string strDef, StringBuilder objRetVal, int iSize, string strFilePath);

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="strIniPath">Path of the INI file</param>
        public WorkerIniConfiguration(string strIniPath)
        {
            _strPath = strIniPath;
        }

        #endregion

        #region METHODES

        /// <summary>
        /// Write on INI file
        /// </summary>
        /// <param name="strSection">Section name on INI file</param>
        /// <param name="strKey">Key of the section</param>
        /// <param name="strValue">Value of the key</param>
        public void IniWriteValue(string strSection, string strKey, string strValue)
        {
            WritePrivateProfileString(strSection, strKey, strValue, _strPath);
        }

        /// <summary>
        /// Read from INI file
        /// </summary>
        /// <param name="strSection">Section name on INI file</param>
        /// <param name="strKey">Key of the section</param>
        /// <returns>The information associated to the key</returns>
        public string IniReadValue(string strSection, string strKey)
        {
            var objTemp = new StringBuilder(255);
            GetPrivateProfileString(strSection, strKey, string.Empty, objTemp, 255, _strPath);
            return objTemp.ToString();
        }

        #endregion

    }
}
