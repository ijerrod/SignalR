﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests.Internal.Formatters
{
    public partial class BinaryMessageFormatterTests
    {
        [Fact]
        public void WriteMultipleMessages()
        {
            var expectedEncoding = new byte[]
            {
                /* length: */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    /* body: <empty> */
                /* length: */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E,
                    /* body: */ 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2C, 0x0D, 0x0A, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21,
            };

            var messages = new[]
            {
                new byte[0],
                Encoding.UTF8.GetBytes("Hello,\r\nWorld!")
            };

            var output = new MemoryStream(); // Use small chunks to test Advance/Enlarge and partial payload writing
            foreach (var message in messages)
            {
                BinaryMessageFormatter.WriteMessage(message, output);
            }

            Assert.Equal(expectedEncoding, output.ToArray());
        }

        [Theory]
        [InlineData(0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, new byte[0])]
        [InlineData(0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0xAB, 0xCD, 0xEF, 0x12 }, new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        [InlineData(4, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, new byte[0])]
        [InlineData(4, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0xAB, 0xCD, 0xEF, 0x12 }, new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        public void WriteBinaryMessage(int offset, byte[] encoded, byte[] payload)
        {
            var output = new MemoryStream();

            if (offset > 0)
            {
                output.Seek(offset, SeekOrigin.Begin);
            }

            BinaryMessageFormatter.WriteMessage(payload, output);

            Assert.Equal(encoded, output.ToArray().Skip(offset));
        }

        [Theory]
        [InlineData(0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, "")]
        [InlineData(0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x41, 0x42, 0x43 }, "ABC")]
        [InlineData(0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x41, 0x0A, 0x52, 0x0D, 0x43, 0x0D, 0x0A, 0x3B, 0x44, 0x45, 0x46 }, "A\nR\rC\r\n;DEF")]
        [InlineData(4, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, "")]
        public void WriteTextMessage(int offset, byte[] encoded, string payload)
        {
            var message = Encoding.UTF8.GetBytes(payload);
            var output = new MemoryStream();

            if (offset > 0)
            {
                output.Seek(offset, SeekOrigin.Begin);
            }

            BinaryMessageFormatter.WriteMessage(message, output);

            Assert.Equal(encoded, output.ToArray().Skip(offset));
        }
    }
}
