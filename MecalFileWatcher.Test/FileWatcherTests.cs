using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestApp;

namespace MecalFileWatcher.Test
{
    [TestClass]
    public class FileWatcherTests
    {
        private Mock<INotifiable> _callerMock = new Mock<INotifiable>();

        [TestInitialize]
        public void TestInitialize()
        {
            _callerMock.Setup(
                m => m.NotifyChanges(It.IsAny<IEnumerable<string>>())
                );
        }
        [TestMethod]
        public void CreateStructure()
        {
            var callingIE = new string[] { "a", "b" };
            _callerMock.Object.NotifyChanges(callingIE);
            var testingIE = new string[] { "b", "a" };
            
            _callerMock.Verify(
                cm => cm.NotifyChanges(
                    It.Is<IEnumerable<string>> (ch => enumsMatch(ch,testingIE))
                                        )
                                );
        }

        private bool enumsMatch(IEnumerable<string> received, IEnumerable<string> expected)
        {
            if (received.Count() != expected.Count())
                return false;
            return Enumerable.SequenceEqual(received.OrderBy(t => t), expected.OrderBy(t => t));
            
        }
    }
}
