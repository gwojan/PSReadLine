﻿using System;
using System.Linq;
using System.Windows;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PSConsoleUtilities;

namespace UnitTestPSReadLine
{
    // Disgusting language hack to make it easier to read a sequence of keys.
    using _ = Keys;

    public partial class UnitTest
    {

        [TestMethod]
        public void TestCaptureScreen()
        {
            TestSetup(KeyMode.Cmd,
                new KeyHandler("Ctrl+Z", PSConsoleReadLine.CaptureScreen));

            var line  = new [] {"echo alpha", "echo beta", "echo phi", "echo rho"};
            Test(line[0], Keys(line[0], _.CtrlZ, _.Enter, _.Enter));
            AssertClipboardTextIs(line[0]);

            var cancelKeys = new[] {_.Escape, _.CtrlC, _.CtrlG};
            for (int i = 0; i < cancelKeys.Length; i++)
            {
                // Start CaptureScreen but cancel
                Test(line[i + 1], Keys(line[i + 1], _.CtrlZ, cancelKeys[i], _.Enter), resetCursor: false);
                // Make sure the clipboard doesn't change
                AssertClipboardTextIs(line[0]);
            }

            // Make sure we know where we are on the screen.
            AssertCursorTopIs(4);

            var shiftUpArrow = new ConsoleKeyInfo((char)0, ConsoleKey.UpArrow, true, false, false);
            var shiftDownArrow = new ConsoleKeyInfo((char)0, ConsoleKey.DownArrow, true, false, false);

            using (ShimsContext.Create())
            {
                bool ding = false;
                PSConsoleUtilities.Fakes.ShimPSConsoleReadLine.Ding =
                    () => ding = true;

                Test("", Keys(
                    // Basic up/down arrows
                    _.CtrlZ, _.UpArrow, _.Enter, CheckThat(() => AssertClipboardTextIs(line[3])),
                    _.CtrlZ, _.UpArrow, _.UpArrow, _.Enter, CheckThat(() => AssertClipboardTextIs(line[2])),
                    _.CtrlZ, _.UpArrow, _.UpArrow, _.DownArrow, _.Enter, CheckThat(() => AssertClipboardTextIs(line[3])),

                    // Select multiple lines
                    _.CtrlZ, _.UpArrow, shiftUpArrow, _.Enter,
                    CheckThat(() => AssertClipboardTextIs(line[2], line[3])),
                    _.CtrlZ, Enumerable.Repeat(_.UpArrow, 10), shiftDownArrow, _.Enter,
                    CheckThat(() => AssertClipboardTextIs(line[0], line[1])),

                    // Select multiple lines, then shorten selection
                    _.CtrlZ, _.UpArrow, shiftUpArrow, shiftUpArrow, shiftDownArrow, _.Enter,
                    CheckThat(() => AssertClipboardTextIs(line[2], line[3])),
                    _.CtrlZ, Enumerable.Repeat(_.UpArrow, 10), shiftDownArrow, shiftDownArrow, shiftUpArrow, _.Enter,
                    CheckThat(() => AssertClipboardTextIs(line[0], line[1])),

                    // Test trying to arrow down past end of buffer (arrowing past top of buffer covered above)
                    _.CtrlZ, Enumerable.Repeat(_.DownArrow, Console.BufferHeight), _.Escape,

                    // Test that we ding input that doesn't do anything
                    _.CtrlZ,
                    'c', CheckThat(() => { Assert.IsTrue(ding); ding = false; }),
                    'g', CheckThat(() => { Assert.IsTrue(ding); ding = false; }),
                    'a', CheckThat(() => { Assert.IsTrue(ding); ding = false; }),
                    _.Escape,

                    _.Enter),
                     resetCursor: false);
            }

            // To test:
            // * Selected lines are inverted
            // * Rtf output
            // * Rtf special characters
        }
    }
}
