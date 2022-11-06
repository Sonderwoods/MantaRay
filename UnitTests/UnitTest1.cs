using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using MantaRay;

namespace UnitTests
{
    [TestClass]
    public class PathTests
    {
        [TestMethod]
        public void WindowsPaths()
        {

            MantaRay.Helpers.SSH_Helper.
            string[] inputPaths = new string[]
            {
                "/mnt/c/testPath",
                "/C:/Users/test",
                "/root/blabla",
                "C:\\How are you doing\\",
                "/#¤\"/wellhelo"

            };

            string[] outputPaths = new string[inputPaths.Length];

            for (int i = 0; i < inputPaths.Length; i++)
            {
                outputPaths[i] = MantaRay.Helpers.PathHelper.ToWindowsPath(inputPaths[i]);
            }

        }
    }
}
